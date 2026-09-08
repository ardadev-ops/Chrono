// ============================================================
//  User.cs  (Datenmodell: Login-Account)
// ============================================================
//
//  Beschreibt die SQL-Tabelle "tblUsers".
//  Ein User ist der LOGIN-ACCOUNT eines Mitarbeiters.
//
//  WICHTIG: Mitarbeiter ≠ User!
//   - Mitarbeiter = Name, E-Mail, Abteilung (wer ist die Person?)
//   - User        = Benutzername, Passwort, Rolle (wie loggt sie sich ein?)
//   - Jeder User gehört genau zu einem Mitarbeiter (FK_MitID)
//
//  Rollen in CHRONO:
//   - "Mitarbeiter"    → nur eigene Zeiten sehen
//   - "Abteilungsleiter" → Zeiten seiner Abteilung sehen, genehmigen
//   - "HR"             → alle Mitarbeiter verwalten, genehmigen
//   - "Buchhaltung"    → Lohnabrechnung und Zeitexport
//   - "Admin"          → alles
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models
{
    [Table("tblUsers")]
    public class User
    {
        // Eindeutige ID des Users
        [Key]
        public int UserID { get; set; }

        // Fremdschlüssel: Welchem Mitarbeiter gehört dieser Account?
        // Über FK_MitID ist User mit Mitarbeiter verbunden.
        public int FK_MitID { get; set; }

        // Benutzername für den Login (z.B. "anna.mueller")
        // Wird automatisch generiert: vorname.nachname
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        // GEHASHTES Passwort (nie im Klartext speichern!)
        // BCrypt erzeugt einen Hash wie: "$2b$12$abc123..."
        // MaxLength(500) weil BCrypt-Hashes etwa 60 Zeichen lang sind,
        // aber wir 500 reservieren für alle Fälle.
        [Required]
        [MaxLength(500)]
        public string UserPassword { get; set; } = string.Empty;

        // Die Rolle bestimmt, was der User darf
        // Mögliche Werte: "Mitarbeiter", "Abteilungsleiter", "HR", "Buchhaltung", "Admin"
        [Required]
        [MaxLength(50)]
        public string UserRole { get; set; } = string.Empty;

        // Wann hat sich der User zuletzt eingeloggt? (für Übersicht)
        public DateTime? LastLogin { get; set; }

        // Ist der Account aktiv?
        // false = gesperrt (z.B. Mitarbeiter ausgetreten)
        public bool UserAktiv { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ── PASSWORT-SICHERHEIT ───────────────────────────────

        // Muss das Passwort beim nächsten Login geändert werden?
        // true = User hat noch das temporäre Passwort von HR
        // false = User hat sein eigenes Passwort gesetzt
        public bool MustChangePassword { get; set; } = true;

        public DateTime? PasswordChangedAt { get; set; }   // Wann wurde Passwort geändert?
        public int? PasswordResetBy { get; set; }           // Wer hat es zurückgesetzt? (UserID von HR/Admin)
        public DateTime? PasswordResetAt { get; set; }      // Wann wurde es zurückgesetzt?

        // ── ACCOUNT-SPERRUNG (Brute-Force-Schutz) ────────────
        // Wenn jemand zu oft das falsche Passwort eingibt, wird der Account gesperrt.
        // Verhindert automatisierte "Rate me" Angriffe (Brute Force).

        // Zählt wie oft das falsche Passwort eingegeben wurde.
        // Nach 5 Fehlversuchen → Account gesperrt für 30 Minuten.
        public int FailedLoginAttempts { get; set; } = 0;

        // Bis wann ist der Account gesperrt?
        // null = nicht gesperrt
        // Datum = gesperrt bis zu diesem Zeitpunkt
        public DateTime? LockedUntil { get; set; }

        // ── NAVIGATION PROPERTY ───────────────────────────────
        // EF Core füllt das automatisch bei .Include(u => u.Mitarbeiter)
        [ForeignKey("FK_MitID")]
        public Mitarbeiter? Mitarbeiter { get; set; }
    }
}
