// ============================================================
//  Abwesenheit.cs  (Datenmodell: Abwesenheitsantrag)
// ============================================================
//
//  Beschreibt die SQL-Tabelle "tblAbwesenheit".
//  Ein Abwesenheitsantrag = wenn ein Mitarbeiter Urlaub,
//  Krankenstand oder andere Abwesenheit beantragt.
//
//  Status-Ablauf:
//   1. Mitarbeiter stellt Antrag  → Status = "Pending"  (ausstehend)
//   2. Vorgesetzter genehmigt     → Status = "Approved" (genehmigt)
//      ODER Vorgesetzter lehnt ab → Status = "Rejected" (abgelehnt)
//
//  Typen (AbwesenType):
//   - "Urlaub"         → Urlaubstage werden vom Konto abgezogen
//   - "Krankenstand"   → keine Abzüge, ärztliches Attest nötig
//   - "PrivateGruende" → sonstige persönliche Gründe
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models
{
    [Table("tblAbwesenheit")]
    public class Abwesenheit
    {
        // Eindeutige ID des Antrags
        [Key]
        public int AbwesenID { get; set; }

        // Welcher Mitarbeiter hat den Antrag gestellt?
        public int FK_MitID { get; set; }

        // Art der Abwesenheit: "Urlaub", "Krankenstand", "PrivateGruende"
        [Required]
        [MaxLength(100)]
        public string AbwesenType { get; set; } = string.Empty;

        // Von wann bis wann ist der Mitarbeiter abwesend?
        // DateOnly = nur Datum (kein Uhrzeit-Anteil) → z.B. 2026-06-01
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        // Optionale Begründung vom Mitarbeiter
        [MaxLength(500)]
        public string? AbwesenGrund { get; set; }

        // Aktueller Status des Antrags
        // Standard = "Pending" (neu gestellt, noch nicht entschieden)
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Wer hat genehmigt/abgelehnt? (MitarbeiterID des Vorgesetzten)
        // null = noch nicht entschieden
        public int? ApprovedBy { get; set; }

        // Wann wurde entschieden?
        public DateTime? ApprovedAt { get; set; }

        // Wann wurde der Antrag erstellt?
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property: Mitarbeiterdaten werden via FK_MitID geladen
        [ForeignKey("FK_MitID")]
        public Mitarbeiter? Mitarbeiter { get; set; }
    }
}
