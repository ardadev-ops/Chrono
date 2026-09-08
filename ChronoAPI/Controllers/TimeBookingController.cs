// ============================================================
//  TimeBookingController.cs  (Zeitbuchungen / Stempeluhr)
// ============================================================
//
//  Endpunkte:
//   GET    api/TimeBooking              → Alle Buchungen (je nach Rolle)
//   GET    api/TimeBooking/{id}         → Einzelne Buchung
//   GET    api/TimeBooking/mitarbeiter/{mitId} → Buchungen eines Mitarbeiters
//   GET    api/TimeBooking/offen        → Buchungen ohne Checkout (eigene)
//   POST   api/TimeBooking              → Einstempeln (Check-in)
//   PUT    api/TimeBooking/{id}         → Ausstempeln (Check-out)
//   DELETE api/TimeBooking/{id}         → Buchung löschen (nur Admin)
//
//  ABLAUF BEIM STEMPELN:
//   1. Mitarbeiter hält NFC-Karte an das Lesegerät
//   2. Lesegerät ruft POST api/TimeBooking auf → Check-in (nur CheckInTime)
//   3. Mitarbeiter hält Karte nochmal ran
//   4. Lesegerät ruft PUT api/TimeBooking/{id} auf → Check-out (CheckOutTime wird gesetzt)
//
//  SICHTBARKEIT JE NACH ROLLE:
//   - Mitarbeiter: sehen nur eigene Buchungen (via /mitarbeiter/{mitId})
//   - Abteilungsleiter: sehen Buchungen ihrer Abteilung
//   - HR/Admin/Buchhaltung: sehen ALLE Buchungen
// ============================================================

using ChronoAPI.Data;
using ChronoAPI.Extensions;
using ChronoAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Jeder muss eingeloggt sein
    public class TimeBookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TimeBookingController(AppDbContext context) => _context = context;

        // ── ALLE BUCHUNGEN ABRUFEN ────────────────────────────
        // GET api/TimeBooking
        // Nur bestimmte Rollen dürfen alle Buchungen sehen
        [HttpGet]
        [Authorize(Roles = "HR,Admin,Abteilungsleiter,Buchhaltung")]
        public async Task<ActionResult<IEnumerable<TimeBooking>>> GetAll()
        {
            var query = _context.TimeBookings
                .Include(t => t.Mitarbeiter)
                .AsQueryable();

            // Abteilungsleiter → nur Buchungen seiner Abteilung
            if (User.IstAufAbteilungBeschraenkt())
            {
                var eigeneAbteilung = User.GetAbteilungsId();

                if (eigeneAbteilung == null)
                    return Ok(new List<TimeBooking>());

                query = query.Where(t => t.Mitarbeiter!.FK_AbtID == eigeneAbteilung);
            }

            // HR, Admin, Buchhaltung → alle Buchungen aller Mitarbeiter (kein Filter)
            return Ok(await query.OrderByDescending(t => t.CheckInTime).ToListAsync());
        }

        // ── EINZELNE BUCHUNG ABRUFEN ──────────────────────────
        // GET api/TimeBooking/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeBooking>> GetById(int id)
        {
            var b = await _context.TimeBookings
                .Include(t => t.Mitarbeiter)
                .FirstOrDefaultAsync(t => t.TimeID == id);
            if (b == null) return NotFound();
            return b;
        }

        // ── BUCHUNGEN EINES MITARBEITERS ──────────────────────
        // GET api/TimeBooking/mitarbeiter/5
        // Jeder eingeloggte User darf seine EIGENEN Buchungen sehen.
        // (Das Frontend sendet die eigene MitarbeiterID, die API vertraut darauf.)
        [HttpGet("mitarbeiter/{mitId}")]
        public async Task<ActionResult<IEnumerable<TimeBooking>>> GetByMitarbeiter(int mitId)
        {
            return await _context.TimeBookings
                .Where(t => t.FK_MitID == mitId)
                .OrderByDescending(t => t.CheckInTime)
                .ToListAsync();
        }

        // ── EINSTEMPELN (CHECK-IN) ────────────────────────────
        // POST api/TimeBooking
        // Erstellt eine neue Buchung mit CheckInTime aber noch OHNE CheckOutTime
        [HttpPost]
        public async Task<ActionResult<TimeBooking>> CheckIn(TimeBooking booking)
        {
            booking.CheckInTime = DateTime.Now;
            booking.CreatedAt = DateTime.Now;
            booking.BreakMinutes = 0; // Wird beim Ausstempeln automatisch gesetzt
            _context.TimeBookings.Add(booking);
            await _context.SaveChangesAsync();

            // 201 Created + Link zur neuen Ressource zurückgeben
            return CreatedAtAction(nameof(GetById),
                new { id = booking.TimeID }, booking);
        }

        [HttpGet("aktuell")]
        public async Task<ActionResult<TimeBooking>> GetAktuell()
        {
            var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value; //Liest aus dem JWT-Token die MitarbeiterID aus
            if (!int.TryParse(mitIdClaim, out int mitId))
                return Unauthorized();

            var heute = DateTime.Today;

            // Suche nach einer offenen Buchung (ohne CheckOutTime) für den eingeloggten Mitarbeiter
            var offeneBuchung = await _context.TimeBookings
                .Where(t => t.FK_MitID == mitId && t.CheckOutTime == null)
                .OrderByDescending(t => t.CheckInTime) // Falls mehrere offen, die neueste nehmen
                .FirstOrDefaultAsync();
            return Ok(offeneBuchung);
        }

        // ── AUSSTEMPELN (CHECK-OUT) ───────────────────────────
        // PUT api/TimeBooking/5
        // Aktualisiert eine bestehende Buchung und setzt CheckOutTime
        [HttpPut("{id}")]
        public async Task<IActionResult> CheckOut(int id, TimeBooking booking)
        {
            if (id != booking.TimeID) return BadRequest();

            // Aus DB laden - nicht das was vom Frontend kommt!
            var existing = await _context.TimeBookings.FindAsync(id);
            if (existing == null) return NotFound();

            existing.CheckOutTime = DateTime.Now;
            existing.UpdatedAt = DateTime.Now;

            // Pausenzeit nach österreichischem Recht automatisch berechnen
            var arbeitsstunden = (existing.CheckOutTime.Value - existing.CheckInTime).TotalHours;
            existing.BreakMinutes = BerechnePflichtpause(arbeitsstunden);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── OFFENE BUCHUNGEN (ohne Checkout) ──────────────────
        // GET api/TimeBooking/offen
        // Zeigt Buchungen des eingeloggten Users bei denen noch kein
        // Checkout stattgefunden hat UND die von gestern oder früher sind.
        // (Heute offene Buchungen = noch am Arbeiten → nicht anzeigen)
        [HttpGet("offen")]
        public async Task<ActionResult<IEnumerable<TimeBooking>>> GetOffene()
        {
            // MitarbeiterID aus dem JWT-Token lesen
            var mitIdClaim = User.FindFirst("MitarbeiterID")?.Value;
            int.TryParse(mitIdClaim, out int mitId);

            var heute = DateTime.Today; // Nur das Datum (ohne Uhrzeit) von heute

            // Buchungen suchen die:
            //  - dem eingeloggten Mitarbeiter gehören
            //  - keinen Checkout haben
            //  - von gestern oder früher sind (also vergessener Checkout)
            return await _context.TimeBookings
                .Where(t => t.FK_MitID == mitId
                         && t.CheckOutTime == null          // Noch kein Checkout
                         && t.CheckInTime.Date < heute)     // Von gestern oder früher
                .OrderByDescending(t => t.CheckInTime)
                .ToListAsync();
        }

        // ── BUCHUNG LÖSCHEN ───────────────────────────────────
        // DELETE api/TimeBooking/5  (nur Admin!)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var b = await _context.TimeBookings.FindAsync(id);
            if (b == null) return NotFound();
            _context.TimeBookings.Remove(b);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── HILFSMETHODE ──────────────────────────────────────
        // § 11 AZG: ab 6h = 30 Min, ab 8h = 60 Min Pflichtpause
        private static int BerechnePflichtpause(double arbeitsstunden)
        {
            if (arbeitsstunden >= 8) return 60;
            if (arbeitsstunden >= 6) return 30;
            return 0;
        }

        [HttpPost("nfc")]
        [AllowAnonymous]
        public async Task<IActionResult> NfcBooking([FromBody] NfcRequest request)
        {
            // Leere oder fehlende UID abfangen
            if (string.IsNullOrWhiteSpace(request?.Uid))
            {
                return BadRequest(new NFCResponse
                {
                    Success = false,
                    Message = "Keine Karten-ID uebermittelt"
                });
            }

            // ── Schritt 1: Mitarbeiter anhand der Karten-UID suchen ──
            var mitarbeiter = await _context.Mitarbeiter
                .FirstOrDefaultAsync(m => m.NFCCardUid == request.Uid);

            // ── Schritt 2: Unbekannte Karte -> Abbruch ──
            if (mitarbeiter == null)
            {
                return NotFound(new NFCResponse
                {
                    Success = false,
                    Message = "Karte unbekannt"
                });
            }

            var vollName = $"{mitarbeiter.MitVorname} {mitarbeiter.MitNachname}";
            var jetzt = DateTime.Now;

            // ── Schritt 3: Offene Buchung des Mitarbeiters suchen ──
            var offeneBuchung = await _context.TimeBookings
                .Where(b => b.FK_MitID == mitarbeiter.MitID && b.CheckOutTime == null)
                .OrderByDescending(b => b.CheckInTime)
                .FirstOrDefaultAsync();

            // ── Schritt 4: Keine offene Buchung -> CHECK-IN ──
            if (offeneBuchung == null)
            {
                var neueBuchung = new TimeBooking
                {
                    FK_MitID = mitarbeiter.MitID,
                    CheckInTime = jetzt,
                    CreatedAt = jetzt,
                    BreakMinutes = 0,
                    Notes = "NFC-Terminal"
                };

                _context.TimeBookings.Add(neueBuchung);
                await _context.SaveChangesAsync();

                return Ok(new NFCResponse
                {
                    Success = true,
                    Action = "CHECK-IN",
                    Message = $"Willkommen {mitarbeiter.MitVorname}!",
                    Time = jetzt.ToString("HH:mm"),
                    MitarbeiterName = vollName
                });
            }

            // ── Schritt 5: Offene Buchung vorhanden -> CHECK-OUT ──
            var arbeitsstunden = (jetzt - offeneBuchung.CheckInTime).TotalHours;

            offeneBuchung.CheckOutTime = jetzt;
            offeneBuchung.UpdatedAt = jetzt;
            offeneBuchung.BreakMinutes = BerechnePflichtpause(arbeitsstunden);

            await _context.SaveChangesAsync();

            var netto = arbeitsstunden - (offeneBuchung.BreakMinutes / 60.0);

            return Ok(new NFCResponse
            {
                Success = true,
                Action = "CHECK-OUT",
                Message = $"Tschuess {mitarbeiter.MitVorname}! {netto:F1}h",
                Time = jetzt.ToString("HH:mm"),
                MitarbeiterName = vollName
            });


        }
    }
}
