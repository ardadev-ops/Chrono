// ============================================================
//  ZeitkorrekturController.cs  (Zeitkorrektur-Anträge)
// ============================================================
//
//  Endpunkte:
//   GET api/Zeitkorrektur          → Liste abrufen (je nach Rolle)
//   GET api/Zeitkorrektur/pending  → Nur ausstehende (für Genehmiger)
//   POST api/Zeitkorrektur         → Neuen Antrag stellen
//   PUT  api/Zeitkorrektur/{id}/approve → Genehmigen (erstellt TimeBooking!)
//   PUT  api/Zeitkorrektur/{id}/reject  → Ablehnen
//
//  WAS PASSIERT BEI GENEHMIGUNG?
//  Wenn ein Zeitkorrektur-Antrag genehmigt wird, erstellt die API
//  AUTOMATISCH eine echte TimeBooking in tblTimeBookings.
//  So ist die genehmigte Zeit in der normalen Zeiterfassung sichtbar –
//  genauso als hätte der Mitarbeiter seine NFC-Karte benutzt.
// ============================================================

using ChronoAPI.Data;
using ChronoAPI.Extensions;
using ChronoAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronoAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ZeitkorrekturController : ControllerBase
{
    private readonly AppDbContext _context;

    public ZeitkorrekturController(AppDbContext context) => _context = context;

    // ── ALLE ZEITKORREKTUREN ABRUFEN ──────────────────────────
    // GET api/Zeitkorrektur
    // Jeder sieht nur das, was für seine Rolle relevant ist
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Zeitkorrektur>>> GetAll()
    {
        // Eigene MitarbeiterID aus dem JWT-Token lesen
        var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value;
        int.TryParse(mitIdClaim, out int mitId);

        // HR/Admin → alle Zeitkorrekturen aller Mitarbeiter
        if (User.IsInRole("Admin") || User.IsInRole("HR"))
        {
            return await _context.Zeitkorrekturen
                .Include(z => z.Mitarbeiter)   // Antragsteller-Daten mitladen
                .Include(z => z.Genehmiger)    // Genehmiger-Daten mitladen
                .OrderByDescending(z => z.CreatedAt)
                .ToListAsync();
        }

        // Abteilungsleiter → nur seine Abteilung
        if (User.IstAufAbteilungBeschraenkt())
        {
            var eigeneAbteilung = User.GetAbteilungsId();
            if (eigeneAbteilung == null) return Ok(new List<Zeitkorrektur>());

            return await _context.Zeitkorrekturen
                .Include(z => z.Mitarbeiter)
                .Include(z => z.Genehmiger)
                .Where(z => z.Mitarbeiter!.FK_AbtID == eigeneAbteilung)
                .OrderByDescending(z => z.CreatedAt)
                .ToListAsync();
        }

        // Normaler Mitarbeiter → nur eigene Anträge
        return await _context.Zeitkorrekturen
            .Include(z => z.Mitarbeiter)
            .Where(z => z.FK_MitID == mitId)
            .OrderByDescending(z => z.CreatedAt)
            .ToListAsync();
    }

    // ── AUSSTEHENDE ANTRÄGE (für Genehmiger) ─────────────────
    // GET api/Zeitkorrektur/pending
    [HttpGet("pending")]
    [Authorize(Roles = "Abteilungsleiter,HR,Admin")]
    public async Task<ActionResult<IEnumerable<Zeitkorrektur>>> GetPending()
    {
        // HR/Admin → alle ausstehenden
        if (User.IsInRole("Admin") || User.IsInRole("HR"))
        {
            return await _context.Zeitkorrekturen
                .Include(z => z.Mitarbeiter)
                .Where(z => z.Status == "Pending")
                .OrderByDescending(z => z.CreatedAt)
                .ToListAsync();
        }

        // Abteilungsleiter → nur seiner Abteilung
        var eigeneAbteilung = User.GetAbteilungsId();
        if (eigeneAbteilung == null) return Ok(new List<Zeitkorrektur>());

        return await _context.Zeitkorrekturen
            .Include(z => z.Mitarbeiter)
            .Where(z => z.Status == "Pending" &&
                        z.Mitarbeiter!.FK_AbtID == eigeneAbteilung)
            .OrderByDescending(z => z.CreatedAt)
            .ToListAsync();
    }

    // ── ZEITKORREKTUR BEANTRAGEN ──────────────────────────────
    // POST api/Zeitkorrektur
    [HttpPost]
    public async Task<ActionResult<Zeitkorrektur>> Create([FromBody] Zeitkorrektur korrektur)
    {
        // MitarbeiterID aus dem Token lesen
        var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value;
        int.TryParse(mitIdClaim, out int mitId);

        // Sicherheit: Normaler Mitarbeiter kann nur für sich selbst beantragen.
        // HR/Admin dürfen für andere beantragen (FK_MitID bleibt wie gesendet).
        if (!User.IsInRole("HR") && !User.IsInRole("Admin"))
            korrektur.FK_MitID = mitId;

        // Status immer auf "Pending" setzen (nie kann jemand sich selbst genehmigen)
        korrektur.Status = "Pending";
        korrektur.CreatedAt = DateTime.Now;

        // Navigation Properties zurücksetzen (damit EF Core nicht versucht
        // die verknüpften Mitarbeiter-Objekte neu in die DB zu schreiben)
        korrektur.Mitarbeiter = null;
        korrektur.Genehmiger = null;

        // Validierung: CheckOut muss nach CheckIn liegen
        if (korrektur.CheckOut <= korrektur.CheckIn)
            return BadRequest(new { message = "CheckOut muss nach CheckIn liegen!" });

        // Validierung: Datum darf nicht in der Zukunft liegen
        // (man kann nur vergangene Zeiten korrigieren, nicht zukünftige)
        if (korrektur.KorrDatum > DateOnly.FromDateTime(DateTime.Now))
            return BadRequest(new { message = "Datum darf nicht in der Zukunft liegen!" });

        _context.Zeitkorrekturen.Add(korrektur);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = korrektur.KorrID }, korrektur);
    }

    // ── ZEITKORREKTUR GENEHMIGEN ──────────────────────────────
    // PUT api/Zeitkorrektur/5/approve
    // Bei Genehmigung wird AUTOMATISCH eine echte TimeBooking erstellt!
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Abteilungsleiter,HR,Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        // Wer genehmigt? (aus dem JWT-Token)
        var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value;
        int.TryParse(mitIdClaim, out int genehmigerId);

        // Zeitkorrektur-Antrag finden
        var korrektur = await _context.Zeitkorrekturen
            .Include(z => z.Mitarbeiter)
            .FirstOrDefaultAsync(z => z.KorrID == id);
        if (korrektur == null) return NotFound();

        // Abteilungsleiter darf nur seine eigene Abteilung genehmigen
        if (User.IstAufAbteilungBeschraenkt()
            && korrektur.Mitarbeiter?.FK_AbtID != User.GetAbteilungsId())
        {
            return Forbid();
        }

        // Status auf "Approved" setzen
        korrektur.Status = "Approved";
        korrektur.ApprovedBy = genehmigerId;
        korrektur.ApprovedAt = DateTime.Now;

        // ── TIMEBOOKING AUTOMATISCH ERSTELLEN ─────────────────
        // DateOnly + TimeOnly → DateTime kombinieren
        // KorrDatum = 2026-05-06, CheckIn = 08:15
        // → CheckInTime = 2026-05-06 08:15:00
        var checkInDateTime  = korrektur.KorrDatum.ToDateTime(korrektur.CheckIn);
        var checkOutDateTime = korrektur.KorrDatum.ToDateTime(korrektur.CheckOut);

        // Neue echte Zeitbuchung erstellen (als wäre der Mitarbeiter normal gestempelt)
        var booking = new TimeBooking
        {
            FK_MitID     = korrektur.FK_MitID,
            CheckInTime  = checkInDateTime,
            CheckOutTime = checkOutDateTime,
            BreakMinutes = 30, // Standard-Pausenzeit 30 Minuten
            Notes = $"Zeitkorrektur genehmigt von MitID {genehmigerId}. Grund: {korrektur.Grund}",
            CreatedAt = DateTime.Now
        };
        _context.TimeBookings.Add(booking);

        // Zeitkorrektur-Update UND neue TimeBooking in einem Speicher-Aufruf
        await _context.SaveChangesAsync();
        return Ok(new { message = "Zeitkorrektur genehmigt und Buchung erstellt!" });
    }

    // ── ZEITKORREKTUR ABLEHNEN ────────────────────────────────
    // PUT api/Zeitkorrektur/5/reject
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Abteilungsleiter,HR,Admin")]
    public async Task<IActionResult> Reject(int id)
    {
        var korrektur = await _context.Zeitkorrekturen
            .Include(z => z.Mitarbeiter)
            .FirstOrDefaultAsync(z => z.KorrID == id);
        if (korrektur == null) return NotFound();

        // Abteilungsleiter darf nur seine eigene Abteilung ablehnen
        if (User.IstAufAbteilungBeschraenkt()
            && korrektur.Mitarbeiter?.FK_AbtID != User.GetAbteilungsId())
        {
            return Forbid();
        }

        korrektur.Status = "Rejected";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Zeitkorrektur abgelehnt." });
    }
}
