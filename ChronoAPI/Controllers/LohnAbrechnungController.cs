using ChronoAPI.Data;
using ChronoAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LohnAbrechnungController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LohnAbrechnungController(AppDbContext context)
        {
            _context = context;
        }

        // Nur Buchhaltung und Admin sehen alle Abrechnungen
        [HttpGet]
        [Authorize(Roles = "Buchhaltung,Admin")]
        public async Task<ActionResult<IEnumerable<LohnAbrechnung>>> GetAll()
        {
            return await _context.LohnAbrechnungen
                .Include(l => l.Mitarbeiter)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Buchhaltung,Admin")]
        public async Task<ActionResult<LohnAbrechnung>> GetById(int id)
        {
            var lohn = await _context.LohnAbrechnungen
                .Include(l => l.Mitarbeiter)
                .FirstOrDefaultAsync(l => l.LohnID == id);
            if (lohn == null) return NotFound();
            return lohn;
        }

        // Mitarbeiter dürfen ausschliesslich die eigene Abrechnung sehen.
        // Buchhaltung und Admin dürfen jede Abrechnung abrufen.
        [HttpGet("mitarbeiter/{mitId}")]
        [Authorize(Roles = "Buchhaltung,Admin")]
        public async Task<ActionResult<IEnumerable<LohnAbrechnung>>> GetByMitarbeiter(int mitId)
        {
            return await _context.LohnAbrechnungen
                .Where(l => l.FK_MitID == mitId)
                .OrderByDescending(l => l.PeriodStart)
                .ToListAsync();
        }

        // Nur Buchhaltung und Admin erstellen Abrechnungen
        [HttpPost]
        [Authorize(Roles = "Buchhaltung,Admin")]
        public async Task<ActionResult<LohnAbrechnung>> Create(LohnAbrechnung lohn)
        {
            _context.LohnAbrechnungen.Add(lohn);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById),
                new { id = lohn.LohnID }, lohn);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Buchhaltung,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var lohn = await _context.LohnAbrechnungen.FindAsync(id);
            if (lohn == null) return NotFound();
            _context.LohnAbrechnungen.Remove(lohn);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}