using System.ComponentModel.DataAnnotations;

namespace MetalERP.Models.ViewModels
{
    public class EditarEmpleadoViewModel : AltaEmpleadoViewModel
    {
        [Required(ErrorMessage = "Selecciona un empleado para editar.")]
        public int EmpleadoID { get; set; }
        public List<MetalERP.Models.Entities.ExpedienteEmpleado> DocumentosActuales { get; set; } = new();
    }
}
