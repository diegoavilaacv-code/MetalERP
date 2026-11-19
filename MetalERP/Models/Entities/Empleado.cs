using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("Empleados")]
    public class Empleado
    {
        [Key]
        public int EmpleadoID { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ApellidoMaterno { get; set; }

        [StringLength(60)]
        public string? Correo { get; set; }

        public int? CargoID { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaContratacion { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; }

        [NotMapped]
        public string NombreCompleto => $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim();
    }
}
