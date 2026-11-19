using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("ContactosEmpleados")]
    public class ContactoEmpleado
    {
        [Key]
        public int ContactoID { get; set; }

        public int EmpleadoID { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Parentesco { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Correo { get; set; }

        public bool Activo { get; set; }
    }
}
