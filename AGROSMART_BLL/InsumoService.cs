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
    public class InsumoService : ICrudLectura<INSUMO>, ICrudEscritura<INSUMO>
    {
        private readonly InsumoRepository _repo = new InsumoRepository();

        public ReadOnlyCollection<INSUMO> Consultar()
        {
            return new ReadOnlyCollection<INSUMO>(_repo.Consultar().ToList());
        }

        public INSUMO ObtenerPorId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("El ID del insumo debe ser mayor a cero.");

            return _repo.ObtenerPorId(id);
        }

        public string Guardar(INSUMO entidad)
        {
            // Validaciones
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (string.IsNullOrWhiteSpace(entidad.NOMBRE))
                throw new ArgumentException("El nombre del insumo es obligatorio.");

            if (string.IsNullOrWhiteSpace(entidad.TIPO))
                throw new ArgumentException("El tipo de insumo es obligatorio.");

            if (entidad.TIPO != "CONSUMIBLE" && entidad.TIPO != "ACTIVO_FIJO")
                throw new ArgumentException("El tipo debe ser CONSUMIBLE o ACTIVO_FIJO.");

            if (entidad.STOCK_ACTUAL < 0)
                throw new ArgumentException("El stock actual no puede ser negativo.");

            if (entidad.STOCK_MINIMO < 0)
                throw new ArgumentException("El stock mínimo no puede ser negativo.");

            if (entidad.COSTO_UNITARIO <= 0)
                throw new ArgumentException("El costo unitario debe ser mayor a cero.");

            if (entidad.ID_ADMIN_REGISTRO <= 0)
                throw new ArgumentException("Debe especificar un administrador válido.");

            return _repo.Guardar(entidad);
        }

        public bool Actualizar(INSUMO entidad)
        {
            // Validaciones
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_INSUMO <= 0)
                throw new ArgumentException("El ID del insumo es inválido.");

            if (string.IsNullOrWhiteSpace(entidad.NOMBRE))
                throw new ArgumentException("El nombre del insumo es obligatorio.");

            if (entidad.STOCK_ACTUAL < 0)
                throw new ArgumentException("El stock actual no puede ser negativo.");

            if (entidad.COSTO_UNITARIO <= 0)
                throw new ArgumentException("El costo unitario debe ser mayor a cero.");

            return _repo.Actualizar(entidad);
        }

        public bool Eliminar(INSUMO entidad)
        {
            if (entidad == null || entidad.ID_INSUMO <= 0)
                throw new ArgumentException("Insumo inválido para eliminar.");

            return _repo.Eliminar(entidad);
        }

        // Métodos adicionales
        public List<INSUMO> ObtenerInsumosConStockBajo()
        {
            return _repo.ObtenerInsumosConStockBajo();
        }

        public int ContarInsumosConStockBajo()
        {
            return _repo.ContarInsumosConStockBajo();
        }

        public bool ActualizarStock(int idInsumo, decimal nuevoStock)
        {
            var insumo = _repo.ObtenerPorId(idInsumo);
            if (insumo == null)
                throw new ArgumentException("Insumo no encontrado.");

            if (nuevoStock < 0)
                throw new ArgumentException("El stock no puede ser negativo.");

            insumo.STOCK_ACTUAL = nuevoStock;
            return _repo.Actualizar(insumo);
        }
    }
}
