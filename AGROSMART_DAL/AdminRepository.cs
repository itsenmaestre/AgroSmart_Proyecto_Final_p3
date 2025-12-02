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
    public class AdminRepository : BaseRepository<ADMINISTRADOR>
    {
        public override IList<ADMINISTRADOR> Consultar()
        {
            const string sql = @"
             SELECT A.ID_USUARIO, A.MONTO_MENSUAL,
                    U.PRIMER_NOMBRE, U.PRIMER_APELLIDO, U.EMAIL
             FROM ADMINISTRADOR A
             JOIN USUARIO U ON U.ID_USUARIO = A.ID_USUARIO
             ORDER BY U.PRIMER_NOMBRE";

            List<ADMINISTRADOR> lista = new List<ADMINISTRADOR>();

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

        public override ADMINISTRADOR ObtenerPorId(int id)
        {
            const string sql = @"
             SELECT A.ID_USUARIO, A.MONTO_MENSUAL,
                    U.PRIMER_NOMBRE, U.PRIMER_APELLIDO, U.EMAIL
             FROM ADMINISTRADOR A
             JOIN USUARIO U ON U.ID_USUARIO = A.ID_USUARIO
             WHERE A.ID_USUARIO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = id;
                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    return dr.Read() ? Mapear(dr) : null;
            }
        }

        public override string Guardar(ADMINISTRADOR entidad)
        {
            const string sql = @"
             INSERT INTO ADMINISTRADOR (ID_USUARIO, MONTO_MENSUAL)
             VALUES (:id, :monto)";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_USUARIO;
                cmd.Parameters.Add(":monto", OracleDbType.Decimal).Value = entidad.MONTO_MENSUAL;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se insertó el administrador";
            }
        }

        public override bool Actualizar(ADMINISTRADOR entidad)
        {
            const string sql = @"
             UPDATE ADMINISTRADOR
             SET MONTO_MENSUAL = :monto
             WHERE ID_USUARIO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":monto", OracleDbType.Decimal).Value = entidad.MONTO_MENSUAL;
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_USUARIO;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public override bool Eliminar(ADMINISTRADOR entidad)
        {
            const string sql = "DELETE FROM ADMINISTRADOR WHERE ID_USUARIO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_USUARIO;
                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        private ADMINISTRADOR Mapear(OracleDataReader dr)
        {
            return new ADMINISTRADOR
            {
                ID_USUARIO = Convert.ToInt32(dr["ID_USUARIO"]),
                MONTO_MENSUAL = Convert.ToDecimal(dr["MONTO_MENSUAL"])
            };
        }

        // Método para obtener estadísticas del admin
        public Dictionary<string, int> ObtenerEstadisticasGeneral(int idAdmin)
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();
            using (var cn = CrearConexion())
            {
                cn.Open();

                // Total de cultivos supervisados
                using (var cmd = new OracleCommand("SELECT COUNT(*) FROM CULTIVO WHERE ID_ADMIN_SUPERVISOR = :id", cn))
                {
                    cmd.Parameters.Add(":id", OracleDbType.Int32).Value = idAdmin;
                    stats["CultivosActivos"] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Total de tareas creadas
                using (var cmd = new OracleCommand("SELECT COUNT(*) FROM TAREA WHERE ID_ADMIN_CREADOR = :id", cn))
                {
                    cmd.Parameters.Add(":id", OracleDbType.Int32).Value = idAdmin;
                    stats["TareasCreadas"] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Total de empleados
                using (var cmd = new OracleCommand("SELECT COUNT(*) FROM EMPLEADO", cn))
                {
                    stats["TotalEmpleados"] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Tareas pendientes
                using (var cmd = new OracleCommand("SELECT COUNT(*) FROM TAREA WHERE ESTADO = 'PENDIENTE'", cn))
                {
                    stats["TareasPendientes"] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // *AGREGAR: Total de insumos*
                using (var cmd = new OracleCommand("SELECT COUNT(*) FROM INSUMO", cn))
                {
                    stats["TotalInsumos"] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // *AGREGAR: Insumos con stock bajo (opcional)*

            }
            return stats;
        }
    }
}
