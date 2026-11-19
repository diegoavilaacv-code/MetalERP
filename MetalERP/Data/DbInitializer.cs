using MetalERP.Models;
using Microsoft.AspNetCore.Identity;

namespace MetalERP.Data
{
    public static class DbInitializer
    {
        public static async Task InicializarAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Definir los puestos de trabajo (Roles)
            string[] roles = { "Administrador", "Gerente", "Operador" };

            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            // 2. Crear un usuario Administrador inicial
            string emailAdmin = "admin@tuempresa.com";
            if (await userManager.FindByEmailAsync(emailAdmin) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = emailAdmin,
                    Email = emailAdmin,
                    NombreCompleto = "Administrador del Sistema",
                    Departamento = "TI / Sistemas",
                    NumeroEmpleado = "EMP-001",
                    EmailConfirmed = true
                };

                var resultado = await userManager.CreateAsync(admin, "Admin123*");
                if (resultado.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrador");
                }
            }
        }
    }
}
