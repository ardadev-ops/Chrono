// ============================================================
//  Urlaubskonto.cs  (Datenmodell: Urlaubskonto)
// ============================================================
//
//  Beschreibt die SQL-Tabelle "tblUrlaubskonto".
//
//  Jeder Mitarbeiter hat pro Jahr ein Urlaubskonto.
//  Es speichert wie viele Urlaubstage er hat und wie viele
//  er schon genommen hat.
//
//  Beispiel:
//   GesamtTage = 25  (hat Anspruch auf 25 Tage Urlaub dieses Jahr)
//   GenommTage = 10  (hat bereits 10 Tage genommen)
//   RestTage   = 15  (kann noch 15 Tage nehmen) ← berechnet, nicht in DB!
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models;

[Table("tblUrlaubskonto")]
public class Urlaubskonto
{
    // Eindeutige ID des Kontos
    [Key]
    public int KontoID { get; set; }

    // Welchem Mitarbeiter gehört dieses Konto?
    public int FK_MitID { get; set; }

    // Für welches Jahr gilt dieses Konto? (z.B. 2026)
    // Pro Mitarbeiter gibt es für jedes Jahr einen eigenen Eintrag.
    [Required]
    public int Jahr { get; set; }

    // Wie viele Urlaubstage stehen dem Mitarbeiter insgesamt zu?
    // Standard = 25 (gesetzlicher Mindesturlaub in Österreich)
    public int GesamtTage { get; set; } = 25;

    // Wie viele Tage hat der Mitarbeiter bereits genommen?
    // decimal statt int weil halbe Tage möglich sind (z.B. 0,5 Tage)
    public decimal GenommTage { get; set; } = 0;

    public DateTime? CreatedAt { get; set; }   // Wann wurde das Konto angelegt?
    public DateTime? UpdatedAt { get; set; }   // Wann zuletzt aktualisiert?

    // [NotMapped] bedeutet: Diese Eigenschaft wird NICHT in der Datenbank
    // gespeichert. Sie wird stattdessen jedes Mal neu berechnet.
    // => = "Pfeilausdruck" (Expression Body) – kurze Schreibweise für:
    //    get { return GesamtTage - GenommTage; }
    [NotMapped]
    public decimal RestTage => GesamtTage - GenommTage;

    // Navigation Property: Mitarbeiterdaten
    [ForeignKey("FK_MitID")]
    public Mitarbeiter? Mitarbeiter { get; set; }
}
