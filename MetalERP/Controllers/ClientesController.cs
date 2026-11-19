using MetalERP.Data;
using MetalERP.Models.Entities;
using MetalERP.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace MetalERP.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly MetalErpContext _context;

        public ClientesController(MetalErpContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ALTA DE CLIENTE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarCatalogosUbicacion();
            return View(new ClienteFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ClienteFormViewModel model)
        {
            string? rfcLimpio = string.IsNullOrWhiteSpace(model.RFC) ? null : model.RFC.Trim().ToUpper();
            if (rfcLimpio != null && await _context.Clientes.AnyAsync(c => c.RFC == rfcLimpio))
                ModelState.AddModelError("RFC", "Ya existe un cliente registrado con este RFC.");

            if (await _context.Clientes.AnyAsync(c => c.Clave.ToLower() == model.Clave.Trim().ToLower()))
                ModelState.AddModelError("Clave", "Esta clave de cliente ya está en uso.");

            var contactosValidos = ValidarContactos(model.Contactos);

            if (!ModelState.IsValid)
            {
                await CargarCatalogosUbicacion(model.PaisID, model.EntidadFederativa);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevoCliente = new Cliente
                {
                    RazonSocial_Nombre = model.RazonSocial_Nombre.Trim(),
                    RFC = rfcLimpio,
                    Clave = model.Clave.Trim().ToUpper(),
                    Correo = model.Correo?.Trim(),
                    Telefono = model.TelefonoPrincipal?.Trim(),
                    PaginaWeb = model.PaginaWeb?.Trim(),
                    FechaRegistro = DateTime.Today,
                    Activo = true
                };

                _context.Clientes.Add(nuevoCliente);
                await _context.SaveChangesAsync();

                foreach (var tel in (model.TelefonosAdicionales ?? new()).Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    _context.TelefonosClientes.Add(new TelefonoCliente
                    {
                        ClienteID = nuevoCliente.ClienteID,
                        Telefono = tel.Trim(),
                        Activo = true
                    });
                }

                _context.DireccionesClientes.Add(new DireccionCliente
                {
                    ClienteID = nuevoCliente.ClienteID,
                    PaisID = model.PaisID!.Value,
                    EntidadFederativa = model.EntidadFederativa!.Value,
                    Municipio = model.Municipio!.Value,
                    ColoniaLocalidad = model.ColoniaLocalidad?.Trim(),
                    Calle = model.Calle?.Trim(),
                    NumeroExterior = model.NumeroExterior?.Trim(),
                    NumeroInterior = model.NumeroInterior?.Trim(),
                    CodigoPostal = model.CodigoPostal?.Trim(),
                    Referencia = model.Referencia?.Trim(),
                    Activo = true
                });

                foreach (var c in contactosValidos)
                {
                    _context.ContactosClientes.Add(new ContactoCliente
                    {
                        ClienteID = nuevoCliente.ClienteID,
                        Nombre = c.Nombre!.Trim(),
                        Cargo = c.Cargo!.Trim(),
                        Telefono = c.Telefono!.Trim(),
                        Correo = c.Correo?.Trim(),
                        Activo = true
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["MensajeExito"] = $"Cliente '{nuevoCliente.RazonSocial_Nombre}' registrado correctamente.";
                return RedirectToAction(nameof(Crear));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Error al guardar el cliente: " + ex.Message);
                await CargarCatalogosUbicacion(model.PaisID, model.EntidadFederativa);
                return View(model);
            }
        }

        // ==========================================
        // 2. EDITAR CLIENTE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Editar(int? id)
        {
            await CargarListaClientesActivos();

            if (!id.HasValue || id.Value <= 0)
            {
                await CargarCatalogosUbicacion();
                return View(new ClienteFormViewModel());
            }

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.ClienteID == id.Value && c.Activo);
            if (cliente == null) return RedirectToAction(nameof(Editar));

            var dir = await _context.DireccionesClientes.FirstOrDefaultAsync(d => d.ClienteID == cliente.ClienteID && d.Activo);
            var tels = await _context.TelefonosClientes.Where(t => t.ClienteID == cliente.ClienteID && t.Activo).Select(t => t.Telefono).ToListAsync();
            var conts = await _context.ContactosClientes.Where(c => c.ClienteID == cliente.ClienteID && c.Activo)
                .Select(c => new ContactoClienteItemViewModel { Nombre = c.Nombre, Cargo = c.Cargo, Telefono = c.Telefono, Correo = c.Correo }).ToListAsync();

            var model = new ClienteFormViewModel
            {
                ClienteID = cliente.ClienteID,
                RazonSocial_Nombre = cliente.RazonSocial_Nombre,
                RFC = cliente.RFC,
                Clave = cliente.Clave,
                Correo = cliente.Correo,
                TelefonoPrincipal = cliente.Telefono,
                PaginaWeb = cliente.PaginaWeb,
                TelefonosAdicionales = tels,
                PaisID = dir?.PaisID,
                EntidadFederativa = dir?.EntidadFederativa,
                Municipio = dir?.Municipio,
                ColoniaLocalidad = dir?.ColoniaLocalidad,
                Calle = dir?.Calle,
                NumeroExterior = dir?.NumeroExterior,
                NumeroInterior = dir?.NumeroInterior,
                CodigoPostal = dir?.CodigoPostal,
                Referencia = dir?.Referencia,
                Contactos = conts
            };

            await CargarCatalogosUbicacion(dir?.PaisID, dir?.EntidadFederativa);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ClienteFormViewModel model)
        {
            string? rfcLimpio = string.IsNullOrWhiteSpace(model.RFC) ? null : model.RFC.Trim().ToUpper();
            if (rfcLimpio != null && await _context.Clientes.AnyAsync(c => c.RFC == rfcLimpio && c.ClienteID != model.ClienteID))
                ModelState.AddModelError("RFC", "Otro cliente ya tiene registrado ese RFC.");

            if (await _context.Clientes.AnyAsync(c => c.Clave.ToLower() == model.Clave.Trim().ToLower() && c.ClienteID != model.ClienteID))
                ModelState.AddModelError("Clave", "Esa clave ya pertenece a otro cliente.");

            var contactosValidos = ValidarContactos(model.Contactos);

            if (!ModelState.IsValid)
            {
                await CargarListaClientesActivos();
                await CargarCatalogosUbicacion(model.PaisID, model.EntidadFederativa);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cliente = await _context.Clientes.FindAsync(model.ClienteID);
                if (cliente == null) return RedirectToAction(nameof(Editar));

                cliente.RazonSocial_Nombre = model.RazonSocial_Nombre.Trim();
                cliente.RFC = rfcLimpio;
                cliente.Clave = model.Clave.Trim().ToUpper();
                cliente.Correo = model.Correo?.Trim();
                cliente.Telefono = model.TelefonoPrincipal?.Trim();
                cliente.PaginaWeb = model.PaginaWeb?.Trim();

                var dir = await _context.DireccionesClientes.FirstOrDefaultAsync(d => d.ClienteID == cliente.ClienteID && d.Activo);
                if (dir == null)
                {
                    dir = new DireccionCliente { ClienteID = cliente.ClienteID, Activo = true };
                    _context.DireccionesClientes.Add(dir);
                }

                dir.PaisID = model.PaisID!.Value;
                dir.EntidadFederativa = model.EntidadFederativa!.Value;
                dir.Municipio = model.Municipio!.Value;
                dir.ColoniaLocalidad = model.ColoniaLocalidad?.Trim();
                dir.Calle = model.Calle?.Trim();
                dir.NumeroExterior = model.NumeroExterior?.Trim();
                dir.NumeroInterior = model.NumeroInterior?.Trim();
                dir.CodigoPostal = model.CodigoPostal?.Trim();
                dir.Referencia = model.Referencia?.Trim();

                var telsAnt = await _context.TelefonosClientes.Where(t => t.ClienteID == cliente.ClienteID && t.Activo).ToListAsync();
                telsAnt.ForEach(t => t.Activo = false);
                foreach (var tel in (model.TelefonosAdicionales ?? new()).Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    _context.TelefonosClientes.Add(new TelefonoCliente { ClienteID = cliente.ClienteID, Telefono = tel.Trim(), Activo = true });
                }

                var contAnt = await _context.ContactosClientes.Where(c => c.ClienteID == cliente.ClienteID && c.Activo).ToListAsync();
                contAnt.ForEach(c => c.Activo = false);
                foreach (var c in contactosValidos)
                {
                    _context.ContactosClientes.Add(new ContactoCliente
                    {
                        ClienteID = cliente.ClienteID,
                        Nombre = c.Nombre!.Trim(),
                        Cargo = c.Cargo!.Trim(),
                        Telefono = c.Telefono!.Trim(),
                        Correo = c.Correo?.Trim(),
                        Activo = true
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["MensajeExito"] = $"Datos del cliente '{cliente.RazonSocial_Nombre}' actualizados.";
                return RedirectToAction(nameof(Editar), new { id = cliente.ClienteID });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Error al actualizar: " + ex.Message);
                await CargarListaClientesActivos();
                await CargarCatalogosUbicacion(model.PaisID, model.EntidadFederativa);
                return View(model);
            }
        }

        // ==========================================
        // 3. BAJA LÓGICA DE CLIENTE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Baja()
        {
            var clientes = await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.RazonSocial_Nombre).ToListAsync();
            return View(clientes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarBaja(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                cliente.Activo = false;
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"El cliente '{cliente.RazonSocial_Nombre}' ha sido dado de baja.";
            }
            return RedirectToAction(nameof(Baja));
        }

        // --- MÉTODOS AUXILIARES ---
        private List<ContactoClienteItemViewModel> ValidarContactos(List<ContactoClienteItemViewModel>? contactos)
        {
            var validos = new List<ContactoClienteItemViewModel>();
            if (contactos == null) return validos;

            for (int i = 0; i < contactos.Count; i++)
            {
                var c = contactos[i];
                bool tieneDato = !string.IsNullOrWhiteSpace(c.Nombre) || !string.IsNullOrWhiteSpace(c.Cargo) || !string.IsNullOrWhiteSpace(c.Telefono);
                if (tieneDato)
                {
                    if (string.IsNullOrWhiteSpace(c.Nombre) || string.IsNullOrWhiteSpace(c.Cargo) || string.IsNullOrWhiteSpace(c.Telefono))
                        ModelState.AddModelError(string.Empty, $"El contacto #{i + 1} requiere Nombre, Cargo y Teléfono.");
                    else
                        validos.Add(c);
                }
            }
            return validos;
        }

        private async Task CargarCatalogosUbicacion(int? paisId = null, int? estadoId = null)
        {
            var paises = await _context.Paises.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.Paises = new SelectList(paises, "PaisID", "Nombre");

            ViewBag.Estados = paisId.HasValue
                ? new SelectList(await _context.Estados.Where(e => e.PaisID == paisId && e.Activo).OrderBy(e => e.Nombre).ToListAsync(), "EstadoID", "Nombre")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.Municipios = estadoId.HasValue
                ? new SelectList(await _context.Municipios.Where(m => m.EstadoID == estadoId && (m.Activo == null || m.Activo == true)).OrderBy(m => m.Nombre).ToListAsync(), "MunicipioID", "Nombre")
                : new SelectList(Enumerable.Empty<SelectListItem>());
        }

        private async Task CargarListaClientesActivos()
        {
            var clientes = await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.RazonSocial_Nombre).ToListAsync();
            ViewBag.Clientes = new SelectList(clientes.Select(c => new { c.ClienteID, Texto = $"[{c.Clave}] {c.RazonSocial_Nombre}" }), "ClienteID", "Texto");
        }

        // GET: /Clientes/SugerirClave?nombre=Aceros+del+Centro&clienteId=0
        [HttpGet]
        public async Task<IActionResult> SugerirClave(string nombre, int clienteId = 0)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Json(new { clave = "" });

            // Obtener todas las claves ya registradas en la BD (excluyendo al cliente actual si se está editando)
            var clavesOcupadas = await _context.Clientes
                .Where(c => c.ClienteID != clienteId)
                .Select(c => c.Clave.ToUpper())
                .ToHashSetAsync();

            string clavePropuesta = GenerarClaveUnica(nombre, clavesOcupadas);
            return Json(new { clave = clavePropuesta });
        }

        private static string GenerarClaveUnica(string nombreOriginal, HashSet<string> ocupadas)
        {
            // 1. Quitar acentos, pasar a mayúsculas y conservar solo letras y espacios
            string sinAcentos = SinAcentos(nombreOriginal).ToUpperInvariant();
            var sbLimpio = new StringBuilder();
            foreach (char c in sinAcentos)
            {
                if (char.IsLetter(c) || char.IsWhiteSpace(c))
                    sbLimpio.Append(c);
            }

            var palabras = sbLimpio.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (palabras.Count == 0) return "";

            var candidatos = new List<string>();
            string p1 = palabras[0];

            if (palabras.Count == 1)
            {
                // Regla 1 palabra: 2 primeras letras (ej. BIMBO -> BI)
                if (p1.Length >= 2) candidatos.Add(p1.Substring(0, 2));
                else candidatos.Add(p1);

                // Variaciones de 2 letras con la 1ra + siguientes (BM, BO...)
                for (int i = 2; i < p1.Length; i++)
                    candidatos.Add($"{p1[0]}{p1[i]}");

                // Variaciones de 3 letras (BIM, BIO...)
                if (p1.Length >= 3) candidatos.Add(p1.Substring(0, 3));
                for (int i = 1; i < p1.Length - 1; i++)
                    for (int j = i + 1; j < p1.Length; j++)
                        candidatos.Add($"{p1[0]}{p1[i]}{p1[j]}");
            }
            else if (palabras.Count == 2)
            {
                string p2 = palabras[1];

                // Regla 2 palabras: 1ra de palabra 1 + 1ra de palabra 2 (ej. ACEROS CENTRO -> AC)
                candidatos.Add($"{p1[0]}{p2[0]}");

                // Si ya existe: 1ra y 2da letra de la primera palabra (ej. ACEROS -> AE, o si ya existía, AR, AO, AS)
                if (p1.Length >= 2) candidatos.Add(p1.Substring(0, 2));
                for (int i = 2; i < p1.Length; i++)
                    candidatos.Add($"{p1[0]}{p1[i]}");

                // Variaciones de 2 letras combinando p1 y p2
                for (int i = 1; i < p2.Length; i++)
                    candidatos.Add($"{p1[0]}{p2[i]}");

                // Variaciones de 3 letras (ej. ACE, ACC...)
                if (p1.Length >= 2) candidatos.Add($"{p1[0]}{p1[1]}{p2[0]}");
                if (p2.Length >= 2) candidatos.Add($"{p1[0]}{p2[0]}{p2[1]}");
                if (p1.Length >= 3) candidatos.Add(p1.Substring(0, 3));
            }
            else
            {
                // Regla 3 o más palabras: 1ra letra de cada palabra (máximo 3 letras)
                candidatos.Add($"{palabras[0][0]}{palabras[1][0]}{palabras[2][0]}");

                // Si ya existe, combinar 1ra y 2da letra con las demás palabras
                for (int k = 3; k < palabras.Count; k++)
                    candidatos.Add($"{palabras[0][0]}{palabras[1][0]}{palabras[k][0]}");

                if (p1.Length >= 2)
                {
                    candidatos.Add($"{p1[0]}{p1[1]}{palabras[1][0]}");
                    candidatos.Add($"{p1[0]}{p1[1]}{palabras[2][0]}");
                    candidatos.Add(p1.Substring(0, 2));
                }

                candidatos.Add($"{palabras[0][0]}{palabras[1][0]}");
                candidatos.Add($"{palabras[0][0]}{palabras[2][0]}");
            }

            // Respaldo final de 2 y 3 letras si todas las combinaciones anteriores estuvieran ocupadas
            string letrasUnidas = string.Join("", palabras);
            for (int i = 0; i < letrasUnidas.Length; i++)
                for (int j = 0; j < letrasUnidas.Length; j++)
                    if (i != j) candidatos.Add($"{letrasUnidas[i]}{letrasUnidas[j]}");

            const string alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            foreach (char extra in alfabeto)
            {
                if (p1.Length >= 2) candidatos.Add($"{p1[0]}{p1[1]}{extra}");
                candidatos.Add($"{p1[0]}{extra}");
            }

            // Devolver el primer candidato único de 2 o 3 caracteres
            foreach (var cand in candidatos.Distinct())
            {
                if (cand.Length >= 2 && cand.Length <= 3 && !ocupadas.Contains(cand))
                    return cand;
            }

            return candidatos.FirstOrDefault() ?? "CL";
        }

        private static string SinAcentos(string texto)
        {
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // ==========================================
        // 4. CONSULTA DE CLIENTES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Consultar(string busqueda, string estatus = "activos")
        {
            var query = _context.Clientes.AsQueryable();

            // Filtro por texto (Clave, Razón Social/Nombre o RFC)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string termino = busqueda.Trim().ToLower();
                query = query.Where(c =>
                    c.Clave.ToLower().Contains(termino) ||
                    c.RazonSocial_Nombre.ToLower().Contains(termino) ||
                    (c.RFC != null && c.RFC.ToLower().Contains(termino)));
            }

            // Filtro por estatus
            if (estatus == "activos")
                query = query.Where(c => c.Activo);
            else if (estatus == "inactivos")
                query = query.Where(c => !c.Activo);

            var clientes = await query
                .OrderBy(c => c.RazonSocial_Nombre)
                .Select(c => new ConsultaClienteViewModel
                {
                    ClienteID = c.ClienteID,
                    Clave = c.Clave,
                    RazonSocial_Nombre = c.RazonSocial_Nombre,
                    RFC = string.IsNullOrEmpty(c.RFC) ? "Sin RFC" : c.RFC,
                    Telefono = string.IsNullOrEmpty(c.Telefono) ? "-" : c.Telefono,
                    Correo = string.IsNullOrEmpty(c.Correo) ? "-" : c.Correo,
                    PaginaWeb = string.IsNullOrEmpty(c.PaginaWeb) ? "" : c.PaginaWeb,
                    FechaRegistro = c.FechaRegistro,
                    Activo = c.Activo,
                    // Obtiene resumen de dirección activa
                    UbicacionResumen = _context.DireccionesClientes
                        .Where(d => d.ClienteID == c.ClienteID && d.Activo)
                        .Select(d => (d.ColoniaLocalidad != null ? d.ColoniaLocalidad + ", " : "") + "C.P. " + (d.CodigoPostal ?? "N/A"))
                        .FirstOrDefault() ?? "Sin dirección registrada",
                    // Cuenta contactos activos
                    TotalContactos = _context.ContactosClientes
                        .Count(cc => cc.ClienteID == c.ClienteID && cc.Activo)
                })
                .ToListAsync();

            ViewBag.BusquedaActual = busqueda;
            ViewBag.EstatusActual = estatus;

            return View(clientes);
        }

        // GET: Clientes/ObtenerDetalle/5 (Para el Modal de Ficha Técnica)
        [HttpGet]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ClienteID == id);

            if (cliente == null)
                return NotFound();

            var direccion = await _context.DireccionesClientes
                .Where(d => d.ClienteID == id && d.Activo)
                .Select(d => new
                {
                    CalleNumero = $"{d.Calle} #{d.NumeroExterior}" + (string.IsNullOrEmpty(d.NumeroInterior) ? "" : $" Int. {d.NumeroInterior}"),
                    d.ColoniaLocalidad,
                    d.CodigoPostal,
                    d.Referencia
                })
                .FirstOrDefaultAsync();

            var telefonosAdicionales = await _context.TelefonosClientes
                .Where(t => t.ClienteID == id && t.Activo)
                .Select(t => t.Telefono)
                .ToListAsync();

            var contactos = await _context.ContactosClientes
                .Where(c => c.ClienteID == id && c.Activo)
                .Select(c => new
                {
                    c.Nombre,
                    c.Cargo,
                    c.Telefono,
                    c.Correo
                })
                .ToListAsync();

            return Json(new
            {
                cliente.ClienteID,
                cliente.Clave,
                cliente.RazonSocial_Nombre,
                RFC = cliente.RFC ?? "No registrado",
                TelefonoPrincipal = cliente.Telefono ?? "No registrado",
                Correo = cliente.Correo ?? "No registrado",
                PaginaWeb = cliente.PaginaWeb ?? "",
                FechaRegistro = cliente.FechaRegistro.ToString("dd/MM/yyyy"),
                Activo = cliente.Activo,
                Direccion = direccion,
                TelefonosAdicionales = telefonosAdicionales,
                Contactos = contactos
            });
        }
    }
}