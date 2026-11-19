using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetalERP.Controllers
{
    [Authorize] // Nadie entra sin iniciar sesión; si lo intentan, los manda al Login automáticamente
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Supervisor")] // Solo estos roles pueden entrar a esta pantalla
        public IActionResult ConfiguracionPlanta()
        {
            return View();
        }
    }
}
