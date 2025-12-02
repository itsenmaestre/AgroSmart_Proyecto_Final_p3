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
    public class AdminService : ICrudLectura<ADMINISTRADOR>, ICrudEscritura<ADMINISTRADOR>
    {
        private readonly AdminRepository _repo = new AdminRepository();

        public ReadOnlyCollection<ADMINISTRADOR> Consultar()
        {
            return new ReadOnlyCollection<ADMINISTRADOR>(_repo.Consultar().ToList());
        }

        public ADMINISTRADOR ObtenerPorId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("El ID del administrador debe ser mayor a cero.");

            return _repo.ObtenerPorId(id);
        }

        public string Guardar(ADMINISTRADOR entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_USUARIO <= 0)
                throw new ArgumentException("El ID de usuario es inválido.");

            if (entidad.MONTO_MENSUAL < 0)
                throw new ArgumentException("El monto mensual no puede ser negativo.");

            return _repo.Guardar(entidad);
        }

        public bool Actualizar(ADMINISTRADOR entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_USUARIO <= 0)
                throw new ArgumentException("El ID de usuario es inválido.");

            if (entidad.MONTO_MENSUAL < 0)
                throw new ArgumentException("El monto mensual no puede ser negativo.");

            return _repo.Actualizar(entidad);
        }

        public bool Eliminar(ADMINISTRADOR entidad)
        {
            if (entidad == null || entidad.ID_USUARIO <= 0)
                throw new ArgumentException("Administrador inválido para eliminar.");

            return _repo.Eliminar(entidad);
        }

        public Dictionary<string, int> ObtenerEstadisticas(int idAdmin)
        {
            if (idAdmin <= 0)
                throw new ArgumentException("ID de administrador inválido.");

            return _repo.ObtenerEstadisticasGeneral(idAdmin);
        }
    }
}
