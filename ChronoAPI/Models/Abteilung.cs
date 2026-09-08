// ============================================================
//  Abteilung.cs  (Datenmodell: Abteilung)
// ============================================================
//
//  Ein "Modell" (auch "Entity" genannt) ist eine C#-Klasse,
//  die eine Tabelle in der Datenbank beschreibt.
//
//  Diese Klasse beschreibt die Tabelle "tblAbteilung".
//  Jedes Property (= Eigenschaft) entspricht einer Spalte.
//
//  Beispiel-Datensatz in der DB:
//    AbtID=1, AbtName="Bodenabfertigung", AbtBez="Gepäck & Check-in"
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models
{
    // [Table("tblAbteilung")] sagt EF Core:
    // "Diese C#-Klasse gehört zur SQL-Tabelle namens tblAbteilung"
    [Table("tblAbteilung")]
    public class Abteilung
    {
        // [Key] markiert diese Eigenschaft als PRIMARY KEY in der DB.
        // Das ist eine eindeutige Nummer, die jede Abteilung identifiziert.
        // SQL Server generiert diese automatisch (AUTO INCREMENT).
        [Key]
        public int AbtID { get; set; }

        // [Required] = NOT NULL in der Datenbank → muss immer ausgefüllt sein
        // [MaxLength(100)] = VARCHAR(100) in SQL → maximal 100 Zeichen
        [Required]
        [MaxLength(100)]
        public string AbtName { get; set; } = string.Empty;  // z.B. "Vertrieb"

        // ? nach string bedeutet: nullable = darf leer/null sein
        // [MaxLength(500)] = darf bis zu 500 Zeichen lang sein
        [MaxLength(500)]
        public string? AbtBez { get; set; }  // Kurze Beschreibung der Abteilung (optional)

        // Wann wurde dieser Eintrag erstellt?
        // DateTime.Now = aktuelles Datum+Uhrzeit beim Erstellen
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Wann wurde zuletzt geändert? null = noch nie geändert
        public DateTime? UpdatedAt { get; set; }

        // Navigation Property: Liste aller Mitarbeiter in dieser Abteilung.
        // EF Core füllt das automatisch wenn man .Include(a => a.Mitarbeiter) verwendet.
        // In der DB gibt es keine echte Spalte "Mitarbeiter" – das ist nur C#!
        // Es wird über FK_AbtID in tblMitarbeiter verbunden.
        public ICollection<Mitarbeiter> Mitarbeiter { get; set; } = new List<Mitarbeiter>();
    }
}
