using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("Privilegios")]
    public class Privilegio
    {
        [Key]
        public int PrivilegioID { get; set; }

        public int? CargoID { get; set; }

        public int? ModuloID { get; set; }

        public bool? PuedeVer { get; set; }

        public bool? PuedeCrear { get; set; }

        public bool? PuedeEditar { get; set; }

        public bool? PuedeEliminar { get; set; }

        // Propiedad visual para desplegables (no busca columna en SQL Server)
        [NotMapped]
        public string DescripcionVisual => $"Privilegio #{PrivilegioID} (Cargo: {CargoID ?? 0} | Módulo: {ModuloID ?? 0})";
    }
}
