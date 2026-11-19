using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("TelefonosEmpleados")]
    public class TelefonoEmpleado
    {
        [Key]
        public int TelefonoID { get; set; }

        public int EmpleadoID { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        public int Activo { get; set; } // Tipo int NOT NULL según tu tabla SQL
    }
}
