// ============================================================
//  AppDbContext.cs  (Datenbankverbindung)
// ============================================================
//
//  DbContext = die "Brücke" zwischen C#-Code und SQL-Datenbank.
//  Entity Framework Core (EF Core) übersetzt unsere C#-Klassen
//  automatisch in SQL-Befehle:
//
//    C#-Code:   _context.Mitarbeiter.ToListAsync()
//    SQL:       SELECT * FROM tblMitarbeiter
//
//  Wir müssen kein SQL schreiben – EF Core macht das für uns!
//
//  Jedes "DbSet<XYZ>" entspricht einer Tabelle in der Datenbank.
//  Durch "Include()" kann man verknüpfte Tabellen mitnehmen (JOIN).
// ============================================================

using ChronoAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChronoAPI.Data
{
    // AppDbContext erbt von DbContext (von EF Core vorgegeben)
    public class AppDbContext : DbContext
    {
        // Konstruktor: Bekommt die Datenbankverbindungs-Optionen
        // (Serveradresse, Datenbankname, Passwort) aus Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ── TABELLEN (DbSets) ─────────────────────────────────
        // Jede Zeile hier = eine Tabelle in der SQL-Datenbank.
        // DbSet<Abteilung> = Liste aller Abteilungen aus tblAbteilung.
        // Man kann diese wie C#-Listen verwenden:
        //   _context.Abteilungen.Add(...)   → INSERT
        //   _context.Abteilungen.Find(id)   → SELECT WHERE id=...
        //   _context.Abteilungen.Remove(a)  → DELETE

        public DbSet<Abteilung>     Abteilungen    { get; set; }  // tblAbteilung
        public DbSet<Mitarbeiter>   Mitarbeiter    { get; set; }  // tblMitarbeiter
        public DbSet<User>          Users          { get; set; }  // tblUsers
        public DbSet<TimeBooking>   TimeBookings   { get; set; }  // tblTimeBookings
        public DbSet<Abwesenheit>   Abwesenheiten  { get; set; }  // tblAbwesenheit
        public DbSet<AbwesenGen>    AbwesenGen     { get; set; }  // tblAbwesenGen
        public DbSet<LohnAbrechnung> LohnAbrechnungen { get; set; } // tblLohnAbrechnung
        public DbSet<AuditLog>      AuditLogs      { get; set; }  // tblAuditLog
        public DbSet<Zeitkorrektur> Zeitkorrekturen { get; set; } // tblZeitkorrektur
        public DbSet<Urlaubskonto>  Urlaubskonten  { get; set; }  // tblUrlaubskonto

        // ── BEZIEHUNGSREGELN ──────────────────────────────────
        // OnModelCreating wird einmal beim Start aufgerufen und
        // konfiguriert spezielle Beziehungen zwischen Tabellen.
        //
        // PROBLEM: Zeitkorrektur hat ZWEI Fremdschlüssel auf Mitarbeiter:
        //   FK_MitID   → wer hat den Antrag gestellt?
        //   ApprovedBy → wer hat ihn genehmigt?
        //
        // SQL Server erlaubt das nicht einfach so (Fehler: "multiple cascade paths").
        // Deshalb müssen wir manuell sagen, was beim Löschen eines Mitarbeiters
        // mit seinen Zeitkorrekturen passieren soll.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Zeitkorrektur>(entity =>
            {
                // Wenn der Antragsteller (FK_MitID) gelöscht wird:
                // → Restrict = KEIN automatisches Löschen der Zeitkorrektur.
                //   (Zuerst muss die Zeitkorrektur manuell gelöscht werden.)
                entity.HasOne(z => z.Mitarbeiter)
                      .WithMany()
                      .HasForeignKey(z => z.FK_MitID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Wenn der Genehmiger (ApprovedBy) gelöscht wird:
                // → SetNull = ApprovedBy wird auf NULL gesetzt.
                //   (Die Zeitkorrektur bleibt erhalten, aber ohne Genehmiger.)
                entity.HasOne(z => z.Genehmiger)
                      .WithMany()
                      .HasForeignKey(z => z.ApprovedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Urlaubskonto>(entity =>
            {
                // Urlaubskonto-Eintrag bleibt bestehen, auch wenn Mitarbeiter gelöscht wird
                entity.HasOne(u => u.Mitarbeiter)
                      .WithMany()
                      .HasForeignKey(u => u.FK_MitID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
