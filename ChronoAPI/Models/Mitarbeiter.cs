// ============================================================
//  Mitarbeiter.cs  (Datenmodell: Mitarbeiter)
// ============================================================
//
//  Beschreibt die SQL-Tabelle "tblMitarbeiter".
//  Ein Mitarbeiter ist eine Person die am Flughafen arbeitet.
//
//  WICHTIG: Mitarbeiter ≠ User!
//   - Mitarbeiter = persönliche Daten (Name, E-Mail, Abteilung, NFC-Karte)
//   - User        = Login-Daten (Benutzername, Passwort, Rolle)
//   - Ein Mitarbeiter hat genau einen User (1:1-Beziehung)
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models
{
    [Table("tblMitarbeiter")]
    public class Mitarbeiter
    {
        // Eindeutige ID des Mitarbeiters (automatisch generiert)
        [Key]
        public int MitID { get; set; }

        // Pflichtfelder: Dürfen nicht leer sein
        [Required]
        [MaxLength(50)]
        public string MitVorname { get; set; } = string.Empty;  // z.B. "Anna"

        [Required]
        [MaxLength(50)]
        public string MitNachname { get; set; } = string.Empty; // z.B. "Müller"

        // [EmailAddress] prüft, dass das Format eine gültige E-Mail ist
        [Required]
        [MaxLength(200)]
        [EmailAddress]
        public string MitMail { get; set; } = string.Empty;     // z.B. "a.mueller@innsbruck-airport.com"

        // Optionale Felder (? = darf null sein)
        [MaxLength(50)]
        public string? MitTelefon { get; set; }  // z.B. "+43 512 225 0"

        // Fremdschlüssel zur tblAbteilung.
        // FK_ = "Foreign Key" (Fremdschlüssel) – verweist auf eine andere Tabelle.
        // ? = ein Mitarbeiter kann auch ohne Abteilung existieren (z.B. beim Anlegen).
        public int? FK_AbtID { get; set; }

        // Die eindeutige ID der NFC-Karte des Mitarbeiters.
        // Damit checkt er an der NFC-Station ein und aus.
        // Null = noch keine Karte zugewiesen.
        [MaxLength(50)]
        public string? NFCCardUid { get; set; }

        // DateOnly = nur Datum ohne Uhrzeit (z.B. 2022-03-15)
        // Das ist das Eintrittsdatum des Mitarbeiters.
        public DateOnly MitBeiDat { get; set; }

        // Ist der Mitarbeiter noch aktiv?
        // false = gesperrt/ausgetreten (wird nicht gelöscht, bleibt in der DB)
        public bool MitAktiv { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Rolle des zugehoerigen Login-Accounts – nur zur Anzeige.
        // [NotMapped] = kein eigenes Spalten-Feld, wird von den Controllern
        // gezielt befuellt (ohne den kompletten User inkl. Passwort-Hash zu laden).
        [NotMapped]
        public string? UserRolle { get; set; }

        // ── Navigation Properties ─────────────────────────────
        // Diese Properties werden von EF Core automatisch gefüllt,
        // wenn man .Include() beim Abfragen verwendet.
        // Sie sind NICHT direkt in der DB als Spalte vorhanden!

        // [ForeignKey("FK_AbtID")] sagt EF Core:
        // "Verknüpfe Abteilung über die Spalte FK_AbtID"
        [ForeignKey("FK_AbtID")]
        public Abteilung? Abteilung { get; set; }  // Die Abteilung des Mitarbeiters

        // Alle Zeitbuchungen dieses Mitarbeiters (Check-in/Check-out)
        public ICollection<TimeBooking> TimeBookings { get; set; } = new List<TimeBooking>();

        // Login-Account des Mitarbeiters (Benutzername, Passwort, Rolle)
        public User? User { get; set; }
    }
}
