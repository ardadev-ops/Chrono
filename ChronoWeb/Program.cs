// ============================================================
//  ChronoWeb/Program.cs  (Einstiegspunkt des Web-Frontends)
// ============================================================
//
//  Das hier startet die Blazor-Web-App (die Benutzeroberfläche).
//  Blazor Server = der C#-Code läuft auf dem Server,
//  der Browser bekommt nur HTML/CSS und eine SignalR-Verbindung
//  (eine dauerhafte Verbindung für Echtzeit-Updates).
//
//  Vergleich:
//    ChronoAPI  = das "Gehirn" (Datenbank, Logik, Regeln)
//    ChronoWeb  = das "Gesicht" (was der User sieht und klickt)
//    ApiService = die "Brücke" zwischen beiden
//
//  Wichtige Konzepte:
//   - "Scoped" Service: Eine neue Instanz pro Browser-Tab/Session
//   - "Singleton" Service: Eine einzige Instanz für alle User (selten)
//   - LocalStorage: Im Browser gespeicherte Daten (z.B. der Login-Token)
// ============================================================

using Blazored.LocalStorage;
using ChronoWeb.Components;
using ChronoWeb.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ── BLAZOR SERVER AKTIVIEREN ──────────────────────────────────
// AddRazorComponents = Razor-Komponenten (.razor-Dateien) aktivieren
// AddInteractiveServerComponents = C#-Code läuft auf dem Server,
// Benutzeraktionen (Klicks etc.) werden via SignalR übertragen
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── LOCALSTORAGE ──────────────────────────────────────────────
// Blazored.LocalStorage erlaubt uns, Daten im Browser zu speichern.
// Wir speichern dort den JWT-Token damit der User nach dem
// Browser-Neustart noch eingeloggt bleibt.
// Vergleich: wie Cookies, aber einfacher zu verwenden in Blazor.
builder.Services.AddBlazoredLocalStorage();

// ── SERVICES REGISTRIEREN ─────────────────────────────────────
// AddScoped = jede Browser-Session (jeder Tab) bekommt eine eigene Instanz.
// Das ist wichtig damit zwei verschiedene User nicht dieselben Daten sehen!

// ApiService: kümmert sich um alle HTTP-Anfragen an ChronoAPI
// (Login, Mitarbeiter laden, Zeitbuchungen abrufen, ...)
builder.Services.AddScoped<ApiService>();

// AppState: speichert den Login-Zustand des aktuellen Users
// (ist er eingeloggt? Welche Rolle hat er? Wie heißt er?)
builder.Services.AddScoped<AppState>();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"C:\inetpub\keys"))
        .SetApplicationName("ChronoWeb");
}

var app = builder.Build();

// ── MIDDLEWARE-PIPELINE ───────────────────────────────────────
// Middleware = Code der bei JEDER Anfrage ausgeführt wird, in dieser Reihenfolge.

// Im Produktionsmodus: Fehlerseite anzeigen statt technischer Details
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); // HTTPS erzwingen für Sicherheit
}

//app.UseHttpsRedirection();  // HTTP → HTTPS umleiten
app.UseStaticFiles();        // Statische Dateien ausliefern (CSS, JS, Bilder)
app.UseAntiforgery();        // CSRF-Schutz (verhindert gefälschte Formular-Angriffe)

// Alle .razor-Seiten zugänglich machen
// App = die Root-Komponente (Components/App.razor)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// App starten (blockiert bis die App beendet wird)
app.Run();
