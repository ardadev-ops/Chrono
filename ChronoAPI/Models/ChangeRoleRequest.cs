namespace ChronoAPI.Models
{
    /// <summary>
    /// Anfrage zur Aenderung der Rolle eines bestehenden Mitarbeiters.
    /// </summary>
    public class ChangeRoleRequest
    {
        public int MitarbeiterId { get; set; }
        public string NeueRolle { get; set; } = string.Empty;
    }
}
