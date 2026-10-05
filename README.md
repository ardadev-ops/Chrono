# CHRONO – NFC-basiertes Zeiterfassungssystem

**Auftraggeber:** Flughafen Innsbruck (simuliert – Schulprojekt)  
**Student:** Arda | WIFI Fachakademie, 3. und 4. Semester  
**Zeitraum:** März 2026 – September 2026 (160 Stunden)  
**Zuletzt aktualisiert:** 07.05.2026 (Session 3)

---

## Inhaltsverzeichnis

1. [Projektidee](#1-projektidee)
2. [Systemarchitektur](#2-systemarchitektur)
3. [Infrastruktur (VMs)](#3-infrastruktur-vms)
4. [Datenbank](#4-datenbank)
5. [Backend API (ChronoAPI)](#5-backend-api-chronoapi)
6. [Frontend (ChronoWeb)](#6-frontend-chronoweb)
7. [JWT Authentication – Erklärung](#7-jwt-authentication--erklärung)
8. [Rollen & Zugriffsrechte](#8-rollen--zugriffsrechte)
9. [Bekannte Bugs & Fixes](#9-bekannte-bugs--fixes)
10. [Aktueller Stand](#10-aktueller-stand)
11. [Lokales Setup](#11-lokales-setup)

---

## 1. Projektidee

Der Flughafen Innsbruck erfasst Arbeitszeiten bisher analog (Papierlisten, Excel). CHRONO ersetzt das durch:

1. **NFC-Terminal** (Raspberry Pi 4 + PN532): Mitarbeiter stempelt mit Karte ein/aus
2. **REST API** (.NET 10): Backend-Logik und Datenpersistenz
3. **Blazor Server Web-App**: Rollenbasierte Portale
4. **SQL Server 2022**: Datenhaltung

> *"Jeder Mitarbeiter beginnt seinen Tag mit einem NFC-Scan – der Rest läuft automatisch."*

---

## 2. Systemarchitektur

```
[NFC-Karte + Raspberry Pi]
         |
         | HTTP POST (NFC-UID)
         v
+-----------------------------+       +-----------------------------+
|      ChronoAPI              |  ←——  |       ChronoWeb             |
|  ASP.NET Core .NET 10       |       |   Blazor Server .NET 10     |
|  https://localhost:7127     |       |  https://localhost:7068     |
|  JWT Bearer Authentication  |       |  ApiService → HTTPS-Calls   |
+-----------------------------+       |  AppState → LocalStorage    |
         |                           +-----------------------------+
         | EF Core / SQL
         v
+-----------------------------+
|    SQL Server 2022 Express  |
|  VM1: 192.168.137.10:1433  |
|  DB: ChronoTimeTracking     |
+-----------------------------+
```

---

## 3. Infrastruktur (VMs)

**Microsoft Hyper-V** auf Windows 11 Pro – 2 virtuelle Maschinen:

```
HOST-LAPTOP (192.168.137.1) → Entwicklung (VS 2022, .NET 10 SDK)
├── VM1: ChronoDB-Server (192.168.137.10)
│   ├── Windows Server 2022 Standard Eval
│   ├── SQL Server 2022 Express (Port 1433, TCP/IP aktiviert)
│   └── sa / ServerAdmin2026! / Mixed Mode
└── VM2: ChronoApp-Server (192.168.137.11)
    ├── Windows Server 2022 Standard Eval
    ├── .NET 10 ASP.NET Core Runtime
    └── IIS 10 (für späteres Deployment)
```

**Connection String:**
```
Server=192.168.137.10,1433;Database=ChronoTimeTracking;
User Id=sa;Password=;TrustServerCertificate=True;Encrypt=False;
```
> ⚠️ WICHTIG: Port `,1433` direkt nach IP angeben – KEIN `\\SQLEXPRESS`!

---

## 4. Datenbank

**8 Tabellen in 3. Normalform (3NF):**

| Tabelle | Inhalt | Wichtig |
|---------|--------|---------|
| tblAbteilung | IT, HR, Vertrieb, Finanzen, Betrieb | |
| tblMitarbeiter | Stammdaten, NFCCardUid | NFCCardUid = Bindeglied zum NFC-Terminal |
| tblUsers | Login-Accounts | UserRole = Mitarbeiter/HR/Abteilungsleiter/Buchhaltung/Admin |
| tblTimeBookings | Check-In/Out Buchungen | CheckOutTime = NULL bedeutet: Mitarbeiter gerade anwesend |
| tblAbwesenheit | Urlaubsanträge | Status: Pending → Approved/Rejected |
| tblAbwesenGen | Archiv genehmigter Abwesenheiten | |
| tblLohnAbrechnung | Monatliche Gehaltsabrechnungen | BruttoLohn, NettoLohn als decimal(10,2) |
| tblAuditLogs | Protokoll aller Systemaktionen | OldValues/NewValues als JSON |

**Test-User:**
| Username | Passwort | Rolle |
|----------|----------|-------|
| max.mustermann | Mitarbeiter123! | Mitarbeiter |
| anna.schmidt | HR123! | HR |
| tom.weber | Manager123! | Abteilungsleiter |
| lisa.meyer | Mitarbeiter123! | Mitarbeiter |
| paul.fischer | Buchhaltung123! | Buchhaltung |

---

## 5. Backend API (ChronoAPI)

**Pfad:** `PROJEKT CHRONO\ChronoAPI\`  
**HTTP:** http://localhost:5088  
**HTTPS:** https://localhost:7127  
**Swagger:** http://localhost:5088/swagger

### Alle Endpoints

```
AUTH
  POST /api/Auth/login           → öffentlich, gibt JWT-Token zurück

ABTEILUNG [Authorize]
  GET    /api/Abteilung          → alle Rollen
  POST   /api/Abteilung          → [HR, Admin]
  PUT    /api/Abteilung/{id}     → [HR, Admin]
  DELETE /api/Abteilung/{id}     → [Admin]

MITARBEITER [Authorize]
  GET    /api/Mitarbeiter        → [HR, Admin, Abteilungsleiter]
  GET    /api/Mitarbeiter/{id}   → alle eingeloggten User
  POST   /api/Mitarbeiter        → [HR, Admin]
  PUT    /api/Mitarbeiter/{id}   → [HR, Admin]
  DELETE /api/Mitarbeiter/{id}   → [Admin]

TIME BOOKINGS [Authorize]
  GET    /api/TimeBooking                     → [HR, Admin, Abteilungsleiter]
  GET    /api/TimeBooking/{id}               → alle
  GET    /api/TimeBooking/mitarbeiter/{mitId} → alle (eigene Buchungen)
  POST   /api/TimeBooking                    → alle
  PUT    /api/TimeBooking/{id}               → alle
  DELETE /api/TimeBooking/{id}               → [Admin]

ABWESENHEIT [Authorize]
  GET    /api/Abwesenheit                  → alle
  GET    /api/Abwesenheit/pending          → [HR, Admin, Abteilungsleiter]
  POST   /api/Abwesenheit                  → alle
  PUT    /api/Abwesenheit/{id}/approve     → [HR, Admin, Abteilungsleiter]
  PUT    /api/Abwesenheit/{id}/reject      → [HR, Admin, Abteilungsleiter]
  DELETE /api/Abwesenheit/{id}             → [Admin]

LOHNABRECHNUNG [Authorize]
  GET    /api/LohnAbrechnung               → [Buchhaltung, Admin]
  POST   /api/LohnAbrechnung               → [Buchhaltung, Admin]
  DELETE /api/LohnAbrechnung/{id}          → [Admin]
```

### NuGet-Pakete

| Paket | Wozu |
|-------|------|
| EF Core SqlServer | SQL Server Treiber, ORM |
| EF Core Design + Tools | `dotnet ef` Befehle (Migrations) |
| Swashbuckle.AspNetCore | Swagger UI |
| Microsoft.AspNetCore.OpenApi | OpenAPI-Typen |
| JwtBearer | Token-Validierung bei eingehenden Requests |
| System.IdentityModel.Tokens.Jwt | Token generieren (im AuthController) |

### JWT in Program.cs (kritische Konfiguration)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "ChronoAPI",
        ValidAudience = "ChronoApp",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        RoleClaimType = "role",        // ← KRITISCH: JWT short name, nicht URI
        NameClaimType = "unique_name"  // ← KRITISCH: JWT short name
    };
});

// Middleware-Reihenfolge (WICHTIG!):
app.UseCors("AllowAll");       // ERST CORS
app.UseHttpsRedirection();     // dann Redirect
app.UseAuthentication();       // ERST Auth
app.UseAuthorization();        // DANN Autorisierung
app.MapControllers();
```

---

## 6. Frontend (ChronoWeb)

**Pfad:** `PROJEKT CHRONO\ChronoWeb\`  
**HTTPS:** https://localhost:7068

### Seiten & Routen

| Seite | Route | Zugriff | Status |
|-------|-------|---------|--------|
| Login.razor | / | Öffentlich | ✅ |
| Dashboard.razor | /dashboard | Eingeloggt | ✅ |
| MeineZeiten.razor | /meine-zeiten | Alle | ✅ |
| Urlaub.razor | /urlaub | Alle | ✅ |
| Antraege.razor | /antraege | CanApprove | ✅ |
| MitarbeiterPage.razor | /mitarbeiter | HR, Admin | ✅ (Modal gefixt) |
| LohnabrechnungPage.razor | /lohn | Admin only | ✅ |
| ZeitExportPage.razor | /zeitexport | Buchhaltung | ✅ |
| TeamZeiten.razor | /team-zeiten | CanApprove | ✅ |

### Services – Aktuelle Architektur (Stand 06.05.2026)

**ApiService.cs** – Alle HTTP-Aufrufe zur ChronoAPI
```csharp
// BaseUrl: HTTPS direkt – kein HTTP→HTTPS Redirect mehr
private const string BaseUrl = "https://localhost:7127/api/";

// AllowAutoRedirect = false: verhindert Header-Loss bei Redirects
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = ...,
    AllowAutoRedirect = false   // ← NEU: Schutz vor Header-Drop
};

// Jede Request baut eigene Authorization-Header (Req()-Helper):
private HttpRequestMessage Req(HttpMethod method, string url)
{
    var r = new HttpRequestMessage(method, url);
    if (!string.IsNullOrEmpty(_token))
        r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    return r;
}
```

**AppState.cs** – JWT Parsing, Login-State, LocalStorage-Persistenz
```csharp
// NEU: Constructor Injection für LocalStorage und ApiService
public AppState(ILocalStorageService localStorage, ApiService api) { ... }

// NEU: Token übersteht Page-Reload (LocalStorage)
public async Task InitializeAsync()   // → aufgerufen in MainLayout.OnAfterRenderAsync
public async Task LoginAsync(string token, string vollName)  // → Login.razor
public async Task LogoutAsync()       // → Dashboard.razor

// JWT wird mit ReadJwtToken() RAW gelesen (kein Claim-Remapping!):
Role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value
MitarbeiterID = int.Parse(jwt.Claims.First(c => c.Type == "MitarbeiterID").Value)

// Rollen-Checks:
IsHR           => Role == "HR"
IsAdmin        => Role == "Admin"
IsBuchhaltung  => Role == "Buchhaltung"
CanApprove     => IsAbteilungsleiter || IsHR || IsAdmin
```

**MainLayout.razor** – Initialisierungsgate (NEU)
```razor
// Zeigt "Lade..." bis AppState.InitializeAsync() fertig ist
// Verhindert Race-Condition: Seite lädt bevor Token aus LocalStorage gelesen wurde
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await AppState.InitializeAsync();
        _initialized = true;
        StateHasChanged();
    }
}
```

**Program.cs** – Dienste
```csharp
builder.Services.AddBlazoredLocalStorage();  // ← NEU
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AppState>();
```

### NuGet-Pakete (ChronoWeb)

| Paket | Wozu |
|-------|------|
| ch1seL.Blazored.LocalStorage.Concurrent | Thread-sicheres LocalStorage für Blazor Server |
| Blazored.LocalStorage | Basis-Paket (automatische Abhängigkeit) |
| System.IdentityModel.Tokens.Jwt | JWT-Token auf Client-Seite parsen |

### Design-System

- **Hintergrund:** `#07091a` (sehr dunkel, Marineblau)
- **Header:** `rgba(13,20,40,0.95)` mit `border-bottom: 1px solid rgba(79,142,247,0.1)`
- **Primärfarbe:** `#4f8ef7` (Blau)
- **Erfolg:** `#00c9a7` (Türkis)
- **Fehler:** `#f87171` (Rot)
- **Karten:** `rgba(13,20,40,0.7)` mit `border: 1px solid rgba(79,142,247,0.1)`
- **Text:** `#e8f0ff` (hell) / `rgba(212,224,255,0.4)` (gedimmt)
- **Animations:** `fadeInUp`, `pulse`, `spin` – überall konsistent

---

## 7. JWT Authentication – Erklärung

### Wie JWT funktioniert

```
1. Login: POST /api/Auth/login { username, password }
   → API prüft DB → generiert Token → gibt Token zurück

2. Token-Inhalt (Payload):
   {
     "nameid": "1",           ← UserID
     "unique_name": "max.mustermann",
     "role": "HR",            ← Rolle als SHORT NAME!
     "MitarbeiterID": "2",    ← Custom Claim
     "VollName": "Max Mustermann"
   }

3. Client sendet Token bei jeder Anfrage:
   Authorization: Bearer eyJhbGci...

4. API prüft automatisch via [Authorize]:
   [Authorize]               → nur: gültiger Token nötig
   [Authorize(Roles="HR")]   → Token UND Rolle "HR" nötig
```

### Kritisches Detail: RoleClaimType

`JwtSecurityTokenHandler.WriteToken()` wandelt `ClaimTypes.Role` (langer URI) automatisch in `"role"` (kurz) um.

In .NET 7+ nutzt JWT Bearer `JsonWebTokenHandler` – der KEIN Remapping macht.
Daher bleibt der Claim-Type `"role"` (kurz), aber `[Authorize(Roles="HR")]` sucht nach dem langen URI.

**Fix:** `RoleClaimType = "role"` in `TokenValidationParameters` setzen → sagt der Middleware: suche nach `"role"`, nicht nach dem URI.

---

## 8. Rollen & Zugriffsrechte

| Rolle | Eigene Zeiten | Urlaub | Anträge | Mitarbeiter | Lohn | ZeitExport | Team-Zeiten |
|-------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Mitarbeiter | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Abteilungsleiter | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |
| HR | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| Buchhaltung | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Admin | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

> Buchhaltung sieht KEINE Gehaltsdaten – nur Zeitbuchungen (ZeitExportPage)

---

## 9. Bekannte Bugs & Fixes

### ✅ BEHOBEN: Duplicate Routes (Build-Fehler)
**Problem:** `UrlaubPage.razor` + `Urlaub.razor` hatten beide `@page "/urlaub"`.  
**Fix:** Redundante Dateien gelöscht (`UrlaubPage.razor`, `AntraegePage.razor`).

---

### ✅ BEHOBEN: JWT RoleClaimType (erste 403-Welle)
**Problem:** `[Authorize(Roles = "HR")]` gab immer 403, obwohl User Rolle HR hatte.  
**Ursache:** JWT speichert Rolle als `"role"` (kurz), Middleware suchte nach langem URI.  
**Fix:** `RoleClaimType = "role"` in `TokenValidationParameters` → `ChronoAPI/Program.cs`.

---

### ✅ BEHOBEN: Stille Exception-Handler (schwarzer Bildschirm)
**Problem:** `catch { }` schluckte alle API-Fehler still → leerer Bildschirm.  
**Fix:** `catch (Exception ex)` mit sichtbarer `loadError`-Anzeige im UI.

---

### ✅ BEHOBEN: Buchhaltung konnte Lohnabrechnung sehen
**Fix:** LohnabrechnungPage auf Admin-only, neue ZeitExportPage für Buchhaltung.

---

### ✅ BEHOBEN: HTTP 401 bei allen API-Calls nach Login (05.05.2026)

**Zwei gleichzeitige Root Causes:**

**Problem A – Blazor Circuit Lifecycle:**
- `AppState` ist Scoped → bei F5 oder Navigation neuer Circuit → Token weg (nur im Memory)
- Token überstand keinen Page-Reload

**Problem B – HTTP→HTTPS Redirect Header-Loss:**
- `ApiService.BaseUrl` war `http://localhost:5088/api/`
- `ChronoAPI` hat `app.UseHttpsRedirection()` aktiv
- `HttpClient` folgte 301 Redirect automatisch → löscht `Authorization`-Header bei Schema-Wechsel
- HTTPS-Request kam ohne Token an → 401

**Gelöste Fixes (06.05.2026):**

| Datei | Änderung |
|-------|----------|
| `Services/AppState.cs` | Komplett neu: `ILocalStorageService` + `ApiService` per DI. `LoginAsync()` speichert Token in LocalStorage. `InitializeAsync()` liest Token bei Reload. `LogoutAsync()` löscht aus LocalStorage. |
| `Services/ApiService.cs` | `AllowAutoRedirect = false` im `HttpClientHandler` – verhindert Header-Drop bei Redirects. |
| `Components/Layout/MainLayout.razor` | Neu: Initialisierungsgate – zeigt "Lade..." bis `AppState.InitializeAsync()` abgeschlossen. `OnAfterRenderAsync` statt `OnInitializedAsync` (LocalStorage nur nach Client-Render verfügbar). |
| `Components/Pages/Login.razor` | `ApiService.SetToken()` + `AppState.Login()` → `await AppState.LoginAsync()`. |
| `Components/Pages/Dashboard.razor` | `void Logout()` → `async Task Logout()` mit `await AppState.LogoutAsync()`. |
| `Program.cs` | `builder.Services.AddBlazoredLocalStorage()` registriert. |
| `Components/_Imports.razor` | `@using ChronoWeb.Services` hinzugefügt (fehlte – MainLayout konnte AppState nicht finden). |

**Ergebnis:** 401 behoben. Token wird persistent gespeichert und bei jedem Reload wiederhergestellt.

---

### ✅ BEHOBEN: HTTP 403 Forbidden bei API-Calls (06.05.2026)

**Ursache:** Drei zusammenhängende Probleme im JWT-Pipeline:
1. `MapInboundClaims` war nicht explizit deaktiviert → ASP.NET remapped Claims automatisch auf lange URIs
2. `ClaimTypes.Role` (langer URI) statt String-Literal `"role"` im AuthController
3. `RoleClaimType = "role"` fehlte in `TokenValidationParameters`

**Fix in `ChronoAPI/Program.cs`:**
```csharp
options.MapInboundClaims = false;   // ← KRITISCH: verhindert URI-Remapping
options.TokenValidationParameters = new TokenValidationParameters {
    RoleClaimType = "role",         // ← suche nach "role", nicht nach URI
    NameClaimType = "unique_name"
};
```

**Fix in `ChronoAPI/Controllers/AuthController.cs`:**
```csharp
new Claim("role", user.UserRole),   // ← String-Literal, NICHT ClaimTypes.Role
```

**Ergebnis:** Alle Seiten (MeineZeiten, Urlaub, Anträge, Mitarbeiter, Lohn, ZeitExport, TeamZeiten) laden korrekt.

---

### ✅ BEHOBEN: Mitarbeiter Modal erscheint nicht (06.05.2026)

**Symptom:** Debug-Bar zeigte `showForm: True`, aber das Modal blieb unsichtbar.

**Root Cause – CSS `animation` + `fill-mode: both`:**
```css
/* ALT – BUGGY: */
.modal {
    animation: fadeInUp 0.3s cubic-bezier(0.16,1,0.3,1) both;
}
@@keyframes fadeInUp {
    from { opacity: 0; transform: translateY(16px); }  /* ← starts invisible! */
    to   { opacity: 1; transform: translateY(0); }
}
```
`fill-mode: both` + `from { opacity: 0 }` → wenn die Animation nicht abspielt (GPU-Problem, `prefers-reduced-motion`, Opera GX Rendering-Bug) bleibt das Element **dauerhaft transparent**.

**Weitere Ursachen:**
- Modal war innerhalb des `.page`-Containers → potenzielle CSS-Stacking-Context-Konflikte
- `backdrop-filter: blur(8px)` → bekannte Rendering-Probleme in bestimmten GPU-Kontexten
- `StateHasChanged()` direkt statt `await InvokeAsync(StateHasChanged)` → Blazor-Circuit-Timing

**Fix – 5 Änderungen in `MitarbeiterPage.razor`:**

| Was | Vorher | Nachher |
|-----|--------|---------|
| Modal-Position | Innerhalb `.page` div | Außerhalb, Geschwister von `.page` |
| CSS animation | `animation: fadeInUp ... both` | Komplett entfernt, `opacity: 1` explizit |
| backdrop-filter | `backdrop-filter: blur(8px)` | Entfernt |
| z-index | `100` | `9000` |
| StateHasChanged | `StateHasChanged()` direkt | `await InvokeAsync(StateHasChanged)` |
| OpenForm/CloseForm | `void` | `async Task` |
| Debug-Bar | Temporäre rote Leiste oben | Entfernt |

**Lernlektion:**
> `animation: X duration both` mit `from { opacity: 0 }` ist in Blazor Server gefährlich. Wenn Animationen deaktiviert sind (GPU, Browser-Setting, Opera GX), bleibt das Element dauerhaft bei `opacity: 0`. Modals IMMER außerhalb verschachtelter Container platzieren.

---

### ✅ BEHOBEN: Mitarbeiter Modal erscheint nicht – Root Cause 2 (07.05.2026)

Trotz Fix aus Session 2 war das Modal weiterhin unsichtbar. Nach vollständiger Code-Analyse wurden **zwei neue Root Causes** gefunden und behoben.

---

#### Bug 1: `@` in HTML-Kommentar zerstört den Razor-Parser

**Symptom:**
- Build: 6 Fehler (`RZ9981`, `RZ1026`) – unmatched closing `</div>` tags
- Compiler-Warnung: `CS0414` – `showForm` und `formOk` wurden „zugewiesen aber nie verwendet"
- Modal trotz `showForm = true` unsichtbar

**Ursache:**
Der HTML-Kommentar im Razor-File enthielt `@if`-Ausdrücke:

```razor
<!-- IMMER im DOM (kein @if außen) – JS steuert display:flex/none.
     Nur der Inhalt wird via @if (showForm) bedingt gerendert. -->
```

Der Razor-Parser verarbeitet `@`-Zeichen **auch innerhalb von HTML-Kommentaren**. Das `@if (showForm)` im Kommentar wurde als echte Razor-Direktive interpretiert. Dadurch:
1. Der Parser „verbrauchte" `showForm` im Kommentar → CS0414 Warnung (nie gelesen)
2. Der echte `@if (showForm)` im Markup wurde fehlerhaft geparst
3. Schließende `</div>`-Tags im Modal-Block hatten scheinbar keine öffnenden Tags mehr

**Diagnose-Schlüssel:** Die C#-Warnung `CS0414: showForm wurde zugewiesen, aber nie verwendet` – obwohl `@if (showForm)` im Markup stand – bewies, dass der Razor-Compiler die Variable dort NICHT als gelesen erkannte.

**Fix:**
```razor
<!-- VORHER (BUGGY): -->
<!-- Inhalt wird via @if (showForm) bedingt gerendert. -->

<!-- NACHHER (FIX): -->
<!-- Modal: ausserhalb aller else-Bloecke, kein Stacking-Context-Problem -->
```
Alle `@`-Zeichen aus HTML-Kommentaren entfernt. Build danach: 0 Fehler, 0 Warnungen.

---

#### Bug 2: JS-Interop Timing Race-Condition in Blazor Server

**Symptom:** Auch nach Build-Fix war das Modal nach Server-Neustart weiterhin unsichtbar (kein schwarzer Hintergrund, kein Formular).

**Ursache:**
Das Modal nutzte JavaScript-Interop (`JS.InvokeVoidAsync("showModal", "mitModal")`). In Blazor Server läuft Rendering über SignalR:

```
Server:  showForm = true → StateHasChanged() → [Diff berechnet] → SignalR →
Client:  [Diff empfangen] → [DOM aktualisiert]
```

`await InvokeAsync(StateHasChanged)` wartet nur auf das Ende des **server-seitigen** Renders – NICHT darauf, dass der Browser die DOM-Änderungen via SignalR erhalten und angewendet hat. Der JS-Aufruf `showModal("mitModal")` konnte daher auf ein noch nicht aktualisiertes DOM treffen.

Zusätzlich: Da das Modal-HTML im vorherigen Build durch Bug 1 falsch kompiliert war, musste der Server **neugestartet** werden – ein laufender Server lädt neue kompilierte Assemblies nicht automatisch nach.

**Fix – Komplette Abkehr von JS-Interop:**

| Vorher | Nachher |
|--------|---------|
| `@inject IJSRuntime JS` | Entfernt |
| `display: none` in CSS | Entfernt (kein Toggle mehr nötig) |
| `JS.InvokeVoidAsync("showModal", "mitModal")` | Entfernt |
| Modal immer im DOM, JS schaltet Sichtbarkeit | Modal nur im DOM wenn `showForm = true` |
| Async OpenForm/CloseForm wegen JS-Await | Einfache synchrone Methoden |

**Neue Architektur (endgültig):**

```razor
@* Modal ausserhalb aller else-Bloecke – kein Stacking-Context *@
@if (showForm)
{
    <div class="modal-overlay" @onclick="CloseForm">
        <div class="modal-box" @onclick:stopPropagation="true">
            ...Formular...
        </div>
    </div>
}
```

```css
/* Kein display:none/flex Toggle. Wenn das div existiert, ist es sichtbar. */
.modal-overlay {
    position: fixed;
    top: 0; left: 0; width: 100%; height: 100%;
    background: rgba(0, 0, 0, 0.85);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 99999;
}
```

```csharp
private async Task OpenForm(Mitarbeiter? m)
{
    // Setup editMitarbeiter...
    showForm = true;
    // Blazor re-rendert automatisch → modal-overlay div wird in den DOM eingefügt
    // Kein JS noetig. Kein Timing-Problem.
}

private void CloseForm()
{
    showForm = false;   // → div verschwindet aus dem DOM
    formMsg = "";
}
```

**Warum das funktioniert:**
Das Modal-Div existiert **nur wenn `showForm = true`**. Wenn Blazor es rendert, hat es bereits `display: flex` (kein Toggle). Es gibt kein Timing-Problem, weil kein JS mehr beteiligt ist. Blazor steuert alles direkt über den virtuellen DOM-Diff.

**Lesson Learned:**

> 1. **`@` in HTML-Kommentaren in Razor-Files ist VERBOTEN.** Der Razor-Parser liest `@if`, `@bind`, `@onclick` etc. auch innerhalb von `<!-- ... -->`. Immer Razor-Kommentare `@* ... *@` für Code-nahe Hinweise verwenden.
>
> 2. **`await InvokeAsync(StateHasChanged)` ≠ DOM fertig.** In Blazor Server ist nach dem Await das DOM im Browser NICHT garantiert aktuell. JS-Interop direkt danach kann auf ein veraltetes DOM treffen. Lösung: JS-Interop vermeiden oder mit `Task.Delay` / `JSRuntime.InvokeAsync` in einem separaten Render-Cycle absichern.
>
> 3. **Server neu starten nach Build.** `dotnet build` kompiliert nur. Eine laufende `dotnet run`-Instanz lädt keine neuen Assemblies – Ctrl+C + Neustart ist Pflicht.

---

## 10. Aktueller Stand

```
Phase 1 – Infrastruktur     ✅ 100% (VM1, VM2, Netzwerk, SQL Server)
Phase 2 – Datenbank         ✅ 100% (8 Tabellen, 5 Test-User, Testdaten)
Phase 3 – Backend API       ✅ 100% (JWT, Rollen, 25 Endpoints, alle Bugs behoben)
Phase 4 – Blazor Frontend   ✅ 100% (alle Seiten + Modal vollständig funktionsfähig)
Phase 5 – NFC Hardware      ❌   0% (Raspberry Pi + PN532 noch nicht begonnen)
Phase 6 – Deployment VM2    ❌   0% (IIS-Deployment noch ausstehend)
Phase 7 – Dokumentation     🔶  laufend
```

### Was funktioniert (07.05.2026 – Session 3)
- ✅ Login mit JWT (Token wird korrekt ausgestellt)
- ✅ Token persistent in LocalStorage (übersteht F5-Reload)
- ✅ Token in jeder API-Request im `Authorization`-Header
- ✅ HTTPS-Kommunikation direkt (kein HTTP→HTTPS Redirect)
- ✅ Alle 9 UI-Seiten – Laden, Anzeige, Interaktion
- ✅ MitarbeiterPage Modal – Erstellen & Bearbeiten vollständig funktionsfähig (browsergetestet in Opera GX)
- ✅ Neuer Mitarbeiter wird in Datenbank gespeichert und sofort in der Liste angezeigt
- ✅ Bearbeiten öffnet Modal mit vorausgefüllten Daten
- ✅ Abteilung-Dropdown ohne Crash (string-Variable statt int?)
- ✅ Deaktivieren/Aktivieren mit Rollback bei Fehler
- ✅ Dashboard, Navigation, Logout

### Was noch offen ist
- ❌ NFC-Integration (Raspberry Pi + PN532 noch nicht begonnen)
- ❌ Deployment auf VM2/IIS (noch ausstehend, nach Frontend-Stabilisierung)
- ❌ Technische Dokumentation (50–70 Seiten)
- ❌ PowerPoint Präsentation

---

## 11. Lokales Setup

### Voraussetzungen
- Windows 11 Pro + Hyper-V
- Visual Studio 2022 + .NET 10 SDK
- VM1 (ChronoDB-Server) gestartet

### Self-Signed Cert akzeptieren (einmalig!)
```
1. https://localhost:7127/swagger im Browser öffnen
2. "Erweitert" → "Trotzdem fortfahren" klicken
   (Sonst schlägt HttpClient mit SSL-Fehler fehl)
```

### API starten
```bash
cd "PROJEKT CHRONO\ChronoAPI"
dotnet run
# → https://localhost:7127/swagger
# → http://localhost:5088/swagger  (auch möglich)
```

### Frontend starten
```bash
cd "PROJEKT CHRONO\ChronoWeb"
dotnet run
# → https://localhost:7068/login
```

### Schnelltest
1. `https://localhost:7127/swagger` öffnen
2. `POST /api/Auth/login` mit `{ "userName": "anna.schmidt", "password": "HR123!" }`
3. Token kopieren → Authorize-Button → `Bearer <token>` einfügen
4. `GET /api/Mitarbeiter` → sollte 200 zurückgeben (wenn 403: RoleClaimType-Problem)

### Browser-Cache löschen (nach Frontend-Änderungen)
```
F12 → Application → Storage → Clear site data
(löscht auch LocalStorage – Token weg → neu einloggen)
```

### VM-Probleme

| Problem | Symptom | Fix |
|---------|---------|-----|
| VM1 nicht gestartet | API startet, DB-Fehler | Hyper-V → ChronoDB-Server starten |
| TCP/IP deaktiviert | Timeout | SQL Config Manager → TCP/IP → Enable → Dienst restart |
| Port 1433 blockiert | Timeout | VM1: `netsh advfirewall firewall add rule name="SQL" protocol=TCP dir=in localport=1433 action=allow` |
| Ping fehlschlägt | Kein Netzwerk | VM1: `netsh advfirewall firewall add rule name="ICMP" protocol=icmpv4 dir=in action=allow` |
| Laptop-IP fehlt | Kann VM nicht erreichen | `ncpa.cpl` → vEthernet (ChronoNetwork) → 192.168.137.1 / 255.255.255.0 |
| Self-signed Cert | `HttpRequestException` | https://localhost:7127/swagger im Browser öffnen + akzeptieren |

---

*Dokumentation zuletzt aktualisiert: 07.05.2026 – Session 3 (Modal-Fix vollständig abgeschlossen)*
