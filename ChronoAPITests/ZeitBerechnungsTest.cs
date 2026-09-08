using ChronoAPI.Services;
using Xunit;

namespace ChronoAPI.Tests
{
    public class ZeitBerechnungTests
    {
        // ---------- Arbeitstage ----------

        [Fact]
        public void Arbeitstage_MontagBisFreitag_Gibt5()
        {
            var result = ZeitBerechnung.BerechneArbeitstage(
                new DateTime(2026, 8, 3), new DateTime(2026, 8, 7));
            Assert.Equal(5, result);
        }

        [Fact]
        public void Arbeitstage_MitWochenende_ZaehltNurWerktage()
        {
            var result = ZeitBerechnung.BerechneArbeitstage(
                new DateTime(2026, 8, 7), new DateTime(2026, 8, 13));
            Assert.Equal(5, result);
        }

        [Fact]
        public void Arbeitstage_NurSamstag_Gibt0()
        {
            var result = ZeitBerechnung.BerechneArbeitstage(
                new DateTime(2026, 8, 8), new DateTime(2026, 8, 8));
            Assert.Equal(0, result);
        }

        [Fact]
        public void Arbeitstage_EinzelnerWerktag_Gibt1()
        {
            var result = ZeitBerechnung.BerechneArbeitstage(
                new DateTime(2026, 8, 5), new DateTime(2026, 8, 5));
            Assert.Equal(1, result);
        }

        [Fact]
        public void Arbeitstage_EndeVorBeginn_Gibt0()
        {
            var result = ZeitBerechnung.BerechneArbeitstage(
                new DateTime(2026, 8, 10), new DateTime(2026, 8, 5));
            Assert.Equal(0, result);
        }

        [Fact]
        public void Arbeitstage_GanzeWoche_Gibt5()
        {
            var result = ZeitBerechnung.BerechneArbeitstage(
                new DateTime(2026, 8, 3), new DateTime(2026, 8, 9));
            Assert.Equal(5, result);
        }

        // ---------- Arbeitsstunden ----------

        [Fact]
        public void Arbeitsstunden_AchtStundenOhnePause_Gibt8()
        {
            var result = ZeitBerechnung.BerechneArbeitsstunden(
                new DateTime(2026, 8, 5, 8, 0, 0),
                new DateTime(2026, 8, 5, 16, 0, 0), 0);
            Assert.Equal(8.0, result);
        }

        [Fact]
        public void Arbeitsstunden_MitDreissigMinutenPause_Gibt7Komma5()
        {
            var result = ZeitBerechnung.BerechneArbeitsstunden(
                new DateTime(2026, 8, 5, 8, 0, 0),
                new DateTime(2026, 8, 5, 16, 0, 0), 30);
            Assert.Equal(7.5, result);
        }

        [Fact]
        public void Arbeitsstunden_OhneCheckOut_Gibt0()
        {
            var result = ZeitBerechnung.BerechneArbeitsstunden(
                new DateTime(2026, 8, 5, 8, 0, 0), null, 0);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Arbeitsstunden_CheckOutVorCheckIn_Gibt0()
        {
            var result = ZeitBerechnung.BerechneArbeitsstunden(
                new DateTime(2026, 8, 5, 16, 0, 0),
                new DateTime(2026, 8, 5, 8, 0, 0), 0);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Arbeitsstunden_PauseLaengerAlsArbeitszeit_Gibt0()
        {
            var result = ZeitBerechnung.BerechneArbeitsstunden(
                new DateTime(2026, 8, 5, 8, 0, 0),
                new DateTime(2026, 8, 5, 9, 0, 0), 120);
            Assert.Equal(0, result);
        }

        [Fact]
        public void Arbeitsstunden_UeberMitternacht_RechnetKorrekt()
        {
            var result = ZeitBerechnung.BerechneArbeitsstunden(
                new DateTime(2026, 8, 5, 22, 0, 0),
                new DateTime(2026, 8, 6, 6, 0, 0), 0);
            Assert.Equal(8.0, result);
        }

        // ---------- Überstunden ----------

        [Fact]
        public void Ueberstunden_NeunStunden_Gibt1()
        {
            Assert.Equal(1.0, ZeitBerechnung.BerechneUeberstunden(9.0));
        }

        [Fact]
        public void Ueberstunden_AchtStunden_Gibt0()
        {
            Assert.Equal(0, ZeitBerechnung.BerechneUeberstunden(8.0));
        }

        [Fact]
        public void Ueberstunden_SechsStunden_Gibt0()
        {
            Assert.Equal(0, ZeitBerechnung.BerechneUeberstunden(6.0));
        }

        // ---------- Urlaubskonto ----------

        [Fact]
        public void Urlaub_GenugTage_GibtTrue()
        {
            Assert.True(ZeitBerechnung.UrlaubReichtAus(25, 10, 5));
        }

        [Fact]
        public void Urlaub_ZuWenigTage_GibtFalse()
        {
            Assert.False(ZeitBerechnung.UrlaubReichtAus(25, 20, 10));
        }

        [Fact]
        public void Urlaub_GenauAufgebraucht_GibtTrue()
        {
            Assert.True(ZeitBerechnung.UrlaubReichtAus(25, 20, 5));
        }

        // ---------- Auto-Checkout ----------

        [Fact]
        public void AutoCheckout_ElfStunden_GibtTrue()
        {
            var result = ZeitBerechnung.IstUeberMaximaldauer(
                new DateTime(2026, 8, 5, 8, 0, 0),
                new DateTime(2026, 8, 5, 19, 0, 0));
            Assert.True(result);
        }

        [Fact]
        public void AutoCheckout_AchtStunden_GibtFalse()
        {
            var result = ZeitBerechnung.IstUeberMaximaldauer(
                new DateTime(2026, 8, 5, 8, 0, 0),
                new DateTime(2026, 8, 5, 16, 0, 0));
            Assert.False(result);
        }
    }
}