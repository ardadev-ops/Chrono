using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoAPI.Models
{
    [Table("tblAbwesenGen")]
    public class AbwesenGen
    {
        [Key]
        public int AbwesenGenID { get; set; }

        public int FK_MitID { get; set; }
        public int? FK_AbwesenID { get; set; }

        [Required]
        [MaxLength(50)]
        public string FK_AbwesenTyp { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Tage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("FK_MitID")]
        public Mitarbeiter? Mitarbeiter { get; set; }

        [ForeignKey("FK_AbwesenID")]
        public Abwesenheit? Abwesenheit { get; set; }
    }
}