// ============================================================
//  PasswordRequests.cs  (DTOs für Passwort-Verwaltung)
// ============================================================
//
//  Diese Klassen sind nur für die Übertragung von Passwort-Daten
//  zwischen Frontend und API zuständig (keine DB-Tabellen!).
//
//  "record" ist wie eine Klasse, aber kürzer zu schreiben und
//  automatisch unveränderlich (immutable). Ideal für einfache
//  Datencontainer die nur übertragen werden.
// ============================================================

namespace ChronoAPI.Models;

// Passwort selber ändern (User ändert sein eigenes Passwort)
// Alle drei Felder müssen mitgeschickt werden.
public record ChangePasswordRequest(
    string OldPassword,      // Das aktuelle (alte) Passwort zur Verifikation
    string NewPassword,      // Das neue gewünschte Passwort
    string ConfirmPassword   // Nochmal das neue Passwort (Tippfehler-Schutz)
);

// Passwort zurücksetzen (HR/Admin setzt das Passwort eines anderen Users zurück)
// z.B. wenn jemand sein Passwort vergessen hat
public record ResetPasswordRequest(
    int MitarbeiterId,   // Für welchen Mitarbeiter soll das Passwort zurückgesetzt werden?
    string NewPassword   // Das neue temporäre Passwort (muss beim nächsten Login geändert werden)
);

// Wird beim Erstellen eines neuen Mitarbeiters verwendet.
// HR gibt Mitarbeiterdaten + temporäres Passwort + Rolle ein.
public class CreateMitarbeiterWithUserRequest
{
    // Die Stammdaten des neuen Mitarbeiters (Name, Mail, Abteilung, ...)
    public Mitarbeiter Mitarbeiter { get; set; } = new();

    // Temporäres Passwort, das HR festlegt.
    // Der Mitarbeiter muss es beim ersten Login ändern.
    public string TempPassword { get; set; } = "";

    // Welche Rolle bekommt der neue Mitarbeiter?
    // Standard = "Mitarbeiter" (niedrigste Berechtigung)
    public string Role { get; set; } = "Mitarbeiter";
}
