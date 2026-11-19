using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("DireccionesEmpleados")]
    public class DireccionEmpleado
    {
        [Key]
        public int DireccionID { get; set; }

        public int EmpleadoID { get; set; }

        public int EntidadFederativa { get; set; }

        public int Municipio { get; set; }

        [StringLength(100)]
        public string? ColoniaLocalidad { get; set; }

        [StringLength(100)]
        public string? Calle { get; set; }

        [StringLength(20)]
        public string? NumeroExterior { get; set; }

        [StringLength(20)]
        public string? NumeroInterior { get; set; }

        [StringLength(5)]
        public string? CodigoPostal { get; set; }

        [StringLength(20)]
        public string? Referencia { get; set; }

        public bool Activo { get; set; }
    }
}
