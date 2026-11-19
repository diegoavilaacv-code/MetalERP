using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalERP.Models.Entities
{
    [Table("Clientes")]
    public class Cliente
    {
        [Key]
        public int ClienteID { get; set; }

        [Required]
        [StringLength(50)]
        public string RazonSocial_Nombre { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RFC { get; set; }

        [Required]
        [StringLength(10)]
        public string Clave { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Correo { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(50)]
        public string? PaginaWeb { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; }
    }

    [Table("TelefonosClientes")]
    public class TelefonoCliente
    {
        [Key]
        public int TelefonoID { get; set; }
        public int ClienteID { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        public bool Activo { get; set; } // En esta tabla es bit NOT NULL
    }

    [Table("DireccionesClientes")]
    public class DireccionCliente
    {
        [Key]
        public int DireccionID { get; set; }
        public int ClienteID { get; set; }
        public int PaisID { get; set; }
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

        [StringLength(50)]
        public string? CodigoPostal { get; set; }

        [StringLength(50)]
        public string? Referencia { get; set; }

        public bool Activo { get; set; }
    }

    [Table("ContactosClientes")]
    public class ContactoCliente
    {
        [Key]
        public int ContactoID { get; set; }
        public int ClienteID { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Cargo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Correo { get; set; }

        public bool Activo { get; set; }
    }
}
