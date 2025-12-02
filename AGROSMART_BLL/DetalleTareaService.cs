using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using AGROSMART_ENTITY.ENTIDADES_DTOS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public class DetalleTareaService
    {
        private readonly DetalleTareaRepository _repo = new DetalleTareaRepository();
        private readonly InsumoRepository _insumoRepo = new InsumoRepository();

      
        public ReadOnlyCollection<DETALLE_TAREA> Consultar()
        {
            try
            {
                return new ReadOnlyCollection<DETALLE_TAREA>(_repo.Consultar().ToList());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al consultar detalles de tarea: {ex.Message}", ex);
            }
        }

        
        public DETALLE_TAREA ObtenerPorId(int idDetalle)
        {
            if (idDetalle <= 0)
                throw new ArgumentException("El ID del detalle debe ser mayor a cero.");

            try
            {
                return _repo.ObtenerPorId(idDetalle);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener detalle por ID: {ex.Message}", ex);
            }
        }

      
        public List<DETALLE_TAREA> ObtenerPorTarea(int idTarea)
        {
            if (idTarea <= 0)
                throw new ArgumentException("ID de tarea inválido.");

            try
            {
                return _repo.ObtenerPorTarea(idTarea);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener detalles por tarea: {ex.Message}", ex);
            }
        }


        public string Guardar(DETALLE_TAREA entidad)
        {
            // Validaciones básicas
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad), "El detalle no puede ser nulo.");

            if (entidad.ID_TAREA <= 0)
                throw new ArgumentException("El ID de tarea es inválido.");

            if (entidad.ID_INSUMO <= 0)
                throw new ArgumentException("El ID de insumo es inválido.");

            if (entidad.CANTIDAD_USADA <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero.");

            try
            {
                // Validación previa
                var insumo = _insumoRepo.ObtenerPorId(entidad.ID_INSUMO);
                if (insumo == null)
                    throw new ArgumentException("El insumo especificado no existe.");

                if (insumo.TIPO != "CONSUMIBLE")
                    throw new ArgumentException("Solo se pueden usar insumos de tipo CONSUMIBLE en tareas.");

                if (insumo.STOCK_ACTUAL < entidad.CANTIDAD_USADA)
                {
                    throw new InvalidOperationException(
                        $"Stock insuficiente. Disponible: {insumo.STOCK_ACTUAL} {insumo.UNIDAD_MEDIDA}, " +
                        $"Requerido: {entidad.CANTIDAD_USADA} {insumo.UNIDAD_MEDIDA}");
                }

                // Intentar guardar
                string resultado = _repo.Guardar(entidad);

                // ⭐ Procesar resultado
                if (resultado.StartsWith("STOCK_INSUFICIENTE|"))
                {
                    var partes = resultado.Split('|');
                    throw new InvalidOperationException(
                        $"Stock insuficiente para '{partes[1]}'.\n" +
                        $"Disponible: {partes[2]} {partes[3]}\n" +
                        $"Solicitado: {partes[4]} {partes[3]}");
                }

                if (resultado.StartsWith("STOCK_NEGATIVO_BLOQUEADO|"))
                {
                    var partes = resultado.Split('|');
                    throw new InvalidOperationException(
                        $"OPERACIÓN BLOQUEADA: El stock de '{partes[1]}' quedaría en {partes[5]} {partes[3]}.\n" +
                        $"No se permiten stocks negativos.");
                }

                if (resultado.StartsWith("CONCURRENCIA|"))
                {
                    throw new InvalidOperationException(resultado.Split('|')[1]);
                }

                return resultado;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-lanzar excepciones de negocio
            }
            catch (ArgumentException)
            {
                throw; // Re-lanzar excepciones de validación
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar detalle de tarea: {ex.Message}", ex);
            }
        }

        public string RegistrarInsumosConDescuento(int idTarea, List<DETALLE_TAREA> detalles)
        {
            if (idTarea <= 0)
                throw new ArgumentException("ID de tarea inválido.");

            if (detalles == null || detalles.Count == 0)
                throw new ArgumentException("Debe especificar al menos un insumo.");

            // Validar cantidades y stock disponible
            foreach (var det in detalles)
            {
                if (det.CANTIDAD_USADA <= 0)
                    throw new ArgumentException("Las cantidades deben ser mayores a cero.");

                var insumo = _insumoRepo.ObtenerPorId(det.ID_INSUMO);
                if (insumo == null)
                    throw new ArgumentException($"El insumo con ID {det.ID_INSUMO} no existe.");

                if (insumo.TIPO != "CONSUMIBLE")
                    throw new ArgumentException($"El insumo '{insumo.NOMBRE}' no es de tipo CONSUMIBLE.");

                if (insumo.STOCK_ACTUAL < det.CANTIDAD_USADA)
                {
                    throw new InvalidOperationException(
                        $"Stock insuficiente para '{insumo.NOMBRE}'. " +
                        $"Disponible: {insumo.STOCK_ACTUAL}, Requerido: {det.CANTIDAD_USADA}");
                }
            }

            try
            {
                return _repo.RegistrarInsumosConDescuento(idTarea, detalles);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al registrar insumos con descuento: {ex.Message}", ex);
            }
        }

       
        public bool Actualizar(DETALLE_TAREA entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad), "El detalle no puede ser nulo.");

            if (entidad.ID_DETALLE_TAREA <= 0)
                throw new ArgumentException("El ID del detalle es inválido.");

            if (entidad.CANTIDAD_USADA <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero.");

            try
            {
               
                var detalleAnterior = _repo.ObtenerPorId(entidad.ID_DETALLE_TAREA);
                if (detalleAnterior == null)
                    throw new ArgumentException("El detalle especificado no existe.");

                
                decimal diferencia = entidad.CANTIDAD_USADA - detalleAnterior.CANTIDAD_USADA;

                
                if (diferencia > 0)
                {
                    var insumo = _insumoRepo.ObtenerPorId(entidad.ID_INSUMO);
                    if (insumo == null)
                        throw new ArgumentException("El insumo no existe.");

                    if (insumo.STOCK_ACTUAL < diferencia)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente para el ajuste. " +
                            $"Disponible: {insumo.STOCK_ACTUAL} {insumo.UNIDAD_MEDIDA}, " +
                            $"Adicional requerido: {diferencia} {insumo.UNIDAD_MEDIDA}");
                    }
                }

                return _repo.Actualizar(entidad);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar detalle de tarea: {ex.Message}", ex);
            }
        }

        public bool Eliminar(DETALLE_TAREA entidad)
        {
            if (entidad == null || entidad.ID_DETALLE_TAREA <= 0)
                throw new ArgumentException("Detalle inválido para eliminar.");

            try
            {
                // El repositorio se encarga de devolver el stock automáticamente
                return _repo.Eliminar(entidad);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar detalle de tarea: {ex.Message}", ex);
            }
        }

        public bool EliminarPorId(int idDetalle)
        {
            if (idDetalle <= 0)
                throw new ArgumentException("ID de detalle inválido.");

            try
            {
                var detalle = _repo.ObtenerPorId(idDetalle);
                if (detalle == null)
                    throw new ArgumentException("El detalle no existe.");

                return _repo.Eliminar(detalle);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar detalle por ID: {ex.Message}", ex);
            }
        }

     
        public bool EliminarPorTarea(int idTarea)
        {
            if (idTarea <= 0)
                throw new ArgumentException("ID de tarea inválido.");

            try
            {
                return _repo.EliminarPorTarea(idTarea);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar detalles por tarea: {ex.Message}", ex);
            }
        }

       
        public decimal CalcularCostoInsumosPorTarea(int idTarea)
        {
            if (idTarea <= 0)
                throw new ArgumentException("ID de tarea inválido.");

            try
            {
                var detalles = _repo.ObtenerPorTarea(idTarea);
                decimal total = 0;

                foreach (var detalle in detalles)
                {
                    var insumo = _insumoRepo.ObtenerPorId(detalle.ID_INSUMO);
                    if (insumo != null)
                    {
                        total += detalle.CANTIDAD_USADA * insumo.COSTO_UNITARIO;
                    }
                }

                return total;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al calcular costo de insumos: {ex.Message}", ex);
            }
        }

       
        public List<ResumenInsumoTarea> ObtenerResumenInsumosPorTarea(int idTarea)
        {
            if (idTarea <= 0)
                throw new ArgumentException("ID de tarea inválido.");

            try
            {
                var detalles = _repo.ObtenerPorTarea(idTarea);
                var resumen = new List<ResumenInsumoTarea>();

                foreach (var detalle in detalles)
                {
                    var insumo = _insumoRepo.ObtenerPorId(detalle.ID_INSUMO);
                    if (insumo != null)
                    {
                        resumen.Add(new ResumenInsumoTarea
                        {
                            NombreInsumo = insumo.NOMBRE,
                            CantidadUsada = detalle.CANTIDAD_USADA,
                            UnidadMedida = insumo.UNIDAD_MEDIDA,
                            CostoUnitario = insumo.COSTO_UNITARIO,
                            CostoTotal = detalle.CANTIDAD_USADA * insumo.COSTO_UNITARIO
                        });
                    }
                }

                return resumen;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener resumen de insumos: {ex.Message}", ex);
            }
        }

        public int ContarInsumosUsadosEnTarea(int idTarea)
        {
            if (idTarea <= 0)
                throw new ArgumentException("ID de tarea inválido.");

            try
            {
                return _repo.ObtenerPorTarea(idTarea).Count;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al contar insumos usados: {ex.Message}", ex);
            }
        }

        public bool TareaTieneInsumos(int idTarea)
        {
            if (idTarea <= 0)
                return false;

            try
            {
                return _repo.ObtenerPorTarea(idTarea).Count > 0;
            }
            catch
            {
                return false;
            }
        }

       
        public (bool esValido, string mensaje) ValidarDisponibilidadInsumo(int idInsumo, decimal cantidad)
        {
            if (idInsumo <= 0)
                return (false, "ID de insumo inválido.");

            if (cantidad <= 0)
                return (false, "La cantidad debe ser mayor a cero.");

            try
            {
                var insumo = _insumoRepo.ObtenerPorId(idInsumo);

                if (insumo == null)
                    return (false, "El insumo no existe.");

                if (insumo.TIPO != "CONSUMIBLE")
                    return (false, $"El insumo '{insumo.NOMBRE}' no es de tipo CONSUMIBLE.");

                if (insumo.STOCK_ACTUAL < cantidad)
                {
                    return (false, $"Stock insuficiente. Disponible: {insumo.STOCK_ACTUAL} {insumo.UNIDAD_MEDIDA}, " +
                                  $"Requerido: {cantidad} {insumo.UNIDAD_MEDIDA}");
                }

                return (true, "Stock disponible");
            }
            catch (Exception ex)
            {
                return (false, $"Error al validar: {ex.Message}");
            }
        }
    }

   
    public class ResumenInsumoTarea
    {
        public string NombreInsumo { get; set; }
        public decimal CantidadUsada { get; set; }
        public string UnidadMedida { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CostoTotal { get; set; }
    }
}