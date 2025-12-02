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
    public class CultivoService : ICrudLectura<CULTIVO>, ICrudEscritura<CULTIVO>
    {
        private readonly CultivoRepository _repo = new CultivoRepository();

        public ReadOnlyCollection<CULTIVO> Consultar()
        {
            return new ReadOnlyCollection<CULTIVO>(_repo.Consultar().ToList());
        }

        public CULTIVO ObtenerPorId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("El ID del cultivo debe ser mayor a cero.");

            return _repo.ObtenerPorId(id);
        }

        public string Guardar(CULTIVO entidad)
        {
            // Validaciones
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (string.IsNullOrWhiteSpace(entidad.NOMBRE_LOTE))
                throw new ArgumentException("El nombre del lote es obligatorio.");

            if (entidad.FECHA_COSECHA_ESTIMADA <= entidad.FECHA_SIEMBRA)
                throw new ArgumentException("La fecha de cosecha estimada debe ser posterior a la fecha de siembra.");

            if (entidad.ID_ADMIN_SUPERVISOR <= 0)
                throw new ArgumentException("Debe especificar un administrador supervisor válido.");

            return _repo.Guardar(entidad);
        }

        public bool Actualizar(CULTIVO entidad)
        {
            // Validaciones
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_CULTIVO <= 0)
                throw new ArgumentException("El ID del cultivo es inválido.");

            if (string.IsNullOrWhiteSpace(entidad.NOMBRE_LOTE))
                throw new ArgumentException("El nombre del lote es obligatorio.");

            if (entidad.FECHA_COSECHA_ESTIMADA <= entidad.FECHA_SIEMBRA)
                throw new ArgumentException("La fecha de cosecha estimada debe ser posterior a la fecha de siembra.");

            return _repo.Actualizar(entidad);
        }

        public bool Eliminar(CULTIVO entidad)
        {
            if (entidad == null || entidad.ID_CULTIVO <= 0)
                throw new ArgumentException("Cultivo inválido para eliminar.");

            return _repo.Eliminar(entidad);
        }

        // Métodos adicionales
        public List<CULTIVO> ObtenerCultivosProximosACosechar(int diasAnticipacion = 30)
        {
            var todos = _repo.Consultar().ToList();
            DateTime fechaLimite = DateTime.Now.AddDays(diasAnticipacion);

            return todos.Where(c => c.FECHA_COSECHA_ESTIMADA <= fechaLimite && c.FECHA_COSECHA_ESTIMADA >= DateTime.Now)
                        .OrderBy(c => c.FECHA_COSECHA_ESTIMADA)
                        .ToList();
        }

        public int ContarCultivosActivos()
        {
            return _repo.Consultar().Count;
        }
    }
}
