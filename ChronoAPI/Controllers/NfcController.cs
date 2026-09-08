// ============================================================
//  NfcController.cs  (Karten-Scan für Mitarbeiter-Zuordnung)
// ============================================================
//
//  Getrennt vom Check-in/Check-out-Flow (TimeBookingController.NfcBooking)!
//  Dieser Controller dient nur dazu, dass HR/Admin im Mitarbeiter-Formular
//  auf "Karte scannen" klicken kann und die UID automatisch übernommen wird,
//  statt sie händisch vom Pi-Terminal abzutippen.
//
//  Ablauf: Pi liest Karte -> POST hierher -> Web-Formular pollt und holt sie ab.
// ============================================================

using ChronoAPI.Models;
using ChronoAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChronoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NfcController : ControllerBase
    {
        private readonly NfcScanStore _store;

        public NfcController(NfcScanStore store)
        {
            _store = store;
        }

        // POST api/Nfc/scan
        // Wird vom Raspberry Pi (nfc_scan.py) für JEDE neu gelesene Karte aufgerufen.
        [HttpPost("scan")]
        [AllowAnonymous]
        public IActionResult Scan([FromBody] NfcRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Uid))
                return BadRequest(new { message = "Keine Karten-ID uebermittelt" });

            _store.Report(request.Uid);
            return Ok();
        }

        // GET api/Nfc/latest?after=2026-08-23T10:00:00Z
        // Wird vom Mitarbeiter-Formular gepollt, waehrend "Karte scannen" aktiv ist.
        [HttpGet("latest")]
        [Authorize(Roles = "HR,Admin")]
        public IActionResult Latest([FromQuery] DateTime after)
        {
            var result = _store.GetLatestAfter(after.ToUniversalTime());
            if (result == null) return NoContent();

            return Ok(new { uid = result.Value.Uid, scannedAtUtc = result.Value.ScannedAtUtc });
        }
    }
}
