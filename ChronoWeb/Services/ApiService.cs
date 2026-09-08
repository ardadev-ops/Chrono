// ============================================================
//  ApiService.cs  (HTTP-Client für die ChronoAPI)
// ============================================================
//
//  ApiService ist die "Brücke" zwischen ChronoWeb (Frontend)
//  und ChronoAPI (Backend).
//
//  Jede Methode hier = eine API-Anfrage.
//  Beispiel:
//    ApiService.GetMitarbeiter()
//    → schickt GET https://localhost:7127/api/Mitarbeiter
//    → API prüft das JWT-Token im Header
//    → API gibt JSON zurück
//    → ApiService wandelt JSON in List<Mitarbeiter> um
//    → zurückgeben an die Razor-Seite
//
//  WICHTIGES KONZEPT – HTTP-Methoden:
//   GET    = Daten abrufen (lesen)
//   POST   = Neue Daten erstellen
//   PUT    = Bestehende Daten ändern
//   DELETE = Daten löschen
//
//  WICHTIGES KONZEPT – Bearer Token:
//   Jede Anfrage (außer Login) schickt den JWT-Token im Header mit:
//   "Authorization: Bearer eyJhbGc..."
//   So weiß die API, wer die Anfrage stellt und was er darf.
// ============================================================

using ChronoWeb.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace ChronoWeb.Services
{
    public class ApiService
    {
        // HttpClient = das Werkzeug für HTTP-Anfragen
        private readonly HttpClient _http;

        // Der aktuell gespeicherte JWT-Token (nach Login gesetzt)
        private string? _token;

        // Basis-URL der API – alle Anfragen gehen an diese Adresse
       

        public ApiService(IConfiguration config)
        {
            var baseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7127/api/";
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                AllowAutoRedirect = false
            };

            _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        }

        // ── TOKEN-VERWALTUNG ──────────────────────────────────

        // Token setzen (nach erfolgreichem Login)
        // Speichert den Token für alle zukünftigen Anfragen
        public void SetToken(string token)
        {
            _token = token;
            // DefaultRequestHeaders = Header die bei JEDER Anfrage mitgeschickt werden
            // "Bearer eyJhbGc..." = Standard-Format für JWT-Authentifizierung
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // Token löschen (beim Ausloggen)
        public void ClearToken()
        {
            _token = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }

        // Ist der User eingeloggt? (Hat er einen Token?)
        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

        // ── PRIVATE HILFSMETHODEN ─────────────────────────────
        // Diese Methoden bauen HTTP-Anfragen auf und verarbeiten die Antworten.
        // Sie werden von den öffentlichen Methoden weiter unten verwendet.

        // HttpRequestMessage mit JWT-Token erstellen
        private HttpRequestMessage Req(HttpMethod method, string url)
        {
            var r = new HttpRequestMessage(method, url);
            // Token pro Anfrage nochmal setzen (sicherer als DefaultRequestHeaders)
            if (!string.IsNullOrEmpty(_token))
                r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            return r;
        }

        // GET-Anfrage für eine Liste (z.B. alle Mitarbeiter)
        // T = Typ-Parameter → funktioniert für jede Klasse
        private async Task<List<T>> GetList<T>(string url)
        {
            using var req = Req(HttpMethod.Get, url);
            var resp = await _http.SendAsync(req);

            // Bei Erfolg: JSON → C#-Liste umwandeln
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync<List<T>>() ?? new();

            // Bei Fehler: Fehlermeldung aus der Antwort lesen und Exception werfen
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)resp.StatusCode} – {body}");
        }

        // GET-Anfrage für ein einzelnes Objekt (z.B. ein Mitarbeiter)
        private async Task<T?> GetItem<T>(string url)
        {
            using var req = Req(HttpMethod.Get, url);
            var resp = await _http.SendAsync(req);

            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync<T>();

            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)resp.StatusCode} – {body}");
        }

        // POST-Anfrage mit JSON-Body (z.B. neuen Mitarbeiter erstellen)
        private async Task<HttpResponseMessage> PostJson<T>(string url, T payload)
        {
            using var req = Req(HttpMethod.Post, url);
            // JsonContent.Create wandelt das C#-Objekt in JSON um
            req.Content = JsonContent.Create(payload);
            return await _http.SendAsync(req);
        }

        // PUT-Anfrage mit JSON-Body (z.B. Mitarbeiter bearbeiten)
        private async Task<HttpResponseMessage> PutJson<T>(string url, T payload)
        {
            using var req = Req(HttpMethod.Put, url);
            req.Content = JsonContent.Create(payload);
            return await _http.SendAsync(req);
        }

        // DELETE-Anfrage (z.B. Mitarbeiter löschen)
        private async Task<HttpResponseMessage> DeleteReq(string url)
        {
            using var req = Req(HttpMethod.Delete, url);
            return await _http.SendAsync(req);
        }

        // ── AUTHENTIFIZIERUNG ─────────────────────────────────

        // Einloggen: Benutzername + Passwort → JWT-Token
        public async Task<LoginResponse?> Login(LoginRequest request)
        {
            // Login braucht KEIN Token (wir haben noch keins)
            var req = new HttpRequestMessage(HttpMethod.Post, "Auth/login");
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);

            // Erfolgreich? → LoginResponse (mit Token) zurückgeben
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync<LoginResponse>();

            // Fehler? → Den genauen Fehlertext aus der API lesen
            // und als LoginException werfen, damit Login.razor ihn anzeigen kann
            var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new LoginException(
                error?.Message ?? "Login fehlgeschlagen.",
                resp.StatusCode);
        }

        // Eigenes Passwort ändern
        public async Task<(bool ok, string message)> ChangePassword(
            string oldPassword, string newPassword, string confirmPassword)
        {
            using var req = Req(HttpMethod.Post, "Auth/change-password");
            req.Content = JsonContent.Create(new
            {
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            });
            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadFromJsonAsync<MessageResponse>();
            // Tupel zurückgeben: (ob erfolgreich, Nachrichtentext)
            return (resp.IsSuccessStatusCode, body?.Message ?? "Unbekannter Fehler");
        }

        // Passwort eines anderen Mitarbeiters zurücksetzen (HR/Admin)
        public async Task<(bool ok, string message)> ResetPassword(int mitarbeiterId, string newPassword)
        {
            using var req = Req(HttpMethod.Post, "Auth/reset-password");
            req.Content = JsonContent.Create(new { MitarbeiterId = mitarbeiterId, NewPassword = newPassword });
            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadFromJsonAsync<MessageResponse>();
            return (resp.IsSuccessStatusCode, body?.Message ?? "Unbekannter Fehler");
        }

        // Rolle eines bestehenden Mitarbeiters aendern (HR/Admin)
        public async Task<(bool ok, string message, string? hinweis)> ChangeRole(
            int mitarbeiterId, string neueRolle)
        {
            var req = Req(HttpMethod.Post, "Auth/change-role");
            req.Content = JsonContent.Create(new
            {
                mitarbeiterId,
                neueRolle
            });

            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var msg = doc.RootElement.TryGetProperty("message", out var m)
                          ? m.GetString() ?? "" : "";

                string? hinweis = null;
                if (doc.RootElement.TryGetProperty("hinweis", out var h)
                    && h.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    hinweis = h.GetString();
                }

                return (resp.IsSuccessStatusCode, msg, hinweis);
            }
            catch
            {
                return (resp.IsSuccessStatusCode,
                        resp.IsSuccessStatusCode ? "Rolle geaendert." : "Aenderung fehlgeschlagen.",
                        null);
            }
        }

        // ── ABTEILUNG ─────────────────────────────────────────

        // Alle Abteilungen laden (für Dropdown-Menüs)
        public Task<List<Abteilung>> GetAbteilungen()
            => GetList<Abteilung>("Abteilung");

        // ── MITARBEITER ───────────────────────────────────────

        // Alle Mitarbeiter laden (je nach Rolle: alle oder nur eigene Abteilung)
        public Task<List<Mitarbeiter>> GetMitarbeiter()
            => GetList<Mitarbeiter>("Mitarbeiter");

        // Einen bestimmten Mitarbeiter nach ID laden
        public Task<Mitarbeiter?> GetMitarbeiterById(int id)
            => GetItem<Mitarbeiter>($"Mitarbeiter/{id}");

        // Neuen Mitarbeiter UND User-Account erstellen
        public async Task<CreateMitarbeiterResult?> CreateMitarbeiterWithUser(
            Mitarbeiter mitarbeiter, string tempPassword, string role)
        {
            using var req = Req(HttpMethod.Post, "Mitarbeiter");
            req.Content = JsonContent.Create(new
            {
                Mitarbeiter = mitarbeiter,
                TempPassword = tempPassword,
                Role = role
            });
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HTTP {(int)resp.StatusCode} – {body}");
            }
            return await resp.Content.ReadFromJsonAsync<CreateMitarbeiterResult>();
        }

        // Mitarbeiterdaten aktualisieren
        public async Task<bool> UpdateMitarbeiter(int id, Mitarbeiter m)
        {
            var resp = await PutJson($"Mitarbeiter/{id}", m);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HTTP {(int)resp.StatusCode} – {body}");
            }
            return true;
        }

        // ── ZEITBUCHUNGEN ─────────────────────────────────────

        // Alle Zeitbuchungen (für HR/Admin/Abteilungsleiter)
        public Task<List<TimeBooking>> GetTimeBookings()
            => GetList<TimeBooking>("TimeBooking");

        // Zeitbuchungen eines bestimmten Mitarbeiters
        public Task<List<TimeBooking>> GetTimeBookingsByMitarbeiter(int mitId)
            => GetList<TimeBooking>($"TimeBooking/mitarbeiter/{mitId}");

        // ── ABWESENHEIT ───────────────────────────────────────

        // Alle Abwesenheitsanträge (je nach Rolle)
        public Task<List<Abwesenheit>> GetAbwesenheiten()
            => GetList<Abwesenheit>("Abwesenheit");

        // Nur ausstehende Anträge (für Genehmiger)
        public Task<List<Abwesenheit>> GetPendingAbwesenheiten()
            => GetList<Abwesenheit>("Abwesenheit/pending");

        // Antrag genehmigen
        public async Task<bool> ApproveAbwesenheit(int id, int approvedBy)
        {
            var resp = await PutJson($"Abwesenheit/{id}/approve", approvedBy);
            return resp.IsSuccessStatusCode;
        }

        // Antrag ablehnen
        public async Task<bool> RejectAbwesenheit(int id)
        {
            var resp = await PutJson($"Abwesenheit/{id}/reject", "");
            return resp.IsSuccessStatusCode;
        }

        // Neuen Antrag stellen
        public async Task<(bool ok, string message)> CreateAbwesenheit(Abwesenheit abwesenheit)
        {
            var req = Req(HttpMethod.Post, "Abwesenheit");
            req.Content = JsonContent.Create(abwesenheit);
            var resp = await _http.SendAsync(req);

            if (resp.IsSuccessStatusCode)
                return (true, "Antrag erfolgreich gestellt!");

            try
            {
                var error = await resp.Content.ReadFromJsonAsync<MessageResponse>();
                return (false, error?.Message ?? "Fehler beim Senden.");
            }
            catch
            {
                return (false, "Fehler beim Senden. Bitte nochmal versuchen.");
            }
        }

        // ── ZEITKORREKTUR ─────────────────────────────────────

        // Alle Zeitkorrektur-Anträge laden
        public Task<List<Zeitkorrektur>> GetZeitkorrekturen()
            => GetList<Zeitkorrektur>("Zeitkorrektur");

        // Nur ausstehende Zeitkorrekturen (für Genehmiger)
        public Task<List<Zeitkorrektur>> GetPendingZeitkorrekturen()
            => GetList<Zeitkorrektur>("Zeitkorrektur/pending");

        // Neuen Zeitkorrektur-Antrag stellen
        public async Task<bool> CreateZeitkorrektur(Zeitkorrektur k)
        {
            var resp = await PostJson("Zeitkorrektur", k);
            return resp.IsSuccessStatusCode;
        }

        // Zeitkorrektur genehmigen (erstellt auch automatisch eine TimeBooking in der API)
        public async Task<(bool ok, string msg)> ApproveZeitkorrektur(int id)
        {
            var resp = await _http.SendAsync(Req(HttpMethod.Put, $"Zeitkorrektur/{id}/approve"));
            var body = await resp.Content.ReadFromJsonAsync<MessageResponse>();
            return (resp.IsSuccessStatusCode, body?.Message ?? "Fehler");
        }

        // Zeitkorrektur ablehnen
        public async Task<(bool ok, string msg)> RejectZeitkorrektur(int id)
        {
            var resp = await _http.SendAsync(Req(HttpMethod.Put, $"Zeitkorrektur/{id}/reject"));
            var body = await resp.Content.ReadFromJsonAsync<MessageResponse>();
            return (resp.IsSuccessStatusCode, body?.Message ?? "Fehler");
        }

        // ── URLAUBSKONTO ──────────────────────────────────────

        // Eigenes Urlaubskonto laden (für das Dashboard und die Urlaub-Seite)
        // try/catch weil die API 404 zurückgibt wenn noch kein Konto angelegt wurde
        public async Task<Urlaubskonto?> GetMeinUrlaubskonto()
        {
            try { return await GetItem<Urlaubskonto>("Urlaubskonto/mein"); }
            catch { return null; }  // null = kein Konto vorhanden
        }

        // Urlaubskonto eines bestimmten Mitarbeiters laden
        public async Task<Urlaubskonto?> GetUrlaubskontoByMitarbeiter(int mitId, int? jahr = null)
        {
            var url = $"Urlaubskonto/mitarbeiter/{mitId}";
            if (jahr.HasValue) url += $"?jahr={jahr}"; // Optional: anderes Jahr abfragen
            try { return await GetItem<Urlaubskonto>(url); }
            catch { return null; }
        }

        // Alle Urlaubskonten (für HR/Admin-Übersicht)
        public Task<List<Urlaubskonto>> GetAlleUrlaubskonten(int? jahr = null)
        {
            var url = "Urlaubskonto/alle";
            if (jahr.HasValue) url += $"?jahr={jahr}";
            return GetList<Urlaubskonto>(url);
        }

        // Urlaubskonto erstellen oder aktualisieren
        public async Task<bool> SaveUrlaubskonto(Urlaubskonto konto)
        {
            var resp = await PostJson("Urlaubskonto", konto);
            return resp.IsSuccessStatusCode;
        }

        // ── OFFENE BUCHUNGEN ──────────────────────────────────

        // Zeitbuchungen ohne Checkout (Mitarbeiter hat vergessen auszustempeln)
        // Diese werden im Dashboard als Warnung angezeigt
        public async Task<List<TimeBooking>> GetOffeneBuchungen()
        {
            try { return await GetList<TimeBooking>("TimeBooking/offen"); }
            catch { return new(); }  // Bei Fehler: leere Liste zurückgeben
        }

        // ── LOHNABRECHNUNG ────────────────────────────────────

        // Alle Lohnabrechnungen laden (Buchhaltung/Admin)
        public Task<List<Lohnabrechnung>> GetLohnabrechnungen()
            => GetList<Lohnabrechnung>("LohnAbrechnung");

        // Neue Lohnabrechnung erstellen
        public async Task<bool> CreateLohnAbrechnung(Lohnabrechnung abrechnung)
        {
            var resp = await PostJson("LohnAbrechnung", abrechnung);
            return resp.IsSuccessStatusCode;
        }

        // Lohnabrechnung löschen
        public async Task<bool> DeleteLohnAbrechnung(int id)
        {
            var resp = await DeleteReq($"LohnAbrechnung/{id}");
            return resp.IsSuccessStatusCode;
        }

        // ── NFC KARTEN-SCAN (Mitarbeiter-Zuordnung) ───────────

        // Fragt ab, ob seit "after" eine neue Karte am Pi-Leser gescannt wurde.
        // null = noch keine neue Karte (weiter pollen).
        public async Task<string?> GetLatestNfcScan(DateTime after)
        {
            var afterUtc = after.ToUniversalTime().ToString("o");
            var resp = await _http.SendAsync(Req(HttpMethod.Get, $"Nfc/latest?after={afterUtc}"));
            if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("uid", out var u) ? u.GetString() : null;
        }

        //Aktuelle offene Buchung aufrufen (null = nicht eingestempelt)
        public async Task<TimeBooking?> GetAktuelleBuchung()
        {
            try
            {
                var resp = await _http.SendAsync(Req(HttpMethod.Get, "TimeBooking/aktuell"));
                if (!resp.IsSuccessStatusCode) return null;
                var content = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content) || content == "null")
                    return null;
                return System.Text.Json.JsonSerializer.Deserialize<TimeBooking>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        //Einstempeln
        public async Task<bool> CheckIn(int mitarbeiterId)
        {
            var req = Req(HttpMethod.Post, "TimeBooking");
            req.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    fK_MitID = mitarbeiterId,
                    checkInTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    breakMinutes = 0,
                    notes = "Web Check-In"
                }),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }        //Ausstempeln
        public async Task<bool> CheckOut(int timeId, int mitarbeiterId)
        {
            var req = Req(HttpMethod.Put, $"TimeBooking/{timeId}");
            req.Content = JsonContent.Create(new
            {
                TimeID = timeId,
                FK_MitID = mitarbeiterId,
                CheckOutTime = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
    }
}
