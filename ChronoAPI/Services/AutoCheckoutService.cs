// ============================================================
//  AutoCheckoutService.cs  (Hintergrunddienst: Auto-Checkout)
// ============================================================
//
//  Was macht dieser Dienst?
//  Manchmal vergessen Mitarbeiter sich auszustempeln.
//  Dieser Dienst läuft automatisch alle 30 Minuten im Hintergrund
//  und checkt alle Mitarbeiter aus, die schon mehr als 10 Stunden
//  eingestempelt sind ohne ausgecheckt zu haben.
//
//  Beispiel:
//   Mitarbeiter stempelt um 07:00 ein.
//   Um 17:00 (10 Stunden später) → Auto-Checkout auf 17:00
//   CheckOutTime = CheckInTime + 10 Stunden
//
//  WICHTIGES Konzept – BackgroundService:
//   BackgroundService läuft ständig im Hintergrund, auch wenn
//   keine Anfragen kommen. Er wird beim Start der API gestartet
//   und läuft bis die API beendet wird.
//
//  WICHTIGES Konzept – Scoped vs. Singleton:
//   DbContext (Datenbankverbindung) ist "Scoped" – wird pro Anfrage neu erstellt.
//   BackgroundService ist "Singleton" – existiert nur einmal für die ganze Laufzeit.
//   Singleton darf Scoped nicht direkt verwenden! Deshalb brauchen wir
//   IServiceProvider.CreateScope() um einen temporären Bereich zu erstellen.
// ============================================================

using ChronoAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace ChronoAPI.Services;

// BackgroundService ist die Basisklasse für Hintergrunddienste in .NET
public class AutoCheckoutService : BackgroundService
{
    // IServiceProvider erlaubt uns, Services "on demand" zu erstellen
    // (notwendig wegen dem Scoped/Singleton-Problem, siehe oben)
    private readonly IServiceProvider _services;

    // ILogger schreibt Logs in die Konsole/Datei
    // So können wir sehen was der Dienst tut (hilfreich zum Debuggen)
    private readonly ILogger<AutoCheckoutService> _logger;

    // Konstruktor: IServiceProvider und ILogger werden automatisch eingefügt
    public AutoCheckoutService(IServiceProvider services, ILogger<AutoCheckoutService> logger)
    {
        _services = services;
        _logger = logger;
    }

    // ── HAUPTSCHLEIFE ─────────────────────────────────────────
    // ExecuteAsync wird einmal beim Start aufgerufen.
    // Es läuft in einer Endlosschleife bis die App beendet wird.
    //
    // stoppingToken = ein Signal das gesetzt wird wenn die App
    // heruntergefahren wird (dann soll die Schleife enden)
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Checkout Service gestartet.");

        // Wiederhole solange die App läuft (!stoppingToken.IsCancellationRequested)
        while (!stoppingToken.IsCancellationRequested)
        {
            // Auto-Checkout-Prüfung durchführen
            await CheckAndAutoCheckout();

            // 30 Minuten warten, dann wieder prüfen
            // Task.Delay ist wie "Thread.Sleep" aber ohne CPU zu blockieren
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    // ── AUTO-CHECKOUT LOGIK ───────────────────────────────────
    private async Task CheckAndAutoCheckout()
    {
        // Temporären Scope erstellen um DbContext zu benutzen
        // (notwendig wegen Scoped/Singleton-Konflikt)
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Zeitpunkt vor 10 Stunden berechnen
        // Alle Buchungen die VOR diesem Zeitpunkt eingecheckt haben
        // und noch nicht ausgecheckt sind → werden jetzt ausgestempelt
        var zehnStundenZurueck = DateTime.Now.AddHours(-10);

        // Alle offenen Buchungen die älter als 10 Stunden sind suchen
        var offeneBuchungen = await context.TimeBookings
            .Where(t => t.CheckOutTime == null          // noch nicht ausgecheckt
                     && t.CheckInTime <= zehnStundenZurueck) // und älter als 10h
            .ToListAsync();

        // Jede offene Buchung automatisch auschecken
        foreach (var buchung in offeneBuchungen)
        {
            // CheckOut = CheckIn + 10 Stunden (Maximal-Arbeitszeit)
            buchung.CheckOutTime = buchung.CheckInTime.AddHours(10);

            // Markieren dass dies ein Auto-Checkout war (nicht manuell!)
            buchung.IsAutoCheckout = true;

            // Notiz anhängen (falls schon eine Notiz da ist, wird sie ergänzt)
            buchung.Notes = (buchung.Notes ?? "") + " [AUTO-CHECKOUT nach 10h]";
            buchung.UpdatedAt = DateTime.Now;

            // Log-Eintrag schreiben (damit wir sehen wer automatisch ausgestempelt wurde)
            _logger.LogInformation(
                "Auto-Checkout: MitID {MitID}, CheckIn {CheckIn}",
                buchung.FK_MitID, buchung.CheckInTime);
        }

        // Alle Änderungen auf einmal in die DB schreiben
        // (nur wenn es auch welche gibt – sonst unnötiger DB-Aufruf)
        if (offeneBuchungen.Any())
            await context.SaveChangesAsync();
    }
}
