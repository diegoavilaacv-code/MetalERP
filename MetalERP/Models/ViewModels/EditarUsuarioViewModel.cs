using System.ComponentModel.DataAnnotations;

namespace MetalERP.Models.ViewModels
{
    public class EditarUsuarioViewModel
    {
        [Required(ErrorMessage = "Selecciona un usuario para editar.")]
        public int UsuarioID { get; set; }

        public string NombreEmpleado { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string? NuevaPassword { get; set; }

        [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string? ConfirmarPassword { get; set; }

    }
}
