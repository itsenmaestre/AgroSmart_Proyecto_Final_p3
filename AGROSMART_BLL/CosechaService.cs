using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AGROSMART_BLL
{
    public class CosechaService
    {
        private readonly CosechaRepository _repository;

        public CosechaService()
        {
            _repository = new CosechaRepository();
        }

        // =============================================
        // CONSULTAR TODAS LAS COSECHAS
        // =============================================
        public IList<COSECHA> Consultar()
        {
            return _repository.Consultar();
        }

        // =============================================
        // OBTENER POR ID
        // =============================================
        public COSECHA ObtenerPorId(int id)
        {
            return _repository.ObtenerPorId(id);
        }

        // =============================================
        // OBTENER COSECHAS POR CULTIVO
        // =============================================
        public IList<COSECHA> ObtenerPorCultivo(int idCultivo)
        {
            return _repository.ObtenerPorCultivo(idCultivo);
        }

        // =============================================
        // BUSCAR COSECHA ACTIVA DE UN CULTIVO
        // ⭐ Solo puede haber 1 cosecha EN_PROCESO por cultivo
        // =============================================
        public COSECHA BuscarCosechaActiva(int idCultivo)
        {
            var lista = _repository.ObtenerPorCultivo(idCultivo);
            return lista.FirstOrDefault(x => x.ESTADO == "EN_PROCESO");
        }

        // =============================================
        // GUARDAR NUEVA COSECHA
        // ⚠️ No se puede crear si ya hay una EN_PROCESO
        // =============================================
        public string Guardar(COSECHA entidad)
        {
            // Validar que no haya cosecha activa
            var activa = BuscarCosechaActiva(entidad.ID_CULTIVO);

            if (activa != null)
            {
                throw new InvalidOperationException(
                    $"Ya existe una cosecha EN PROCESO para este cultivo (ID: {activa.ID_COSECHA}).\n" +
                    "Debe finalizarla antes de crear una nueva.");
            }

            // Validación: cantidad inicial debe ser 0
            entidad.CANTIDAD_OBTENIDA = 0;
            entidad.ESTADO = "EN_PROCESO";
            entidad.FECHA_FINALIZACION = null;

            return _repository.Guardar(entidad);
        }

        // =============================================
        // ACTUALIZAR COSECHA (SOLO DATOS BÁSICOS)
        // ⚠️ Solo se puede actualizar si está EN_PROCESO
        // =============================================
        public bool Actualizar(COSECHA entidad)
        {
            var cosecha = ObtenerPorId(entidad.ID_COSECHA);
            if (cosecha == null)
                throw new Exception("No existe la cosecha especificada.");

            if (cosecha.ESTADO == "TERMINADA")
            {
                throw new InvalidOperationException(
                    "La cosecha ya está TERMINADA.\n" +
                    "No se puede modificar una cosecha finalizada.");
            }

            return _repository.Actualizar(entidad);
        }

        // =============================================
        // FINALIZAR UNA COSECHA
        // ⭐ Marca como TERMINADA y registra FECHA_FINALIZACION
        // =============================================
        public bool TerminarCosecha(int idCosecha)
        {
            var cosecha = _repository.ObtenerPorId(idCosecha);

            if (cosecha == null)
                throw new Exception("Cosecha no encontrada.");

            if (cosecha.ESTADO == "TERMINADA")
            {
                throw new InvalidOperationException(
                    "La cosecha ya está TERMINADA.");
            }

            // Validar que haya al menos 1 empleado registrado
            // (opcional, puedes comentar esto si quieres permitir finalizarla sin empleados)
            /*
            var empleados = new EmpleadoCosechaRepository().ObtenerPorCosecha(idCosecha);
            if (empleados.Count == 0)
            {
                throw new InvalidOperationException(
                    "No se puede finalizar una cosecha sin empleados registrados.");
            }
            */

            return _repository.TerminarCosecha(idCosecha);
        }

        // =============================================
        // ACTUALIZAR CANTIDAD TOTAL DESDE EMPLEADO_COSECHA
        // =============================================
        public bool ActualizarCantidadTotal(int idCosecha)
        {
            return _repository.ActualizarCantidadTotal(idCosecha);
        }

        // =============================================
        // VALIDAR SI SE PUEDE AGREGAR EMPLEADOS
        // =============================================
        public (bool valido, string mensaje) ValidarAgregarEmpleado(int idCosecha)
        {
            var cosecha = ObtenerPorId(idCosecha);

            if (cosecha == null)
                return (false, "La cosecha no existe.");

            if (cosecha.ESTADO == "TERMINADA")
                return (false, "La cosecha está TERMINADA. No se pueden agregar más empleados.");

            return (true, "OK");
        }
    }
}