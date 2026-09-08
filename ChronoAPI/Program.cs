// ============================================================
//  ChronoAPI – Program.cs  (Einstiegspunkt der API)
// ============================================================
//
//  Das hier ist die STARTKONFIGURATION der API.
//  Alles was die API zum Laufen braucht wird hier eingestellt:
//    – Datenbankverbindung
//    – Sicherheit (JWT-Token / Passwortschutz)
//    – CORS (welche Webseiten dürfen die API aufrufen?)
//    – Swagger (eine automatische Doku/Testseite für die API)
//    – Hintergrunddienste (z.B. Auto-Checkout)
//
//  Merke: In .NET heißt dieses Muster "Builder Pattern".
//  Zuerst alles KONFIGURIEREN (builder.Services.Add...),
//  danach erst STARTEN (app.Use... / app.Run()).
// ============================================================

using ChronoAPI.Data;
using ChronoAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;

// WebApplication.CreateBuilder liest die appsettings.json ein
// und bereitet die App vor (aber startet sie noch nicht).
var builder = WebApplication.CreateBuilder(args);

// ── 1. DATENBANK ──────────────────────────────────────────────
// Hier verbinden wir uns mit der SQL-Datenbank.
// Die Verbindungszeichenfolge (Server, Datenbankname, Passwort)
// steht in appsettings.json unter "ConnectionStrings:DefaultConnection".
// AppDbContext ist unsere "Brücke" zwischen C#-Klassen und der DB.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. CORS ───────────────────────────────────────────────────
// CORS = Cross-Origin Resource Sharing
// Browser blockieren normalerweise Anfragen von einer anderen Domain.
// Beispiel: ChronoWeb läuft auf localhost:5000,
//           ChronoAPI läuft auf localhost:7127.
// Damit das Web-Frontend die API aufrufen darf, erlauben wir hier
// ALLE Herkunftsadressen (AllowAnyOrigin) – nur für Entwicklung ok!
// In echten Produktionssystemen würde man hier nur die eigene Domain erlauben.
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── 3. JWT-AUTHENTIFIZIERUNG ──────────────────────────────────
// JWT = JSON Web Token. Das ist ein verschlüsselter "Ausweis",
// den der Benutzer beim Login bekommt und bei jeder Anfrage mitschickt.
// Die API prüft diesen Ausweis und weiß so, wer der Benutzer ist.
//
// Aufbau eines JWT:
//   Header.Payload.Signatur
//   z.B.: eyJhbGc... (base64 kodiert)
//
// In appsettings.json stehen:
//   SecretKey  – geheimer Schlüssel zum Signieren des Tokens
//   Issuer     – wer hat das Token ausgestellt? (unsere API)
//   Audience   – für wen ist das Token? (unser Frontend)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    // Standard-Schema für Authentifizierung setzen
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // MapInboundClaims = false: Claim-Namen NICHT automatisch umbenennen.
    // Ohne das würde ASP.NET z.B. "role" in einen langen Microsoft-Namen umwandeln,
    // was [Authorize(Roles="...")] kaputt machen würde.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,           // Prüfe: Stimmt der Aussteller?
        ValidateAudience = true,         // Prüfe: Stimmt der Empfänger?
        ValidateLifetime = true,         // Prüfe: Ist das Token noch gültig?
        ValidateIssuerSigningKey = true, // Prüfe: Stimmt die Signatur?
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)), // Geheimschlüssel als Bytes

        // Diese zwei Zeilen sind wichtig!
        // Sie sagen: Das "role"-Feld im Token entspricht der Rolle,
        // und "unique_name" entspricht dem Benutzernamen.
        // Damit funktioniert [Authorize(Roles="HR")] usw. korrekt.
        RoleClaimType = "role",
        NameClaimType = "unique_name"
    };
});

// Autorisierung aktivieren (prüft ob Benutzer die nötige Rolle hat)
builder.Services.AddAuthorization();

// ── 4. HINTERGRUNDDIENST ──────────────────────────────────────
// AutoCheckoutService läuft im Hintergrund und checkt Mitarbeiter
// automatisch aus, falls sie es vergessen haben (nach 10 Stunden).
// BackgroundService = läuft dauerhaft, unabhängig von Anfragen.
builder.Services.AddHostedService<AutoCheckoutService>();

// NfcScanStore: haelt sich die zuletzt gescannte Karten-UID (In-Memory,
// fuer den "Karte scannen"-Button im Mitarbeiter-Formular).
builder.Services.AddSingleton<NfcScanStore>();

// ── 5. CONTROLLER + JSON-KREISREFERENZ-FIX ───────────────────
// Controller = Klassen die HTTP-Anfragen bearbeiten (unsere API-Endpunkte).
// ReferenceHandler.IgnoreCycles:
//   Wenn Mitarbeiter → TimeBookings → Mitarbeiter → ... (Kreis!) enthält,
//   würde das JSON unendlich groß werden. IgnoreCycles verhindert das.
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// ── 6. SWAGGER ───────────────────────────────────────────────
// Swagger erstellt automatisch eine Webseite unter /swagger
// wo man alle API-Endpunkte sehen und direkt testen kann.
// Sehr hilfreich zum Entwickeln und Debuggen!
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChronoAPI", Version = "v1" });

    // Bearer Token Support für Swagger hinzufügen:
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Token eingeben: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null),
            new List<string>()
        }
    });
});
// ── APP ZUSAMMENBAUEN UND STARTEN ────────────────────────────
// Alles ist konfiguriert – jetzt die App erstellen.
var app = builder.Build();

// Swagger-UI immer anzeigen (auch im Produktionsmodus)
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS erzwingen (HTTP-Anfragen → HTTPS umleiten)
app.UseHttpsRedirection();

// CORS-Policy "AllowAll" aktivieren (muss VOR UseAuthentication stehen!)
app.UseCors("AllowAll");

// Authentifizierung aktivieren (JWT-Token prüfen)
app.UseAuthentication();

// Autorisierung aktivieren ([Authorize]-Attribute prüfen)
app.UseAuthorization();

// Alle Controller-Routen registrieren (api/Auth, api/Mitarbeiter, ...)
app.MapControllers();

// App starten und auf Anfragen warten
app.Run();
