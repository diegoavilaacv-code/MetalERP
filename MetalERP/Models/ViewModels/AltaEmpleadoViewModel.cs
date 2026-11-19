using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MetalERP.Models.ViewModels
{
    public class ContactoItemViewModel
    {
        [StringLength(150)]
        public string? Nombre { get; set; }

        [StringLength(50)]
        public string? Parentesco { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Correo inválido.")]
        [StringLength(50)]
        public string? Correo { get; set; }
    }

    public class AltaEmpleadoViewModel
    {
        // --- 1. DATOS DEL EMPLEADO ---
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
        [StringLength(50)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ApellidoMaterno { get; set; }

        [EmailAddress(ErrorMessage = "Correo inválido.")]
        [StringLength(60)]
        public string? CorreoEmpleado { get; set; }

        [Required(ErrorMessage = "Selecciona un área.")]
        public int? AreaID { get; set; }

        [Required(ErrorMessage = "Selecciona un cargo.")]
        public int? CargoID { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaContratacion { get; set; } = DateTime.Today;

        // Inicia vacía (0 teléfonos por defecto)
        public List<string> Telefonos { get; set; } = new List<string>();

        // --- 2. DIRECCIÓN DEL EMPLEADO ---
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

        [StringLength(5, MinimumLength = 5, ErrorMessage = "Debe tener 5 dígitos.")]
        public string? CodigoPostal { get; set; }

        [StringLength(20)]
        public string? Referencia { get; set; }

        // --- 3. CONTACTOS DE EMERGENCIA ---
        public List<ContactoItemViewModel> Contactos { get; set; } = new List<ContactoItemViewModel>();

        // --- 4. ARCHIVOS DEL EXPEDIENTE ELECTRÓNICO ---
        public IFormFile? DocSolicitudEmpleo { get; set; }
        public IFormFile? DocActaNacimiento { get; set; }
        public IFormFile? DocINE { get; set; }
        public IFormFile? DocCURP { get; set; }
        public IFormFile? DocComprobanteEstudios { get; set; }
        public IFormFile? DocComprobanteDomicilio { get; set; }
        public IFormFile? DocConstanciaIMSS { get; set; }
        public IFormFile? DocContratoConfidencialidad { get; set; }
        public IFormFile? DocContratoVinculacion { get; set; }
        public IFormFile? DocReglamentoFirmado { get; set; }
        public IFormFile? DocFoto { get; set; }
    }
}