namespace MetalERP.Models.ViewModels
{
    public class ConsultaEmpleadoViewModel
    {
        public int EmpleadoID { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string FechaContratacion { get; set; } = string.Empty;
        public string TelefonosConcatenados { get; set; } = string.Empty;

        // Datos del Domicilio (para la 1ra subtabla)
        public string Pais { get; set; } = "-";
        public string Estado { get; set; } = "-";
        public string Municipio { get; set; } = "-";
        public string Colonia { get; set; } = "-";
        public string CalleYNumeros { get; set; } = "-";
        public string CodigoPostal { get; set; } = "-";
        public string Referencia { get; set; } = "-";

        // Lista de Contactos de Emergencia (para la 2da subtabla)
        public List<ContactoItemViewModel> Contactos { get; set; } = new List<ContactoItemViewModel>();

        public List<MetalERP.Models.Entities.ExpedienteEmpleado> Documentos { get; set; } = new();
    }
}
