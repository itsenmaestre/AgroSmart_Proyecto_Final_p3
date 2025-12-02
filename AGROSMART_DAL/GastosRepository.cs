using Oracle.ManagedDataAccess.Client;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AGROSMART_ENTITY.ENTIDADES_DTOS;

namespace AGROSMART_DAL
{
    public class GastoRepository
    {
        public List<GASTOS_DTO> ListarGastos()
        {
            var lista = new List<GASTOS_DTO>();

            // Query actualizado con FECHA_PROGRAMADA y ESTADO
            string sql = @"
            SELECT 
                t.ID_TAREA,
                t.TIPO_ACTIVIDAD AS NOMBRE_TAREA,
                NVL(c.NOMBRE_LOTE, 'Sin Cultivo') AS CULTIVO,
                t.FECHA_PROGRAMADA,
                t.ESTADO,
                NVL(SUM(dt.CANTIDAD_USADA * i.COSTO_UNITARIO), 0) AS GASTO_INSUMOS,
                NVL(SUM(a.PAGO_ACORDADO), 0) AS PAGO_EMPLEADOS,
                NVL(t.COSTO_TRANSPORTE, 0) AS GASTO_TRANSPORTE,
                (NVL(SUM(dt.CANTIDAD_USADA * i.COSTO_UNITARIO), 0) + 
                 NVL(SUM(a.PAGO_ACORDADO), 0) + 
                 NVL(t.COSTO_TRANSPORTE, 0)) AS TOTAL_GASTO
            FROM TAREA t
            LEFT JOIN CULTIVO c ON t.ID_CULTIVO = c.ID_CULTIVO
            LEFT JOIN DETALLE_TAREA dt ON dt.ID_TAREA = t.ID_TAREA
            LEFT JOIN INSUMO i ON dt.ID_INSUMO = i.ID_INSUMO AND i.TIPO = 'CONSUMIBLE'
            LEFT JOIN ASIGNACION_TAREA a ON a.ID_TAREA = t.ID_TAREA
            GROUP BY t.ID_TAREA, t.TIPO_ACTIVIDAD, c.NOMBRE_LOTE, t.COSTO_TRANSPORTE, t.FECHA_PROGRAMADA, t.ESTADO
            ORDER BY t.FECHA_PROGRAMADA DESC";

            try
            {
                using (OracleConnection cn = Conexion.CrearConexion())
                using (OracleCommand cmd = new OracleCommand(sql, cn))
                {
                    cn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new GASTOS_DTO
                            {
                                IdTarea = Convert.ToInt32(dr["ID_TAREA"]),
                                NombreTarea = dr["NOMBRE_TAREA"]?.ToString() ?? "Sin nombre",
                                Cultivo = dr["CULTIVO"]?.ToString() ?? "N/A",
                                FechaTarea = dr["FECHA_PROGRAMADA"] != DBNull.Value
                                    ? Convert.ToDateTime(dr["FECHA_PROGRAMADA"])
                                    : DateTime.MinValue,
                                Estado = dr["ESTADO"]?.ToString() ?? "SIN ESTADO",
                                GastoInsumos = Convert.ToDecimal(dr["GASTO_INSUMOS"]),
                                PagoEmpleados = Convert.ToDecimal(dr["PAGO_EMPLEADOS"]),
                                GastoTransporte = Convert.ToDecimal(dr["GASTO_TRANSPORTE"]),
                                TotalGasto = Convert.ToDecimal(dr["TOTAL_GASTO"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ListarGastos: {ex.Message}");
                throw new Exception($"Error al listar gastos: {ex.Message}", ex);
            }

            return lista;
        }

        public bool EliminarGasto(int idTarea)
        {
            string sql = "DELETE FROM TAREA WHERE ID_TAREA = :idTarea";

            try
            {
                using (OracleConnection cn = Conexion.CrearConexion())
                using (OracleCommand cmd = new OracleCommand(sql, cn))
                {
                    cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = idTarea;
                    cn.Open();
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    return filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarGasto: {ex.Message}");
                throw new Exception($"Error al eliminar gasto: {ex.Message}", ex);
            }
        }

        public GASTOS_DTO ObtenerGastoPorId(int idTarea)
        {
            string sql = @"
                SELECT 
                    t.ID_TAREA,
                    t.TIPO_ACTIVIDAD AS NOMBRE_TAREA,
                    NVL(c.NOMBRE_LOTE, 'Sin Cultivo') AS CULTIVO,
                    t.FECHA_PROGRAMADA,
                    t.ESTADO,
                    NVL(SUM(dt.CANTIDAD_USADA * i.COSTO_UNITARIO), 0) AS GASTO_INSUMOS,
                    NVL(SUM(a.PAGO_ACORDADO), 0) AS PAGO_EMPLEADOS,
                    NVL(t.COSTO_TRANSPORTE, 0) AS GASTO_TRANSPORTE,
                    (NVL(SUM(dt.CANTIDAD_USADA * i.COSTO_UNITARIO), 0) + 
                     NVL(SUM(a.PAGO_ACORDADO), 0) + 
                     NVL(t.COSTO_TRANSPORTE, 0)) AS TOTAL_GASTO
                FROM TAREA t
                LEFT JOIN CULTIVO c ON t.ID_CULTIVO = c.ID_CULTIVO
                LEFT JOIN DETALLE_TAREA dt ON dt.ID_TAREA = t.ID_TAREA
                LEFT JOIN INSUMO i ON dt.ID_INSUMO = i.ID_INSUMO AND i.TIPO = 'CONSUMIBLE'
                LEFT JOIN ASIGNACION_TAREA a ON a.ID_TAREA = t.ID_TAREA
                WHERE t.ID_TAREA = :idTarea
                GROUP BY t.ID_TAREA, t.TIPO_ACTIVIDAD, c.NOMBRE_LOTE, t.COSTO_TRANSPORTE, t.FECHA_PROGRAMADA, t.ESTADO";

            try
            {
                using (OracleConnection cn = Conexion.CrearConexion())
                using (OracleCommand cmd = new OracleCommand(sql, cn))
                {
                    cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = idTarea;
                    cn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return new GASTOS_DTO
                            {
                                IdTarea = Convert.ToInt32(dr["ID_TAREA"]),
                                NombreTarea = dr["NOMBRE_TAREA"]?.ToString() ?? "Sin nombre",
                                Cultivo = dr["CULTIVO"]?.ToString() ?? "N/A",
                                FechaTarea = dr["FECHA_PROGRAMADA"] != DBNull.Value
                                    ? Convert.ToDateTime(dr["FECHA_PROGRAMADA"])
                                    : DateTime.MinValue,
                                Estado = dr["ESTADO"]?.ToString() ?? "SIN ESTADO",
                                GastoInsumos = Convert.ToDecimal(dr["GASTO_INSUMOS"]),
                                PagoEmpleados = Convert.ToDecimal(dr["PAGO_EMPLEADOS"]),
                                GastoTransporte = Convert.ToDecimal(dr["GASTO_TRANSPORTE"]),
                                TotalGasto = Convert.ToDecimal(dr["TOTAL_GASTO"])
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerGastoPorId: {ex.Message}");
                throw new Exception($"Error al obtener gasto: {ex.Message}", ex);
            }

            return null;
        }
    }
}