using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MetalERP.Data;
using MetalERP.Models.Entities;

namespace MetalERP.Controllers
{
    [Authorize]
    public class CatalogosController : Controller
    {
        private readonly MetalErpContext _context;

        public CatalogosController(MetalErpContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "areas")
        {
            ViewBag.TabActiva = tab.ToLower();

            var areas = await _context.Areas.Where(a => a.Activo).OrderBy(a => a.NombreArea).ToListAsync();
            var cargos = await (from c in _context.Cargos
                                join a in _context.Areas on c.AreaID equals a.AreaID
                                where c.Activo
                                orderby a.NombreArea, c.NombreCargo
                                select new { c.CargoID, c.AreaID, AreaNombre = a.NombreArea, c.NombreCargo }).ToListAsync();

            var paises = await _context.Paises.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            var estados = await (from e in _context.Estados
                                 join p in _context.Paises on e.PaisID equals p.PaisID
                                 where e.Activo
                                 orderby p.Nombre, e.Nombre
                                 select new { e.EstadoID, e.PaisID, PaisNombre = p.Nombre, e.Nombre }).ToListAsync();

            var municipios = await (from m in _context.Municipios
                                    join e in _context.Estados on m.EstadoID equals e.EstadoID
                                    join p in _context.Paises on e.PaisID equals p.PaisID
                                    where m.Activo == null || m.Activo == true
                                    orderby p.Nombre, e.Nombre, m.Nombre
                                    select new
                                    {
                                        m.MunicipioID,
                                        m.EstadoID,
                                        PaisID = p.PaisID, // <-- Agregado para la cascada en Edición
                                        PaisNombre = p.Nombre,
                                        EstadoNombre = e.Nombre,
                                        m.Nombre
                                    }).ToListAsync();

            ViewBag.Areas = areas;
            ViewBag.Cargos = cargos;
            ViewBag.Paises = paises;
            ViewBag.Estados = estados;
            ViewBag.Municipios = municipios;

            ViewBag.SelectAreas = new SelectList(areas, "AreaID", "NombreArea");
            ViewBag.SelectPaises = new SelectList(paises, "PaisID", "Nombre");
            ViewBag.SelectEstados = new SelectList(estados.Select(x => new { x.EstadoID, Texto = $"{x.Nombre} ({x.PaisNombre})" }), "EstadoID", "Texto");

            return View();
        }

        // --- ALTA ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarNuevo(string tipo, string nombre, int? padreId)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre no puede estar vacío.";
                return RedirectToAction(nameof(Index), new { tab = tipo });
            }

            nombre = nombre.Trim();

            if (tipo == "areas")
            {
                string nombreNormalizado = nombre.ToLower();
                var areaExistente = await _context.Areas
                    .FirstOrDefaultAsync(a => a.NombreArea.ToLower() == nombreNormalizado);

                if (areaExistente != null)
                {
                    TempData["Error"] = areaExistente.Activo
                        ? $"El área '{areaExistente.NombreArea}' ya está registrada."
                        : $"El área '{areaExistente.NombreArea}' ya existe en el sistema (dada de baja).";
                }
                else
                {
                    _context.Areas.Add(new Area { NombreArea = nombre, Activo = true });
                    TempData["Exito"] = "Área registrada correctamente.";
                }
            }
            else if (tipo == "cargos" && padreId.HasValue)
            {
                string nombreNormalizado = nombre.ToLower();
                var cargoExistente = await _context.Cargos
                    .FirstOrDefaultAsync(c => c.AreaID == padreId.Value && c.NombreCargo.ToLower() == nombreNormalizado);

                if (cargoExistente != null)
                {
                    TempData["Error"] = cargoExistente.Activo
                        ? $"El cargo '{cargoExistente.NombreCargo}' ya está registrado en esa área."
                        : $"El cargo '{cargoExistente.NombreCargo}' ya existía en esa área (dado de baja).";
                }
                else
                {
                    _context.Cargos.Add(new Cargo { AreaID = padreId.Value, NombreCargo = nombre, Activo = true });
                    TempData["Exito"] = "Cargo registrado correctamente.";
                }
            }
            else if (tipo == "paises")
            {
                string nombreNorm = nombre.ToLower();
                var existente = await _context.Paises.FirstOrDefaultAsync(p => p.Nombre.ToLower() == nombreNorm);

                if (existente != null)
                {
                    TempData["Error"] = existente.Activo
                        ? $"El país '{existente.Nombre}' ya está registrado."
                        : $"El país '{existente.Nombre}' ya existía en el sistema (dado de baja).";
                }
                else
                {
                    _context.Paises.Add(new Pais { Nombre = nombre, Activo = true });
                    TempData["Exito"] = "País registrado correctamente.";
                }
            }
            else if (tipo == "estados" && padreId.HasValue)
            {
                string nombreNorm = nombre.ToLower();
                var existente = await _context.Estados
                    .FirstOrDefaultAsync(e => e.PaisID == padreId.Value && e.Nombre.ToLower() == nombreNorm);

                if (existente != null)
                {
                    TempData["Error"] = existente.Activo
                        ? $"El estado '{existente.Nombre}' ya está registrado en ese país."
                        : $"El estado '{existente.Nombre}' ya existía en ese país (dado de baja).";
                }
                else
                {
                    _context.Estados.Add(new Estado { PaisID = padreId.Value, Nombre = nombre, Activo = true });
                    TempData["Exito"] = "Estado registrado correctamente.";
                }
            }
            else if (tipo == "municipios" && padreId.HasValue)
            {
                string nombreNorm = nombre.ToLower();
                var existente = await _context.Municipios
                    .FirstOrDefaultAsync(m => m.EstadoID == padreId.Value && m.Nombre.ToLower() == nombreNorm);

                if (existente != null)
                {
                    bool estaActivo = existente.Activo == null || existente.Activo == true;
                    TempData["Error"] = estaActivo
                        ? $"El municipio '{existente.Nombre}' ya está registrado en ese estado."
                        : $"El municipio '{existente.Nombre}' ya existía en ese estado (dado de baja).";
                }
                else
                {
                    _context.Municipios.Add(new Municipio { EstadoID = padreId.Value, Nombre = nombre, Activo = true });
                    TempData["Exito"] = "Municipio registrado correctamente.";
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { tab = tipo });
        }

        // --- EDICIÓN ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCatalogo(string tipo, int id, string nombre, int? padreId)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return RedirectToAction(nameof(Index), new { tab = tipo });
            nombre = nombre.Trim();

            if(string.IsNullOrWhiteSpace(nombre))
    {
                TempData["Error"] = "El nombre no puede quedar vacío.";
                return RedirectToAction(nameof(Index), new { tab = tipo });
            }

            nombre = nombre.Trim();
            string nombreNormalizado = nombre.ToLower();

            if (tipo == "areas")
            {
                // Busca si existe OTRA área (AreaID != id) con el mismo nombre sin distinguir mayúsculas/minúsculas
                var areaDuplicada = await _context.Areas
                    .FirstOrDefaultAsync(a => a.NombreArea.ToLower() == nombreNormalizado && a.AreaID != id);

                if (areaDuplicada != null)
                {
                    TempData["Error"] = areaDuplicada.Activo
                        ? $"No se puede actualizar: ya existe un área activa llamada '{areaDuplicada.NombreArea}'."
                        : $"No se puede actualizar: el nombre '{areaDuplicada.NombreArea}' pertenece a un área dada de baja.";

                    return RedirectToAction(nameof(Index), new { tab = tipo });
                }

                var item = await _context.Areas.FindAsync(id);
                if (item != null)
                {
                    item.NombreArea = nombre;
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Área actualizada correctamente.";
                }
            }
            else if (tipo == "cargos")
            {
                var item = await _context.Cargos.FindAsync(id);
                if (item != null)
                {
                    int areaDestinoId = padreId ?? item.AreaID;

                    // Verifica que no haya OTRO cargo (CargoID != id) con el mismo nombre en el área destino
                    var cargoDuplicado = await _context.Cargos
                        .FirstOrDefaultAsync(c => c.AreaID == areaDestinoId
                                               && c.NombreCargo.ToLower() == nombreNormalizado
                                               && c.CargoID != id);

                    if (cargoDuplicado != null)
                    {
                        TempData["Error"] = cargoDuplicado.Activo
                            ? $"No se puede actualizar: ya existe el cargo '{cargoDuplicado.NombreCargo}' en esa área."
                            : $"No se puede actualizar: el cargo '{cargoDuplicado.NombreCargo}' pertenece a un registro dado de baja en esa área.";

                        return RedirectToAction(nameof(Index), new { tab = tipo });
                    }

                    item.NombreCargo = nombre;
                    item.AreaID = areaDestinoId;
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Cargo actualizado correctamente.";
                }
            }
            else if (tipo == "paises")
            {
                var duplicado = await _context.Paises
                    .FirstOrDefaultAsync(p => p.Nombre.ToLower() == nombreNormalizado && p.PaisID != id);

                if (duplicado != null)
                {
                    TempData["Error"] = duplicado.Activo
                        ? $"No se puede actualizar: ya existe un país activo llamado '{duplicado.Nombre}'."
                        : $"No se puede actualizar: el país '{duplicado.Nombre}' pertenece a un registro dado de baja.";
                    return RedirectToAction(nameof(Index), new { tab = tipo });
                }

                var item = await _context.Paises.FindAsync(id);
                if (item != null)
                {
                    item.Nombre = nombre;
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "País actualizado correctamente.";
                }
            }
            else if (tipo == "estados")
            {
                var item = await _context.Estados.FindAsync(id);
                if (item != null)
                {
                    int paisDestinoId = padreId ?? item.PaisID;
                    var duplicado = await _context.Estados
                        .FirstOrDefaultAsync(e => e.PaisID == paisDestinoId
                                               && e.Nombre.ToLower() == nombreNormalizado
                                               && e.EstadoID != id);

                    if (duplicado != null)
                    {
                        TempData["Error"] = duplicado.Activo
                            ? $"No se puede actualizar: ya existe el estado '{duplicado.Nombre}' en ese país."
                            : $"No se puede actualizar: el estado '{duplicado.Nombre}' ya existía en ese país (dado de baja).";
                        return RedirectToAction(nameof(Index), new { tab = tipo });
                    }

                    item.Nombre = nombre;
                    item.PaisID = paisDestinoId;
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Estado actualizado correctamente.";
                }
            }
            else if (tipo == "municipios")
            {
                var item = await _context.Municipios.FindAsync(id);
                if (item != null)
                {
                    int estadoDestinoId = padreId ?? item.EstadoID;
                    var duplicado = await _context.Municipios
                        .FirstOrDefaultAsync(m => m.EstadoID == estadoDestinoId
                                               && m.Nombre.ToLower() == nombreNormalizado
                                               && m.MunicipioID != id);

                    if (duplicado != null)
                    {
                        bool estaActivo = duplicado.Activo == null || duplicado.Activo == true;
                        TempData["Error"] = estaActivo
                            ? $"No se puede actualizar: ya existe el municipio '{duplicado.Nombre}' en ese estado."
                            : $"No se puede actualizar: el municipio '{duplicado.Nombre}' ya existía en ese estado (dado de baja).";
                        return RedirectToAction(nameof(Index), new { tab = tipo });
                    }

                    item.Nombre = nombre;
                    item.EstadoID = estadoDestinoId;
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Municipio actualizado correctamente.";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Exito"] = "Registro actualizado correctamente.";
            return RedirectToAction(nameof(Index), new { tab = tipo });
        }

        // --- BAJA LÓGICA ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BajaCatalogo(string tipo, int id)
        {
            string? motivoBloqueo = await VerificarDependenciasActivas(tipo, id);

            if (!string.IsNullOrEmpty(motivoBloqueo))
            {
                TempData["Error"] = $"No se puede dar de baja este registro porque {motivoBloqueo}.";
                return RedirectToAction(nameof(Index), new { tab = tipo });
            }

            if (tipo == "areas") { var x = await _context.Areas.FindAsync(id); if (x != null) x.Activo = false; }
            else if (tipo == "cargos") { var x = await _context.Cargos.FindAsync(id); if (x != null) x.Activo = false; }
            else if (tipo == "paises") { var x = await _context.Paises.FindAsync(id); if (x != null) x.Activo = false; }
            else if (tipo == "estados") { var x = await _context.Estados.FindAsync(id); if (x != null) x.Activo = false; }
            else if (tipo == "municipios") { var x = await _context.Municipios.FindAsync(id); if (x != null) x.Activo = false; }

            await _context.SaveChangesAsync();
            TempData["Exito"] = "Registro dado de baja correctamente.";
            return RedirectToAction(nameof(Index), new { tab = tipo });
        }

        private async Task<string?> VerificarDependenciasActivas(string tipo, int id)
        {
            if (tipo == "areas")
            {
                // 1. Verificar cargos activos pertenecientes a esta área
                if (await _context.Cargos.AnyAsync(c => c.AreaID == id && c.Activo))
                    return "tiene cargos activos asignados";

                // 2. Verificar empleados activos en cargos de esta área
                bool tieneEmpleados = await (from e in _context.Empleados
                                             join c in _context.Cargos on e.CargoID equals c.CargoID
                                             where c.AreaID == id && e.Activo
                                             select e.EmpleadoID).AnyAsync();
                if (tieneEmpleados)
                    return "está asignada a uno o más empleados activos";
            }
            else if (tipo == "cargos")
            {
                // Verificar empleados activos con este cargo
                if (await _context.Empleados.AnyAsync(e => e.CargoID == id && e.Activo))
                    return "está asignado a uno o más empleados activos";
            }
            else if (tipo == "paises")
            {
                // 1. Verificar estados activos de este país
                if (await _context.Estados.AnyAsync(e => e.PaisID == id && e.Activo))
                    return "tiene estados activos registrados";

                // 2. Verificar domicilios activos de empleados en este país
                bool enEmpleados = await (from d in _context.DireccionesEmpleados
                                          join e in _context.Estados on d.EntidadFederativa equals e.EstadoID
                                          where e.PaisID == id && d.Activo
                                          select d.DireccionID).AnyAsync();
                if (enEmpleados)
                    return "está relacionado con el domicilio de un empleado activo";
            }
            else if (tipo == "estados")
            {
                // 1. Verificar municipios activos de este estado
                if (await _context.Municipios.AnyAsync(m => m.EstadoID == id && (m.Activo == null || m.Activo == true)))
                    return "tiene municipios activos registrados";

                // 2. Verificar domicilios activos de empleados
                if (await _context.DireccionesEmpleados.AnyAsync(d => d.EntidadFederativa == id && d.Activo))
                    return "está relacionado con el domicilio de un empleado activo";
            }
            else if (tipo == "municipios")
            {
                // 1. Verificar domicilios activos de empleados
                if (await _context.DireccionesEmpleados.AnyAsync(d => d.Municipio == id && d.Activo))
                    return "está relacionado con el domicilio de un empleado activo";
            }

            return null; // Sin dependencias, se puede dar de baja
        }

    }
}
