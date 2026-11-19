using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MetalERP.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string NombreCompleto { get; set; }

        [Required]
        [MaxLength(50)]
        public string Departamento { get; set; }

        [Required]
        [MaxLength(20)]
        public string NumeroEmpleado { get; set; }
    }
}
