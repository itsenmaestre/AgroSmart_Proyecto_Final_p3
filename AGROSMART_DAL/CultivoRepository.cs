using AGROSMART_ENTITY.ENTIDADES;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_DAL
{
    public class CultivoRepository : BaseRepository<CULTIVO>
    {
        public override IList<CULTIVO> Consultar()
        {
            const string sql = @"
                SELECT ID_CULTIVO, ID_ADMIN_SUPERVISOR, NOMBRE_LOTE, 
                       FECHA_SIEMBRA, FECHA_COSECHA_ESTIMADA, ALERTA_N8N
                FROM CULTIVO
                ORDER BY FECHA_SIEMBRA DESC";

            List<CULTIVO> lista = new List<CULTIVO>();

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

        public override CULTIVO ObtenerPorId(int id)
        {
            const string sql = @"
                SELECT ID_CULTIVO, ID_ADMIN_SUPERVISOR, NOMBRE_LOTE, 
                       FECHA_SIEMBRA, FECHA_COSECHA_ESTIMADA, ALERTA_N8N
                FROM CULTIVO
                WHERE ID_CULTIVO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = id;
                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    return dr.Read() ? Mapear(dr) : null;
            }
        }

        public override string Guardar(CULTIVO entidad)
        {
            const string sql = @"
                INSERT INTO CULTIVO (ID_ADMIN_SUPERVISOR, NOMBRE_LOTE, 
                                     FECHA_SIEMBRA, FECHA_COSECHA_ESTIMADA, ALERTA_N8N)
                VALUES (:admin, :nombre, :siembra, :cosecha, :alerta)";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":admin", OracleDbType.Int32).Value = entidad.ID_ADMIN_SUPERVISOR;
                cmd.Parameters.Add(":nombre", OracleDbType.Varchar2).Value = entidad.NOMBRE_LOTE;
                cmd.Parameters.Add(":siembra", OracleDbType.Date).Value = entidad.FECHA_SIEMBRA;
                cmd.Parameters.Add(":cosecha", OracleDbType.Date).Value = entidad.FECHA_COSECHA_ESTIMADA;
                cmd.Parameters.Add(":alerta", OracleDbType.Varchar2).Value = entidad.ALERTA_N8N ?? "SIN ALERTA";

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se insertó el cultivo";
            }
        }

        public override bool Actualizar(CULTIVO entidad)
        {
            const string sql = @"
                UPDATE CULTIVO
                SET ID_ADMIN_SUPERVISOR = :admin,
                    NOMBRE_LOTE = :nombre,
                    FECHA_SIEMBRA = :siembra,
                    FECHA_COSECHA_ESTIMADA = :cosecha,
                    ALERTA_N8N = :alerta
                WHERE ID_CULTIVO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":admin", OracleDbType.Int32).Value = entidad.ID_ADMIN_SUPERVISOR;
                cmd.Parameters.Add(":nombre", OracleDbType.Varchar2).Value = entidad.NOMBRE_LOTE;
                cmd.Parameters.Add(":siembra", OracleDbType.Date).Value = entidad.FECHA_SIEMBRA;
                cmd.Parameters.Add(":cosecha", OracleDbType.Date).Value = entidad.FECHA_COSECHA_ESTIMADA;
                cmd.Parameters.Add(":alerta", OracleDbType.Varchar2).Value = entidad.ALERTA_N8N ?? "SIN ALERTA";
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_CULTIVO;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public override bool Eliminar(CULTIVO entidad)
        {
            const string sql = "DELETE FROM CULTIVO WHERE ID_CULTIVO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_CULTIVO;
                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        private CULTIVO Mapear(OracleDataReader dr)
        {
            return new CULTIVO
            {
                ID_CULTIVO = Convert.ToInt32(dr["ID_CULTIVO"]),
                ID_ADMIN_SUPERVISOR = Convert.ToInt32(dr["ID_ADMIN_SUPERVISOR"]),
                NOMBRE_LOTE = dr["NOMBRE_LOTE"].ToString(),
                FECHA_SIEMBRA = Convert.ToDateTime(dr["FECHA_SIEMBRA"]),
                FECHA_COSECHA_ESTIMADA = Convert.ToDateTime(dr["FECHA_COSECHA_ESTIMADA"]),
                ALERTA_N8N = dr["ALERTA_N8N"].ToString()
            };
        }
    }
}
