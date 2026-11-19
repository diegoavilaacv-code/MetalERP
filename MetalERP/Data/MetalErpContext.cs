using MetalERP.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalERP.Data
{
    public class MetalErpContext : DbContext
    {
        public MetalErpContext(DbContextOptions<MetalErpContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Privilegio> Privilegios { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<ContactoEmpleado> ContactosEmpleados { get; set; }
        public DbSet<DireccionEmpleado> DireccionesEmpleados { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Pais> Paises { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Municipio> Municipios { get; set; }
        public DbSet<TelefonoEmpleado> TelefonosEmpleados { get; set; }
        public DbSet<ExpedienteEmpleado> ExpedientesEmpleados { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<TelefonoCliente> TelefonosClientes { get; set; }
        public DbSet<DireccionCliente> DireccionesClientes { get; set; }
        public DbSet<ContactoCliente> ContactosClientes { get; set; }
    }
}
