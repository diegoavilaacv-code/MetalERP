using ClosedXML.Excel;
using MetalERP.Data;
using MetalERP.Models.Entities;
using MetalERP.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace MetalERP.Controllers
{
    [Authorize]
    public class EmpleadosController : Controller
    {
        private readonly MetalErpContext _context;
        private readonly IWebHostEnvironment _env;

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarCatalogosIniciales();
            return View(new AltaEmpleadoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(AltaEmpleadoViewModel model)
        {
            // Filtrar teléfonos no vacíos
            var telefonosValidos = (model.Telefonos ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList();

            // Filtrar contactos donde se haya escrito algo y verificar campos NOT NULL de SQL
            var contactosValidos = new List<ContactoItemViewModel>();
            if (model.Contactos != null)
            {
                for (int i = 0; i < model.Contactos.Count; i++)
                {
                    var c = model.Contactos[i];
                    bool tieneAlgunDato = !string.IsNullOrWhiteSpace(c.Nombre) ||
                                          !string.IsNullOrWhiteSpace(c.Parentesco) ||
                                          !string.IsNullOrWhiteSpace(c.Telefono);

                    if (tieneAlgunDato)
                    {
                        if (string.IsNullOrWhiteSpace(c.Nombre) ||
                            string.IsNullOrWhiteSpace(c.Parentesco) ||
                            string.IsNullOrWhiteSpace(c.Telefono))
                        {
                            ModelState.AddModelError(string.Empty, $"El contacto #{i + 1} requiere Nombre, Parentesco y Teléfono.");
                        }
                        else
                        {
                            contactosValidos.Add(c);
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarCatalogosIniciales(model.AreaID, model.PaisID, model.EntidadFederativa);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Guardar Empleado
                var nuevoEmpleado = new Empleado
                {
                    Nombre = model.Nombre.Trim(),
                    ApellidoPaterno = model.ApellidoPaterno.Trim(),
                    ApellidoMaterno = model.ApellidoMaterno?.Trim(),
                    Correo = model.CorreoEmpleado?.Trim(),
                    CargoID = model.CargoID,
                    FechaContratacion = model.FechaContratacion ?? DateTime.Today,
                    FechaRegistro = DateTime.Today,
                    Activo = true
                };
                _context.Empleados.Add(nuevoEmpleado);
                await _context.SaveChangesAsync();

                // 2. Guardar Teléfonos (0 o más)
                foreach (var tel in telefonosValidos)
                {
                    _context.TelefonosEmpleados.Add(new TelefonoEmpleado
                    {
                        EmpleadoID = nuevoEmpleado.EmpleadoID,
                        Telefono = tel,
                        Activo = 1
                    });
                }

                // 3. Guardar Dirección
                _context.DireccionesEmpleados.Add(new DireccionEmpleado
                {
                    EmpleadoID = nuevoEmpleado.EmpleadoID,
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

                // 4. Guardar Contactos de Emergencia (0 o más)
                foreach (var c in contactosValidos)
                {
                    _context.ContactosEmpleados.Add(new ContactoEmpleado
                    {
                        EmpleadoID = nuevoEmpleado.EmpleadoID,
                        Nombre = c.Nombre!.Trim(),
                        Parentesco = c.Parentesco!.Trim(),
                        Telefono = c.Telefono!.Trim(),
                        Correo = c.Correo?.Trim(),
                        Activo = true
                    });
                }

                await ProcesarArchivosExpediente(nuevoEmpleado.EmpleadoID, model);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["MensajeExito"] = $"Empleado '{nuevoEmpleado.NombreCompleto}' registrado con {telefonosValidos.Count} teléfono(s) y {contactosValidos.Count} contacto(s).";
                return RedirectToAction(nameof(Crear));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Error al guardar el expediente: " + ex.Message);
                await CargarCatalogosIniciales(model.AreaID, model.PaisID, model.EntidadFederativa);
                return View(model);
            }
        }

        // --- ENDPOINTS JSON PARA LISTAS DESPLEGABLES EN CASCADA ---

        [HttpGet]
        public async Task<IActionResult> ObtenerCargosPorArea(int areaId)
        {
            var cargos = await _context.Cargos
                .Where(c => c.AreaID == areaId && c.Activo)
                .OrderBy(c => c.NombreCargo)
                .Select(c => new { id = c.CargoID, nombre = c.NombreCargo })
                .ToListAsync();

            return Json(cargos);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEstadosPorPais(int paisId)
        {
            var estados = await _context.Estados
                .Where(e => e.PaisID == paisId && e.Activo)
                .OrderBy(e => e.Nombre)
                .Select(e => new { id = e.EstadoID, nombre = e.Nombre })
                .ToListAsync();

            return Json(estados);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerMunicipiosPorEstado(int estadoId)
        {
            var municipios = await _context.Municipios
                .Where(m => m.EstadoID == estadoId && (m.Activo == null || m.Activo == true))
                .OrderBy(m => m.Nombre)
                .Select(m => new { id = m.MunicipioID, nombre = m.Nombre })
                .ToListAsync();

            return Json(municipios);
        }

        private async Task CargarCatalogosIniciales(int? areaId = null, int? paisId = null, int? estadoId = null)
        {
            var areas = await _context.Areas.Where(a => a.Activo).OrderBy(a => a.NombreArea).ToListAsync();
            ViewBag.Areas = new SelectList(areas, "AreaID", "NombreArea");

            var paises = await _context.Paises.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.Paises = new SelectList(paises, "PaisID", "Nombre");

            // Si hubo error de validación en POST, recargar listas dependientes seleccionadas
            ViewBag.Cargos = areaId.HasValue
                ? new SelectList(await _context.Cargos.Where(c => c.AreaID == areaId && c.Activo).ToListAsync(), "CargoID", "NombreCargo")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.Estados = paisId.HasValue
                ? new SelectList(await _context.Estados.Where(e => e.PaisID == paisId && e.Activo).ToListAsync(), "EstadoID", "Nombre")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.Municipios = estadoId.HasValue
                ? new SelectList(await _context.Municipios.Where(m => m.EstadoID == estadoId).ToListAsync(), "MunicipioID", "Nombre")
                : new SelectList(Enumerable.Empty<SelectListItem>());
        }

        // ==========================================
        // 1. EDITAR EMPLEADO (GET y POST)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Editar(int? id)
        {
            await CargarListaEmpleadosActivos();

            if (!id.HasValue || id.Value <= 0)
            {
                await CargarCatalogosIniciales();
                return View(new EditarEmpleadoViewModel());
            }

            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.EmpleadoID == id.Value && e.Activo);
            if (empleado == null) return RedirectToAction(nameof(Editar));

            var cargoActual = await _context.Cargos.FirstOrDefaultAsync(c => c.CargoID == empleado.CargoID);
            int? areaIdActual = cargoActual?.AreaID;

            var direccion = await _context.DireccionesEmpleados
                .FirstOrDefaultAsync(d => d.EmpleadoID == empleado.EmpleadoID && d.Activo);

            int? paisIdActual = null;
            if (direccion != null)
            {
                var estadoActual = await _context.Estados.FirstOrDefaultAsync(e => e.EstadoID == direccion.EntidadFederativa);
                paisIdActual = estadoActual?.PaisID;
            }

            var telefonos = await _context.TelefonosEmpleados
                .Where(t => t.EmpleadoID == empleado.EmpleadoID && t.Activo == 1)
                .Select(t => t.Telefono)
                .ToListAsync();

            var contactos = await _context.ContactosEmpleados
                .Where(c => c.EmpleadoID == empleado.EmpleadoID && c.Activo)
                .Select(c => new ContactoItemViewModel
                {
                    Nombre = c.Nombre,
                    Parentesco = c.Parentesco,
                    Telefono = c.Telefono,
                    Correo = c.Correo
                })
                .ToListAsync();

            var model = new EditarEmpleadoViewModel
            {
                EmpleadoID = empleado.EmpleadoID,
                Nombre = empleado.Nombre,
                ApellidoPaterno = empleado.ApellidoPaterno,
                ApellidoMaterno = empleado.ApellidoMaterno,
                CorreoEmpleado = empleado.Correo,
                AreaID = areaIdActual,
                CargoID = empleado.CargoID,
                FechaContratacion = empleado.FechaContratacion,

                PaisID = paisIdActual,
                EntidadFederativa = direccion?.EntidadFederativa,
                Municipio = direccion?.Municipio,
                ColoniaLocalidad = direccion?.ColoniaLocalidad,
                Calle = direccion?.Calle,
                NumeroExterior = direccion?.NumeroExterior,
                NumeroInterior = direccion?.NumeroInterior,
                CodigoPostal = direccion?.CodigoPostal,
                Referencia = direccion?.Referencia,

                Telefonos = telefonos,
                Contactos = contactos
            };

            model.DocumentosActuales = await _context.ExpedientesEmpleados
            .Where(x => x.EmpleadoID == empleado.EmpleadoID && x.Activo)
            .ToListAsync();

            await CargarCatalogosIniciales(areaIdActual, paisIdActual, direccion?.EntidadFederativa);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarEmpleadoViewModel model)
        {
            var telefonosValidos = (model.Telefonos ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList();

            var contactosValidos = new List<ContactoItemViewModel>();
            if (model.Contactos != null)
            {
                for (int i = 0; i < model.Contactos.Count; i++)
                {
                    var c = model.Contactos[i];
                    bool tieneDato = !string.IsNullOrWhiteSpace(c.Nombre) ||
                                     !string.IsNullOrWhiteSpace(c.Parentesco) ||
                                     !string.IsNullOrWhiteSpace(c.Telefono);
                    if (tieneDato)
                    {
                        if (string.IsNullOrWhiteSpace(c.Nombre) ||
                            string.IsNullOrWhiteSpace(c.Parentesco) ||
                            string.IsNullOrWhiteSpace(c.Telefono))
                        {
                            ModelState.AddModelError(string.Empty, $"El contacto #{i + 1} requiere Nombre, Parentesco y Teléfono.");
                        }
                        else
                        {
                            contactosValidos.Add(c);
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarListaEmpleadosActivos();
                await CargarCatalogosIniciales(model.AreaID, model.PaisID, model.EntidadFederativa);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var empleado = await _context.Empleados.FindAsync(model.EmpleadoID);
                if (empleado == null) return RedirectToAction(nameof(Editar));

                // 1. Actualizar [Empleados]
                empleado.Nombre = model.Nombre.Trim();
                empleado.ApellidoPaterno = model.ApellidoPaterno.Trim();
                empleado.ApellidoMaterno = model.ApellidoMaterno?.Trim();
                empleado.Correo = model.CorreoEmpleado?.Trim();
                empleado.CargoID = model.CargoID;
                empleado.FechaContratacion = model.FechaContratacion;

                // 2. Actualizar [DireccionesEmpleados]
                var direccion = await _context.DireccionesEmpleados
                    .FirstOrDefaultAsync(d => d.EmpleadoID == empleado.EmpleadoID && d.Activo);

                if (direccion == null)
                {
                    direccion = new DireccionEmpleado { EmpleadoID = empleado.EmpleadoID, Activo = true };
                    _context.DireccionesEmpleados.Add(direccion);
                }

                direccion.EntidadFederativa = model.EntidadFederativa!.Value;
                direccion.Municipio = model.Municipio!.Value;
                direccion.ColoniaLocalidad = model.ColoniaLocalidad?.Trim();
                direccion.Calle = model.Calle?.Trim();
                direccion.NumeroExterior = model.NumeroExterior?.Trim();
                direccion.NumeroInterior = model.NumeroInterior?.Trim();
                direccion.CodigoPostal = model.CodigoPostal?.Trim();
                direccion.Referencia = model.Referencia?.Trim();

                // 3. Actualizar [TelefonosEmpleados]
                var telsAnteriores = await _context.TelefonosEmpleados.Where(t => t.EmpleadoID == empleado.EmpleadoID && t.Activo == 1).ToListAsync();
                telsAnteriores.ForEach(t => t.Activo = 0);

                foreach (var tel in telefonosValidos)
                {
                    _context.TelefonosEmpleados.Add(new TelefonoEmpleado
                    {
                        EmpleadoID = empleado.EmpleadoID,
                        Telefono = tel,
                        Activo = 1
                    });
                }

                // 4. Actualizar [ContactosEmpleados]
                var contAnteriores = await _context.ContactosEmpleados.Where(c => c.EmpleadoID == empleado.EmpleadoID && c.Activo).ToListAsync();
                contAnteriores.ForEach(c => c.Activo = false);

                foreach (var c in contactosValidos)
                {
                    _context.ContactosEmpleados.Add(new ContactoEmpleado
                    {
                        EmpleadoID = empleado.EmpleadoID,
                        Nombre = c.Nombre!.Trim(),
                        Parentesco = c.Parentesco!.Trim(),
                        Telefono = c.Telefono!.Trim(),
                        Correo = c.Correo?.Trim(),
                        Activo = true
                    });
                }

                await ProcesarArchivosExpediente(empleado.EmpleadoID, model);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["MensajeExito"] = $"Expediente completo de '{empleado.NombreCompleto}' actualizado correctamente.";
                return RedirectToAction(nameof(Editar), new { id = empleado.EmpleadoID });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Error al actualizar el expediente: " + ex.Message);
                await CargarListaEmpleadosActivos();
                await CargarCatalogosIniciales(model.AreaID, model.PaisID, model.EntidadFederativa);
                return View(model);
            }
        }

        private async Task CargarListaEmpleadosActivos()
        {
            var empleados = await _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            var lista = empleados.Select(e => new
            {
                EmpleadoID = e.EmpleadoID,
                Texto = $"{e.NombreCompleto} (ID: {e.EmpleadoID})"
            });

            ViewBag.Empleados = new SelectList(lista, "EmpleadoID", "Texto");
        }

        // ==========================================
        // 2. BAJA LÓGICA DE EMPLEADO (GET y POST)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Baja()
        {
            var empleadosActivos = await (from e in _context.Empleados
                                          join c in _context.Cargos on e.CargoID equals c.CargoID into cargosJoin
                                          from c in cargosJoin.DefaultIfEmpty()
                                          where e.Activo
                                          orderby e.Nombre
                                          select new
                                          {
                                              e.EmpleadoID,
                                              NombreCompleto = e.Nombre + " " + e.ApellidoPaterno + " " + (e.ApellidoMaterno ?? ""),
                                              Cargo = c != null ? c.NombreCargo : "Sin cargo",
                                              e.FechaContratacion
                                          }).ToListAsync();

            ViewBag.ListaEmpleados = empleadosActivos;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarBaja(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado != null)
            {
                // 1. Baja lógica del empleado (Activo = 0 / false)
                empleado.Activo = false;

                // 2. Si tiene una cuenta de usuario vinculada, desactivarla también por seguridad
                var usuarioVinculado = await _context.Usuarios.FirstOrDefaultAsync(u => u.EmpleadoID == id && u.Activo == true);
                if (usuarioVinculado != null)
                {
                    usuarioVinculado.Activo = false;
                }

                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"El empleado '{empleado.NombreCompleto}' ha sido dado de baja correctamente.";
            }

            return RedirectToAction(nameof(Baja));
        }

        /// <summary>
        /// Consulta de empleados
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Consultar()
        {
            // 1. Traer empleados activos con su Área y Cargo
            var empleadosBase = await (from e in _context.Empleados
                                       join c in _context.Cargos on e.CargoID equals c.CargoID into cJoin
                                       from c in cJoin.DefaultIfEmpty()
                                       join a in _context.Areas on c.AreaID equals a.AreaID into aJoin
                                       from a in aJoin.DefaultIfEmpty()
                                       where e.Activo
                                       orderby e.EmpleadoID
                                       select new
                                       {
                                           e.EmpleadoID,
                                           NombreCompleto = e.Nombre + " " + e.ApellidoPaterno + " " + (e.ApellidoMaterno ?? ""),
                                           Correo = e.Correo ?? "Sin correo",
                                           Area = a != null ? a.NombreArea : "Sin área",
                                           Cargo = c != null ? c.NombreCargo : "Sin cargo",
                                           e.FechaContratacion
                                       }).ToListAsync();

            // 2. Traer todos los teléfonos activos y agruparlos por EmpleadoID
            var telefonos = await _context.TelefonosEmpleados
                .Where(t => t.Activo == 1)
                .ToListAsync();

            // 3. Traer domicilios activos cruzados con Municipio, Estado y País
            var direcciones = await (from d in _context.DireccionesEmpleados
                                     join m in _context.Municipios on d.Municipio equals m.MunicipioID into mJoin
                                     from m in mJoin.DefaultIfEmpty()
                                     join est in _context.Estados on d.EntidadFederativa equals est.EstadoID into estJoin
                                     from est in estJoin.DefaultIfEmpty()
                                     join p in _context.Paises on est.PaisID equals p.PaisID into pJoin
                                     from p in pJoin.DefaultIfEmpty()
                                     where d.Activo
                                     select new
                                     {
                                         d.EmpleadoID,
                                         Pais = p != null ? p.Nombre : "-",
                                         Estado = est != null ? est.Nombre : "-",
                                         Municipio = m != null ? m.Nombre : "-",
                                         Colonia = d.ColoniaLocalidad ?? "-",
                                         Calle = d.Calle ?? "",
                                         NumExt = d.NumeroExterior ?? "",
                                         NumInt = d.NumeroInterior ?? "",
                                         CP = d.CodigoPostal ?? "-",
                                         Ref = d.Referencia ?? "-"
                                     }).ToListAsync();

            // 4. Traer contactos de emergencia activos
            var contactos = await _context.ContactosEmpleados
                .Where(c => c.Activo)
                .ToListAsync();

            // Cargar los expedientes activos desde SQL Server
            var expedientes = await _context.ExpedientesEmpleados
                .Where(x => x.Activo)
                .ToListAsync();

            // 5. Armar la lista final para la vista
            var resultado = empleadosBase.Select(emp =>
            {
                var telsEmp = telefonos
                    .Where(t => t.EmpleadoID == emp.EmpleadoID)
                    .Select(t => t.Telefono);

                var dir = direcciones.FirstOrDefault(d => d.EmpleadoID == emp.EmpleadoID);

                var contsEmp = contactos
                    .Where(c => c.EmpleadoID == emp.EmpleadoID)
                    .Select(c => new ContactoItemViewModel
                    {
                        Nombre = c.Nombre,
                        Parentesco = c.Parentesco,
                        Telefono = c.Telefono,
                        Correo = c.Correo ?? "-"
                    }).ToList();

                // <-- AQUÍ ADENTRO sí existe 'emp'
                var docsEmp = expedientes
                    .Where(x => x.EmpleadoID == emp.EmpleadoID)
                    .ToList();

                return new ConsultaEmpleadoViewModel
                {
                    EmpleadoID = emp.EmpleadoID,
                    NombreCompleto = emp.NombreCompleto.Trim(),
                    Correo = emp.Correo,
                    Area = emp.Area,
                    Cargo = emp.Cargo,
                    FechaContratacion = emp.FechaContratacion?.ToString("dd/MM/yyyy") ?? "-",
                    TelefonosConcatenados = telsEmp.Any() ? string.Join(", ", telsEmp) : "Sin teléfonos",

                    Pais = dir?.Pais ?? "-",
                    Estado = dir?.Estado ?? "-",
                    Municipio = dir?.Municipio ?? "-",
                    Colonia = dir?.Colonia ?? "-",
                    CalleYNumeros = dir != null ? $"{dir.Calle} Ext. {dir.NumExt} {(string.IsNullOrEmpty(dir.NumInt) ? "" : "Int. " + dir.NumInt)}".Trim() : "-",
                    CodigoPostal = dir?.CP ?? "-",
                    Referencia = dir?.Ref ?? "-",
                    Contactos = contsEmp,
                    Documentos = docsEmp // <-- Asignación limpia dentro del return
                };
            }).ToList();

            return View(resultado);
        }

        // Método auxiliar que aplica los mismos filtros de la tabla en pantalla
        private async Task<List<ConsultaEmpleadoViewModel>> ObtenerEmpleadosFiltrados(
            string? busqueda, string? id, string? nombre, string? correo,
            string? telefono, string? area, string? cargo, string? fecha)
        {
            var empleadosBase = await (from e in _context.Empleados
                                       join c in _context.Cargos on e.CargoID equals c.CargoID into cJoin
                                       from c in cJoin.DefaultIfEmpty()
                                       join a in _context.Areas on c.AreaID equals a.AreaID into aJoin
                                       from a in aJoin.DefaultIfEmpty()
                                       where e.Activo
                                       orderby e.EmpleadoID
                                       select new
                                       {
                                           e.EmpleadoID,
                                           NombreCompleto = e.Nombre + " " + e.ApellidoPaterno + " " + (e.ApellidoMaterno ?? ""),
                                           Correo = e.Correo ?? "Sin correo",
                                           Area = a != null ? a.NombreArea : "Sin área",
                                           Cargo = c != null ? c.NombreCargo : "Sin cargo",
                                           e.FechaContratacion
                                       }).ToListAsync();

            var telefonos = await _context.TelefonosEmpleados.Where(t => t.Activo == 1).ToListAsync();

            var lista = empleadosBase.Select(emp =>
            {
                var tels = telefonos.Where(t => t.EmpleadoID == emp.EmpleadoID).Select(t => t.Telefono);
                return new ConsultaEmpleadoViewModel
                {
                    EmpleadoID = emp.EmpleadoID,
                    NombreCompleto = emp.NombreCompleto.Trim(),
                    Correo = emp.Correo,
                    TelefonosConcatenados = tels.Any() ? string.Join(", ", tels) : "Sin teléfonos",
                    Area = emp.Area,
                    Cargo = emp.Cargo,
                    FechaContratacion = emp.FechaContratacion?.ToString("dd/MM/yyyy") ?? "-"
                };
            }).ToList();

            //var expedientes = await _context.ExpedientesEmpleados.Where(x => x.Activo).ToListAsync();
            //var Documentos = expedientes.Where(x => x.EmpleadoID == emp.EmpleadoID).ToList();

            // Buscador global sin acentos
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string b = SinAcentos(busqueda);
                lista = lista.Where(x =>
                    SinAcentos($"#{x.EmpleadoID}").Contains(b) ||
                    SinAcentos(x.NombreCompleto).Contains(b) ||
                    SinAcentos(x.Correo).Contains(b) ||
                    SinAcentos(x.TelefonosConcatenados).Contains(b) ||
                    SinAcentos(x.Area).Contains(b) ||
                    SinAcentos(x.Cargo).Contains(b) ||
                    SinAcentos(x.FechaContratacion).Contains(b)).ToList();
            }

            // Filtros por columna sin acentos
            if (!string.IsNullOrWhiteSpace(id)) lista = lista.Where(x => SinAcentos($"#{x.EmpleadoID}").Contains(SinAcentos(id))).ToList();
            if (!string.IsNullOrWhiteSpace(nombre)) lista = lista.Where(x => SinAcentos(x.NombreCompleto).Contains(SinAcentos(nombre))).ToList();
            if (!string.IsNullOrWhiteSpace(correo)) lista = lista.Where(x => SinAcentos(x.Correo).Contains(SinAcentos(correo))).ToList();
            if (!string.IsNullOrWhiteSpace(telefono)) lista = lista.Where(x => SinAcentos(x.TelefonosConcatenados).Contains(SinAcentos(telefono))).ToList();
            if (!string.IsNullOrWhiteSpace(area)) lista = lista.Where(x => SinAcentos(x.Area).Contains(SinAcentos(area))).ToList();
            if (!string.IsNullOrWhiteSpace(cargo)) lista = lista.Where(x => SinAcentos(x.Cargo).Contains(SinAcentos(cargo))).ToList();
            if (!string.IsNullOrWhiteSpace(fecha)) lista = lista.Where(x => SinAcentos(x.FechaContratacion).Contains(SinAcentos(fecha))).ToList();



            return lista;
        }

        // ==========================================
        // DESCARGAR EXCEL (.xlsx)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(
            string? busqueda, string? id, string? nombre, string? correo,
            string? telefono, string? area, string? cargo, string? fecha)
        {
            var datos = await ObtenerEmpleadosFiltrados(busqueda, id, nombre, correo, telefono, area, cargo, fecha);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Empleados");

            // Encabezados corporativos
            string[] headers = { "ID", "Nombre Completo", "Correo", "Teléfonos", "Área", "Cargo", "Fecha Ingreso" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B192C");
            }

            // Filas de datos
            int row = 2;
            foreach (var emp in datos)
            {
                ws.Cell(row, 1).Value = emp.EmpleadoID;
                ws.Cell(row, 2).Value = emp.NombreCompleto;
                ws.Cell(row, 3).Value = emp.Correo;
                ws.Cell(row, 4).Value = emp.TelefonosConcatenados;
                ws.Cell(row, 5).Value = emp.Area;
                ws.Cell(row, 6).Value = emp.Cargo;
                ws.Cell(row, 7).Value = emp.FechaContratacion;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Empleados_MetalERP_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ==========================================
        // DESCARGAR PDF (.pdf)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportarPDF(
            string? busqueda, string? id, string? nombre, string? correo,
            string? telefono, string? area, string? cargo, string? fecha)
        {
            var datos = await ObtenerEmpleadosFiltrados(busqueda, id, nombre, correo, telefono, area, cargo, fecha);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("MetalERP — Directorio de Empleados Activos").FontSize(16).Bold().FontColor("#0B192C");
                            col.Item().Text($"ACV Precision • Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#64748B");
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);  // ID
                            columns.RelativeColumn(3);   // Nombre
                            columns.RelativeColumn(2.5f);// Correo
                            columns.RelativeColumn(2);   // Teléfonos
                            columns.RelativeColumn(2);   // Área
                            columns.RelativeColumn(2);   // Cargo
                            columns.ConstantColumn(65);  // Ingreso
                        });

                        table.Header(header =>
                        {
                            string[] cols = { "ID", "Nombre Completo", "Correo", "Teléfonos", "Área", "Cargo", "Ingreso" };
                            foreach (var c in cols)
                            {
                                header.Cell().Background("#0B192C").Padding(5).Text(c).Bold().FontColor("#FFFFFF");
                            }
                        });

                        foreach (var emp in datos)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text($"#{emp.EmpleadoID}");
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(emp.NombreCompleto);
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(emp.Correo);
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(emp.TelefonosConcatenados);
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(emp.Area);
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(emp.Cargo);
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(emp.FechaContratacion);
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Empleados_MetalERP_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // ==========================================
        // ENDPOINTS JSON PARA ALTA RÁPIDA DE CATÁLOGOS
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CrearAreaRapido([FromBody] string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del área es obligatorio.");

            nombre = nombre.Trim();
            string nombreNormalizado = nombre.ToLower();

            // Verifica en toda la tabla sin distinguir mayúsculas ni minúsculas
            var areaExistente = await _context.Areas
                .FirstOrDefaultAsync(a => a.NombreArea.ToLower() == nombreNormalizado);

            if (areaExistente != null)
            {
                if (areaExistente.Activo)
                    return BadRequest($"El área '{areaExistente.NombreArea}' ya se encuentra registrada.");
                else
                    return BadRequest($"El área '{areaExistente.NombreArea}' ya existe en el historial (actualmente dada de baja).");
            }

            var nueva = new Area { NombreArea = nombre, Activo = true };
            _context.Areas.Add(nueva);
            await _context.SaveChangesAsync();

            return Json(new { id = nueva.AreaID, nombre = nueva.NombreArea });
        }

        [HttpPost]
        public async Task<IActionResult> CrearCargoRapido(int areaId, [FromBody] string nombre)
        {
            if (areaId <= 0)
                return BadRequest("Primero debes seleccionar un área.");

            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del cargo es obligatorio.");

            nombre = nombre.Trim();
            string nombreNormalizado = nombre.ToLower();

            // Verifica que no exista ese cargo dentro de la MISMA área (sin distinguir mayúsculas/minúsculas)
            var cargoExistente = await _context.Cargos
                .FirstOrDefaultAsync(c => c.AreaID == areaId && c.NombreCargo.ToLower() == nombreNormalizado);

            if (cargoExistente != null)
            {
                return BadRequest(cargoExistente.Activo
                    ? $"El cargo '{cargoExistente.NombreCargo}' ya existe dentro de esta área."
                    : $"El cargo '{cargoExistente.NombreCargo}' ya existía en esta área (actualmente dado de baja).");
            }

            var nuevo = new Cargo { AreaID = areaId, NombreCargo = nombre, Activo = true };
            _context.Cargos.Add(nuevo);
            await _context.SaveChangesAsync();

            return Json(new { id = nuevo.CargoID, nombre = nuevo.NombreCargo });
        }

        [HttpPost]
        public async Task<IActionResult> CrearPaisRapido([FromBody] string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del país es obligatorio.");

            nombre = nombre.Trim();
            string nombreNorm = nombre.ToLower();

            var existente = await _context.Paises
                .FirstOrDefaultAsync(p => p.Nombre.ToLower() == nombreNorm);

            if (existente != null)
            {
                return BadRequest(existente.Activo
                    ? $"El país '{existente.Nombre}' ya se encuentra registrado."
                    : $"El país '{existente.Nombre}' ya existía en el sistema (dado de baja).");
            }

            var nuevo = new Pais { Nombre = nombre, Activo = true };
            _context.Paises.Add(nuevo);
            await _context.SaveChangesAsync();

            return Json(new { id = nuevo.PaisID, nombre = nuevo.Nombre });
        }

        [HttpPost]
        public async Task<IActionResult> CrearEstadoRapido(int paisId, [FromBody] string nombre)
        {
            if (paisId <= 0)
                return BadRequest("Primero debes seleccionar un país.");

            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del estado es obligatorio.");

            nombre = nombre.Trim();
            string nombreNorm = nombre.ToLower();

            var existente = await _context.Estados
                .FirstOrDefaultAsync(e => e.PaisID == paisId && e.Nombre.ToLower() == nombreNorm);

            if (existente != null)
            {
                return BadRequest(existente.Activo
                    ? $"El estado '{existente.Nombre}' ya existe dentro de este país."
                    : $"El estado '{existente.Nombre}' ya existía en este país (dado de baja).");
            }

            var nuevo = new Estado { PaisID = paisId, Nombre = nombre, Activo = true };
            _context.Estados.Add(nuevo);
            await _context.SaveChangesAsync();

            return Json(new { id = nuevo.EstadoID, nombre = nuevo.Nombre });
        }

        [HttpPost]
        public async Task<IActionResult> CrearMunicipioRapido(int estadoId, [FromBody] string nombre)
        {
            if (estadoId <= 0)
                return BadRequest("Primero debes seleccionar un estado.");

            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del municipio es obligatorio.");

            nombre = nombre.Trim();
            string nombreNorm = nombre.ToLower();

            var existente = await _context.Municipios
                .FirstOrDefaultAsync(m => m.EstadoID == estadoId && m.Nombre.ToLower() == nombreNorm);

            if (existente != null)
            {
                bool estaActivo = existente.Activo == null || existente.Activo == true;
                return BadRequest(estaActivo
                    ? $"El municipio '{existente.Nombre}' ya existe dentro de este estado."
                    : $"El municipio '{existente.Nombre}' ya existía en este estado (dado de baja).");
            }

            var nuevo = new Municipio { EstadoID = estadoId, Nombre = nombre, Activo = true };
            _context.Municipios.Add(nuevo);
            await _context.SaveChangesAsync();

            return Json(new { id = nuevo.MunicipioID, nombre = nuevo.Nombre });
        }

        // Método auxiliar para eliminar acentos en C#
        private static string SinAcentos(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        public EmpleadosController(MetalErpContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private async Task ProcesarArchivosExpediente(int empleadoId, AltaEmpleadoViewModel model)
        {
            var mapaDocumentos = new Dictionary<string, IFormFile?>
            {
                { "Solicitud de empleo", model.DocSolicitudEmpleo },
                { "Acta de nacimiento", model.DocActaNacimiento },
                { "INE", model.DocINE },
                { "CURP", model.DocCURP },
                { "Comprobante de estudios", model.DocComprobanteEstudios },
                { "Comprobante de domicilio", model.DocComprobanteDomicilio },
                { "Constancia del IMSS", model.DocConstanciaIMSS },
                { "Contrato de confidencialidad firmado", model.DocContratoConfidencialidad },
                { "Contrato de vinculación", model.DocContratoVinculacion },
                { "Reglamento firmado", model.DocReglamentoFirmado },
                { "Foto", model.DocFoto }
            };

            string[] extensionesPermitidas = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };

            // Crear carpeta wwwroot/ArchivoElectronico/Emp_{ID} si no existe
            string carpetaBase = Path.Combine(_env.WebRootPath, "ArchivoElectronico", $"Emp_{empleadoId}");
            if (!Directory.Exists(carpetaBase))
            {
                Directory.CreateDirectory(carpetaBase);
            }

            foreach (var item in mapaDocumentos)
            {
                var archivo = item.Value;
                if (archivo != null && archivo.Length > 0)
                {
                    string ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                    if (!extensionesPermitidas.Contains(ext))
                        throw new Exception($"El archivo '{archivo.FileName}' no tiene un formato permitido (PDF, Word o Imagen).");

                    // Si ya existía un documento activo de este mismo tipo (al editar), darle baja lógica
                    var anteriores = await _context.ExpedientesEmpleados
                        .Where(x => x.EmpleadoID == empleadoId && x.TipoDocumento == item.Key && x.Activo)
                        .ToListAsync();
                    anteriores.ForEach(x => x.Activo = false);

                    // Guardar archivo físico con nombre único seguro
                    string nombreSeguro = $"{item.Key.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                    string rutaFisica = Path.Combine(carpetaBase, nombreSeguro);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }

                    // Registrar en SQL Server
                    _context.ExpedientesEmpleados.Add(new ExpedienteEmpleado
                    {
                        EmpleadoID = empleadoId,
                        TipoDocumento = item.Key,
                        NombreArchivoOriginal = archivo.FileName,
                        RutaArchivo = $"/ArchivoElectronico/Emp_{empleadoId}/{nombreSeguro}",
                        FechaSubida = DateTime.Now,
                        Activo = true
                    });
                }
            }
        }
    }
}
