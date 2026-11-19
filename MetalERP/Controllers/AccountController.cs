using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalERP.Data;
using MetalERP.Models.ViewModels;
using MetalERP.Services;

namespace MetalERP.Controllers
{
    public class AccountController : Controller
    {
        private readonly MetalErpContext _context;

        public AccountController(MetalErpContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1. Buscar usuario incluyendo Privilegio y Empleado
            var usuario = await _context.Usuarios
                .Include(u => u.Empleado)
                .FirstOrDefaultAsync(u => u.Username == model.Usuario && u.Activo == true);

            if (usuario == null || !SeguridadService.VerificarPassword(model.Password, usuario.PasswordHash, usuario.Salt))
            {
                ViewBag.Error = "Credenciales inválidas o cuenta inactiva.";
                return View(model);
            }

            // 2. Obtener el nombre completo (si tiene empleado asignado) o usar el Username
            string nombreAMostrar = !string.IsNullOrWhiteSpace(usuario.Empleado?.NombreCompleto)
                ? usuario.Empleado.NombreCompleto
                : usuario.Username;

            // 3. Guardar en los Claims de la Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioID.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim("NombreCompleto", nombreAMostrar),
                new Claim("EmpleadoID", usuario.EmpleadoID?.ToString() ?? "0"),
                new Claim(ClaimTypes.Role, "Usuario") // Más adelante lo vinculamos con el CargoID del Empleado
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}