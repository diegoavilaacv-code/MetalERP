using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int UsuarioID { get; set; }

        public int? EmpleadoID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        public Guid? Salt { get; set; }

        public bool? Activo { get; set; }

        [ForeignKey("EmpleadoID")]
        public virtual Empleado? Empleado { get; set; }
    }
}
