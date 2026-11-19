using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("Areas")]
    public class Area
    {
        [Key]
        public int AreaID { get; set; }
        public string NombreArea { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    [Table("Cargos")]
    public class Cargo
    {
        [Key]
        public int CargoID { get; set; }
        public int AreaID { get; set; }
        public string NombreCargo { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    [Table("Paises")]
    public class Pais
    {
        [Key]
        public int PaisID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    [Table("Estados")]
    public class Estado
    {
        [Key]
        public int EstadoID { get; set; }
        public int PaisID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    [Table("Municipios")]
    public class Municipio
    {
        [Key]
        public int MunicipioID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int EstadoID { get; set; }
        public bool? Activo { get; set; }
    }
}
