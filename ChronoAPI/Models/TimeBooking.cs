// ============================================================
//  TimeBooking.cs  (Datenmodell: Zeitbuchung)
// ============================================================
//
//  Beschreibt die SQL-Tabelle "tblTimeBookings".
//  Eine Zeitbuchung = ein Check-in/Check-out-Paar eines Mitarbeiters.
//
//  Ablauf:
//   1. Mitarbeiter hält NFC-Karte an → CheckInTime wird gesetzt
//   2. Mitarbeiter hält NFC-Karte nochmal an → CheckOutTime wird gesetzt
//   3. Die Arbeitszeit = CheckOutTime - CheckInTime - BreakMinutes
//
//  Beispiel:
//   CheckInTime  = 08:00
//   CheckOutTime = 16:30
//   BreakMinutes = 30
//   Arbeitszeit  = 8h 30min - 30min = 8h
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models
{
    [Table("tblTimeBookings")]
    public class TimeBooking
    {
        // Eindeutige ID der Buchung
        [Key]
        public int TimeID { get; set; }

        // Welchem Mitarbeiter gehört diese Buchung?
        public int FK_MitID { get; set; }

        // Wann hat sich der Mitarbeiter eingestempelt?
        // [Required] = muss immer vorhanden sein (ohne Check-in keine Buchung)
        [Required]
        public DateTime CheckInTime { get; set; }

        // Wann hat sich der Mitarbeiter ausgestempelt?
        // ? (nullable) = kann null sein, wenn noch nicht ausgecheckt
        public DateTime? CheckOutTime { get; set; }

        // Pausenzeit in Minuten (Standard: 0, wird manuell eingetragen)
        public int BreakMinutes { get; set; } = 0;

        // Zusätzliche Notizen (z.B. "Überstunden wegen Flugverspätung")
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Wurde diese Buchung automatisch ausgestempelt?
        // true = der AutoCheckoutService hat nach 10h automatisch ausgecheckt
        //        (weil der Mitarbeiter es vergessen hat)
        // false = normaler manueller/NFC-Checkout
        public bool IsAutoCheckout { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }  // Wann zuletzt geändert?

        // Navigation Property: EF Core lädt den zugehörigen Mitarbeiter
        // mit .Include(t => t.Mitarbeiter) automatisch dazu.
        [ForeignKey("FK_MitID")]
        public Mitarbeiter? Mitarbeiter { get; set; }
    }
}
