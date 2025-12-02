using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;

namespace AGROSMART_BLL
{
    public class EmpleadoCosechaService
    {
        private readonly EmpleadoCosechaRepository _repository;
        private readonly CosechaService _cosechaService;

        public EmpleadoCosechaService()
        {
            _repository = new EmpleadoCosechaRepository();
            _cosechaService = new CosechaService();
        }

        // =============================================
        // OBTENER EMPLEADOS DE UNA COSECHA
        // =============================================
        public IList<EMPLEADO_COSECHA> ObtenerPorCosecha(int idCosecha)
        {
            return _repository.ObtenerPorCosecha(idCosecha);
        }

        // =============================================
        // AGREGAR O SUMAR TRABAJO DE UN EMPLEADO (A1)
        //
        // Si YA existe registro ese día → SUMA
        // Si NO existe → INSERT
        //
        // Después de guardar, se recalcula la cantidad total
        // en COSECHA usando: ActualizarCantidadTotal
        // =============================================
        public bool RegistrarTrabajo(EMPLEADO_COSECHA entidad)
        {
            // Validar que la cosecha no esté terminada
            var cosecha = _cosechaService.ObtenerPorId(entidad.ID_COSECHA);
            if (cosecha == null)
                throw new Exception("La cosecha no existe.");

            if (cosecha.ESTADO == "TERMINADA")
                throw new Exception("La cosecha ya está terminada. No se puede registrar más trabajo.");

            if (entidad.CANTIDAD_COSECHADA <= 0)
                throw new Exception("La cantidad debe ser mayor que cero.");

            if (entidad.FECHA_TRABAJO == DateTime.MinValue)
                throw new Exception("La fecha de trabajo no es válida.");

            // Insertar o sumar (A1)
            bool ok = _repository.InsertarOSumar(entidad);

            if (!ok)
                return false;

            // Recalcular cantidad total en COSECHA
            _cosechaService.ActualizarCantidadTotal(entidad.ID_COSECHA);

            return true;
        }

        // =============================================
        // ELIMINAR REGISTRO INDIVIDUAL
        // =============================================
        public bool Eliminar(int id)
        {
            return _repository.Eliminar(id);
        }
    }
}
