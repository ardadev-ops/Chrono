// ============================================================
//  AbwesenheitController.cs  (Urlaubsanträge & Abwesenheiten)
// ============================================================
//
//  Endpunkte:
//   GET  api/Abwesenheit          → Liste abrufen (je nach Rolle)
//   GET  api/Abwesenheit/{id}     → Einzelnen Antrag abrufen
//   GET  api/Abwesenheit/pending  → Nur ausstehende Anträge (für Genehmiger)
//   POST api/Abwesenheit          → Neuen Antrag stellen (jeder)
//   PUT  api/Abwesenheit/{id}/approve → Antrag genehmigen (AL/HR/Admin)
//   PUT  api/Abwesenheit/{id}/reject  → Antrag ablehnen (AL/HR/Admin)
//   DELETE api/Abwesenheit/{id}   → Antrag löschen (nur Admin)
//
//  Bei Genehmigung von Urlaub:
//  Die Urlaubstage werden automatisch vom Urlaubskonto abgezogen!
//  (Nur Werktage werden gezählt – Sa/So zählen nicht)
// ============================================================

using ChronoAPI.Data;
using ChronoAPI.Extensions;
using ChronoAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AbwesenheitController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AbwesenheitController> _logger;

        public AbwesenheitController(AppDbContext context, ILogger<AbwesenheitController> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // ── ALLE ANTRÄGE ABRUFEN ──────────────────────────────
        // GET api/Abwesenheit
        // Jeder sieht nur das, was für seine Rolle relevant ist
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Abwesenheit>>> GetAll()
        {
            // Eigene MitarbeiterID aus dem JWT-Token lesen
            var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value;
            int.TryParse(mitIdClaim, out int mitId);

            // Admin + HR → alle Anträge aller Mitarbeiter
            if (User.IsInRole("Admin") || User.IsInRole("HR"))
                return await _context.Abwesenheiten
                    .Include(a => a.Mitarbeiter)
                    .OrderByDescending(a => a.CreatedAt) // Neueste zuerst
                    .ToListAsync();

            // Abteilungsleiter → nur Anträge seiner Abteilung
            if (User.IstAufAbteilungBeschraenkt())
            {
                var eigeneAbteilung = User.GetAbteilungsId();
                if (eigeneAbteilung == null) return Ok(new List<Abwesenheit>());

                return await _context.Abwesenheiten
                    .Include(a => a.Mitarbeiter)
                    .Where(a => a.Mitarbeiter!.FK_AbtID == eigeneAbteilung)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
            }

            // Normaler Mitarbeiter → nur eigene Anträge
            return await _context.Abwesenheiten
                .Include(a => a.Mitarbeiter)
                .Where(a => a.FK_MitID == mitId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // ── EINZELNEN ANTRAG ABRUFEN ──────────────────────────
        // GET api/Abwesenheit/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Abwesenheit>> GetById(int id)
        {
            var a = await _context.Abwesenheiten
                .Include(a => a.Mitarbeiter)
                .FirstOrDefaultAsync(a => a.AbwesenID == id);
            if (a == null) return NotFound();
            return a;
        }

        // ── AUSSTEHENDE ANTRÄGE (für Genehmiger) ─────────────
        // GET api/Abwesenheit/pending
        // Zeigt nur Anträge mit Status="Pending" – zur Genehmigung bereit
        [HttpGet("pending")]
        [Authorize(Roles = "Abteilungsleiter,HR,Admin")]
        public async Task<ActionResult<IEnumerable<Abwesenheit>>> GetPending()
        {
            // HR/Admin → alle ausstehenden Anträge
            if (User.IsInRole("Admin") || User.IsInRole("HR"))
                return await _context.Abwesenheiten
                    .Include(a => a.Mitarbeiter)
                    .Where(a => a.Status == "Pending")
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

            // Abteilungsleiter → nur ausstehende Anträge seiner Abteilung
            var eigeneAbteilung = User.GetAbteilungsId();
            if (eigeneAbteilung == null) return Ok(new List<Abwesenheit>());

            return await _context.Abwesenheiten
                .Include(a => a.Mitarbeiter)
                .Where(a => a.Status == "Pending" &&
                            a.Mitarbeiter!.FK_AbtID == eigeneAbteilung)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // ── ANTRAG STELLEN ────────────────────────────────────
        // POST api/Abwesenheit
        // Jeder eingeloggte Mitarbeiter darf einen Antrag stellen
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Abwesenheit abwesenheit)
        {
            var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value;
            int.TryParse(mitIdClaim, out int mitId);

            abwesenheit.FK_MitID = mitId;
            abwesenheit.Status = "Pending";
            abwesenheit.CreatedAt = DateTime.Now;

            if (abwesenheit.AbwesenType == "Urlaub")
            {
                decimal arbeitstage = BerechneArbeitstage(abwesenheit.StartDate, abwesenheit.EndDate);

                var konto = await _context.Urlaubskonten
                    .FirstOrDefaultAsync(u => u.FK_MitID == mitId
                                           && u.Jahr == DateTime.Now.Year);

                if (konto == null)
                    return BadRequest(new {
                        message = "Kein Urlaubskonto gefunden. Bitte HR kontaktieren."
                    });

                if (konto.RestTage < arbeitstage)
                    return BadRequest(new {
                        message = $"Nicht genug Resturlaub! " +
                                  $"Verfügbar: {konto.RestTage} Tage, " +
                                  $"beantragt: {arbeitstage} Tage."
                    });
            }

            _context.Abwesenheiten.Add(abwesenheit);
            await _context.SaveChangesAsync();
            return Ok(abwesenheit);
        }

        // ── ANTRAG GENEHMIGEN ─────────────────────────────────
        // PUT api/Abwesenheit/5/approve
        // approvedBy = MitarbeiterID des Genehmigers (wird aus dem Token gelesen,
        // aber auch als Body mitgeschickt – Frontend schickt es mit)
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Abteilungsleiter,HR,Admin")]
        public async Task<IActionResult> Approve(int id, [FromBody] int approvedBy)
        {
            var a = await _context.Abwesenheiten
                .Include(x => x.Mitarbeiter)
                .FirstOrDefaultAsync(x => x.AbwesenID == id);
            if (a == null) return NotFound();

            // Abteilungsleiter duerfen nur Antraege der eigenen Abteilung bearbeiten
            if (User.IstAufAbteilungBeschraenkt()
                && a.Mitarbeiter?.FK_AbtID != User.GetAbteilungsId())
            {
                return Forbid();
            }

            if (a.AbwesenType == "Urlaub")
            {
                decimal arbeitstage = BerechneArbeitstage(a.StartDate, a.EndDate);

                var konto = await _context.Urlaubskonten
                    .FirstOrDefaultAsync(u => u.FK_MitID == a.FK_MitID
                                           && u.Jahr == DateTime.Now.Year);

                if (konto == null)
                    return BadRequest(new { message = "Kein Urlaubskonto gefunden." });

                if (konto.RestTage < arbeitstage)
                    return BadRequest(new { message = $"Nicht genug Resturlaub! Verfügbar: {konto.RestTage} Tage, beantragt: {arbeitstage} Tage." });

                _logger.LogInformation("Ziehe {tage} Tage ab für MitID {id}", arbeitstage, a.FK_MitID);
                konto.GenommTage += arbeitstage;
                konto.UpdatedAt = DateTime.Now;
            }

            a.Status = "Approved";
            a.ApprovedBy = approvedBy;
            a.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── ANTRAG ABLEHNEN ───────────────────────────────────
        // PUT api/Abwesenheit/5/reject
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Abteilungsleiter,HR,Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var a = await _context.Abwesenheiten
                .Include(x => x.Mitarbeiter)
                .FirstOrDefaultAsync(x => x.AbwesenID == id);
            if (a == null) return NotFound();

            // Abteilungsleiter duerfen nur Antraege der eigenen Abteilung bearbeiten
            if (User.IstAufAbteilungBeschraenkt()
                && a.Mitarbeiter?.FK_AbtID != User.GetAbteilungsId())
            {
                return Forbid();
            }

            a.Status = "Rejected";
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── ANTRAG LÖSCHEN ────────────────────────────────────
        // DELETE api/Abwesenheit/5  (nur Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.Abwesenheiten.FindAsync(id);
            if (a == null) return NotFound();
            _context.Abwesenheiten.Remove(a);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── HILFSMETHODEN ─────────────────────────────────────

        private static HashSet<DateTime> GetOesterreichischeFeiertage(int jahr)
        {
            var feiertage = new HashSet<DateTime>
            {
                new DateTime(jahr, 1,  1),   // Neujahr
                new DateTime(jahr, 1,  6),   // Heilige Drei Könige
                new DateTime(jahr, 5,  1),   // Staatsfeiertag
                new DateTime(jahr, 8,  15),  // Mariä Himmelfahrt
                new DateTime(jahr, 10, 26),  // Nationalfeiertag
                new DateTime(jahr, 11, 1),   // Allerheiligen
                new DateTime(jahr, 12, 8),   // Mariä Empfängnis
                new DateTime(jahr, 12, 25),  // Christtag
                new DateTime(jahr, 12, 26),  // Stefanitag
            };

            var ostern = BerechneOstern(jahr);
            feiertage.Add(ostern.AddDays(39));  // Christi Himmelfahrt
            feiertage.Add(ostern.AddDays(50));  // Pfingstmontag
            feiertage.Add(ostern.AddDays(60));  // Fronleichnam

            return feiertage;
        }

        private static DateTime BerechneOstern(int jahr)
        {
            int a = jahr % 19;
            int b = jahr / 100;
            int c = jahr % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day   = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(jahr, month, day);
        }

        private static decimal BerechneArbeitstage(DateOnly startDate, DateOnly endDate)
        {
            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end   = endDate.ToDateTime(TimeOnly.MinValue);

            var feiertage = new HashSet<DateTime>();
            for (int jahr = start.Year; jahr <= end.Year; jahr++)
                foreach (var ft in GetOesterreichischeFeiertage(jahr))
                    feiertage.Add(ft);

            decimal arbeitstage = 0;
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Saturday ||
                    d.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                if (feiertage.Contains(d.Date))
                    continue;
                arbeitstage++;
            }
            return arbeitstage;
        }
    }
}
