using System.Security.Claims;

namespace ChronoAPI.Extensions
{
    /// <summary>
    /// Hilfsmethoden zum Auslesen der Benutzerinformationen aus dem JWT-Token.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Mitarbeiter-ID des angemeldeten Benutzers.</summary>
        public static int GetMitarbeiterId(this ClaimsPrincipal user)
        {
            var wert = user.FindFirst("MitarbeiterID")?.Value;
            return int.TryParse(wert, out var id) ? id : 0;
        }

        /// <summary>Abteilungs-ID des angemeldeten Benutzers. Null wenn keiner zugeordnet.</summary>
        public static int? GetAbteilungsId(this ClaimsPrincipal user)
        {
            var wert = user.FindFirst("AbteilungsID")?.Value;
            if (int.TryParse(wert, out var id) && id > 0) return id;
            return null;
        }

        /// <summary>Rolle des angemeldeten Benutzers.</summary>
        public static string GetRolle(this ClaimsPrincipal user)
            => user.FindFirst("role")?.Value ?? string.Empty;

        /// <summary>
        /// True wenn der Benutzer alle Abteilungen einsehen darf.
        /// HR, Buchhaltung und Admin haben unternehmensweiten Zugriff.
        /// </summary>
        public static bool DarfAlleAbteilungenSehen(this ClaimsPrincipal user)
        {
            var rolle = user.GetRolle();
            return rolle == "HR" || rolle == "Admin" || rolle == "Buchhaltung";
        }

        /// <summary>True wenn der Benutzer nur seine eigene Abteilung sehen darf.</summary>
        public static bool IstAufAbteilungBeschraenkt(this ClaimsPrincipal user)
            => user.GetRolle() == "Abteilungsleiter";
    }
}
