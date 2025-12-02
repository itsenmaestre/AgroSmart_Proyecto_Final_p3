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
    public class UsuarioRepository : BaseRepository<USUARIO>
    {
    
        public USUARIO Autenticar(int idUsuario, string contrasena)
        {
            const string sql = @"
                SELECT ID_USUARIO, PRIMER_NOMBRE, SEGUNDO_NOMBRE, PRIMER_APELLIDO, SEGUNDO_APELLIDO,
                        EMAIL, CONTRASENA, TELEFONO
                FROM USUARIO
                WHERE ID_USUARIO = :p_id AND CONTRASENA = :p_contra";

            using (var cn = Conexion.CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = idUsuario;
                cmd.Parameters.Add(":p_contra", OracleDbType.Varchar2).Value = contrasena;

                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!dr.Read()) return null;
                    return Mapear(dr);
                }
            }
        }

     
        private USUARIO Mapear(OracleDataReader dr)
        {
            return new USUARIO
            {
                ID_USUARIO = dr.GetInt32(dr.GetOrdinal("ID_USUARIO")),
                PRIMER_NOMBRE = dr["PRIMER_NOMBRE"] as string,
                SEGUNDO_NOMBRE = dr["SEGUNDO_NOMBRE"] as string,
                PRIMER_APELLIDO = dr["PRIMER_APELLIDO"] as string,
                SEGUNDO_APELLIDO = dr["SEGUNDO_APELLIDO"] as string,
                EMAIL = dr["EMAIL"] as string,
                CONTRASENA = dr["CONTRASENA"] as string,
                TELEFONO = dr["TELEFONO"] as string
            };
        }

        public override IList<USUARIO> Consultar()
        {
            const string sql = "SELECT * FROM USUARIO ORDER BY ID_USUARIO";
            List<USUARIO> lista = new List<USUARIO>();

            using (var cn = Conexion.CrearConexion())
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

       
        public override USUARIO ObtenerPorId(int id)
        {
            const string sql = "SELECT * FROM USUARIO WHERE ID_USUARIO = :p_id";
            using (var cn = Conexion.CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = id;
                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    return dr.Read() ? Mapear(dr) : null;
            }
        }

       
        public override string Guardar(USUARIO e)
        {
            const string sql = @"
                INSERT INTO USUARIO
                (ID_USUARIO, PRIMER_NOMBRE, SEGUNDO_NOMBRE, PRIMER_APELLIDO, SEGUNDO_APELLIDO,
                  EMAIL, CONTRASENA, TELEFONO)
                VALUES
                (:p_id, :p_pnom, :p_snom, :p_pape, :p_sape, :p_mail, :p_pwd, :p_tel)";

            using (var cn = Conexion.CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = e.ID_USUARIO;
                cmd.Parameters.Add(":p_pnom", OracleDbType.Varchar2).Value = e.PRIMER_NOMBRE;
                cmd.Parameters.Add(":p_snom", OracleDbType.Varchar2).Value = (object)e.SEGUNDO_NOMBRE ?? DBNull.Value;
                cmd.Parameters.Add(":p_pape", OracleDbType.Varchar2).Value = e.PRIMER_APELLIDO;
                cmd.Parameters.Add(":p_sape", OracleDbType.Varchar2).Value = (object)e.SEGUNDO_APELLIDO ?? DBNull.Value;
                cmd.Parameters.Add(":p_mail", OracleDbType.Varchar2).Value = e.EMAIL;
                cmd.Parameters.Add(":p_pwd", OracleDbType.Varchar2).Value = e.CONTRASENA;
                cmd.Parameters.Add(":p_tel", OracleDbType.Varchar2).Value = e.TELEFONO;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se insertó usuario";
            }
        }

        
        public override bool Actualizar(USUARIO e)
        {
            const string sql = @"
                UPDATE USUARIO
                   SET PRIMER_NOMBRE=:p_pnom, SEGUNDO_NOMBRE=:p_snom, PRIMER_APELLIDO=:p_pape,
                       SEGUNDO_APELLIDO=:p_sape, EMAIL=:p_mail,
                       CONTRASENA=:p_pwd, TELEFONO=:p_tel
                 WHERE ID_USUARIO=:p_id";

            using (var cn = Conexion.CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_pnom", OracleDbType.Varchar2).Value = e.PRIMER_NOMBRE;
                cmd.Parameters.Add(":p_snom", OracleDbType.Varchar2).Value = (object)e.SEGUNDO_NOMBRE ?? DBNull.Value;
                cmd.Parameters.Add(":p_pape", OracleDbType.Varchar2).Value = e.PRIMER_APELLIDO;
                cmd.Parameters.Add(":p_sape", OracleDbType.Varchar2).Value = (object)e.SEGUNDO_APELLIDO ?? DBNull.Value;
                cmd.Parameters.Add(":p_mail", OracleDbType.Varchar2).Value = e.EMAIL;
                cmd.Parameters.Add(":p_pwd", OracleDbType.Varchar2).Value = e.CONTRASENA;
                cmd.Parameters.Add(":p_tel", OracleDbType.Varchar2).Value = e.TELEFONO;
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = e.ID_USUARIO;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

      
        public override bool Eliminar(USUARIO e)
        {
            const string sql = "DELETE FROM USUARIO WHERE ID_USUARIO = :p_id";
            using (var cn = Conexion.CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = e.ID_USUARIO;
                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

     
        public string RegistrarEmpleado(USUARIO u, EMPLEADO e)
        {
            using (OracleConnection cn = Conexion.CrearConexion())
            {
                cn.Open();
                OracleTransaction tx = cn.BeginTransaction();

                try
                {
                    
                    const string sqlUser = @"
                        INSERT INTO USUARIO
                        (ID_USUARIO, PRIMER_NOMBRE, SEGUNDO_NOMBRE, PRIMER_APELLIDO, SEGUNDO_APELLIDO,
                          EMAIL, CONTRASENA, TELEFONO)
                        VALUES
                        (:p_id, :p_pnom, :p_snom, :p_pape, :p_sape, :p_mail, :p_pwd, :p_tel)";

                    using (OracleCommand cmd = new OracleCommand(sqlUser, cn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = u.ID_USUARIO;
                        cmd.Parameters.Add(":p_pnom", OracleDbType.Varchar2).Value = u.PRIMER_NOMBRE;
                        cmd.Parameters.Add(":p_snom", OracleDbType.Varchar2).Value = (object)u.SEGUNDO_NOMBRE ?? DBNull.Value;
                        cmd.Parameters.Add(":p_pape", OracleDbType.Varchar2).Value = u.PRIMER_APELLIDO;
                        cmd.Parameters.Add(":p_sape", OracleDbType.Varchar2).Value = (object)u.SEGUNDO_APELLIDO ?? DBNull.Value;
                        cmd.Parameters.Add(":p_mail", OracleDbType.Varchar2).Value = u.EMAIL;
                        cmd.Parameters.Add(":p_pwd", OracleDbType.Varchar2).Value = u.CONTRASENA;
                        cmd.Parameters.Add(":p_tel", OracleDbType.Varchar2).Value = u.TELEFONO;

                        if (cmd.ExecuteNonQuery() != 1)
                            throw new Exception("No se insertó USUARIO.");
                    }

                    
                    const string sqlEmp = @"
                        INSERT INTO EMPLEADO (ID_USUARIO, MONTO_POR_HORA, MONTO_POR_JORNAL)
                        VALUES (:p_id, :p_hora, :p_jornal)";

                    using (OracleCommand cmd = new OracleCommand(sqlEmp, cn))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = u.ID_USUARIO;
                        cmd.Parameters.Add(":p_hora", OracleDbType.Decimal).Value = e.MONTO_POR_HORA;
                        cmd.Parameters.Add(":p_jornal", OracleDbType.Decimal).Value = e.MONTO_POR_JORNAL;

                        if (cmd.ExecuteNonQuery() != 1)
                            throw new Exception("No se insertó EMPLEADO.");
                    }

                    tx.Commit();
                    return "OK";
                }
                catch (OracleException ex)
                {
                    tx.Rollback();

                    
                    if (ex.Number == 1) 
                        return "Error: La cédula ya está registrada en el sistema.";

                    return $"Error Oracle {ex.Number}: {ex.Message}";
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return $"Error: {ex.Message}";
                }
            }
        }

       
        public bool EsAdministrador(int idUsuario)
        {
            const string sql = "SELECT 1 FROM ADMINISTRADOR WHERE ID_USUARIO=:p_id";
            using (var cn = Conexion.CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = idUsuario;
                cn.Open();
                var r = cmd.ExecuteScalar();
                return r != null;
            }
        }

        public bool EsEmpleado(int idUsuario)
        {
            const string sql = "SELECT 1 FROM EMPLEADO WHERE ID_USUARIO=:p_id";
            using (var cn = Conexion.CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":p_id", OracleDbType.Int32).Value = idUsuario;
                cn.Open();
                var r = cmd.ExecuteScalar();
                return r != null;
            }
        }
    }
}
