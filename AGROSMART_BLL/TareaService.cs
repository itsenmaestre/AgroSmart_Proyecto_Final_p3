using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public class TareaService : ICrudLectura<TAREA>, ICrudEscritura<TAREA>
    {
        private readonly TareaRepository _repo = new TareaRepository();

        public ReadOnlyCollection<TAREA> Consultar()
        {
            return new ReadOnlyCollection<TAREA>(_repo.Consultar().ToList());
        }

        public TAREA ObtenerPorId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("El ID de la tarea debe ser mayor a cero.");

            return _repo.ObtenerPorId(id);
        }

        public string Guardar(TAREA entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_CULTIVO <= 0)
                throw new ArgumentException("Debe especificar un cultivo válido.");

            if (entidad.ID_ADMIN_CREADOR <= 0)
                throw new ArgumentException("Debe especificar un administrador válido.");

            if (string.IsNullOrWhiteSpace(entidad.TIPO_ACTIVIDAD))
                throw new ArgumentException("El tipo de actividad es obligatorio.");

            if (entidad.TIEMPO_TOTAL_TAREA <= 0)
                throw new ArgumentException("El tiempo total debe ser mayor a cero.");

            return _repo.Guardar(entidad);
        }

        public bool Actualizar(TAREA entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_TAREA <= 0)
                throw new ArgumentException("El ID de la tarea es inválido.");

            if (string.IsNullOrWhiteSpace(entidad.TIPO_ACTIVIDAD))
                throw new ArgumentException("El tipo de actividad es obligatorio.");

            return _repo.Actualizar(entidad);
        }

        public bool Eliminar(TAREA entidad)
        {
            if (entidad == null || entidad.ID_TAREA <= 0)
                throw new ArgumentException("Tarea inválida para eliminar.");

            return _repo.Eliminar(entidad);
        }

     
        public List<TAREA> ObtenerPorCultivo(int idCultivo)
        {
            if (idCultivo <= 0)
                throw new ArgumentException("ID de cultivo inválido.");

            return _repo.ObtenerPorCultivo(idCultivo);
        }

        public List<TAREA> ObtenerPorEstado(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                throw new ArgumentException("Estado inválido.");

            return _repo.ObtenerPorEstado(estado);
        }

        public int ContarTareasPendientes()
        {
            return _repo.ObtenerPorEstado("PENDIENTE").Count;
        }

        public int ContarTareasEnProgreso()
        {
            return _repo.ObtenerPorEstado("EN_PROGRESO").Count;
        }

       
        public DateTime? ObtenerFechaProgramada(int idTarea)
        {
            try
            {
                var tarea = _repo.ObtenerPorId(idTarea);
                return tarea?.FECHA_PROGRAMADA;
            }
            catch
            {
                return null;
            }
        }

        public int ContarTareasDeHoy(int idEmpleado)
        {
            try
            {
                var hoy = DateTime.Today;
                return _repo.ContarTareasPorEmpleadoYFecha(idEmpleado, hoy);
            }
            catch
            {
                return 0;
            }
        }
        public int ContarPorEstado(int idEmpleado, string estado)
        {
            try
            {
                return _repo.ContarTareasPorEmpleadoYEstado(idEmpleado, estado);
            }
            catch
            {
                return 0;
            }
        }

     
        public int ContarVencidas(int idEmpleado)
        {
            try
            {
                var hoy = DateTime.Today;
                return _repo.ContarTareasVencidasPorEmpleado(idEmpleado, hoy);
            }
            catch
            {
                return 0;
            }
        }
    }
}
