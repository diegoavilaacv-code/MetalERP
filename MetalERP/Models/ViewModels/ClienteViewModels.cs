using System.ComponentModel.DataAnnotations;

namespace MetalERP.Models.ViewModels
{
    public class ContactoClienteItemViewModel
    {
        [StringLength(150)]
        public string? Nombre { get; set; }

        [StringLength(50)]
        public string? Cargo { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Correo inválido.")]
        [StringLength(50)]
        public string? Correo { get; set; }
    }

    public class ClienteFormViewModel
    {
        public int ClienteID { get; set; }

        [Required(ErrorMessage = "La razón social o nombre es obligatorio.")]
        [StringLength(50)]
        public string RazonSocial_Nombre { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RFC { get; set; }

        [Required(ErrorMessage = "La clave del cliente es obligatoria.")]
        [StringLength(10)]
        public string Clave { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(255)]
        public string? Correo { get; set; }

        [StringLength(20)]
        public string? TelefonoPrincipal { get; set; }

        [StringLength(50)]
        public string? PaginaWeb { get; set; }

        // Teléfonos adicionales (0 o más)
        public List<string> TelefonosAdicionales { get; set; } = new();

        // Dirección
        [Required(ErrorMessage = "Selecciona un país.")]
        public int? PaisID { get; set; }

        [Required(ErrorMessage = "Selecciona un estado.")]
        public int? EntidadFederativa { get; set; }

        [Required(ErrorMessage = "Selecciona un municipio.")]
        public int? Municipio { get; set; }

        [StringLength(100)]
        public string? ColoniaLocalidad { get; set; }

        [StringLength(100)]
        public string? Calle { get; set; }

        [StringLength(20)]
        public string? NumeroExterior { get; set; }

        [StringLength(20)]
        public string? NumeroInterior { get; set; }

        [StringLength(50)]
        public string? CodigoPostal { get; set; }

        [StringLength(50)]
        public string? Referencia { get; set; }

        // Contactos comerciales (0 o más)
        public List<ContactoClienteItemViewModel> Contactos { get; set; } = new();
    }
}
