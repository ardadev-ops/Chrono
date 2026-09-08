namespace ChronoAPI.Services
{
    public static class ZeitBerechnung
    {
        public static int BerechneArbeitstage(DateTime von, DateTime bis)
        {
            if (bis < von) return 0;

            int tage = 0;
            for (var d = von.Date; d <= bis.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Saturday &&
                    d.DayOfWeek != DayOfWeek.Sunday)
                {
                    tage++;
                }
            }
            return tage;
        }

        public static double BerechneArbeitsstunden(
            DateTime checkIn, DateTime? checkOut, int pauseMinuten)
        {
            if (checkOut == null) return 0;
            if (checkOut < checkIn) return 0;

            var dauer = checkOut.Value - checkIn;
            var netto = dauer.TotalMinutes - pauseMinuten;

            if (netto < 0) return 0;
            return Math.Round(netto / 60.0, 2);
        }

        public static double BerechneUeberstunden(double istStunden, double sollStunden = 8.0)
        {
            var diff = istStunden - sollStunden;
            return diff > 0 ? Math.Round(diff, 2) : 0;
        }

        public static bool UrlaubReichtAus(int gesamtTage, double genommenTage, int beantragteTage)
        {
            return (gesamtTage - genommenTage) >= beantragteTage;
        }

        public static bool IstUeberMaximaldauer(DateTime checkIn, DateTime jetzt, int maxStunden = 10)
        {
            return (jetzt - checkIn).TotalHours >= maxStunden;
        }
    }
}