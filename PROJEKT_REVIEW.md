# Code-Review: CHRONO – NFC-basiertes Zeiterfassungssystem

**Datum:** 12.08.2026
**Projekt:** PROJEKT CHRONO (WIFI Fachakademie, Flughafen Innsbruck simuliert)
**Umfang:** ChronoAPI (.NET 10), ChronoWeb (Blazor Server .NET 10), Chrono (Lab-Dateien), Dokumentation

---

## 0. Zusammenfassung

Das Projekt ist architektonisch klar aufgebaut, das Datenmodell ist durchdacht und die
Frontend-Optik ist sehr gelungen. Besonders stark: das durchgängige Rollenkonzept,
BCrypt-Hashing, der Auto-Checkout-Dienst und eine ausführliche Selbst-Dokumentation.

Die **größten Schwächen liegen bei der Absicherung der API** (Geheimnisse im Quellcode,
fehlende Ownership-Checks = IDOR, Gehaltsdaten für jeden eingeloggten User) sowie bei
**widersprüchlichen Projekt-Dokumenten** (README vs. PROJECT_DNA vs. tatsächlicher Code).

| Bereich | Bewertung |
|---|---|
| Architektur | Gut |
| Datenmodell | Gut |
| Backend-Logik | Befriedigend (Sicherheitslücken) |
| Frontend/UX | Sehr gut |
| Dokumentation | Gut, aber teilweise veraltet/widersprüchlich |
| Tests/CI | Fehlt |
| Projektstand (Phasen 5–7) | 0 % |

---

## 1. Was gut gelungen ist

### 1.1 Architektur
- Saubere 3-Schichten-Aufteilung: **NFC-Terminal → ChronoAPI (REST) → ChronoWeb (Blazor Server) + SQL Server**. Separate Projekte, klare Verantwortlichkeiten.
- Rollenkonzept (Mitarbeiter / Abteilungsleiter / HR / Buchhaltung / Admin) konsequent in API (`[Authorize(Roles=…)]`) und Frontend (Sidebar, Kacheln, Seitenzugriff) umgesetzt.
- Gute Middleware-Reihenfolge in `Program.cs` (CORS → Auth → Authorization) und saubere JWT-Konfiguration:
  `MapInboundClaims = false`, `RoleClaimType = "role"`, `NameClaimType = "unique_name"` (ChronoAPI/Program.cs:76–97).

### 1.2 Datenmodell
- 8 Tabellen in 3NF mit sinnvoller Entitätsaufteilung (Abteilung, Mitarbeiter, Users, TimeBookings, Abwesenheit + Archiv `tblAbwesenGen`, Urlaubskonto, LohnAbrechnung, AuditLogs).
- `RestTage` als berechnetes `[NotMapped]`-Feld statt redundanter DB-Spalte – gute Entscheidung.
- Testdaten mit 5 Rollen-Usern machen Demo und Entwicklung leicht.

### 1.3 Security-Basics, die stimmen
- Passwörter werden mit **BCrypt (workFactor 12)** gehasht (AuthController, MitarbeiterController).
- **MustChangePassword-Flow**: HR setzt Temp-Passwort → User muss es beim ersten Login ändern (`/change-password`).
- Passwort-Policy (mind. 8 Zeichen, 1 Zahl, 1 Sonderzeichen) sowohl API-seitig als auch im UI mit Live-Stärke-Anzeige.
- JWT-Fehler (RoleClaimType/403) wurden richtig analysiert und behoben – in README sehr gut dokumentiert.

### 1.4 Fachliche Logik
- **AutoCheckoutService** (ChronoAPI/Services/AutoCheckoutService.cs): vergessene Checkouts werden nach 10 h automatisch geschlossen, korrekt als `BackgroundService` mit `IServiceProvider.CreateScope()` (Scoped/Singleton-Thema verstanden).
- **Österreichisches Arbeitsrecht eingebaut**: Pflichtpausen nach AZG § 11 (ab 6 h → 30 min, ab 8 h → 60 min) in `BerechnePflichtpause` (TimeBookingController.cs:198).
- Urlaubsanträge haben einen sauberen Workflow (Pending → Approved/Rejected) mit automatischem Urlaubskonto-Abzug und Werktags-Zählung (Sa/So ausgenommen).

### 1.5 Frontend/UX
- Konsistentes Dark-/Glassmorphism-Design mit eigenen Design-Tokens, Analog-Uhr auf dem Login, Logo-Animation.
- **Rollenbasierte Navigation** (ChronoSidebar.razor) mit `CanApprove`/`IsHR`/`IsAdmin`/`IsBuchhaltung`-Logik.
- **Fehlerbehandlung im UI**: keine stillen `catch { }`-Blöcke mehr, sichtbare Fehlerboxen mit Diagnose (MitarbeiterID etc.).
- CSV-Export mit UTF-8 BOM (Excel-kompatibel) in app.js.
- Lokales State-Management sauber gelöst: `AppState.InitializeAsync()` in `MainLayout.OnAfterRenderAsync` als „Init-Gate“ verhindert Race-Conditions mit dem LocalStorage-Token.

### 1.6 Fehlerkultur / Dokumentation
- README.md ist außergewöhnlich ausführlich: Endpunkt-Liste, JWT-Erklärung, Fehlerdokumentation mit Ursachen-Analyse, VM-Troubleshooting-Tabelle.
- Wichtige Lektionen (z. B. „kein `@` in HTML-Kommentaren in Razor“, „`await InvokeAsync(StateHasChanged)` ≠ DOM fertig“) sind festgehalten – das zeigt echtes Verständnis.

---

## 2. Kritische Fehler (Sicherheit) – müssen gefixt werden

### K1 Geheimnisse im Quellcode
- `ChronoAPI/appsettings.json:10` enthält das **SQL-sa-Passwort im Klartext**: `Password=Server2026!`.
- `ChronoAPI/appsettings.json:13` enthält den **JWT-SecretKey im Klartext**: `ChronoTimeTracking2026!SuperSecretKey!…`.
- Die Passwörter werden zusätzlich in README.md und PROJECT_DNA.md veröffentlicht.
- **Empfehlung:** Secrets in Environment-Variablen / `secrets.json` (User-Secrets) auslagern; nur Dev-Dummy-Werte ins Repo. Test-Passwörter aus README/Projektunterlagen entfernen bzw. klar als Demo kennzeichnen.

### K2 IDOR bei Zeitbuchungen (jeder User kann fremde Daten lesen/schreiben)
- `GET /api/TimeBooking/mitarbeiter/{mitId}` (TimeBookingController.cs:91–98): Die API nimmt die `mitId` **vom Client** und vertraut ihr (Kommentar Zeile 90: „Das Frontend sendet die eigene MitarbeiterID, die API vertraut darauf.“). Ein Mitarbeiter kann so die Zeiten jedes Kollegen abrufen, indem er die ID ändert.
- `POST /api/TimeBooking` (CheckIn, Zeile 103–115): `FK_MitID` kommt ungeprüft aus dem Body → man kann für **jeden Mitarbeiter** Buchungen anlegen (Zeitbetrug möglich).
- `PUT /api/TimeBooking/{id}` (CheckOut, Zeile 137–155): keine Prüfung, dass die Buchung dem eingeloggten User gehört → fremde Buchungen können ausgestempelt werden.
- `GET /api/TimeBooking/{id}` (Zeile 77–85): keine Ownership-Prüfung.
- **Empfehlung:** Ownership immer aus dem JWT-`MitarbeiterID`-Claim ableiten (Muster wie in `GetAktuell`, Zeile 120–131). `mitId`/`FK_MitID` vom Client ignorieren bzw. nur für HR/Admin/Abteilungsleiter erlauben.

### K3 Gehaltsdaten für jeden eingeloggten User
- `GET /api/LohnAbrechnung/mitarbeiter/{mitId}` (LohnAbrechnungController.cs:42–50): **keine Rollen-Einschränkung**, nur Klassen-Level `[Authorize]`. Jeder normale Mitarbeiter kann mit bekannter `mitId` die **Gehaltsdaten jedes Kollegen** lesen. Das Frontend schützt zwar die Seite, aber die API ist offen.
- **Empfehlung:** `[Authorize(Roles = "Buchhaltung,Admin")]` ergänzen – oder für „eigene Abrechnung“ die mitId aus dem Token lesen (dann zusätzlich Rolle als Schutz).

### K4 Ungültige Zertifikats-Prüfung
- `ChronoWeb/Services/ApiService.cs:54`: `ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator` – akzeptiert **jedes Zertifikat** (Man-in-the-Middle möglich).
- **Empfehlung:** Für Produktion entfernen und das echte/richtig konfigurierte Zertifikat verwenden. Im Dev kann man es im Zweifel hinter einer Konfig-Flag lassen.

### K5 Demo-Zugänge fest im Login eingebaut
- `Login.razor:196–213`: Schnell-Login-Buttons mit **Klartext-Passwörtern** (`max.mustermann/Mitarbeiter123!`, `anna.schmidt/HR123!`, …). Sie werden **immer** gerendert, nicht nur in Development – das widerspricht der eigenen Regel in PROJECT_DNA.md („Demo-Logins nur unter `Environment.IsDevelopment()`“).
- **Empfehlung:** `@if (Environment.IsDevelopment())` um den Block; in Produktion entfernen.

### K6 NFC-Endpoint ohne Authentifizierung
- `POST /api/TimeBooking/nfc` (TimeBookingController.cs:205–288) ist `[AllowAnonymous]` – die Kartennummer (UID) ist der einzige Faktor.
- **Einordnung:** Für das Terminal-Szenario vertretbar, aber dokumentieren und absichern (NFC-Scanner läuft nur im Firmen-LAN, ggf. Rate-Limiting/API-Key, Audit-Log des Terminals). Aktuell existiert der Raspberry-Pi-Teil noch nicht (Phase 5).

---

## 3. Weitere Fehler & Schwächen

### F1 Weitere Endpunkte ohne Ownership/Rollen-Check
- `GET /api/Abwesenheit/{id}` (AbwesenheitController.cs:82–90): jeder eingeloggte User kann einzelne fremde Urlaubsanträge per ID lesen (die Liste `GET /` ist dagegen sauber rollen-scoped).
- `GET /api/Mitarbeiter/{id}`: jeder eingeloggte User kann die vollständigen Stammdaten jedes Kollegen abrufen (Privatdaten-Leak).
- **Empfehlung:** einheitlich Ownership aus dem JWT ableiten bzw. explizite Rollen.

### F2 Fehlende Eingabe-Validierung (Logik-Bugs)
- **Doppelter Check-in:** `POST /api/TimeBooking` prüft nicht, ob bereits eine offene Buchung existiert → mehrere parallele Check-ins möglich (das NFC-Terminal verhindert es durch eigene Logik, die API-POST-Route aber nicht).
- **Check-in ohne Prüfung:** `FK_MitID` wird nicht auf Existenz/aktiven Status geprüft.
- **Urlaubskonto-Abziehen** (UrlaubskontoController.cs:129–148): negativer Wert `tage` (z. B. `-5`) wird nie abgelehnt, weil `RestTage < tage` dann nie zutrifft → man kann sich Tage dazubuchen. Es fehlt eine untere/obere Grenze (0 < tage ≤ RestTage).
- **Empfehlung:** Validierung mit Datenannotations + explizite Guards.

### F3 AutoCheckout: Brutto- statt Nettostunden + Inkonsistenz bei Pausen
- AutoCheckoutService.cs:96 setzt `CheckOutTime = CheckInTime + 10h` – die **tatsächliche Ist-Zeit** (z. B. 12 h) wird also rückwirkend auf 10 h „gekürzt“. Ob das gewollt ist, sollte dokumentiert sein.
- AutoCheckout setzt **keine `BreakMinutes`** (bleiben 0), während der manuelle Checkout Pflichtpausen berechnet → unterschiedliche Netto-Zeiten je nach Stempelweg.
- `BerechnePflichtpause` existiert doppelt (TimeBookingController.cs:198 und in der NFC-Logik) – Code-Duplikation.

### F4 Kein Migrations-/Versionsmanagement für die DB
- Es gibt **keinen Migrations-Ordner** im ChronoAPI-Projekt. Das Datenbankschema ist nicht versioniert – Änderungen müssen von Hand auf die VM1 aufgespielt werden.
- **Empfehlung:** `dotnet ef migrations add …` einführen und Migrationen einchecken.

### F5 Keine automatisierten Tests
- Es gibt **kein Testprojekt** (kein xUnit/MSTest/NUnit). Für ein Abgabeprojekt gut vorzeigbar wäre zumindest ein Test für die Pausen-Berechnung und die Auth-Logik.
- **Empfehlung:** kleines xUnit-Projekt mit Tests für `BerechnePflichtpause`, Urlaubskonto-Abzug und JWT-Erzeugung/Validierung.

### F6 Harte URLs und lockere CORS-Konfiguration
- `ApiService.cs:44`: `BaseUrl = "https://localhost:7127/api/"` ist hart verdrahtet → für das geplante Deployment auf VM2/IIS unbrauchbar. Über `IConfiguration` bzw. appsettings lösen.
- `Program.cs:48`: CORS-Policy `AllowAll` (alle Origins) – für Produktion einschränken.
- `appsettings.json`: `"AllowedHosts": "*"` und LaunchSettings mit `0.0.0.0` (alle Netzwerkinterfaces) – ok im Schul-LAN, aber nicht für Produktion.

### F7 Frontend: Navigation nur auf dem Dashboard
- `ChronoSidebar` wird **nur in Dashboard.razor** eingebunden (Dashboard.razor:24). Alle anderen Seiten haben nur „Zurück zum Dashboard“-Buttons – die schöne rollenbasierte Sidebar fehlt dort.
- **Empfehlung:** Sidebar in `MainLayout.razor` hosten (Layout statt pro Seite), damit die Navigation überall konsistent verfügbar ist.

### F8 Toter Template-Code
- `Components/Layout/NavMenu.razor` enthält noch die Blazor-Vorlage (Counter, Weather) – nicht verwendeter Code, sollte gelöscht werden.

### F9 Inkonsistente Sonderzeichen-Definition für Passwörter
- UI-Anzeige (ChangePasswordPage.razor:107) prüft `"!@#$%^&*()_+-="`, die Stärke-Berechnung (Zeile 200) `"!@#$%^&*()_+-=[]{}|"` und die API `IsValidPassword` nutzt eine eigene Liste. Je nach Liste können unterschiedliche Passwörter durchfallen/bestehen. Einheitliche Regel zentral definieren (z. B. Regex in einer Konstante/Service).

### F10 CSS-Duplikation
- Jede Seite bringt ihren eigenen inline `<style>`-Block mit (spinner, fadeInUp, Farbwerte usw. mehrfach kopiert). `wwwroot/css/chrono-design.css` mit Design-Tokens ist angekündigt, wird aber nicht konsequent genutzt.
- **Empfehlung:** gemeinsame Stylesheets/Design-Tokens zentralisieren; Seiten nur noch Seiten-spezifisches CSS.

### F11 Kompilierte Artefakte im Projektordner
- `bin/`, `obj/` liegen unter ChronoAPI/ChronoWeb im Projektverzeichnis und enthalten u. a. die `appsettings.json` mit den Secrets – das erschwert Übersicht und Cleanup (und hätte bei einem Git-Repo zu Secret-Leaks im Repo geführt).

---

## 4. Dokumentations-Widersprüche (README vs. PROJECT_DNA vs. Code)

| Thema | README.md | PROJECT_DNA.md | Tatsächlicher Code |
|---|---|---|---|
| SQL-sa-Passwort | `ServerAdmin2026!` | `ServerAdmin2026!` | `Server2026!` (appsettings.json:10) |
| 401-Problem | „BEHOBEN (05.05.)“ | „NOCH NICHT GELÖST“ (04.05.) | Behoben (ApiService nutzt HTTPS + AllowAutoRedirect=false) |
| Frontend-Pfad | `ChronoWeb\` | `src/ChronoWeb` („regelkonformer Stand“) | `ChronoWeb\` (src/-Version wurde zurückgerollt und existiert nicht mehr) |
| Demo-Logins | – | „nur in IsDevelopment()“ | Immer gerendert (Login.razor:196–213) |
| LESSONS_LEARNED.md | – | – | Nur leeres Template (Punkt-Liste ohne Inhalt) |

**Empfehlung:** Eine einzige Quelle der Wahrheit pflegen (am besten README), PROJECT_DNA.md als historisches Log kennzeichnen oder bereinigen, veraltete Abschnitte („401 offen“, `src/ChronoWeb`) entfernen.

---

## 5. Projektstand & offene Aufgaben

- **Phase 1–4 (Infrastruktur, DB, API, Frontend):** praktisch fertig und laut README funktionsfähig. ✅
- **Phase 5 – NFC Hardware (Raspberry Pi + PN532):** 0 %. Der Ordner `Chrono\` enthält **keinen Terminal-Code**, sondern nur Agent-/Lab-Dateien (MISSION.md, PROJECT_DNA.md, AGENTS.md, SCRATCHPAD.md usw.). Der eigentliche RPi-Endpoint (`POST /api/TimeBooking/nfc`) existiert zwar serverseitig, aber kein Client.
- **Phase 6 – Deployment auf VM2 (IIS):** offen. Die hart verdrahtete BaseUrl und das CORS `AllowAll` müssen vorher angepasst werden.
- **Phase 7 – Technische Dokumentation (50–70 Seiten) + PowerPoint:** laufend.
- **Kein Git-Repo:** Das Projekt liegt nicht unter Versionskontrolle, obwohl es bereits mehrere Rollbacks gab (Premium-UI-Experiment). Git wäre dringend zu empfehlen – auch um z. B. `bin/obj` und Secrets rauszuhalten (`.gitignore`).

---

## 6. Priorisierte Maßnahmen

**P0 – sofort (Sicherheit):**
1. Secrets aus appsettings.json/README/PROJECT_DNA entfernen → User-Secrets/Env-Vars.
2. Ownership-Checks in TimeBooking (GetById, GetByMitarbeiter, POST, PUT) – mitId/FK_MitID aus dem JWT ableiten.
3. `GET /api/LohnAbrechnung/mitarbeiter/{mitId}` auf `Buchhaltung,Admin` beschränken.
4. `DangerousAcceptAnyServerCertificateValidator` nur hinter Dev-Flag erlauben.
5. Schnell-Login-Buttons in Login.razor unter `Environment.IsDevelopment()` stellen.

**P1 – demnächst:**
6. Ownership/Rollen-Checks für `Abwesenheit/{id}` und `Mitarbeiter/{id}`.
7. Validierung: negatives Urlaubskonto-Tage, doppelter Check-in, `FK_MitID`-Existenz.
8. AutoCheckout: `BreakMinutes` berechnen, Ist-Zeit statt `CheckIn+10h` dokumentieren/ändern.
9. EF-Core-Migrations einführen.
10. BaseUrl + CORS konfigurierbar machen (AppSettings), `AllowedHosts` einschränken.

**P2 – Qualität:**
11. xUnit-Testprojekt (Pausen-Berechnung, Auth, Urlaubskonto).
12. Sidebar ins `MainLayout` ziehen, `NavMenu.razor` (Template) löschen.
13. Einheitliche Passwort-Sonderzeichen-Definition; zentrale CSS-Tokens.
14. Doku konsolidieren (README als Single Source of Truth, PROJECT_DNA bereinigen).
15. Git-Repo anlegen + `.gitignore` (bin/, obj/, Secrets).

---

## 7. Fazit

Insgesamt ist CHRONO für ein Schulprojekt **überdurchschnittlich**: klare Architektur, gutes
Datenmodell, stimmige Rollenlogik, modernes UI und eine vorbildlich dokumentierte Fehlerhistorie.
Die verbleibenden Baustellen sind vor allem **Sicherheitslücken in der API** (IDOR, Secrets,
Gehaltsdaten) sowie **Prozess-Themen** (keine Migrations, keine Tests, kein Git, veraltete Doku).
Behebt man die P0-Punkte, ist das Projekt auf einem sehr guten Stand – und die Sicherheits-Erkenntnisse
lassen sich im Dokumentationsteil der Abschlussarbeit sogar als Pluspunkt verkaufen.
