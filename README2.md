# CHRONO – NFC-basiertes Zeiterfassungssystem

**Auftraggeber:** Flughafen Innsbruck (simuliert – Schulprojekt)  
**Student:** Alkan Arda | WIFI Fachakademie, 3. und 4. Semester  
**Betreuer:** Stephan Arnim Franzmann  
**Zeitraum:** März 2026 – September 2026 (160 Stunden)  
**Zuletzt aktualisiert:** 06.05.2026

---

## 🚨 FÜR CLAUDE CODE – BITTE ALLES LESEN BEVOR DU ANFÄNGST!

Dieses README enthält alle Informationen die du brauchst um am CHRONO Projekt weiterzuarbeiten. Lies alles durch, bevor du irgendwas änderst.

---

## 📁 PROJEKTSTRUKTUR

```
PROJEKT CHRONO\
├── ChronoAPI\          → ASP.NET Core .NET 10 Backend API
│   ├── Controllers\    → AuthController, MitarbeiterController, TimeBookingController,
│   │                     AbwesenheitController, AbteilungController, LohnAbrechnungController
│   ├── Data\           → AppDbContext.cs (8 DbSets)
│   ├── Models\         → 8 Entity-Models + LoginRequest/LoginResponse
│   ├── Program.cs      → JWT Config, CORS, Swagger, Middleware-Pipeline
│   └── appsettings.json → Connection String, JWT Settings
│
└── ChronoWeb\          → Blazor Server .NET 10 Frontend
    ├── Components\
    │   ├── Pages\      → Login, Dashboard, MeineZeiten, Urlaub, Antraege,
    │   │                 MitarbeiterPage, LohnabrechnungPage, ZeitExportPage, TeamZeiten
    │   └── Layout\     → MainLayout.razor, NavMenu.razor
    ├── Services\
    │   ├── ApiService.cs  → Alle HTTP-Calls zur API
    │   └── AppState.cs    → JWT-Parsing, Login-State, Rollen-Checks
    ├── Models\         → ChronoModels.cs (alle Model-Klassen)
    └── wwwroot\        → app.css, app.js, chrono-logo.png
```

---

## 🔧 INFRASTRUKTUR

```
HOST-LAPTOP (192.168.137.1) → Entwicklung
├── VM1: ChronoDB-Server (192.168.137.10) → SQL Server 2022 Express
└── VM2: ChronoApp-Server (192.168.137.11) → .NET 10 + IIS (Deployment ausstehend)
```

**VM1 starten:** Hyper-V Manager → ChronoDB-Server starten (muss laufen bevor API startet!)

---

## 🗄️ DATENBANK

**Server:** 192.168.137.10,1433 (KEIN \\SQLEXPRESS – direkt mit Port!)  
**Datenbank:** ChronoTimeTracking  
**User:** sa | **Passwort:** ServerAdmin2026!

**Connection String (appsettings.json):**
```
Server=192.168.137.10,1433;Database=ChronoTimeTracking;User Id=sa;
Password=ServerAdmin2026!;TrustServerCertificate=True;Encrypt=False;
```

**8 Tabellen:** tblAbteilung, tblMitarbeiter, tblUsers, tblTimeBookings,
tblAbwesenheit, tblAbwesenGen, tblLohnAbrechnung, tblAuditLogs

**Test-User:**
| Username | Passwort | Rolle | MitID | Abteilung |
|----------|----------|-------|-------|-----------|
| max.mustermann | Mitarbeiter123! | Mitarbeiter | 1 | IT |
| anna.schmidt | HR123! | HR | 2 | HR |
| tom.weber | Manager123! | Abteilungsleiter | 3 | IT |
| lisa.meyer | Mitarbeiter123! | Mitarbeiter | 4 | IT |
| paul.fischer | Buchhaltung123! | Buchhaltung | 5 | Finanzen |

---

## 🚀 LOKALES SETUP

### 1. VM1 starten
Hyper-V Manager → ChronoDB-Server → Starten

### 2. Self-Signed Cert akzeptieren (einmalig!)
Browser → https://localhost:7127/swagger → "Erweitert" → "Trotzdem fortfahren"  
(Sonst schlägt HttpClient mit SSL-Fehler fehl!)

### 3. API starten
```bash
cd "PROJEKT CHRONO\ChronoAPI"
dotnet run
# Swagger: https://localhost:7127/swagger
```

### 4. Frontend starten
```bash
cd "PROJEKT CHRONO\ChronoWeb"
dotnet run
# Browser: https://localhost:7068
```

### 5. Nach Frontend-Änderungen
Browser: F12 → Application → Storage → Clear site data  
(Löscht LocalStorage – neu einloggen erforderlich)

---

## 🔑 JWT AUTHENTICATION

**KRITISCHE KONFIGURATION in ChronoAPI/Program.cs:**
```csharp
.AddJwtBearer(options => {
    options.MapInboundClaims = false;  // KRITISCH! Ohne das → 403
    options.TokenValidationParameters = new TokenValidationParameters {
        // ...
        RoleClaimType = "role",        // KRITISCH!
        NameClaimType = "unique_name"  // KRITISCH!
    };
});
```

**KRITISCHE KONFIGURATION in AuthController.cs:**
```csharp
// String-Literale verwenden – NICHT ClaimTypes.Role!
new Claim("role", user.UserRole),         // ✅ RICHTIG
// new Claim(ClaimTypes.Role, user.UserRole) // ❌ FALSCH → 403
```

**Middleware-Reihenfolge (NICHT ändern!):**
```csharp
app.UseCors("AllowAll");       // 1. ERST CORS
app.UseHttpsRedirection();     // 2. dann HTTPS
app.UseAuthentication();       // 3. ERST Auth
app.UseAuthorization();        // 4. DANN Autorisierung
app.MapControllers();          // 5. Routing
```

**ApiService.cs BaseUrl:**
```csharp
private const string BaseUrl = "https://localhost:7127/api/";
// HTTPS verwenden! HTTP → 301 Redirect → Authorization Header geht verloren
```

---

## 🎯 ROLLEN & ZUGRIFFSLOGIK

| Rolle | Mitarbeiter | TimeBookings | Abwesenheit |
|-------|-------------|--------------|-------------|
| Mitarbeiter | ❌ | nur eigene | nur eigene |
| Abteilungsleiter | nur eigene Abt. | nur eigene Abt. | nur eigene Abt. |
| HR | alle | alle | alle |
| Buchhaltung | ❌ | alle (Export) | nur eigene |
| Admin | alle | alle | alle |

**Abteilungsfilter-Logik (server-seitig im Controller):**
```csharp
var mitId = int.Parse(User.FindFirst("MitarbeiterID")!.Value);
var selbst = await _context.Mitarbeiter.FirstOrDefaultAsync(m => m.MitID == mitId);
// Nur Mitarbeiter mit derselben FK_AbtID zurückgeben
return await _context.Mitarbeiter.Where(m => m.FK_AbtID == selbst.FK_AbtID)...
```

---

## 📋 AKTUELLER STATUS (06.05.2026)

```
✅ Infrastruktur (VM1, VM2, Netzwerk)         100%
✅ Datenbank (8 Tabellen, Testdaten)           100%
✅ Backend API (JWT, Rollen, 18 Endpoints)     100%
⚠️  Blazor Frontend                             85%
   ✅ Login mit JWT + LocalStorage-Persistenz
   ✅ Dashboard mit rollenbasierten Kacheln
   ✅ MeineZeiten – funktioniert
   ✅ Urlaub – Anträge stellen
   ✅ Anträge – Genehmigen/Ablehnen
   ✅ MitarbeiterPage – Anzeige funktioniert
   ❌ MitarbeiterPage – Modal (Erstellen/Bearbeiten) wird nicht angezeigt
   ✅ LohnabrechnungPage – Admin only
   ✅ ZeitExportPage – Buchhaltung
   ✅ TeamZeiten – CanApprove
❌ NFC Hardware (Raspberry Pi + PN532)           0%
❌ Deployment auf VM2/IIS                        0%
🔶 Dokumentation                               laufend
```

---

## 🐛 OFFENE BUGS

### ❌ KRITISCH: Mitarbeiter Modal erscheint nicht
**Symptom:** showForm = True (Debug-Bar bestätigt), aber Modal ist unsichtbar  
**Was getestet wurde:**
- Modal innerhalb des else-Blocks → unsichtbar
- Modal außerhalb aller if/else-Blöcke → unsichtbar  
- CSS display:none/flex Toggle → unsichtbar
- Alle Inline-Styles, z-index 999999 → unsichtbar
- JavaScript showModal() via IJSRuntime → noch nicht getestet vollständig

**Verdacht:** Blazor Server DOM-Diffing Problem oder Opera GX Browser-spezifisches Rendering-Problem  
**Nächster Test:** Anderen Browser (Chrome/Edge) versuchen. Alternativ: Separate Seite /mitarbeiter-neu als Route statt Modal.

---

## 🏗️ CODING-KONVENTIONEN

```
Namespace API:       ChronoAPI
Namespace Frontend:  ChronoWeb
Tabellennamen:       tblXXX (Prefix)
FK-Felder:           FK_XXX
Rendermode:          @rendermode InteractiveServer (alle Seiten)
CSS:                 Inline <style> Block in jeder .razor Datei
DI:                  AddScoped für ApiService + AppState
LocalStorage Key:    "chrono_token"
```

---

## ⚠️ BEKANNTE FEHLER & FIXES (Lesson Learned)

| Fehler | Ursache | Fix |
|--------|---------|-----|
| HTTP 401 nach Login | HTTP→HTTPS Redirect löscht Authorization Header | BaseUrl auf HTTPS + AllowAutoRedirect=false |
| HTTP 401 nach F5 | Blazor Circuit-Lifecycle, Token nur im Memory | Token in LocalStorage via Blazored.LocalStorage |
| HTTP 403 bei Rollen | ClaimTypes.Role erzeugt langen URI, Middleware findet ihn nicht | String-Literal "role" + MapInboundClaims=false + RoleClaimType="role" |
| 404 auf /swagger | .NET 10 verwendet AddOpenApi() statt Swashbuckle | AddSwaggerGen() + UseSwagger() + UseSwaggerUI() |
| Schwarzer Bildschirm | catch { } schluckt Exceptions still | catch (Exception ex) { loadError = ex.Message; } |
| LocalStorage nicht verfügbar | Blazor Prerendering → kein Browser-Kontext | OnAfterRenderAsync statt OnInitializedAsync |
| Duplicate Routes | Zwei .razor Dateien mit @page "/urlaub" | Redundante Dateien löschen |

---

## 📞 VM-PROBLEME SCHNELLHILFE

| Problem | Symptom | Fix |
|---------|---------|-----|
| VM1 nicht gestartet | DB Connection Timeout | Hyper-V → ChronoDB-Server → Starten |
| TCP/IP deaktiviert | Timeout bei 1433 | SQL Config Manager → TCP/IP → Enable → Dienst restart |
| Port 1433 blockiert | Timeout | `netsh advfirewall firewall add rule name="SQL" protocol=TCP dir=in localport=1433 action=allow` |
| Laptop-IP fehlt | Kein Ping zu VMs | ncpa.cpl → vEthernet (ChronoNetwork) → 192.168.137.1 / 255.255.255.0 |
| Self-signed Cert | HttpRequestException SSL | https://localhost:7127/swagger im Browser öffnen + akzeptieren |

---

*Zuletzt aktualisiert: 06.05.2026 – nach vollständiger Debugging-Session (401→403→Abteilungsfilter)*
