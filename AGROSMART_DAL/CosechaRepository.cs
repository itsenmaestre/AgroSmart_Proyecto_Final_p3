using AGROSMART_ENTITY.ENTIDADES;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;

namespace AGROSMART_DAL
{
    public class CosechaRepository : BaseRepository<COSECHA>
    {
        // =============================================
        // CONSULTAR TODAS LAS COSECHAS
        // =============================================
        public override IList<COSECHA> Consultar()
        {
            const string sql = @"
                SELECT ID_COSECHA, ID_CULTIVO, ID_ADMIN_REGISTRO,
                       FECHA_INICIO, FECHA_REGISTRO, FECHA_FINALIZACION,
                       CANTIDAD_OBTENIDA, UNIDAD_MEDIDA, CALIDAD, 
                       OBSERVACIONES, ESTADO
                FROM COSECHA
                ORDER BY FECHA_INICIO DESC, ID_COSECHA DESC";

            List<COSECHA> lista = new List<COSECHA>();

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

        // =============================================
        // OBTENER COSECHAS POR CULTIVO
        // =============================================
        public IList<COSECHA> ObtenerPorCultivo(int idCultivo)
        {
            const string sql = @"
                SELECT ID_COSECHA, ID_CULTIVO, ID_ADMIN_REGISTRO,
                       FECHA_INICIO, FECHA_REGISTRO, FECHA_FINALIZACION,
                       CANTIDAD_OBTENIDA, UNIDAD_MEDIDA, CALIDAD, 
                       OBSERVACIONES, ESTADO
                FROM COSECHA
                WHERE ID_CULTIVO = :ID
                ORDER BY FECHA_INICIO DESC";

            List<COSECHA> lista = new List<COSECHA>();

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":ID", idCultivo);
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Mapear(dr));
                }
            }
            return lista;
        }

        // =============================================
        // OBTENER POR ID
        // =============================================
        public override COSECHA ObtenerPorId(int id)
        {
            const string sql = @"
                SELECT ID_COSECHA, ID_CULTIVO, ID_ADMIN_REGISTRO,
                       FECHA_INICIO, FECHA_REGISTRO, FECHA_FINALIZACION,
                       CANTIDAD_OBTENIDA, UNIDAD_MEDIDA, CALIDAD, 
                       OBSERVACIONES, ESTADO
                FROM COSECHA
                WHERE ID_COSECHA = :ID";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":ID", id);
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                        return Mapear(dr);
                }
            }
            return null;
        }

        // =============================================
        // GUARDAR (INSERTAR UNA NUEVA COSECHA)
        // =============================================
        public override string Guardar(COSECHA entidad)
        {
            const string sql = @"
                INSERT INTO COSECHA
                (ID_COSECHA, ID_CULTIVO, ID_ADMIN_REGISTRO, FECHA_INICIO,
                 FECHA_REGISTRO, CANTIDAD_OBTENIDA, UNIDAD_MEDIDA,
                 CALIDAD, OBSERVACIONES, ESTADO)
                VALUES
                (SEQ_COSECHA.NEXTVAL, :CULTIVO, :ADMIN, :FINICIO,
                 :FREGISTRO, :CANTIDAD, :UNIDAD, :CALIDAD, :OBS, 'EN_PROCESO')
                RETURNING ID_COSECHA INTO :ID_OUT";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":CULTIVO", entidad.ID_CULTIVO);
                cmd.Parameters.Add(":ADMIN", entidad.ID_ADMIN_REGISTRO);
                cmd.Parameters.Add(":FINICIO", entidad.FECHA_INICIO);
                cmd.Parameters.Add(":FREGISTRO", entidad.FECHA_REGISTRO);
                cmd.Parameters.Add(":CANTIDAD", entidad.CANTIDAD_OBTENIDA);
                cmd.Parameters.Add(":UNIDAD", entidad.UNIDAD_MEDIDA);
                cmd.Parameters.Add(":CALIDAD", entidad.CALIDAD);
                cmd.Parameters.Add(":OBS", entidad.OBSERVACIONES ?? "");

                var outParam = new OracleParameter(":ID_OUT", OracleDbType.Int32)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                cn.Open();
                cmd.ExecuteNonQuery();

                return outParam.Value.ToString();
            }
        }

        // =============================================
        // ACTUALIZAR COSECHA EXISTENTE
        // ⚠️ Solo se pueden actualizar cosechas EN_PROCESO
        // =============================================
        public override bool Actualizar(COSECHA entidad)
        {
            const string sql = @"
                UPDATE COSECHA SET
                    CANTIDAD_OBTENIDA = :CANTIDAD,
                    CALIDAD           = :CALIDAD,
                    OBSERVACIONES     = :OBS
                WHERE ID_COSECHA = :ID
                  AND ESTADO = 'EN_PROCESO'";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":CANTIDAD", entidad.CANTIDAD_OBTENIDA);
                cmd.Parameters.Add(":CALIDAD", entidad.CALIDAD);
                cmd.Parameters.Add(":OBS", entidad.OBSERVACIONES ?? "");
                cmd.Parameters.Add(":ID", entidad.ID_COSECHA);

                cn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // =============================================
        // MARCAR UNA COSECHA COMO TERMINADA
        // =============================================
        public bool TerminarCosecha(int id)
        {
            const string sql = @"
                UPDATE COSECHA
                SET ESTADO = 'TERMINADA',
                    FECHA_FINALIZACION = SYSDATE
                WHERE ID_COSECHA = :ID
                  AND ESTADO = 'EN_PROCESO'";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":ID", id);
                cn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // =============================================
        // ACTUALIZAR LA CANTIDAD TOTAL A PARTIR DE EMPLEADO_COSECHA
        // =============================================
        public bool ActualizarCantidadTotal(int idCosecha)
        {
            const string sql = @"
                UPDATE COSECHA C SET
                    C.CANTIDAD_OBTENIDA = (
                        SELECT NVL(SUM(CANTIDAD_COSECHADA), 0)
                        FROM EMPLEADO_COSECHA
                        WHERE ID_COSECHA = :ID
                    )
                WHERE C.ID_COSECHA = :ID
                  AND C.ESTADO = 'EN_PROCESO'";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":ID", idCosecha);
                cn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // =============================================
        // MAPEAR LECTURA DE ORACLE A LA ENTIDAD
        // =============================================
        private COSECHA Mapear(OracleDataReader dr)
        {
            return new COSECHA
            {
                ID_COSECHA = Convert.ToInt32(dr["ID_COSECHA"]),
                ID_CULTIVO = Convert.ToInt32(dr["ID_CULTIVO"]),
                ID_ADMIN_REGISTRO = Convert.ToInt32(dr["ID_ADMIN_REGISTRO"]),
                FECHA_INICIO = Convert.ToDateTime(dr["FECHA_INICIO"]),
                FECHA_REGISTRO = Convert.ToDateTime(dr["FECHA_REGISTRO"]),
                FECHA_FINALIZACION = dr["FECHA_FINALIZACION"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(dr["FECHA_FINALIZACION"]),
                CANTIDAD_OBTENIDA = Convert.ToDecimal(dr["CANTIDAD_OBTENIDA"]),
                UNIDAD_MEDIDA = dr["UNIDAD_MEDIDA"].ToString(),
                CALIDAD = dr["CALIDAD"].ToString(),
                OBSERVACIONES = dr["OBSERVACIONES"] == DBNull.Value
                    ? ""
                    : dr["OBSERVACIONES"].ToString(),
                ESTADO = dr["ESTADO"].ToString()
            };
        }
    }
}