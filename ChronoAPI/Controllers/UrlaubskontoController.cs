// ============================================================
//  UrlaubskontoController.cs  (Urlaubskonto-Verwaltung)
// ============================================================
//
//  Endpunkte:
//   GET api/Urlaubskonto/mein                → Eigenes Konto abrufen
//   GET api/Urlaubskonto/mitarbeiter/{mitId} → Konto eines Mitarbeiters (HR/Admin/AL)
//   GET api/Urlaubskonto/alle                → Alle Konten (HR/Admin)
//   POST api/Urlaubskonto                    → Konto erstellen oder aktualisieren (HR/Admin)
//   PUT  api/Urlaubskonto/abziehen/{mitId}   → Tage manuell abziehen (HR/Admin/AL)
//
//  Das Urlaubskonto speichert:
//   - Wie viele Urlaubstage ein Mitarbeiter insgesamt hat (GesamtTage)
//   - Wie viele er schon genommen hat (GenommTage)
//   - Wie viele noch übrig sind (RestTage = GesamtTage - GenommTage)
//
//  WICHTIG: RestTage wird nie in der DB gespeichert!
//  Es ist ein [NotMapped] berechnetes Feld in der Urlaubskonto-Klasse.
// ============================================================

using ChronoAPI.Data;
using ChronoAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronoAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UrlaubskontoController : ControllerBase
{
    private readonly AppDbContext _context;

    public UrlaubskontoController(AppDbContext context) => _context = context;

    // ── EIGENES KONTO ABRUFEN ─────────────────────────────────
    // GET api/Urlaubskonto/mein
    // Jeder eingeloggte User kann sein eigenes Konto sehen
    [HttpGet("mein")]
    public async Task<ActionResult<Urlaubskonto>> GetMein()
    {
        // MitarbeiterID aus dem JWT-Token lesen
        var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value;
        int.TryParse(mitIdClaim, out int mitId);

        // Konto für dieses Jahr suchen
        var konto = await _context.Urlaubskonten
            .Include(u => u.Mitarbeiter)
            .FirstOrDefaultAsync(u => u.FK_MitID == mitId && u.Jahr == DateTime.Now.Year);

        // 404 wenn noch kein Konto angelegt wurde (HR muss es erst erstellen)
        if (konto == null)
            return NotFound(new { message = "Kein Urlaubskonto gefunden." });

        return konto;
    }

    // ── KONTO EINES BESTIMMTEN MITARBEITERS ───────────────────
    // GET api/Urlaubskonto/mitarbeiter/5
    
    [HttpGet("mitarbeiter/{mitId}")]
    [Authorize(Roles = "HR,Admin,Abteilungsleiter")]
    public async Task<ActionResult<Urlaubskonto>> GetByMitarbeiter(
        int mitId, [FromQuery] int? jahr)  // [FromQuery] = kommt aus der URL nach dem ?
    {
        // Wenn kein Jahr angegeben → aktuelles Jahr nehmen
        var aktJahr = jahr ?? DateTime.Now.Year;

        var konto = await _context.Urlaubskonten
            .Include(u => u.Mitarbeiter)
            .FirstOrDefaultAsync(u => u.FK_MitID == mitId && u.Jahr == aktJahr);

        if (konto == null)
            return NotFound(new { message = $"Kein Konto für {aktJahr} gefunden." });

        return konto;
    }

    // ── ALLE KONTEN ABRUFEN ───────────────────────────────────
    // GET api/Urlaubskonto/alle
    // GET api/Urlaubskonto/alle?jahr=2025
    [HttpGet("alle")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<ActionResult<IEnumerable<Urlaubskonto>>> GetAlle([FromQuery] int? jahr)
    {
        var aktJahr = jahr ?? DateTime.Now.Year;
        return await _context.Urlaubskonten
            .Include(u => u.Mitarbeiter)
            .Where(u => u.Jahr == aktJahr)
            .OrderBy(u => u.Mitarbeiter!.MitNachname) // Alphabetisch nach Nachname
            .ToListAsync();
    }

    // ── KONTO ERSTELLEN ODER AKTUALISIEREN ────────────────────
    // POST api/Urlaubskonto
    // "Upsert" = Update wenn vorhanden, Insert wenn nicht (Update + Insert)
    [HttpPost]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> CreateOrUpdate([FromBody] Urlaubskonto konto)
    {
        // Gibt es schon ein Konto für diesen Mitarbeiter in diesem Jahr?
        var existing = await _context.Urlaubskonten
            .FirstOrDefaultAsync(u => u.FK_MitID == konto.FK_MitID && u.Jahr == konto.Jahr);

        if (existing != null)
        {
            // Konto existiert → nur GesamtTage aktualisieren
            existing.GesamtTage = konto.GesamtTage;
            existing.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Konto aktualisiert!", konto = existing });
        }

        // Konto existiert noch nicht → neu erstellen
        konto.CreatedAt = DateTime.Now;
        konto.GenommTage = 0;   // Noch keine Tage genommen
        konto.Mitarbeiter = null; // Navigation Property zurücksetzen (EF Core würde sonst Mitarbeiter neu erstellen!)
        _context.Urlaubskonten.Add(konto);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Konto erstellt!", konto });
    }

    // ── TAGE MANUELL ABZIEHEN ─────────────────────────────────
    // PUT api/Urlaubskonto/abziehen/5
    // Body: 2.5  (wie viele Tage abziehen)
    // Wird z.B. aufgerufen wenn jemand halbe Urlaubstage nimmt
    [HttpPut("abziehen/{mitId}")]
    [Authorize(Roles = "HR,Admin,Abteilungsleiter")]
    public async Task<IActionResult> Abziehen(int mitId, [FromBody] decimal tage)
    {
        var konto = await _context.Urlaubskonten
            .FirstOrDefaultAsync(u => u.FK_MitID == mitId && u.Jahr == DateTime.Now.Year);

        if (konto == null)
            return NotFound(new { message = "Kein Urlaubskonto gefunden." });

        // Prüfen ob genug Resturlaub vorhanden ist
        if (konto.RestTage < tage)
            return BadRequest(new { message = $"Nicht genug Resturlaub! Verfügbar: {konto.RestTage} Tage." });

        konto.GenommTage += tage; // Tage abziehen
        konto.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"{tage} Tage abgezogen.", restTage = konto.RestTage });
    }
}
