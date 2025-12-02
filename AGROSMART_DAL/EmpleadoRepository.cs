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
    public class EmpleadoRepository : BaseRepository<EMPLEADO>
    {
        public override string Guardar(EMPLEADO e)
        {
            const string sql = @"
                INSERT INTO EMPLEADO
                (ID_USUARIO, MONTO_POR_HORA, MONTO_POR_JORNAL)
                VALUES
                (:p_id, :p_hora, :p_jornal)";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = e.ID_USUARIO;
                cmd.Parameters.Add(":p_hora", OracleDbType.Decimal).Value = e.MONTO_POR_HORA;
                cmd.Parameters.Add(":p_jornal", OracleDbType.Decimal).Value = e.MONTO_POR_JORNAL;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se insertó empleado";
            }
        }

        public override bool Actualizar(EMPLEADO e)
        {
            const string sql = @"
                UPDATE EMPLEADO
                SET MONTO_POR_HORA = :p_hora,
                    MONTO_POR_JORNAL = :p_jornal
                WHERE ID_USUARIO = :p_id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_hora", OracleDbType.Decimal).Value = e.MONTO_POR_HORA;
                cmd.Parameters.Add(":p_jornal", OracleDbType.Decimal).Value = e.MONTO_POR_JORNAL;
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = e.ID_USUARIO;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public int ContarTareasPorEmpleado(int idEmpleado)
        {
            const string sql = @"
        SELECT COUNT(*) 
        FROM ASIGNACION_TAREA
        WHERE ID_EMPLEADO = :ID_EMPLEADO";

            using (var conn = CrearConexion())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add(new OracleParameter("ID_EMPLEADO", idEmpleado));
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }


        public override bool Eliminar(EMPLEADO e)
        {
            const string sql = "DELETE FROM EMPLEADO WHERE ID_USUARIO = :p_id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = e.ID_USUARIO;
                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public override IList<EMPLEADO> Consultar()
        {
            const string sql = @"
                SELECT ID_USUARIO, MONTO_POR_HORA, MONTO_POR_JORNAL
                FROM EMPLEADO
                ORDER BY ID_USUARIO";

            List<EMPLEADO> lista = new List<EMPLEADO>();

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new EMPLEADO
                        {
                            ID_USUARIO = Convert.ToInt32(dr["ID_USUARIO"]),
                            MONTO_POR_HORA = Convert.ToDecimal(dr["MONTO_POR_HORA"]),
                            MONTO_POR_JORNAL = Convert.ToDecimal(dr["MONTO_POR_JORNAL"])
                        });
                    }
                }
            }
            return lista;
        }

        public override EMPLEADO ObtenerPorId(int id)
        {
            const string sql = @"
                SELECT ID_USUARIO, MONTO_POR_HORA, MONTO_POR_JORNAL
                FROM EMPLEADO
                WHERE ID_USUARIO = :p_id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = id;
                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (dr.Read())
                    {
                        return new EMPLEADO
                        {
                            ID_USUARIO = Convert.ToInt32(dr["ID_USUARIO"]),
                            MONTO_POR_HORA = Convert.ToDecimal(dr["MONTO_POR_HORA"]),
                            MONTO_POR_JORNAL = Convert.ToDecimal(dr["MONTO_POR_JORNAL"])
                        };
                    }
                }
            }
            return null;
        }
    }
}
