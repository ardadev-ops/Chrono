// ============================================================
//  Zeitkorrektur.cs  (Datenmodell: Zeitkorrekturantrag)
// ============================================================
//
//  Beschreibt die SQL-Tabelle "tblZeitkorrektur".
//
//  Was ist eine Zeitkorrektur?
//  Wenn ein Mitarbeiter vergisst seine NFC-Karte zu benutzen
//  (z.B. Karte zuhause vergessen), kann er manuell eintragen
//  wann er da war. Der Vorgesetzte muss das dann genehmigen.
//
//  Bei Genehmigung wird AUTOMATISCH eine echte TimeBooking
//  in tblTimeBookings erstellt – als wäre er normal gestempelt.
//
//  Status-Ablauf:
//   "Pending" → "Approved" (eine TimeBooking wird erstellt)
//           OR → "Rejected"
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models;

[Table("tblZeitkorrektur")]
public class Zeitkorrektur
{
    // Eindeutige ID des Antrags
    [Key]
    public int KorrID { get; set; }

    // Welcher Mitarbeiter hat den Antrag gestellt?
    public int FK_MitID { get; set; }

    // Für welches Datum soll die Korrektur eingetragen werden?
    // DateOnly = nur Datum ohne Uhrzeit (z.B. 2026-05-06)
    [Required]
    public DateOnly KorrDatum { get; set; }

    // Wann war der Mitarbeiter eingestempelt? (nur Uhrzeit, kein Datum)
    // TimeOnly = nur Uhrzeit (z.B. 08:15)
    [Required]
    public TimeOnly CheckIn { get; set; }

    // Wann war der Mitarbeiter ausgestempelt? (nur Uhrzeit)
    [Required]
    public TimeOnly CheckOut { get; set; }

    // Warum braucht der Mitarbeiter eine Korrektur?
    // z.B. "NFC-Karte vergessen", "Lesegerät defekt"
    [Required]
    [MaxLength(500)]
    public string Grund { get; set; } = "";

    // Aktueller Status: "Pending", "Approved" oder "Rejected"
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    // MitarbeiterID des Genehmigers (wer hat genehmigt/abgelehnt?)
    // null = noch nicht entschieden
    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }  // Wann wurde entschieden?
    public DateTime? CreatedAt { get; set; }    // Wann wurde der Antrag erstellt?

    // ── Navigation Properties ─────────────────────────────────
    // Zwei Fremdschlüssel auf dieselbe Tabelle (Mitarbeiter)!
    // Das ist der Grund für OnModelCreating in AppDbContext.cs –
    // SQL Server kann nicht automatisch entscheiden, was beim Löschen
    // eines Mitarbeiters passieren soll, wenn er zwei Rollen hat.

    // Wer hat den Antrag gestellt?
    [ForeignKey("FK_MitID")]
    public Mitarbeiter? Mitarbeiter { get; set; }

    // Wer hat genehmigt? (anderer Mitarbeiter als der Antragsteller)
    [ForeignKey("ApprovedBy")]
    public Mitarbeiter? Genehmiger { get; set; }
}
