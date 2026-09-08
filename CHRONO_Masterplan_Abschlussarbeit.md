# CHRONO – Masterplan Abschlussarbeit

Arbeitsdokument für Alkan Arda · WIFI Fachakademie · Stand 01.08.2026

---

## TEIL 1 — Die Gliederung

Ziel: 50–70 Seiten. Erfahrungswert: **250–300 Wörter pro Seite** bei normalem Fließtext, weniger sobald Abbildungen drin sind. Deine ~20.000 Wörter Rohmaterial ergeben mit Bildern realistisch 55–65 Seiten. Das passt.

| Kap. | Titel | Seiten | Quelle | Status |
|---|---|---|---|---|
| 1 | Einleitung | 5–6 | fertig geschrieben | teilweise |
| 2 | Projektmanagement nach PRINCE2 | 6–8 | **fehlt komplett** | offen |
| 3 | Anforderungsanalyse | 5–6 | **fehlt komplett** | offen |
| 4 | Systemarchitektur & Infrastruktur | 8–10 | Infrastruktur-Doku + Technische Doku | Material da |
| 5 | Datenbankdesign | 6–8 | Technische Doku v2, Kap. 2 | Material da |
| 6 | Backend: API & Sicherheit | 8–10 | API-Doku + JWT-Doku | Material da |
| 7 | Frontend: Blazor Server | 8–10 | Kapitel6_Frontend | Material da |
| 8 | Zeiterfassung & Business Logic | 6–8 | Kapitel7_Zeiterfassung | Material da |
| 9 | NFC-Terminal (Raspberry Pi) | 6–8 | **noch nicht gebaut** | offen |
| 10 | Deployment auf VM2 | 3–4 | **noch nicht gemacht** | offen |
| 11 | Test & Qualitätssicherung | 4–5 | Blazor_Bugs_LessonsLearned | Material da |
| 12 | Fazit & Ausblick | 3–4 | — | offen |
| — | Verzeichnisse + Anhang | 5–8 | — | am Schluss |

### Detailgliederung

**1 Einleitung**
- 1.1 Ausgangssituation und Problemstellung ✔ geschrieben
- 1.2 Projektziel und Projektaufgaben ✔ geschrieben *(Tippfehler: „Projektziels")*
- 1.3 Projektabgrenzung ← **fehlt**
- 1.4 Methodisches Vorgehen ✔ geschrieben
- 1.5 Aufbau der Arbeit ← **fehlt** (½ Seite, Leserführung)

**2 Projektmanagement nach PRINCE2** ← komplett neu, aber Pflicht laut Vorgabe
- 2.1 Grundprinzipien von PRINCE2
- 2.2 Projektorganisation und Rollen
- 2.3 Phasenplanung (März–September 2026, 160 Stunden, 24 Wochen)
- 2.4 Zeitplan / Gantt-Darstellung
- 2.5 Risikomanagement
- 2.6 Soll-Ist-Vergleich der Projektzeit

**3 Anforderungsanalyse**
- 3.1 Ist-Analyse: der papierbasierte Prozess
- 3.2 Funktionale Anforderungen
- 3.3 Nicht-funktionale Anforderungen (Sicherheit, Performance, Usability)
- 3.4 Benutzerrollen und Berechtigungskonzept
- 3.5 Use-Case-Diagramm
- 3.6 Rechtliche Rahmenbedingungen (österreichisches Arbeitszeitgesetz, DSGVO)

**4 Systemarchitektur & Infrastruktur**
- 4.1 Architekturüberblick (Multi-Tier)
- 4.2 Virtualisierung mit Hyper-V
- 4.3 VM-Spezifikationen (ChronoDB-Server / ChronoApp-Server)
- 4.4 Netzwerkkonzept (Dual-NIC, ChronoNetwork 192.168.137.0/24)
- 4.5 Technologiestack und Begründung der Auswahl
- 4.6 Sicherheitskonzept auf Infrastrukturebene

**5 Datenbankdesign**
- 5.1 Auswahl SQL Server 2022 Express
- 5.2 Entity-Relationship-Modell
- 5.3 Tabellenstruktur im Detail (10 Tabellen)
- 5.4 Normalisierung bis zur 3. Normalform
- 5.5 Referentielle Integrität
- 5.6 Performance-Optimierung (Indizes, Datentypen, View)

**6 Backend: API & Sicherheit**
- 6.1 ASP.NET Core Web API und REST-Prinzipien
- 6.2 Entity Framework Core
- 6.3 Authentifizierung mit JWT
- 6.4 Rollenbasierte Autorisierung
- 6.5 Passwortsicherheit mit BCrypt
- 6.6 Account-Sperre nach Fehlversuchen
- 6.7 Endpoint-Übersicht
- 6.8 Abteilungsbasierte Filterlogik

**7 Frontend: Blazor Server**
- 7.1 Blazor Server: Circuit und SignalR
- 7.2 Projektstruktur ChronoWeb
- 7.3 Services: AppState und ApiService
- 7.4 Token-Persistenz mit LocalStorage
- 7.5 Designkonzept
- 7.6 Die zehn Seiten im Detail
- 7.7 Rollenabhängige Navigation

**8 Zeiterfassung & Business Logic**
- 8.1 Urlaubskonto und Arbeitstageberechnung
- 8.2 Zeitkorrektur-Antragssystem
- 8.3 Auto-Checkout als Background Service
- 8.4 Berechnung von Arbeitszeit, Pausen und Überstunden

**9 NFC-Terminal**
- 9.1 Funktionsweise von NFC / RFID (13,56 MHz, MIFARE)
- 9.2 Hardwareauswahl und Aufbau
- 9.3 Verkabelung (I2C-Bus)
- 9.4 Python-Anwendung auf dem Raspberry Pi
- 9.5 API-Anbindung des Terminals
- 9.6 Benutzerführung am Display

**10 Deployment**
- 10.1 Veröffentlichung der Anwendungen
- 10.2 Konfiguration des IIS auf VM2
- 10.3 Inbetriebnahme und Funktionsprüfung

**11 Test & Qualitätssicherung**
- 11.1 Teststrategie
- 11.2 Testfälle und Ergebnisse
- 11.3 Aufgetretene Fehler und deren Behebung
- 11.4 Lessons Learned

**12 Fazit & Ausblick**
- 12.1 Zielerreichung
- 12.2 Persönliches Fazit
- 12.3 Weiterentwicklungsmöglichkeiten

**Verzeichnisse:** Abbildungs-, Tabellen-, Abkürzungs-, Literaturverzeichnis
**Anhang:** ER-Diagramm groß, Endpoint-Tabelle, Quellcode-Auszüge, Testprotokoll

---

## TEIL 2 — Welches Dokument in welches Kapitel

Wichtig: mehrere deiner Dokumente enthalten denselben Inhalt in unterschiedlichen Versionen. **Immer die höchste Version verwenden**, die ältere ignorieren.

| Dein Dokument | Wandert nach | Hinweis |
|---|---|---|
| Technische_Dokumentation.docx (v1) | — | **veraltet, nicht verwenden** |
| Technische_Dokumentation_v2.docx | Kapitel 4 + 5 | Hauptquelle Infrastruktur & DB |
| Dokumentation_Infrastruktur.docx | Kapitel 4 | sehr ausführlich, ergänzt v2 |
| API_Dokumentation.docx | — | **von v2 abgelöst** |
| API_und_JWT_Dokumentation_v2.docx | Kapitel 6 | Hauptquelle Backend |
| JWT_Authentication_Doku.docx | Kapitel 6.3 | Details zu JWT |
| Blazor_Frontend_Doku.docx | — | **von Kapitel6 abgelöst** |
| Kapitel6_Frontend.docx | Kapitel 7 | Hauptquelle Frontend |
| Kapitel7_Zeiterfassung.docx | Kapitel 8 | Hauptquelle Business Logic |
| Blazor_Bugs_LessonsLearned_v3.docx | Kapitel 11 | Testkapitel |

**Achtung, Kapitelnummern verschieben sich:** Deine Doku „Kapitel 6 Frontend" wird in der Arbeit zu **Kapitel 7**. „Kapitel 7 Zeiterfassung" wird zu **Kapitel 8**. Nicht durcheinanderkommen.

---

## TEIL 3 — Der Screenshot-Plan

Faustregel: **20–30 Abbildungen** auf 60 Seiten. Jede Abbildung braucht eine Nummer, eine Unterschrift und eine Erwähnung im Text.

### Kapitel 1 – Einleitung
1. Das originale Dienstprotokoll in Papierform *(falls du eins hast oder nachbauen kannst – extrem stark als Einstieg, zeigt das Problem sofort)*
2. Die alte Ordnerstruktur im Explorer (Tor 4 / GAC / SIKO / Jahr / Monat / Tag)

### Kapitel 2 – Projektmanagement
3. Gantt-Diagramm der Projektphasen
4. Projektstrukturplan
5. Risikomatrix

### Kapitel 3 – Anforderungsanalyse
6. Use-Case-Diagramm (alle fünf Rollen)
7. Prozessdiagramm alt (Papier) vs. neu (CHRONO)

### Kapitel 4 – Infrastruktur
8. Hyper-V-Manager mit beiden laufenden VMs
9. Architekturdiagramm (Terminal → App-Server → DB-Server)
10. Netzwerkkonfiguration der virtuellen Switches
11. `ipconfig` in der VM mit 192.168.137.10
12. Erfolgreicher `Test-NetConnection` auf Port 1433

### Kapitel 5 – Datenbank
13. **ER-Diagramm** ← das wichtigste Bild der ganzen Arbeit
14. SSMS mit dem Objekt-Explorer und allen Tabellen
15. Tabellenstruktur `tblMitarbeiter` in der Entwurfsansicht
16. Beispiel-Query mit Ergebnismenge

### Kapitel 6 – Backend
17. Swagger-UI mit der Endpoint-Übersicht
18. Ein dekodiertes JWT auf jwt.io (Header / Payload / Signature)
19. Erfolgreicher Login-Request mit Token-Antwort
20. 403-Antwort bei fehlender Berechtigung *(zeigt, dass Sicherheit greift)*
21. Passwort-Hash in der Datenbank *(BCrypt-Zeichenkette statt Klartext)*

### Kapitel 7 – Frontend
22. Login-Seite
23. Dashboard aus Sicht eines Mitarbeiters
24. Dashboard aus Sicht des Admins *(nebeneinander – zeigt Rollenlogik)*
25. Meine Zeiten mit Buchungsliste
26. Urlaubsantrag mit Urlaubskonto
27. Mitarbeiterverwaltung mit geöffnetem Modal
28. Passwort-Reset durch HR

### Kapitel 8 – Business Logic
29. Zeitkorrektur-Antrag aus Mitarbeitersicht
30. Genehmigungsansicht des Abteilungsleiters
31. Fehlermeldung bei zu wenig Urlaubstagen
32. Auto-Checkout-Eintrag in der Datenbank

### Kapitel 9 – NFC-Terminal
33. Verkabelungsschema (Fritzing-Stil oder selbst gezeichnet)
34. **Foto des fertigen Terminals** ← Blickfang
35. Foto: Karte wird aufgelegt, Display zeigt „Willkommen Max"
36. `i2cdetect`-Ausgabe mit erkannten Adressen
37. Ausschnitt des Python-Codes
38. Die Buchung erscheint live in der Weboberfläche

### Kapitel 10 – Deployment
39. IIS-Manager mit den veröffentlichten Sites
40. Die Anwendung läuft unter der VM2-Adresse

### Kapitel 11 – Test
41. Testprotokoll-Tabelle
42. Ein Fehlerfall vorher/nachher

### So sieht eine Bildunterschrift aus

```
Abbildung 13: Entity-Relationship-Diagramm der Datenbank ChronoTimeTracking
```

Zentriert, kursiv, 10 pt, **unter** dem Bild. In Word: Rechtsklick aufs Bild → Beschriftung einfügen. Dann nummeriert Word automatisch und du kannst am Ende ein Abbildungsverzeichnis erzeugen.

**Tabellen bekommen die Beschriftung darüber**, Abbildungen darunter. Das ist die übliche Konvention.

### Screenshot-Qualität

- Fenster vorher aufräumen, keine privaten Daten, keine Desktop-Icons im Bild
- Immer nur den relevanten Ausschnitt, nicht den ganzen Bildschirm
- Windows: `Win + Shift + S` für Ausschnitte
- Konsistente Zoomstufe, damit die Bilder gleich groß wirken
- Bei Screenshots mit Passwörtern: nur Testdaten zeigen

---

## TEIL 4 — Wie du schreiben sollst

### Die vier Grundregeln

**1. Fließtext statt Stichpunkte.**
Deine Dokus bestehen zu großen Teilen aus Tabellen und Aufzählungen. In der Abschlussarbeit gilt: pro Unterkapitel **höchstens eine Tabelle**, der Rest ist zusammenhängender Text. Aufzählungen nur, wenn wirklich eine Liste gemeint ist.

**2. Jede Tabelle und jede Abbildung wird im Text angekündigt und ausgewertet.**
Nie ein Bild einfach hinstellen. Immer: erst ein Satz „Abbildung 13 zeigt …", dann das Bild, dann ein Satz, was daran wichtig ist.

**3. Entscheidungen begründen, nicht nur beschreiben.**
Der Unterschied zwischen einer 2 und einer 1 ist das *Warum*. Nicht „Es wurde SQL Server verwendet", sondern warum SQL Server und nicht MySQL oder PostgreSQL.

**4. Zeitform: Präteritum für das, was du getan hast. Präsens für das, was das System tut.**
„Die Datenbank *wurde* in der dritten Normalform *modelliert*. Sie *besteht* aus zehn Tabellen."

### Ich-Form?

Sparsam. In 1.1 (deine Erfahrung am Flughafen) und im persönlichen Fazit ist „ich" richtig und wirkt authentisch. Im technischen Hauptteil neutral formulieren: „Für die Authentifizierung wurde JWT gewählt" statt „Ich habe JWT gewählt".

### Beispiel: aus Doku wird Arbeit

**So steht es in deiner Doku:**

> **2.1 Technologie-Entscheidung: SQL Server 2022 Express**
> - Native .NET Integration – Entity Framework Core hat erstklassige SQL Server Unterstützung
> - Windows-Ökosystem – optimale Integration mit Windows Server 2022 und IIS
> - SSMS – leistungsstarkes grafisches Tool
> - Express Edition – kostenlos, 10 GB ausreichend

**So gehört es in die Arbeit:**

> Als Datenbanksystem wurde Microsoft SQL Server 2022 in der Express Edition gewählt. Ausschlaggebend war in erster Linie die enge Verzahnung mit dem übrigen Technologiestack: Da die Anwendung auf .NET aufsetzt und Entity Framework Core als objektrelationaler Mapper zum Einsatz kommt, ergibt sich mit SQL Server die geringste Reibung. Beide Komponenten stammen von Microsoft und sind aufeinander abgestimmt, wodurch sich der Konfigurationsaufwand deutlich reduziert.
>
> Hinzu kommt, dass die gesamte Serverinfrastruktur auf Windows Server 2022 basiert und die Anwendung später über den Internet Information Server bereitgestellt wird. Ein Datenbanksystem aus demselben Ökosystem fügt sich hier nahtlos ein. Mit dem SQL Server Management Studio steht darüber hinaus ein ausgereiftes Werkzeug für Administration und Abfrageentwicklung zur Verfügung, das den Einstieg spürbar erleichtert hat.
>
> Die Wahl der Express Edition erfolgte bewusst: Sie ist kostenfrei nutzbar und begrenzt die Datenbankgröße auf zehn Gigabyte. Für ein System, das im Wesentlichen Zeitbuchungen speichert, ist diese Grenze auch bei mehrjährigem Betrieb unkritisch. Eine überschlägige Rechnung verdeutlicht das: Bei 200 Mitarbeitern mit je zwei Buchungen pro Arbeitstag entstehen jährlich rund 100.000 Datensätze, was einem Bruchteil der verfügbaren Kapazität entspricht.

Aus vier Stichpunkten werden drei Absätze. Genau so kommst du auf 60 Seiten – und es liest sich wie eine Arbeit, nicht wie eine Werkzeugliste.

### Was du aus den Dokus streichen musst

- Alle Emojis und ✅/❌/🔶-Symbole
- „LESSON LEARNED"-Kästen in Großbuchstaben
- Anreden und Zuschreibungen aus dem Chat
- Statusangaben wie „Phase 4 – 0 %"
- Doppelte Inhalte aus den älteren Dokumentversionen

### Formalia

| Punkt | Vorgabe |
|---|---|
| Schrift | Times New Roman 12 pt oder Arial 11 pt |
| Zeilenabstand | 1,5-zeilig |
| Ausrichtung | Blocksatz mit Silbentrennung |
| Rand | links 3 cm (Bindung!), rechts 2 cm, oben/unten 2,5 cm |
| Seitenzahlen | ab Kapitel 1, römisch für Verzeichnisse |
| Absatzabstand | 6 pt nach Absatz, nicht mit Leerzeilen arbeiten |

Der **linke Rand von 3 cm ist wichtig**, weil die Arbeit gebunden wird. Bei 2 cm verschwindet Text im Bund.

---

## TEIL 5 — Reihenfolge für heute und morgen

**Heute:**
1. NFC-Terminal aufbauen und zum Laufen bringen (Kapitel 9 braucht Fotos, und Hardware kann schiefgehen – deshalb zuerst)
2. Screenshots dabei sofort mitnehmen
3. Abends: Kapitel 2 (PRINCE2) und 3 (Anforderungen) schreiben – die brauchen keine laufende Software

**Morgen:**
4. Deployment auf VM2
5. Kapitel 4–8 aus den vorhandenen Dokus in Fließtext umschreiben
6. Alle fehlenden Screenshots systematisch abarbeiten

**Warum die Hardware zuerst?** Weil alles andere reine Schreibarbeit ist, die du auch nachts um zwei noch machen kannst. Ein Lötkontakt, der nicht sitzt, oder ein I2C-Bus, der nicht antwortet, kostet dagegen unkalkulierbar Zeit.
