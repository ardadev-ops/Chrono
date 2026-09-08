// ============================================================
//  AuthController.cs  (Authentifizierung / Login)
// ============================================================
//
//  Controller = eine Klasse die HTTP-Anfragen bearbeitet.
//  Dieser Controller ist für alles rund um Login/Passwort zuständig.
//
//  Endpunkte (URLs):
//   POST api/Auth/login           → Einloggen
//   POST api/Auth/change-password → Eigenes Passwort ändern
//   POST api/Auth/reset-password  → Passwort eines anderen zurücksetzen (HR/Admin)
//   POST api/Auth/logout          → Ausloggen
//
//  Wichtiges Konzept – JWT-Token:
//   Nach erfolgreichem Login bekommt das Frontend ein Token.
//   Dieses Token schickt das Frontend bei JEDER Anfrage mit.
//   Dadurch weiß die API: "Dieser Benutzer ist eingeloggt, er heißt XY,
//   hat die Rolle HR, und ist Mitarbeiter mit ID 5."
//
//  Wichtiges Konzept – BCrypt:
//   Passwörter werden NIE im Klartext gespeichert!
//   BCrypt erzeugt aus "MeinPasswort123!" einen unlesbaren Hash:
//   "$2b$12$abc123xyz..." – selbst wenn die DB gestohlen wird,
//   kann man die Passwörter nicht zurückrechnen.
// ============================================================

using ChronoAPI.Data;
using ChronoAPI.Extensions;
using ChronoAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChronoAPI.Controllers
{
    // [Route("api/[controller]")] = die URL-Basis ist "api/Auth"
    // [ApiController] = aktiviert automatische Validierung und JSON-Konvertierung
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Dependency Injection: AppDbContext und IConfiguration werden
        // automatisch vom Framework eingefügt (nicht manuell erstellt).
        // _context = Zugriff auf die Datenbank
        // _config  = Zugriff auf appsettings.json (JWT-Schlüssel, etc.)
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ── LOGIN ─────────────────────────────────────────────
        // POST api/Auth/login
        // [AllowAnonymous] = dieser Endpunkt braucht KEIN Token (logisch,
        // weil man noch keins hat wenn man sich einloggt).
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Eingabe validieren: Sind Benutzername und Passwort überhaupt eingegeben?
            if (string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Benutzername und Passwort erforderlich." });

            // User in der Datenbank suchen.
            // .Include(u => u.Mitarbeiter) lädt auch die Mitarbeiterdaten
            // (Name, etc.) gleich mit (SQL JOIN).
            var user = await _context.Users
                .Include(u => u.Mitarbeiter)
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);

            // Sicherheits-TIPP: Wir sagen "Benutzername ODER Passwort falsch"
            // und nicht "Benutzername nicht gefunden". So weiß ein Angreifer
            // nicht ob der Benutzername existiert oder nicht.
            if (user == null)
                return Unauthorized(new { message = "Benutzername oder Passwort falsch." });

            // Deaktivierter Account?
            if (user.UserAktiv == false)
                return Unauthorized(new { message = "Dieser Account ist deaktiviert. Bitte HR kontaktieren." });

            // ── ACCOUNT-SPERRUNG PRÜFEN ───────────────────────
            // Hat der User zu viele Fehlversuche? Ist er noch gesperrt?
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now)
            {
                // Berechne wie viele Minuten noch gesperrt (aufgerundet)
                var verbleibend = (int)(user.LockedUntil.Value - DateTime.Now).TotalMinutes + 1;
                return Unauthorized(new
                {
                    message = $"Account gesperrt. Bitte warte {verbleibend} Minute(n) oder kontaktiere HR."
                });
            }

            // ── PASSWORT PRÜFEN MIT BCRYPT ────────────────────
            // BCrypt.Verify vergleicht das eingegebene Passwort mit dem Hash in der DB.
            // Es ist NICHT möglich den Hash zurückzurechnen – BCrypt hasht das
            // eingegebene Passwort erneut und vergleicht die Hashes.
            bool passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user.UserPassword);

            if (!passwordOk)
            {
                // Fehlversuch zählen
                user.FailedLoginAttempts++;

                // 5 oder mehr Fehlversuche → Account für 30 Minuten sperren
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.Now.AddMinutes(30);
                    user.FailedLoginAttempts = 0; // Zähler zurücksetzen
                    await _context.SaveChangesAsync();
                    return Unauthorized(new
                    {
                        message = "Zu viele Fehlversuche. Account für 30 Minuten gesperrt. Bitte HR kontaktieren."
                    });
                }

                // Noch nicht gesperrt → zeige wie viele Versuche noch übrig
                var verbleibend = 5 - user.FailedLoginAttempts;
                await _context.SaveChangesAsync();
                return Unauthorized(new
                {
                    message = $"Benutzername oder Passwort falsch. Noch {verbleibend} Versuch(e) vor Sperrung."
                });
            }

            // ── LOGIN ERFOLGREICH ─────────────────────────────
            // Fehlversuch-Zähler und Sperrung zurücksetzen
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            // Vollständigen Namen zusammenbauen
            var mitarbeiter = user.Mitarbeiter;
            var vollName = mitarbeiter != null
                ? $"{mitarbeiter.MitVorname} {mitarbeiter.MitNachname}"
                : user.UserName;

            // Abteilungs-ID des Mitarbeiters (für serverseitige Abteilungsfilterung)
            var abteilungsId = mitarbeiter?.FK_AbtID;

            // JWT-Token generieren (enthält alle wichtigen Infos über den User)
            var token = GenerateToken(user, vollName, abteilungsId);

            // Alles was das Frontend braucht zurückschicken
            return Ok(new
            {
                token,                              // Das Token (für alle weiteren Anfragen)
                userName = user.UserName,           // Login-Name
                role = user.UserRole,               // Rolle (HR, Admin, ...)
                mitarbeiterId = user.FK_MitID,      // Mitarbeiter-ID
                abteilungsId,                       // Abteilungs-ID (für Abteilungsleiter-Filterung)
                vollName,                           // "Anna Müller"
                mustChangePassword = user.MustChangePassword, // Muss Passwort geändert werden?
                expiresAt = DateTime.Now.AddHours(_config.GetValue<int>("JwtSettings:ExpirationHours"))
            });
        }

        // ── PASSWORT ÄNDERN ───────────────────────────────────
        // POST api/Auth/change-password
        // [Authorize] = nur eingeloggte User dürfen das aufrufen (Token erforderlich)
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            // Benutzernamen aus dem JWT-Token lesen (der Token wurde beim Login ausgestellt)
            var userName = User.FindFirst("unique_name")?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null) return NotFound(new { message = "User nicht gefunden" });

            // Altes Passwort korrekt?
            if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, user.UserPassword))
                return BadRequest(new { message = "Altes Passwort ist falsch!" });

            // Ist das neue Passwort dasselbe wie das alte? (nicht erlaubt)
            if (BCrypt.Net.BCrypt.Verify(req.NewPassword, user.UserPassword))
                return BadRequest(new { message = "Neues Passwort darf nicht gleich dem alten sein!" });

            // Haben beide Passwort-Felder denselben Wert?
            if (req.NewPassword != req.ConfirmPassword)
                return BadRequest(new { message = "Passwörter stimmen nicht überein!" });

            // Passwort-Regeln prüfen (mind. 8 Zeichen, 1 Zahl, 1 Sonderzeichen)
            if (!IsValidPassword(req.NewPassword))
                return BadRequest(new { message = "Passwort: mind. 8 Zeichen, 1 Zahl, 1 Sonderzeichen!" });

            // Neues Passwort hashen und speichern
            // workFactor: 12 = wie aufwendig der Hash berechnet wird (höher = sicherer, langsamer)
            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12);
            user.MustChangePassword = false;   // Pflicht-Änderung erledigt
            user.PasswordChangedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Passwort erfolgreich geändert!" });
        }

        // ── PASSWORT ZURÜCKSETZEN (durch HR/Admin/AL) ─────────
        // POST api/Auth/reset-password
        // Nur HR, Admin oder Abteilungsleiter dürfen das aufrufen
        [HttpPost("reset-password")]
        [Authorize(Roles = "HR,Admin,Abteilungsleiter")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            // Wer setzt das Passwort zurück? (aus dem JWT-Token)
            var resetterMitId = int.Parse(User.FindFirst("MitarbeiterID")!.Value);

            // Den Ziel-User finden (für welchen Mitarbeiter soll zurückgesetzt werden?)
            var targetUser = await _context.Users
                .Include(u => u.Mitarbeiter)
                .FirstOrDefaultAsync(u => u.FK_MitID == req.MitarbeiterId);
            if (targetUser == null) return NotFound(new { message = "User nicht gefunden" });

            // Abteilungsleiter dürfen nur Passwörter in ihrer eigenen Abteilung zurücksetzen!
            if (User.IsInRole("Abteilungsleiter"))
            {
                var resetter = await _context.Mitarbeiter
                    .FirstOrDefaultAsync(m => m.MitID == resetterMitId);
                // Gehört der Ziel-Mitarbeiter zur selben Abteilung?
                if (targetUser.Mitarbeiter?.FK_AbtID != resetter?.FK_AbtID)
                    return Forbid(); // Kein Zugriff!
            }

            // Passwort-Regeln prüfen
            if (!IsValidPassword(req.NewPassword))
                return BadRequest(new { message = "Passwort: mind. 8 Zeichen, 1 Zahl, 1 Sonderzeichen!" });

            // Neues temporäres Passwort setzen
            targetUser.UserPassword = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12);
            targetUser.MustChangePassword = true;  // User muss beim nächsten Login ändern!
            targetUser.FailedLoginAttempts = 0;    // Fehlversuche zurücksetzen
            targetUser.LockedUntil = null;          // Sperre aufheben

            // Dokumentieren wer es zurückgesetzt hat und wann
            var resetterUser = await _context.Users
                .FirstOrDefaultAsync(u => u.FK_MitID == resetterMitId);
            targetUser.PasswordResetBy = resetterUser?.UserID;
            targetUser.PasswordResetAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Passwort zurückgesetzt! User muss es beim nächsten Login ändern." });
        }

        // ── ROLLE AENDERN ─────────────────────────────────────
        // POST api/Auth/change-role
        // Nur HR und Admin. Die Rolle Admin darf nur ein Admin vergeben.
        [HttpPost("change-role")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleRequest req)
        {
            // Zulaessige Rollen entsprechen der CHECK-Bedingung in tblUsers
            var erlaubteRollen = new[]
                { "Mitarbeiter", "Abteilungsleiter", "HR", "Buchhaltung", "Admin" };

            if (!erlaubteRollen.Contains(req.NeueRolle))
                return BadRequest(new { message = "Unbekannte Rolle." });

            // Wer fuehrt die Aenderung durch?
            var ausfuehrenderMitId = User.GetMitarbeiterId();
            var ausfuehrenderRolle = User.GetRolle();

            // Niemand darf die eigene Rolle aendern
            if (ausfuehrenderMitId == req.MitarbeiterId)
                return BadRequest(new { message = "Die eigene Rolle kann nicht geaendert werden." });

            // Die Rolle Admin darf nur ein Admin vergeben
            if (req.NeueRolle == "Admin" && ausfuehrenderRolle != "Admin")
                return BadRequest(new { message = "Nur ein Administrator darf die Rolle Admin vergeben." });

            // Zielbenutzer laden
            var zielUser = await _context.Users
                .Include(u => u.Mitarbeiter)
                .FirstOrDefaultAsync(u => u.FK_MitID == req.MitarbeiterId);

            if (zielUser == null)
                return NotFound(new { message = "Zu diesem Mitarbeiter existiert kein Benutzerkonto." });

            var alteRolle = zielUser.UserRole;

            if (alteRolle == req.NeueRolle)
                return BadRequest(new { message = "Der Mitarbeiter hat diese Rolle bereits." });

            // Ein bestehender Admin darf nur degradiert werden, wenn ein weiterer aktiver Admin existiert
            if (alteRolle == "Admin" && req.NeueRolle != "Admin")
            {
                var weitereAdmins = await _context.Users
                    .CountAsync(u => u.UserRole == "Admin"
                                  && u.UserAktiv == true
                                  && u.UserID != zielUser.UserID);

                if (weitereAdmins == 0)
                    return BadRequest(new
                    {
                        message = "Der letzte Administrator kann nicht degradiert werden."
                    });
            }

            // Aenderung durchfuehren
            zielUser.UserRole = req.NeueRolle;

            // Protokolleintrag schreiben
            var ausfuehrenderUser = await _context.Users
                .FirstOrDefaultAsync(u => u.FK_MitID == ausfuehrenderMitId);

            _context.AuditLogs.Add(new AuditLog
            {
                FK_UserId  = ausfuehrenderUser?.UserID,
                Action     = "Rollenaenderung",
                TableName  = "tblUsers",
                RecordId   = zielUser.UserID,
                OldValues  = $"{{\"UserRole\":\"{alteRolle}\"}}",
                NewValues  = $"{{\"UserRole\":\"{req.NeueRolle}\"}}",
                IpAddress  = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt  = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // Hinweis: Wird in derselben Abteilung ein weiterer Abteilungsleiter angelegt,
            // ist das zulaessig (Vertretung), wird aber zurueckgemeldet.
            string? hinweis = null;

            if (req.NeueRolle == "Abteilungsleiter" && zielUser.Mitarbeiter?.FK_AbtID != null)
            {
                var weitereLeiter = await _context.Users
                    .Include(u => u.Mitarbeiter)
                    .CountAsync(u => u.UserRole == "Abteilungsleiter"
                                  && u.UserAktiv == true
                                  && u.UserID != zielUser.UserID
                                  && u.Mitarbeiter!.FK_AbtID == zielUser.Mitarbeiter.FK_AbtID);

                if (weitereLeiter > 0)
                    hinweis = $"In dieser Abteilung gibt es bereits {weitereLeiter} weitere(n) Abteilungsleiter.";
            }

            return Ok(new
            {
                message = $"Rolle geaendert: {alteRolle} → {req.NeueRolle}. "
                        + "Die Aenderung wirkt nach der naechsten Anmeldung des Benutzers.",
                hinweis
            });
        }

        // ── LOGOUT ────────────────────────────────────────────
        // POST api/Auth/logout
        // Das echte Ausloggen passiert im Frontend (Token löschen).
        // Diese Methode ist nur ein Bestätigungs-Endpunkt.
        // JWT-Tokens können serverseitig nicht "ungültig" gemacht werden –
        // das ist eine bekannte Einschränkung von JWT.
        [HttpPost("logout")]
        public IActionResult Logout() => Ok(new { message = "Erfolgreich abgemeldet!" });


        // ── HILFSMETHODE: JWT-TOKEN ERSTELLEN ─────────────────
        // Erstellt einen signierten JWT-Token mit allen User-Infos.
        // Dieser Token läuft nach X Stunden ab (in appsettings.json konfiguriert).
        private string GenerateToken(User user, string vollName, int? abteilungsId)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expirationHours = jwtSettings.GetValue<int>("ExpirationHours");

            // Der Schlüssel mit dem der Token signiert wird.
            // Wer diesen Schlüssel hat, kann Tokens fälschen → GEHEIM halten!
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims = Informationen die im Token gespeichert werden.
            // Das Frontend kann diese auslesen ohne die DB zu fragen.
            // ACHTUNG: Keine geheimen Daten in Claims! JWT ist nur signiert, nicht verschlüsselt.
            var claims = new[]
            {
                new Claim("nameid", user.UserID.ToString()),           // User-ID
                new Claim("unique_name", user.UserName),               // Benutzername
                new Claim("role", user.UserRole),                      // Rolle (z.B. "HR")
                new Claim("MitarbeiterID", user.FK_MitID.ToString()),  // Mitarbeiter-ID
                new Claim("VollName", vollName),                       // "Anna Müller"
                new Claim("AbteilungsID", abteilungsId?.ToString() ?? "0"), // Abteilungs-ID (0 = keiner zugeordnet)
                new Claim("MustChangePassword", user.MustChangePassword.ToString().ToLower()) // "true" / "false"
            };

            // Token zusammenbauen
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(expirationHours), // Ablaufzeit
                signingCredentials: credentials                   // Signatur
            );

            // Token als String serialisieren (der lange "eyJ..."-String)
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ── HILFSMETHODE: PASSWORT-REGELN PRÜFEN ──────────────
        // Gibt true zurück wenn das Passwort die Anforderungen erfüllt:
        //  - Mindestens 8 Zeichen
        //  - Mindestens 1 Zahl (z.B. "3")
        //  - Mindestens 1 Sonderzeichen (z.B. "!" oder "@")
        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            if (!password.Any(char.IsDigit)) return false; // .Any() = enthält mindestens ein...
            const string special = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
            return password.Any(c => special.Contains(c)); // enthält mindestens ein Sonderzeichen?
        }
    }
}
