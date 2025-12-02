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
    public class InsumoRepository : BaseRepository<INSUMO>
    {
        public override IList<INSUMO> Consultar()
        {
            const string sql = @"
                SELECT ID_INSUMO, ID_ADMIN_REGISTRO, NOMBRE, TIPO, 
                       STOCK_ACTUAL, STOCK_MINIMO, COSTO_UNITARIO,
                        UNIDAD_MEDIDA
                FROM INSUMO
                ORDER BY NOMBRE";

            List<INSUMO> lista = new List<INSUMO>();

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

        public override INSUMO ObtenerPorId(int id)
        {
            const string sql = @"
                SELECT ID_INSUMO, ID_ADMIN_REGISTRO, NOMBRE, TIPO, 
                       STOCK_ACTUAL, STOCK_MINIMO, COSTO_UNITARIO,
                        UNIDAD_MEDIDA
                FROM INSUMO
                WHERE ID_INSUMO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = id;
                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    return dr.Read() ? Mapear(dr) : null;
            }
        }

        public override string Guardar(INSUMO entidad)
        {
            const string sql = @"
                INSERT INTO INSUMO (ID_ADMIN_REGISTRO, NOMBRE, TIPO, 
                                    STOCK_ACTUAL, STOCK_MINIMO, COSTO_UNITARIO,UNIDAD_MEDIDA)
                VALUES (:admin, :nombre, :tipo, :stock, :minimo, :costo, :unidad)";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":admin", OracleDbType.Int32).Value = entidad.ID_ADMIN_REGISTRO;
                cmd.Parameters.Add(":nombre", OracleDbType.Varchar2).Value = entidad.NOMBRE;
                cmd.Parameters.Add(":tipo", OracleDbType.Varchar2).Value = entidad.TIPO;
                cmd.Parameters.Add(":stock", OracleDbType.Decimal).Value = entidad.STOCK_ACTUAL;
                cmd.Parameters.Add(":minimo", OracleDbType.Decimal).Value = entidad.STOCK_MINIMO;
                cmd.Parameters.Add(":costo", OracleDbType.Decimal).Value = entidad.COSTO_UNITARIO;
                cmd.Parameters.Add(":unidad", OracleDbType.Varchar2).Value = entidad.UNIDAD_MEDIDA;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1 ? "OK" : "No se insertó el insumo";
            }
        }

        public override bool Actualizar(INSUMO entidad)
        {
            const string sql = @"
        UPDATE INSUMO
        SET ID_ADMIN_REGISTRO = :admin,
            NOMBRE = :nombre,
            TIPO = :tipo,
            STOCK_ACTUAL = :stock,
            STOCK_MINIMO = :minimo,
            COSTO_UNITARIO = :costo,
            UNIDAD_MEDIDA = :unidad
        WHERE ID_INSUMO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.BindByName = true;  // IMPORTANTE: Enlazar por nombre, no por posición

                cmd.Parameters.Add(":admin", OracleDbType.Int32).Value = entidad.ID_ADMIN_REGISTRO;
                cmd.Parameters.Add(":nombre", OracleDbType.Varchar2).Value = entidad.NOMBRE;
                cmd.Parameters.Add(":tipo", OracleDbType.Varchar2).Value = entidad.TIPO;
                cmd.Parameters.Add(":stock", OracleDbType.Decimal).Value = entidad.STOCK_ACTUAL;
                cmd.Parameters.Add(":minimo", OracleDbType.Decimal).Value = entidad.STOCK_MINIMO;
                cmd.Parameters.Add(":costo", OracleDbType.Decimal).Value = entidad.COSTO_UNITARIO;
                cmd.Parameters.Add(":unidad", OracleDbType.Varchar2).Value = entidad.UNIDAD_MEDIDA;
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_INSUMO;

                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public override bool Eliminar(INSUMO entidad)
        {
            const string sql = "DELETE FROM INSUMO WHERE ID_INSUMO = :id";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_INSUMO;
                cn.Open();
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        // Métodos adicionales específicos
        public List<INSUMO> ObtenerInsumosConStockBajo()
        {
            const string sql = @"
                SELECT ID_INSUMO, ID_ADMIN_REGISTRO, NOMBRE, TIPO, 
                       STOCK_ACTUAL, STOCK_MINIMO, COSTO_UNITARIO,
                        UNIDAD_MEDIDA
                FROM INSUMO
                WHERE STOCK_ACTUAL <= STOCK_MINIMO
                ORDER BY STOCK_ACTUAL ASC";

            List<INSUMO> lista = new List<INSUMO>();

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

        public int ContarInsumosConStockBajo()
        {
            const string sql = "SELECT COUNT(*) FROM INSUMO WHERE STOCK_ACTUAL <= STOCK_MINIMO";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }


        private INSUMO Mapear(OracleDataReader dr)
        {
            return new INSUMO
            {
                ID_INSUMO = Convert.ToInt32(dr["ID_INSUMO"]),
                ID_ADMIN_REGISTRO = Convert.ToInt32(dr["ID_ADMIN_REGISTRO"]),
                NOMBRE = dr["NOMBRE"].ToString(),
                TIPO = dr["TIPO"].ToString(),
                STOCK_ACTUAL = Convert.ToDecimal(dr["STOCK_ACTUAL"]),
                STOCK_MINIMO = Convert.ToDecimal(dr["STOCK_MINIMO"]),
                COSTO_UNITARIO = Convert.ToDecimal(dr["COSTO_UNITARIO"]),
                UNIDAD_MEDIDA = dr["UNIDAD_MEDIDA"].ToString()
            };
        }
    }
}
