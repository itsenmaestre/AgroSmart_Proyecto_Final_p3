using AGROSMART_ENTITY.ENTIDADES;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_DAL
{
    public class AsignacionTareaRepository
    {
        public List<ASIGNACION_TAREA> ListarPorEmpleado(int idEmpleado)
        {
            List<ASIGNACION_TAREA> lista = new List<ASIGNACION_TAREA>();
            string sql = "SELECT * FROM ASIGNACION_TAREA WHERE ID_EMPLEADO = :id ORDER BY FECHA_ASIGNACION DESC";

            using (OracleConnection cn = Conexion.CrearConexion())
            using (OracleCommand cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = idEmpleado;
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Mapear(dr));
                }
            }
            return lista;
        }

        public List<ASIGNACION_TAREA> ListarPorTarea(int idTarea)
        {
            List<ASIGNACION_TAREA> lista = new List<ASIGNACION_TAREA>();
            string sql = "SELECT * FROM ASIGNACION_TAREA WHERE ID_TAREA = :id ORDER BY FECHA_ASIGNACION DESC";

            using (OracleConnection cn = Conexion.CrearConexion())
            using (OracleCommand cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = idTarea;
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Mapear(dr));
                }
            }
            return lista;
        }

        public List<ASIGNACION_TAREA> ListarTodas()
        {
            List<ASIGNACION_TAREA> lista = new List<ASIGNACION_TAREA>();
            string sql = "SELECT * FROM ASIGNACION_TAREA ORDER BY FECHA_ASIGNACION DESC";

            using (OracleConnection cn = Conexion.CrearConexion())
            using (OracleCommand cmd = new OracleCommand(sql, cn))
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

        public string ActualizarAvance(ASIGNACION_TAREA a)
        {
            string sql = @"UPDATE ASIGNACION_TAREA
                           SET HORAS_TRABAJADAS = :horas,
                               JORNADAS_TRABAJADAS = :jornadas,
                               ESTADO = :estado
                           WHERE ID_ASIG_TAREA = :id";

            using (OracleConnection cn = Conexion.CrearConexion())
            using (OracleCommand cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":horas", OracleDbType.Decimal).Value = (object)a.HORAS_TRABAJADAS ?? DBNull.Value;
                cmd.Parameters.Add(":jornadas", OracleDbType.Decimal).Value = (object)a.JORNADAS_TRABAJADAS ?? DBNull.Value;
                cmd.Parameters.Add(":estado", OracleDbType.Varchar2).Value = a.ESTADO;
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = a.ID_ASIG_TAREA;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se actualizó el avance.";
            }
        }

        public string Asignar(ASIGNACION_TAREA a)
        {
            string sql = @"INSERT INTO ASIGNACION_TAREA 
                          (ID_TAREA, ID_EMPLEADO, ID_ADMIN_ASIGNADOR, FECHA_ASIGNACION, ESTADO, PAGO_ACORDADO)
                          VALUES (:tarea, :empleado, :admin, SYSDATE, :estado, :pago)";

            using (OracleConnection cn = Conexion.CrearConexion())
            using (OracleCommand cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":tarea", OracleDbType.Int32).Value = a.ID_TAREA;
                cmd.Parameters.Add(":empleado", OracleDbType.Int32).Value = a.ID_EMPLEADO;
                cmd.Parameters.Add(":admin", OracleDbType.Int32).Value = a.ID_ADMIN_ASIGNADOR;
                cmd.Parameters.Add(":estado", OracleDbType.Varchar2).Value = a.ESTADO ?? "ASIGNADA";
                cmd.Parameters.Add(":pago", OracleDbType.Decimal).Value = (object)a.PAGO_ACORDADO ?? DBNull.Value;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se asignó la tarea";
            }
        }

        public bool CancelarAsignacion(int idAsignacion)
        {
            string sql = "UPDATE ASIGNACION_TAREA SET ESTADO = 'CANCELADA' WHERE ID_ASIG_TAREA = :id";

            using (OracleConnection cn = Conexion.CrearConexion())
            using (OracleCommand cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = idAsignacion;
                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        private ASIGNACION_TAREA Mapear(OracleDataReader dr)
        {
            return new ASIGNACION_TAREA
            {
                ID_ASIG_TAREA = Convert.ToInt32(dr["ID_ASIG_TAREA"]),
                ID_TAREA = Convert.ToInt32(dr["ID_TAREA"]),
                ID_EMPLEADO = Convert.ToInt32(dr["ID_EMPLEADO"]),
                ID_ADMIN_ASIGNADOR = Convert.ToInt32(dr["ID_ADMIN_ASIGNADOR"]),
                FECHA_ASIGNACION = Convert.ToDateTime(dr["FECHA_ASIGNACION"]),
                ESTADO = dr["ESTADO"].ToString(),
                HORAS_TRABAJADAS = dr["HORAS_TRABAJADAS"] != DBNull.Value ? Convert.ToDecimal(dr["HORAS_TRABAJADAS"]) : (decimal?)null,
                JORNADAS_TRABAJADAS = dr["JORNADAS_TRABAJADAS"] != DBNull.Value ? Convert.ToDecimal(dr["JORNADAS_TRABAJADAS"]) : (decimal?)null,
                PAGO_ACORDADO = dr["PAGO_ACORDADO"] != DBNull.Value ? Convert.ToDecimal(dr["PAGO_ACORDADO"]) : (decimal?)null
            };
        }
    }
}
