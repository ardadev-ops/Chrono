// ============================================================
//  ChronoModels.cs  (Frontend-Datenmodelle)
// ============================================================
//
//  Diese Klassen sind die "Gegenstücke" zu den API-Modellen.
//  Wenn die API JSON zurückschickt, deserialisiert das Frontend
//  (ChronoWeb) dieses JSON automatisch in diese C#-Klassen.
//
//  Beispiel:
//   API schickt:  {"mitID": 5, "mitVorname": "Anna", ...}
//   ChronoWeb:    var m = new Mitarbeiter { MitID=5, MitVorname="Anna", ... }
//
//  HINWEIS: Diese Klassen müssen die gleichen Eigenschaftsnamen
//  haben wie die API-Modelle, damit die automatische Konvertierung
//  (JSON Deserialisierung) funktioniert.
// ============================================================

namespace ChronoWeb.Models
{
    // ── LOGIN ─────────────────────────────────────────────────

    // Was das Frontend zur API schickt wenn jemand einloggt
    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Was die API nach erfolgreichem Login zurückschickt
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;          // JWT-Token
        public string UserName { get; set; } = string.Empty;       // "anna.mueller"
        public string Role { get; set; } = string.Empty;           // "HR", "Admin", ...
        public string VollName { get; set; } = string.Empty;       // "Anna Müller"
        public DateTime ExpiresAt { get; set; }                     // Token-Ablaufzeit
        public bool MustChangePassword { get; set; }                // Temporäres Passwort?
    }

    // ── ABTEILUNG ─────────────────────────────────────────────

    // Eine Abteilung am Flughafen (z.B. "Bodenabfertigung")
    public class Abteilung
    {
        public int AbtID { get; set; }
        public string AbtName { get; set; } = string.Empty;
        public string? AbtBez { get; set; }   // Beschreibung (optional)
    }

    // ── MITARBEITER ───────────────────────────────────────────

    // Persönliche Daten eines Mitarbeiters
    public class Mitarbeiter
    {
        public int MitID { get; set; }
        public string MitVorname { get; set; } = string.Empty;
        public string MitNachname { get; set; } = string.Empty;
        public string MitMail { get; set; } = string.Empty;
        public string? MitTelefon { get; set; }
        public int? FK_AbtID { get; set; }      // Fremdschlüssel zur Abteilung
        public string? NFCCardUid { get; set; }  // NFC-Karten-ID
        public DateOnly MitBeiDat { get; set; }  // Eintrittsdatum
        public bool MitAktiv { get; set; }        // Noch aktiv?
        public string? UserRolle { get; set; }    // Rolle des Login-Accounts (nur zur Anzeige)
        public Abteilung? Abteilung { get; set; } // Abteilungsdaten (wenn mitgeladen)

        // Berechnete Eigenschaft: Vor- und Nachname kombiniert
        // => = Kurzschreibweise (Expression Body)
        public string FullName => $"{MitVorname} {MitNachname}";
    }

    // ── ZEITBUCHUNG ───────────────────────────────────────────

    // Eine Zeitbuchung = ein Check-in/Check-out-Paar
    public class TimeBooking
    {
        public int TimeID { get; set; }
        public int FK_MitID { get; set; }
        public DateTime CheckInTime { get; set; }    // Wann eingecheckt?
        public DateTime? CheckOutTime { get; set; }  // Wann ausgecheckt? (null = noch offen)
        public int BreakMinutes { get; set; }         // Pausenzeit in Minuten
        public string? Notes { get; set; }            // Zusätzliche Notizen
        public Mitarbeiter? Mitarbeiter { get; set; } // Mitarbeiterdaten (wenn mitgeladen)

        // Berechnete Eigenschaft: Wie viele Stunden gearbeitet?
        // null zurückgeben wenn noch kein Checkout (noch am Arbeiten)
        public double? ArbeitsStunden
        {
            get
            {
                if (CheckOutTime == null) return null; // Noch kein Checkout → unbekannt
                // Gesamtminuten = (CheckOut - CheckIn) - Pause
                var total = (CheckOutTime.Value - CheckInTime).TotalMinutes - BreakMinutes;
                return Math.Round(total / 60, 2); // In Stunden umrechnen, auf 2 Stellen runden
            }
        }
    }

    // ── LOHNABRECHNUNG ────────────────────────────────────────

    // Eine monatliche Gehaltsabrechnung
    public class Lohnabrechnung
    {
        public int LohnID { get; set; }
        public int FK_MitID { get; set; }
        public DateOnly PeriodStart { get; set; }   // Abrechnungsbeginn (z.B. 01.05.2026)
        public DateOnly PeriodEnd { get; set; }     // Abrechnungsende   (z.B. 31.05.2026)
        public double RegularStunden { get; set; }  // Normalstunden
        public double Ueberstunden { get; set; }    // Überstunden
        public double GesamtStunden { get; set; }   // Gesamt
        public decimal BruttoLohn { get; set; }     // Vor Steuerabzug
        public decimal NettoLohn { get; set; }      // Nach Steuerabzug (was ausgezahlt wird)
        public DateTime ProcessedAt { get; set; }   // Wann erstellt?
        public int ProcessedBy { get; set; }        // Wer hat es erstellt? (UserID)
        public Mitarbeiter? Mitarbeiter { get; set; }
    }

    // ── ABWESENHEIT ───────────────────────────────────────────

    // Ein Urlaubs-/Krankenstandsantrag
    public class Abwesenheit
    {
        public int AbwesenID { get; set; }
        public int FK_MitID { get; set; }
        public string AbwesenType { get; set; } = string.Empty;  // "Urlaub", "Krankenstand", ...
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? AbwesenGrund { get; set; }    // Begründung (optional)
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
        public Mitarbeiter? Mitarbeiter { get; set; }
    }

    // ── ZEITKORREKTUR ─────────────────────────────────────────

    // Ein Antrag zum manuellen Nachtragen von Arbeitszeiten
    // (wenn jemand die NFC-Karte vergessen hat)
    public class Zeitkorrektur
    {
        public int KorrID { get; set; }
        public int FK_MitID { get; set; }
        public DateOnly KorrDatum { get; set; }    // Für welches Datum?
        public TimeOnly CheckIn { get; set; }       // Um welche Uhrzeit war er da?
        public TimeOnly CheckOut { get; set; }      // Bis wann?
        public string Grund { get; set; } = "";     // Warum keine NFC-Karte?
        public string Status { get; set; } = "Pending";
        public int? ApprovedBy { get; set; }        // Wer hat genehmigt?
        public DateTime? ApprovedAt { get; set; }   // Wann genehmigt?
        public DateTime? CreatedAt { get; set; }    // Wann erstellt?
        public Mitarbeiter? Mitarbeiter { get; set; }   // Antragsteller
        public Mitarbeiter? Genehmiger { get; set; }    // Wer hat genehmigt
    }

    // ── URLAUBSKONTO ──────────────────────────────────────────

    // Urlaubs-Guthabenkonto eines Mitarbeiters für ein Jahr
    public class Urlaubskonto
    {
        public int KontoID { get; set; }
        public int FK_MitID { get; set; }
        public int Jahr { get; set; }               // z.B. 2026
        public int GesamtTage { get; set; }         // Anspruch gesamt (z.B. 25)
        public decimal GenommTage { get; set; }     // Bereits verbraucht
        // RestTage = GesamtTage - GenommTage (berechnet, nicht aus der DB)
        public decimal RestTage => GesamtTage - GenommTage;
        public Mitarbeiter? Mitarbeiter { get; set; }
    }

    // ── HILFSKLASSEN FÜR API-ANTWORTEN ───────────────────────

    // Einfache Nachricht von der API (z.B. "Passwort erfolgreich geändert!")
    // "record" = kurze Klassen-Variante, nur für einfache Datencontainer
    public record MessageResponse(string Message);

    // Fehlermeldung von der API
    public record ErrorResponse(string Message);

    // ── AUSNAHME FÜR LOGIN-FEHLER ─────────────────────────────

    // Eine eigene Exception-Klasse für Login-Fehler.
    // Warum eine eigene Exception? Damit Login.razor den GENAUEN
    // Fehlertext der API anzeigen kann (z.B. "Account gesperrt für 5 Minuten")
    // statt nur "Login fehlgeschlagen".
    //
    // Exception = ein Fehler der im Programm auftritt und behandelt werden muss.
    // Wir "werfen" (throw) diese Exception im ApiService und "fangen" (catch) sie
    // in der Login-Seite um die Fehlermeldung anzuzeigen.
    public class LoginException : Exception
    {
        // HTTP-Statuscode (z.B. 401 = Unauthorized, 400 = Bad Request)
        public System.Net.HttpStatusCode StatusCode { get; }

        public LoginException(string message, System.Net.HttpStatusCode code)
            : base(message) => StatusCode = code;  // Nachricht an Exception-Basisklasse übergeben
    }

    // Ergebnis wenn ein neuer Mitarbeiter erstellt wird
    public class CreateMitarbeiterResult
    {
        public Mitarbeiter? Mitarbeiter { get; set; }
        public string Username { get; set; } = "";  // Der automatisch generierte Benutzername
        public string Role { get; set; } = "";
        public string Message { get; set; } = "";   // z.B. "Mitarbeiter erstellt! Username: anna.mueller"
    }
}
