// ============================================================
//  AppState.cs  (Login-Zustand des eingeloggten Users)
// ============================================================
//
//  AppState ist ein "zentraler Speicher" für alle Login-Infos.
//  Alle Razor-Seiten können auf AppState zugreifen und wissen so:
//   - Ist jemand eingeloggt?
//   - Wer ist eingeloggt? (Name, Rolle, MitarbeiterID)
//   - Was darf er sehen/tun? (CanApprove, IsAdmin, ...)
//
//  WICHTIGES KONZEPT – JWT-Token lesen:
//  Nach dem Login speichern wir den JWT-Token im Browser (LocalStorage).
//  AppState liest diesen Token aus und extrahiert die Informationen
//  daraus (Name, Rolle, MitarbeiterID) ohne die API zu fragen.
//  Das geht weil im Token alles drin steht!
//
//  WICHTIGES KONZEPT – OnChange-Event:
//  Wenn sich der Login-Zustand ändert (Login/Logout), benachrichtigt
//  AppState alle Komponenten die zuhören (Event-basiert).
//  So aktualisiert sich z.B. die Seite sofort wenn man sich ausloggt.
// ============================================================

using Blazored.LocalStorage;
using System.IdentityModel.Tokens.Jwt;

namespace ChronoWeb.Services
{
    public class AppState
    {
        // Zugriff auf den Browser-LocalStorage (für Token-Speicherung)
        private readonly ILocalStorageService _localStorage;
        // Zugriff auf den ApiService (für das Token-Setzen im HTTP-Client)
        private readonly ApiService _api;

        // ── LOGIN-INFORMATIONEN (nur lesbar von außen) ────────
        // "private set" = kann nur innerhalb dieser Klasse geändert werden.
        // Von außen kann man nur lesen, nicht schreiben.

        public bool IsLoggedIn { get; private set; }       // Ist der User eingeloggt?
        public string UserName { get; private set; } = string.Empty;   // "anna.mueller"
        public string VollName { get; private set; } = string.Empty;   // "Anna Müller"
        public string Role { get; private set; } = string.Empty;       // "HR", "Admin", ...
        public int MitarbeiterID { get; private set; }                  // Numerische ID des Mitarbeiters
        public int? AbteilungsID { get; private set; }                  // Abteilungs-ID (null = keiner zugeordnet)
        public int UserID { get; private set; }                         // Numerische ID des Users
        public bool MustChangePassword { get; private set; }            // Temporäres Passwort?

        // Event: wird aufgerufen wenn sich der Login-Zustand ändert.
        // ? = optional (kann null sein wenn niemand zuhört)
        // Action = eine Methode die keine Parameter und keinen Rückgabewert hat
        public event Action? OnChange;

        // ── BERECHTIGUNGS-EIGENSCHAFTEN ───────────────────────
        // Praktische Abkürzungen um Rollen zu prüfen.
        // Statt überall "AppState.Role == 'HR'" zu schreiben, einfach "AppState.IsHR".
        // => = Kurzschreibweise für "return ..."

        public bool IsMitarbeiter    => Role == "Mitarbeiter";
        public bool IsAbteilungsleiter => Role == "Abteilungsleiter";
        public bool IsHR             => Role == "HR";
        public bool IsBuchhaltung    => Role == "Buchhaltung";
        public bool IsAdmin          => Role == "Admin";

        // Darf dieser User Anträge genehmigen?
        // (Abteilungsleiter, HR oder Admin = ja)
        public bool CanApprove => IsAbteilungsleiter || IsHR || IsAdmin;

        // Konstruktor: ILocalStorageService und ApiService werden
        // automatisch vom Framework eingefügt (Dependency Injection)
        public AppState(ILocalStorageService localStorage, ApiService api)
        {
            _localStorage = localStorage;
            _api = api;
        }

        // ── BEIM SEITENAUFRUF: GESPEICHERTEN TOKEN LADEN ──────
        // Diese Methode wird von jeder Seite beim ersten Aufruf
        // (OnAfterRenderAsync) ausgeführt.
        //
        // WARUM OnAfterRender und nicht OnInitialized?
        // LocalStorage ist ein Browser-Feature. Beim ersten Server-Rendering
        // (Prerendering) gibt es noch keinen Browser → kein LocalStorage.
        // Erst nach dem ersten Render ist der Browser verbunden.
        public async Task InitializeAsync()
        {
            // Wenn schon eingeloggt → nichts tun
            if (IsLoggedIn) return;

            try
            {
                // Gespeicherten Token aus dem Browser holen
                var savedToken = await _localStorage.GetItemAsync<string>("chrono_token");
                if (!string.IsNullOrEmpty(savedToken))
                {
                    // Token vorhanden → User ist noch eingeloggt (Token noch gültig)
                    var savedVollName = await _localStorage.GetItemAsync<string>("chrono_vollname") ?? string.Empty;

                    // Token auslesen und alle Felder befüllen
                    ParseAndSetToken(savedToken, savedVollName);

                    // Token auch im ApiService setzen (für HTTP-Anfragen)
                    _api.SetToken(savedToken);

                    // Alle Komponenten benachrichtigen
                    NotifyStateChanged();
                }
            }
            catch
            {
                // LocalStorage noch nicht verfügbar (Prerendering-Phase)
                // Fehler einfach ignorieren – wird beim nächsten Aufruf nochmal versucht
            }
        }

        // ── EINLOGGEN ─────────────────────────────────────────
        // Wird von Login.razor aufgerufen nachdem die API den Token zurückgegeben hat
        public async Task LoginAsync(string token, string vollName)
        {
            // Token auslesen und alle Felder befüllen
            ParseAndSetToken(token, vollName);

            // Token im ApiService registrieren (für alle weiteren API-Anfragen)
            _api.SetToken(token);

            // Token im Browser speichern (bleibt auch nach Browser-Neustart erhalten)
            await _localStorage.SetItemAsync("chrono_token", token);
            await _localStorage.SetItemAsync("chrono_vollname", vollName);

            // Alle Komponenten über den neuen Zustand informieren
            NotifyStateChanged();
        }

        // ── AUSLOGGEN ─────────────────────────────────────────
        // Alle gespeicherten Daten löschen
        public async Task LogoutAsync()
        {
            // Alle Login-Infos zurücksetzen
            IsLoggedIn = false;
            UserName = string.Empty;
            VollName = string.Empty;
            Role = string.Empty;
            MitarbeiterID = 0;
            AbteilungsID = null;
            UserID = 0;
            MustChangePassword = false;

            // Token aus dem ApiService entfernen (keine weiteren API-Anfragen möglich)
            _api.ClearToken();

            try
            {
                // Token aus dem Browser-LocalStorage löschen
                await _localStorage.RemoveItemAsync("chrono_token");
                await _localStorage.RemoveItemAsync("chrono_vollname");
            }
            catch { } // Fehler ignorieren (z.B. wenn LocalStorage nicht verfügbar)

            // Alle Komponenten informieren (z.B. Header wird aktualisiert)
            NotifyStateChanged();
        }

        // ── PRIVAT: TOKEN AUSLESEN ────────────────────────────
        // Liest den JWT-Token und extrahiert alle Informationen daraus.
        // JWT besteht aus 3 Teilen: Header.Payload.Signatur
        // Der Payload enthält die "Claims" (Informationen über den User).
        private void ParseAndSetToken(string token, string vollName)
        {
            IsLoggedIn = true;
            VollName = vollName;

            // JwtSecurityTokenHandler kann den Token-String lesen und analysieren
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token); // Token-String → Objekt mit Claims

            // Claims auslesen (Claims sind die Schlüssel-Wert-Paare im Token)
            UserName = jwt.Claims
                .FirstOrDefault(c => c.Type == "unique_name" || c.Type == "name")?.Value
                ?? string.Empty;

            Role = jwt.Claims
                .FirstOrDefault(c => c.Type == "role" || c.Type.Contains("role"))?.Value
                ?? string.Empty;

            // MitarbeiterID aus dem Token lesen und zu int konvertieren
            var mitIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "MitarbeiterID");
            MitarbeiterID = mitIdClaim != null ? int.Parse(mitIdClaim.Value) : 0;

            // AbteilungsID aus dem Token lesen (0 oder fehlend = keiner Abteilung zugeordnet)
            var abtClaim = jwt.Claims.FirstOrDefault(c => c.Type == "AbteilungsID")?.Value;
            AbteilungsID = (int.TryParse(abtClaim, out var abtId) && abtId > 0) ? abtId : null;

            // UserID (nameid-Claim) lesen
            var userIdClaim = jwt.Claims
                .FirstOrDefault(c => c.Type == "nameid" || c.Type.Contains("nameidentifier"));
            UserID = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            // Muss Passwort geändert werden? Claim ist ein String "true"/"false"
            var mustChange = jwt.Claims.FirstOrDefault(c => c.Type == "MustChangePassword")?.Value;
            MustChangePassword = mustChange == "true";
        }

        // ── PRIVAT: KOMPONENTEN BENACHRICHTIGEN ───────────────
        // Ruft das OnChange-Event auf → alle Abonnenten werden informiert.
        // ?. = "Null-sicherer Aufruf": nur aufrufen wenn OnChange nicht null ist
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
