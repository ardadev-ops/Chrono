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
    public class AbteilungController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AbteilungController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Abteilung
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Abteilung>>> GetAll()
        {
            try
            {
                return await _context.Abteilungen.ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Datenbankfehler!", details = ex.Message });
            }
        }

        // GET: api/Abteilung/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Abteilung>> GetById(int id)
        {
            var abteilung = await _context.Abteilungen.FindAsync(id);
            if (abteilung == null) return NotFound();
            return abteilung;
        }

        // POST: api/Abteilung
        [HttpPost]
        [Authorize(Roles = "HR,Admin")]
        public async Task<ActionResult<Abteilung>> Create(Abteilung abteilung)
        {
            _context.Abteilungen.Add(abteilung);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = abteilung.AbtID }, abteilung);
        }

        // PUT: api/Abteilung/5
        [HttpPut("{id}")]
        [Authorize(Roles = "HR,Admin")]
        public async Task<IActionResult> Update(int id, Abteilung abteilung)
        {
            if (id != abteilung.AbtID) return BadRequest();

            abteilung.UpdatedAt = DateTime.Now;
            _context.Entry(abteilung).State = EntityState.Modified;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Abteilungen.Any(e => e.AbtID == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Abteilung/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var abteilung = await _context.Abteilungen.FindAsync(id);
            if (abteilung == null) return NotFound();

            _context.Abteilungen.Remove(abteilung);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
