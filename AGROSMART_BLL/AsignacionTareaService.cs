using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public class AsignacionTareaService
    {
        private readonly AsignacionTareaRepository _repo = new AsignacionTareaRepository();

        public List<ASIGNACION_TAREA> ListarPorEmpleado(int idEmpleado)
        {
            if (idEmpleado <= 0)
                throw new ArgumentException("ID de empleado inválido.");

            return _repo.ListarPorEmpleado(idEmpleado);
        }

        public List<ASIGNACION_TAREA> ListarPorTarea(int idTarea)
        {
            if (idTarea <= 0)
                throw new ArgumentException("ID de tarea inválido.");

            return _repo.ListarPorTarea(idTarea);
        }

        public List<ASIGNACION_TAREA> ListarTodas()
        {
            return _repo.ListarTodas();
        }

        public string ActualizarAvance(ASIGNACION_TAREA a)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));

            if (a.ID_ASIG_TAREA <= 0)
                throw new ArgumentException("ID de asignación inválido.");

            return _repo.ActualizarAvance(a);
        }

        public string Asignar(ASIGNACION_TAREA asignacion)
        {
            if (asignacion == null)
                throw new ArgumentNullException(nameof(asignacion));

            if (asignacion.ID_TAREA <= 0)
                throw new ArgumentException("Debe especificar una tarea válida.");

            if (asignacion.ID_EMPLEADO <= 0)
                throw new ArgumentException("Debe especificar un empleado válido.");

            if (asignacion.ID_ADMIN_ASIGNADOR <= 0)
                throw new ArgumentException("Debe especificar un administrador válido.");

            return _repo.Asignar(asignacion);
        }

        public bool CancelarAsignacion(int idAsignacion)
        {
            if (idAsignacion <= 0)
                throw new ArgumentException("ID de asignación inválido.");

            return _repo.CancelarAsignacion(idAsignacion);
        }
    }
}
