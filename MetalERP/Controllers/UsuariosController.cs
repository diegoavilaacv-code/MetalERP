using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MetalERP.Data;
using MetalERP.Models.Entities;
using MetalERP.Models.ViewModels;
using MetalERP.Services;

namespace MetalERP.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly MetalErpContext _context;

        public UsuariosController(MetalErpContext context)
        {
            _context = context;
        }

        // GET: /Usuarios
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.Username)
                .ToListAsync();

            return View(usuarios);
        }

        // GET: /Usuarios/Crear
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarListasDesplegables();
            return View(new CrearUsuarioViewModel());
        }

        // POST: /Usuarios/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasDesplegables();
                return View(model);
            }

            // 1. Validar que el Username no exista (evita error de llave UNIQUE en SQL)
            bool existeUsuario = await _context.Usuarios.AnyAsync(u => u.Username == model.Username);
            if (existeUsuario)
            {
                ModelState.AddModelError("Username", "Este nombre de usuario ya está en uso.");
                await CargarListasDesplegables();
                return View(model);
            }

            // 2. Validar que el EmpleadoID no tenga ya un usuario asignado
            if (model.EmpleadoID.HasValue)
            {
                bool empleadoAsignado = await _context.Usuarios.AnyAsync(u => u.EmpleadoID == model.EmpleadoID);
                if (empleadoAsignado)
                {
                    ModelState.AddModelError("EmpleadoID", "Este empleado ya tiene una cuenta de usuario asignada.");
                    await CargarListasDesplegables();
                    return View(model);
                }
            }

            // 3. Generar Salt único y Hash SHA-512 de 64 bytes
            Guid nuevoSalt = Guid.NewGuid();
            byte[] passwordHash = SeguridadService.GenerarHashSha512(model.Password, nuevoSalt);

            var nuevoUsuario = new Usuario
            {
                Username = model.Username.Trim(),
                PasswordHash = passwordHash,
                Salt = nuevoSalt,
                EmpleadoID = model.EmpleadoID,
                Activo = true
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"Usuario '{nuevoUsuario.Username}' creado correctamente.";
            return RedirectToAction(nameof(Crear));
        }

        // POST: /Usuarios/CambiarEstado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                usuario.Activo = !(usuario.Activo ?? false);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarListasDesplegables()
        {
            // 1. Traer los usuarios que tienen un EmpleadoID asignado
            var usuarios = await _context.Usuarios
                .Where(u => u.EmpleadoID != null)
                .ToListAsync();

            // 2. Filtrar en memoria (C#) los IDs de empleados que ya tienen contraseña
            var empleadosConPassword = usuarios
                .Where(u => u.PasswordHash != null && u.PasswordHash.Length > 0)
                .Select(u => u.EmpleadoID!.Value)
                .ToList();

            // 3. Traer solo los empleados que NO tienen contraseña
            var empleadosSinPassword = await _context.Empleados
                .Where(e => !empleadosConPassword.Contains(e.EmpleadoID))
                .ToListAsync();

            // 4. Armar el desplegable con Nombre + Apellidos
            var listaFinal = empleadosSinPassword
                .OrderBy(e => e.NombreCompleto)
                .Select(e => new
                {
                    EmpleadoID = e.EmpleadoID,
                    Texto = $"{e.NombreCompleto} (ID: {e.EmpleadoID})"
                })
                .ToList();

            ViewBag.Empleados = new SelectList(listaFinal, "EmpleadoID", "Texto");
        }

        // ==========================================
        // 1. EDITAR USUARIO (GET y POST)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Editar(int? id)
        {
            await CargarListaUsuariosActivos();

            if (!id.HasValue)
                return View(new EditarUsuarioViewModel());

            var usuario = await _context.Usuarios
                .Include(u => u.Empleado)
                .FirstOrDefaultAsync(u => u.UsuarioID == id.Value);

            if (usuario == null)
                return RedirectToAction(nameof(Editar));

            var model = new EditarUsuarioViewModel
            {
                UsuarioID = usuario.UsuarioID,
                Username = usuario.Username,
                NombreEmpleado = usuario.Empleado?.NombreCompleto ?? "Cuenta sin empleado vinculado"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarListaUsuariosActivos();
                return View(model);
            }

            var usuario = await _context.Usuarios.FindAsync(model.UsuarioID);
            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "El usuario seleccionado no existe.");
                await CargarListaUsuariosActivos();
                return View(model);
            }

            // Validar que el nuevo Username no pertenezca a otro usuario distinto
            bool usernameOcupado = await _context.Usuarios
                .AnyAsync(u => u.Username == model.Username.Trim() && u.UsuarioID != model.UsuarioID);

            if (usernameOcupado)
            {
                ModelState.AddModelError("Username", "Ese nombre de usuario ya está asignado a otra cuenta.");
                await CargarListaUsuariosActivos();
                return View(model);
            }

            // Actualizar Username
            usuario.Username = model.Username.Trim();

            // Si escribió una nueva contraseña, generar nuevo Salt y nuevo Hash SHA-512
            if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
            {
                Guid nuevoSalt = Guid.NewGuid();
                usuario.Salt = nuevoSalt;
                usuario.PasswordHash = SeguridadService.GenerarHashSha512(model.NuevaPassword, nuevoSalt);
            }

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"Los datos del usuario '{usuario.Username}' se actualizaron correctamente.";
            return RedirectToAction(nameof(Editar), new { id = usuario.UsuarioID });
        }

        private async Task CargarListaUsuariosActivos()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Empleado)
                .Where(u => u.Activo == true)
                .OrderBy(u => u.Username)
                .ToListAsync();

            var lista = usuarios.Select(u => new
            {
                UsuarioID = u.UsuarioID,
                Texto = u.Empleado != null
                    ? $"{u.Username} — ({u.Empleado.NombreCompleto})"
                    : $"{u.Username} — (Administración)"
            });

            ViewBag.Usuarios = new SelectList(lista, "UsuarioID", "Texto");
        }

        // ==========================================
        // 2. DAR DE BAJA A USUARIO (GET y POST)
        // ==========================================
        // GET: /Usuarios/Baja (Muestra únicamente los usuarios activos listos para dar de baja)
        [HttpGet]
        public async Task<IActionResult> Baja()
        {
            var usuariosActivos = await _context.Usuarios
                .Include(u => u.Empleado)
                .Where(u => u.Activo == true)
                .OrderBy(u => u.Username)
                .ToListAsync();

            return View(usuariosActivos);
        }

        // POST: /Usuarios/ProcesarBaja/5 (Aplica la baja lógica colocando Activo = false / 0)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarBaja(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                // Evitar que el usuario actual dé de baja su propia sesión
                if (User.Identity?.Name == usuario.Username)
                {
                    TempData["MensajeError"] = "No puedes dar de baja tu propia cuenta mientras tienes la sesión iniciada.";
                    return RedirectToAction(nameof(Baja));
                }

                // Baja lógica: actualiza el campo bit [Activo] a 0 (false) en SQL Server
                usuario.Activo = false;
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"El usuario '{usuario.Username}' ha sido dado de baja correctamente (Activo = 0).";
            }

            return RedirectToAction(nameof(Baja));
        }
    }
}