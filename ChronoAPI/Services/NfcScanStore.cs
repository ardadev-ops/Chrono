namespace ChronoAPI.Services
{
    // Merkt sich nur die zuletzt vom Pi gescannte NFC-UID (rein transient,
    // keine DB-Tabelle nötig). Dient ausschließlich dem "Karte scannen"-Button
    // im Mitarbeiter-Formular – NICHT dem Check-in/Check-out-Flow.
    public class NfcScanStore
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
        private readonly object _lock = new();
        private string? _uid;
        private DateTime _scannedAtUtc;

        public void Report(string uid)
        {
            lock (_lock)
            {
                _uid = uid;
                _scannedAtUtc = DateTime.UtcNow;
            }
        }

        // Gibt die UID zurück, wenn sie neuer als "after" und noch innerhalb der TTL ist.
        public (string Uid, DateTime ScannedAtUtc)? GetLatestAfter(DateTime after)
        {
            lock (_lock)
            {
                if (_uid == null) return null;
                if (_scannedAtUtc <= after) return null;
                if (DateTime.UtcNow - _scannedAtUtc > Ttl) return null;
                return (_uid, _scannedAtUtc);
            }
        }
    }
}
