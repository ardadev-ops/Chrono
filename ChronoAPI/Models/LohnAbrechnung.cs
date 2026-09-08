// ============================================================
//  LohnAbrechnung.cs  (Datenmodell: Lohnabrechnung)
// ============================================================
//
//  Beschreibt die SQL-Tabelle "tblLohnAbrechnung".
//
//  Eine Lohnabrechnung fasst die Arbeitsstunden und den
//  berechneten Lohn für einen bestimmten Zeitraum zusammen.
//  Wird von Buchhaltung/Admin erstellt.
//
//  Typischer Zeitraum: Monatsabrechnung
//   z.B. PeriodStart = 2026-05-01, PeriodEnd = 2026-05-31
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models
{
    [Table("tblLohnAbrechnung")]
    public class LohnAbrechnung
    {
        // Eindeutige ID der Abrechnung
        [Key]
        public int LohnID { get; set; }

        // Für welchen Mitarbeiter?
        public int FK_MitID { get; set; }

        // Abrechnungszeitraum (Von – Bis)
        public DateOnly PeriodStart { get; set; }  // z.B. 2026-05-01
        public DateOnly PeriodEnd   { get; set; }  // z.B. 2026-05-31

        // [Column(TypeName = "decimal(8,2)")] sagt SQL:
        // Speichere diese Zahl mit max. 8 Stellen, davon 2 nach dem Komma.
        // z.B. 123456.78

        // Reguläre Arbeitsstunden (ohne Überstunden)
        [Column(TypeName = "decimal(8,2)")]
        public decimal RegularStunden { get; set; }

        // Überstunden (Stunden über die Sollarbeitszeit)
        [Column(TypeName = "decimal(8,2)")]
        public decimal Ueberstunden { get; set; } = 0;

        // Gesamtstunden = RegularStunden + Ueberstunden
        [Column(TypeName = "decimal(8,2)")]
        public decimal GesamtStunden { get; set; }

        // Bruttolohn = Lohn vor Steuern und Abgaben (in Euro)
        [Column(TypeName = "decimal(10,2)")]
        public decimal BruttoLohn { get; set; }

        // Nettolohn = was der Mitarbeiter tatsächlich bekommt (nach Abzügen)
        [Column(TypeName = "decimal(10,2)")]
        public decimal NettoLohn { get; set; }

        // Wann wurde die Abrechnung erstellt?
        public DateTime ProcessedAt { get; set; } = DateTime.Now;

        // Wer hat die Abrechnung erstellt? (UserID des Buchhaltungs-Users)
        public int? ProcessedBy { get; set; }

        // Navigation Property: Mitarbeiterdaten
        [ForeignKey("FK_MitID")]
        public Mitarbeiter? Mitarbeiter { get; set; }
    }
}
