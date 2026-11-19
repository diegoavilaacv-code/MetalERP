namespace MetalERP.Models.ViewModels
{
    public class ConsultaClienteViewModel
    {
        public int ClienteID { get; set; }
        public string Clave { get; set; } = string.Empty;
        public string RazonSocial_Nombre { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string PaginaWeb { get; set; } = string.Empty;
        public string UbicacionResumen { get; set; } = string.Empty;
        public int TotalContactos { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
    }
}
