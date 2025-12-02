using System;
using System.Collections.Generic;

namespace AGROSMART_ENTITY.ENTIDADES_DTOS
{
    /// <summary>
    /// DTO para Liquidaciones de Cosecha
    /// Mapea datos de EMPLEADO_COSECHA, EMPLEADO, USUARIO y COSECHA
    /// </summary>
    public class Liquidación_DTO
    {
        #region Identificadores
        public int IdLiquidacion { get; set; }           // ID_EMPLEADO_COSECHA
        public int IdEmpleado { get; set; }              // ID_EMPLEADO
        public int IdUsuario { get; set; }               // ID_USUARIO
        public int IdCosecha { get; set; }               // ID_COSECHA
        #endregion

        #region Datos Principales
        public string Nombre { get; set; }               // Nombre completo del empleado
        public decimal Cantidad { get; set; }            // Cantidad cosechada
        public string UnidadMedida { get; set; }         // Unidad de medida
        public decimal ValorUnidad { get; set; }         // Valor unitario
        public DateTime FechaTrabajo { get; set; }       // Fecha del trabajo
        #endregion

        #region Cálculos Financieros
        public decimal PagoBruto { get; set; }           // Cantidad * ValorUnidad
        public decimal Deducciones { get; set; }         // Valor deducciones
        public decimal PagoNeto { get; set; }            // PagoBruto - Deducciones (CALCULADO)
        #endregion

        #region Información Adicional
        public string Observaciones { get; set; }        // Notas adicionales
        public DateTime FechaRegistro { get; set; }      // Fecha de registro
        public string NombreCultivo { get; set; }        // Nombre del cultivo (para display)
        public string EstadoCosecha { get; set; }        // Estado de la cosecha (EN_PROCESO, TERMINADA)
        public string TiempoCosecha { get; set; }        // Duración de la cosecha (ej: "5 días")
        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Recalcula los valores bruto y neto
        /// </summary>
        public void RecalcularTotales()
        {
            PagoBruto = Cantidad * ValorUnidad;
            PagoNeto = PagoBruto - Deducciones;
        }

        /// <summary>
        /// Valida que los datos sean consistentes
        /// </summary>
        public bool EsValido()
        {
            return IdLiquidacion > 0 &&
                   IdEmpleado > 0 &&
                   IdCosecha > 0 &&
                   Cantidad > 0 &&
                   ValorUnidad > 0 &&
                   Deducciones >= 0 &&
                   FechaTrabajo != DateTime.MinValue;
        }

        #endregion
    }
}