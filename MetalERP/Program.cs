using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MetalERP.Data;

namespace MetalERP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Agregar controladores y vistas MVC
            builder.Services.AddControllersWithViews();

            // 2. Conectar con tu base de datos SQL Server existente
            builder.Services.AddDbContext<MetalErpContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 3. Configurar Autenticación por Cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";             // A dónde redirigir si no ha iniciado sesión
                    options.LogoutPath = "/Account/Logout";           // Ruta para cerrar sesión
                    options.AccessDeniedPath = "/Account/Denied";     // Si intenta entrar a un área sin permiso
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);   // Duración de turno laboral (8 horas)
                    options.SlidingExpiration = true;                 // Renueva el tiempo si el usuario sigue activo
                    options.Cookie.HttpOnly = true;                   // Protege la cookie contra robo por JavaScript (XSS)
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Solo viaja por HTTPS
                });

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var app = builder.Build();

            // Configuración del pipeline HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 4. IMPORTANTE: El orden debe ser exactamente Authentication primero, luego Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
