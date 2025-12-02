using AGROSMART_ENTITY.ENTIDADES;
using AGROSMART_ENTITY.ENTIDADES_DTOS;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_DAL
{
    public class TareaRepository : BaseRepository<TAREA>
    {
        public override IList<TAREA> Consultar()
        {
            const string sql = @"
                SELECT ID_TAREA, ID_CULTIVO, ID_ADMIN_CREADOR, TIPO_ACTIVIDAD,
                       FECHA_PROGRAMADA, TIEMPO_TOTAL_TAREA, ESTADO, ES_RECURRENTE,
                       FRECUENCIA_DIAS, COSTO_TRANSPORTE
                FROM TAREA
                ORDER BY FECHA_PROGRAMADA DESC";

            List<TAREA> lista = new List<TAREA>();

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Mapear(dr));
                }
            }
            return lista;
        }

        public override TAREA ObtenerPorId(int id)
        {
            const string sql = @"
                SELECT ID_TAREA, ID_CULTIVO, ID_ADMIN_CREADOR, TIPO_ACTIVIDAD,
                       FECHA_PROGRAMADA, TIEMPO_TOTAL_TAREA, ESTADO, ES_RECURRENTE,
                       FRECUENCIA_DIAS, COSTO_TRANSPORTE
                FROM TAREA
                WHERE ID_TAREA = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = id;
                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    return dr.Read() ? Mapear(dr) : null;
            }
        }

        public override string Guardar(TAREA entidad)
        {
            const string sql = @"
                INSERT INTO TAREA (ID_CULTIVO, ID_ADMIN_CREADOR, TIPO_ACTIVIDAD,
                                   FECHA_PROGRAMADA, TIEMPO_TOTAL_TAREA, ESTADO, ES_RECURRENTE,
                                   FRECUENCIA_DIAS, COSTO_TRANSPORTE)
                VALUES (:cultivo, :admin, :tipo, :fecha, :tiempo, :estado, :recurrente, :frecuencia, :transporte)";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":cultivo", OracleDbType.Int32).Value = entidad.ID_CULTIVO;
                cmd.Parameters.Add(":admin", OracleDbType.Int32).Value = entidad.ID_ADMIN_CREADOR;
                cmd.Parameters.Add(":tipo", OracleDbType.Varchar2).Value = entidad.TIPO_ACTIVIDAD;
                cmd.Parameters.Add(":fecha", OracleDbType.Date).Value = entidad.FECHA_PROGRAMADA;
                cmd.Parameters.Add(":tiempo", OracleDbType.Decimal).Value = entidad.TIEMPO_TOTAL_TAREA;
                cmd.Parameters.Add(":estado", OracleDbType.Varchar2).Value = entidad.ESTADO ?? "PENDIENTE";
                cmd.Parameters.Add(":recurrente", OracleDbType.Varchar2).Value = entidad.ES_RECURRENTE ?? "F";
                cmd.Parameters.Add(":frecuencia", OracleDbType.Int32).Value = (object)entidad.FRECUENCIA_DIAS ?? DBNull.Value;
                cmd.Parameters.Add(":transporte", OracleDbType.Decimal).Value = (object)entidad.COSTO_TRANSPORTE ?? DBNull.Value;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se insertó la tarea";
            }
        }

        public override bool Actualizar(TAREA entidad)
        {
            const string sql = @"
                UPDATE TAREA
                SET ID_CULTIVO = :cultivo,
                    ID_ADMIN_CREADOR = :admin,
                    TIPO_ACTIVIDAD = :tipo,
                    FECHA_PROGRAMADA = :fecha,
                    TIEMPO_TOTAL_TAREA = :tiempo,
                    ESTADO = :estado,
                    ES_RECURRENTE = :recurrente,
                    FRECUENCIA_DIAS = :frecuencia,
                    COSTO_TRANSPORTE = :transporte
                WHERE ID_TAREA = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":cultivo", OracleDbType.Int32).Value = entidad.ID_CULTIVO;
                cmd.Parameters.Add(":admin", OracleDbType.Int32).Value = entidad.ID_ADMIN_CREADOR;
                cmd.Parameters.Add(":tipo", OracleDbType.Varchar2).Value = entidad.TIPO_ACTIVIDAD;
                cmd.Parameters.Add(":fecha", OracleDbType.Date).Value = entidad.FECHA_PROGRAMADA;
                cmd.Parameters.Add(":tiempo", OracleDbType.Decimal).Value = entidad.TIEMPO_TOTAL_TAREA;
                cmd.Parameters.Add(":estado", OracleDbType.Varchar2).Value = entidad.ESTADO;
                cmd.Parameters.Add(":recurrente", OracleDbType.Varchar2).Value = entidad.ES_RECURRENTE;
                cmd.Parameters.Add(":frecuencia", OracleDbType.Int32).Value = (object)entidad.FRECUENCIA_DIAS ?? DBNull.Value;
                cmd.Parameters.Add(":transporte", OracleDbType.Decimal).Value = (object)entidad.COSTO_TRANSPORTE ?? DBNull.Value;
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_TAREA;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public override bool Eliminar(TAREA entidad)
        {
            const string sql = "DELETE FROM TAREA WHERE ID_TAREA = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_TAREA;
                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public List<TAREA> ObtenerPorCultivo(int idCultivo)
        {
            const string sql = @"
                SELECT ID_TAREA, ID_CULTIVO, ID_ADMIN_CREADOR, TIPO_ACTIVIDAD,
                       FECHA_PROGRAMADA, TIEMPO_TOTAL_TAREA, ESTADO, ES_RECURRENTE,
                       FRECUENCIA_DIAS, COSTO_TRANSPORTE
                FROM TAREA
                WHERE ID_CULTIVO = :id
                ORDER BY FECHA_PROGRAMADA DESC";

            List<TAREA> lista = new List<TAREA>();

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = idCultivo;
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Mapear(dr));
                }
            }
            return lista;
        }

        public List<TAREA> ObtenerPorEstado(string estado)
        {
            const string sql = @"
                SELECT ID_TAREA, ID_CULTIVO, ID_ADMIN_CREADOR, TIPO_ACTIVIDAD,
                       FECHA_PROGRAMADA, TIEMPO_TOTAL_TAREA, ESTADO, ES_RECURRENTE,
                       FRECUENCIA_DIAS, COSTO_TRANSPORTE
                FROM TAREA
                WHERE ESTADO = :estado
                ORDER BY FECHA_PROGRAMADA";

            List<TAREA> lista = new List<TAREA>();

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":estado", OracleDbType.Varchar2).Value = estado;
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Mapear(dr));
                }
            }
            return lista;
        }

        // ===== MÉTODOS NUEVOS PARA ESTADÍSTICAS DEL EMPLEADO =====

        /// <summary>
        /// Cuenta tareas asignadas a un empleado en una fecha específica
        /// </summary>
        public int ContarTareasPorEmpleadoYFecha(int idEmpleado, DateTime fecha)
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM TAREA T
                INNER JOIN ASIGNACION_TAREA A ON A.ID_TAREA = T.ID_TAREA
                WHERE A.ID_EMPLEADO = :idEmpleado 
                AND TRUNC(T.FECHA_PROGRAMADA) = TRUNC(:fecha)";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":idEmpleado", OracleDbType.Int32).Value = idEmpleado;
                cmd.Parameters.Add(":fecha", OracleDbType.Date).Value = fecha;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Cuenta tareas de un empleado por estado (desde ASIGNACION_TAREA)
        /// </summary>
        public int ContarTareasPorEmpleadoYEstado(int idEmpleado, string estado)
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM ASIGNACION_TAREA 
                WHERE ID_EMPLEADO = :idEmpleado 
                AND ESTADO = :estado";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":idEmpleado", OracleDbType.Int32).Value = idEmpleado;
                cmd.Parameters.Add(":estado", OracleDbType.Varchar2).Value = estado;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Cuenta tareas vencidas de un empleado (fecha programada < hoy y no finalizadas)
        /// </summary>
        public int ContarTareasVencidasPorEmpleado(int idEmpleado, DateTime fechaActual)
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM TAREA T
                INNER JOIN ASIGNACION_TAREA A ON A.ID_TAREA = T.ID_TAREA
                WHERE A.ID_EMPLEADO = :idEmpleado 
                AND T.FECHA_PROGRAMADA < :fechaActual
                AND A.ESTADO != 'FINALIZADA'";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":idEmpleado", OracleDbType.Int32).Value = idEmpleado;
                cmd.Parameters.Add(":fechaActual", OracleDbType.Date).Value = fechaActual;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private TAREA Mapear(OracleDataReader dr)
        {
            return new TAREA
            {
                ID_TAREA = Convert.ToInt32(dr["ID_TAREA"]),
                ID_CULTIVO = Convert.ToInt32(dr["ID_CULTIVO"]),
                ID_ADMIN_CREADOR = Convert.ToInt32(dr["ID_ADMIN_CREADOR"]),
                TIPO_ACTIVIDAD = dr["TIPO_ACTIVIDAD"].ToString(),
                FECHA_PROGRAMADA = Convert.ToDateTime(dr["FECHA_PROGRAMADA"]),
                TIEMPO_TOTAL_TAREA = Convert.ToDecimal(dr["TIEMPO_TOTAL_TAREA"]),
                ESTADO = dr["ESTADO"].ToString(),
                ES_RECURRENTE = dr["ES_RECURRENTE"].ToString(),
                FRECUENCIA_DIAS = dr["FRECUENCIA_DIAS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["FRECUENCIA_DIAS"]),
                COSTO_TRANSPORTE = dr["COSTO_TRANSPORTE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["COSTO_TRANSPORTE"])
            };
        }
    }
}
