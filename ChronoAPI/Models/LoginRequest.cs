// ============================================================
//  LoginRequest.cs  (Datenmodelle für Login)
// ============================================================
//
//  Diese Klassen sind keine Datenbanktabellen!
//  Sie sind "DTOs" = Data Transfer Objects.
//  DTOs werden nur für die Kommunikation zwischen Frontend
//  und API verwendet – zum Senden und Empfangen von Daten.
//
//  Ablauf des Logins:
//   1. Frontend schickt LoginRequest (Benutzername + Passwort) → API
//   2. API prüft die Daten, erstellt ein JWT-Token
//   3. API schickt LoginResponse (Token + Infos) zurück → Frontend
//   4. Frontend speichert den Token und ist eingeloggt
// ============================================================

namespace ChronoAPI.Models
{
    // Was das Frontend zur API schickt wenn jemand einloggt
    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;  // z.B. "anna.mueller"
        public string Password { get; set; } = string.Empty;  // z.B. "Mein@Passwort123"
    }

    // Was die API zurückschickt wenn der Login erfolgreich war
    public class LoginResponse
    {
        // Das JWT-Token – dieser String beweist dem Frontend, dass der User eingeloggt ist.
        // Wird bei jeder API-Anfrage als "Bearer TOKEN" im HTTP-Header mitgeschickt.
        public string Token { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;   // Login-Name
        public string Role { get; set; } = string.Empty;        // Rolle (z.B. "HR")
        public string VollName { get; set; } = string.Empty;    // z.B. "Anna Müller"
        public DateTime ExpiresAt { get; set; }                  // Wann läuft das Token ab?

        // Muss das Passwort beim nächsten Login geändert werden?
        // true = User hat noch das temporäre Passwort von HR
        public bool MustChangePassword { get; set; }
    }
}
