using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("ExpedientesEmpleados")]
    public class ExpedienteEmpleado
    {
        [Key]
        public int ExpedienteID { get; set; }
        public int EmpleadoID { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; }
        public bool Activo { get; set; }
    }
}
