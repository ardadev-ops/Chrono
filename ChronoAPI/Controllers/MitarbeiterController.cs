// ============================================================
//  MitarbeiterController.cs  (Mitarbeiterverwaltung)
// ============================================================
//
//  Endpunkte (URLs):
//   GET    api/Mitarbeiter       → Liste aller Mitarbeiter (nur HR/Admin/AL)
//   GET    api/Mitarbeiter/{id}  → Einzelnen Mitarbeiter abrufen
//   POST   api/Mitarbeiter       → Neuen Mitarbeiter + User erstellen (HR/Admin)
//   PUT    api/Mitarbeiter/{id}  → Mitarbeiter bearbeiten (HR/Admin)
//   DELETE api/Mitarbeiter/{id}  → Mitarbeiter löschen (nur Admin)
//
//  WICHTIG: Wenn ein neuer Mitarbeiter angelegt wird, wird
//  AUTOMATISCH auch ein Login-Account (User) erstellt!
//  Der Benutzername wird automatisch generiert: vorname.nachname
//  (z.B. "anna.mueller" für Anna Müller)
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
    [Authorize] // Alle Endpunkte brauchen ein gültiges JWT-Token
    public class MitarbeiterController : ControllerBase
    {
        private readonly AppDbContext _context;

        // _context wird automatisch eingefügt (Dependency Injection)
        public MitarbeiterController(AppDbContext context) => _context = context;

        // ── ALLE MITARBEITER ABRUFEN ──────────────────────────
        // GET api/Mitarbeiter
        // Nur HR, Admin und Abteilungsleiter dürfen das aufrufen
        [HttpGet]
        [Authorize(Roles = "HR,Admin,Abteilungsleiter")]
        public async Task<ActionResult<IEnumerable<Mitarbeiter>>> GetAll()
        {
            var query = _context.Mitarbeiter
                .Include(m => m.Abteilung)
                .AsQueryable();

            // Abteilungsleiter sehen ausschliesslich ihre eigene Abteilung
            if (User.IstAufAbteilungBeschraenkt())
            {
                var eigeneAbteilung = User.GetAbteilungsId();

                if (eigeneAbteilung == null)
                    return Ok(new List<Mitarbeiter>());   // keiner Abteilung zugeordnet

                query = query.Where(m => m.FK_AbtID == eigeneAbteilung);
            }

            var liste = await query.ToListAsync();
            await AttachRollen(liste);
            return Ok(liste);
        }

        // ── EINZELNEN MITARBEITER ABRUFEN ─────────────────────
        // GET api/Mitarbeiter/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Mitarbeiter>> GetById(int id)
        {
            var m = await _context.Mitarbeiter
                .Include(m => m.Abteilung)
                .FirstOrDefaultAsync(m => m.MitID == id);
            if (m == null) return NotFound(); // 404 wenn nicht gefunden

            // Abteilungsleiter duerfen nur Mitarbeiter der eigenen Abteilung abrufen
            if (User.IstAufAbteilungBeschraenkt()
                && m.FK_AbtID != User.GetAbteilungsId())
            {
                return Forbid();
            }

            await AttachRollen(new[] { m });
            return m;
        }

        // ── NEUEN MITARBEITER + USER ERSTELLEN ────────────────
        // POST api/Mitarbeiter
        // Nur HR und Admin dürfen neue Mitarbeiter anlegen
        [HttpPost]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateMitarbeiterWithUserRequest req)
        {
            // Eingabe-Validierungen
            if (string.IsNullOrWhiteSpace(req.TempPassword))
                return BadRequest(new { message = "Temporäres Passwort darf nicht leer sein!" });

            if (!IsValidPassword(req.TempPassword))
                return BadRequest(new { message = "Passwort: mind. 8 Zeichen, 1 Zahl, 1 Sonderzeichen!" });

            // Nur diese Rollen sind erlaubt (Tippfehler-Schutz)
            var erlaubteRollen = new[] { "Mitarbeiter", "HR", "Abteilungsleiter", "Buchhaltung", "Admin" };
            if (!erlaubteRollen.Contains(req.Role))
                return BadRequest(new { message = "Ungültige Rolle!" });

            // ── MITARBEITER IN DB SPEICHERN ───────────────────
            var mitarbeiter = req.Mitarbeiter;
            mitarbeiter.CreatedAt = DateTime.Now;
            mitarbeiter.MitAktiv = true;

            // Navigation Properties zurücksetzen:
            // EF Core würde sonst versuchen verknüpfte Objekte neu zu erstellen.
            // Wir wollen aber nur den FK (Fremdschlüssel) speichern, nicht die verknüpften Objekte.
            mitarbeiter.Abteilung = null;
            mitarbeiter.User = null;
            mitarbeiter.TimeBookings = new List<TimeBooking>();

            _context.Mitarbeiter.Add(mitarbeiter);
            await _context.SaveChangesAsync(); // Mitarbeiter speichern → bekommt jetzt eine MitID

            // ── BENUTZERNAME AUTOMATISCH GENERIEREN ──────────
            // z.B. "Anna Müller" → "anna.mueller"
            // Falls der Name schon existiert → "anna.mueller2", "anna.mueller3", ...
            var username = await GenerateUsername(mitarbeiter.MitVorname, mitarbeiter.MitNachname);

            // ── USER/LOGIN-ACCOUNT ERSTELLEN ──────────────────
            var user = new User
            {
                FK_MitID           = mitarbeiter.MitID,    // Verknüpfung mit dem Mitarbeiter
                UserName           = username,
                // Passwort sofort hashen – nie im Klartext speichern!
                UserPassword       = BCrypt.Net.BCrypt.HashPassword(req.TempPassword, workFactor: 12),
                UserRole           = req.Role,
                MustChangePassword = true, // Muss beim ersten Login geändert werden
                UserAktiv          = true,
                CreatedAt          = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // User speichern

            // Alle wichtigen Infos zurückschicken (inkl. generiertem Benutzernamen)
            return CreatedAtAction(nameof(GetById), new { id = mitarbeiter.MitID }, new
            {
                mitarbeiter,
                username,
                role = req.Role,
                message = $"Mitarbeiter und Login erstellt! Username: {username}"
            });
        }

        // ── MITARBEITER BEARBEITEN ────────────────────────────
        // PUT api/Mitarbeiter/5
        [HttpPut("{id}")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> Update(int id, Mitarbeiter mitarbeiter)
        {
            // Sicherheitscheck: URL-ID muss zur Body-ID passen
            if (id != mitarbeiter.MitID) return BadRequest();

            mitarbeiter.UpdatedAt = DateTime.Now;
            // Navigation Properties zurücksetzen (gleicher Grund wie beim Erstellen)
            mitarbeiter.Abteilung = null;
            mitarbeiter.User = null;
            mitarbeiter.TimeBookings = new List<TimeBooking>();

            // EntityState.Modified sagt EF Core: "Dieser Eintrag wurde geändert, update ihn!"
            _context.Entry(mitarbeiter).State = EntityState.Modified;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                // Tritt auf wenn zwei User gleichzeitig denselben Datensatz ändern
                if (!_context.Mitarbeiter.Any(e => e.MitID == id))
                    return NotFound();
                throw; // Anderer Fehler → weiterschmeißen
            }
            return NoContent(); // 204 = Erfolg, keine Antwort nötig
        }

        // ── MITARBEITER LÖSCHEN ───────────────────────────────
        // DELETE api/Mitarbeiter/5  (nur Admin!)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.Mitarbeiter.FindAsync(id);
            if (m == null) return NotFound();
            _context.Mitarbeiter.Remove(m);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── HILFSMETHODE: BENUTZERROLLE ANHEFTEN ──────────────
        // Laedt NUR die Rolle (UserRole) der zugehoerigen Benutzerkonten –
        // bewusst kein .Include(m => m.User), damit der Passwort-Hash
        // niemals mit an das Frontend serialisiert wird.
        private async Task AttachRollen(IEnumerable<Mitarbeiter> mitarbeiter)
        {
            var mitIds = mitarbeiter.Select(m => m.MitID).ToList();
            var rollen = await _context.Users
                .Where(u => mitIds.Contains(u.FK_MitID))
                .Select(u => new { u.FK_MitID, u.UserRole })
                .ToDictionaryAsync(u => u.FK_MitID, u => u.UserRole);

            foreach (var m in mitarbeiter)
                m.UserRolle = rollen.GetValueOrDefault(m.MitID);
        }

        // ── HILFSMETHODE: PASSWORT-REGELN PRÜFEN ──────────────
        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            if (!password.Any(char.IsDigit)) return false;
            const string special = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
            return password.Any(c => special.Contains(c));
        }

        // ── HILFSMETHODE: BENUTZERNAME GENERIEREN ─────────────
        // Erstellt "anna.mueller" aus "Anna" und "Müller".
        // Wenn der Name schon existiert: "anna.mueller2", "anna.mueller3", ...
        private async Task<string> GenerateUsername(string vorname, string nachname)
        {
            // Lokale Hilfsfunktion: Sonderzeichen und Großbuchstaben bereinigen
            static string Clean(string s) => s.ToLower()
                .Replace("ä", "ae").Replace("ö", "oe")
                .Replace("ü", "ue").Replace("ß", "ss")
                .Replace(" ", ".").Replace("-", "");

            var baseUsername = $"{Clean(vorname)}.{Clean(nachname)}"; // "anna.mueller"
            var candidate = baseUsername;
            int i = 2;

            // Solange der Name schon in der DB existiert, Nummer anhängen
            while (await _context.Users.AnyAsync(u => u.UserName == candidate))
                candidate = $"{baseUsername}{i++}"; // "anna.mueller2", "anna.mueller3", ...

            return candidate;
        }
    }
}
