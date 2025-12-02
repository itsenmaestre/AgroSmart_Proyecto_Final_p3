using AGROSMART_ENTITY.ENTIDADES;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;

namespace AGROSMART_DAL
{
    public class EmpleadoCosechaRepository : BaseRepository<EMPLEADO_COSECHA>
    {
        // =============================================
        // CONSULTAR TODOS LOS REGISTROS
        // =============================================
        public override IList<EMPLEADO_COSECHA> Consultar()
        {
            const string sql = @"
                SELECT ID_EMPLEADO_COSECHA,
                       ID_EMPLEADO,
                       ID_COSECHA,
                       CANTIDAD_COSECHADA,
                       VALOR_UNITARIO,
                       PRECIO_BRUTO,
                       DEDUCCIONES,
                       PRECIO_NETO,
                       FECHA_TRABAJO,
                       OBSERVACIONES
                FROM EMPLEADO_COSECHA
                ORDER BY FECHA_TRABAJO DESC";

            var lista = new List<EMPLEADO_COSECHA>();

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(Mapear(dr));
                    }
                }
            }

            return lista;
        }

        // =============================================
        // OBTENER POR ID
        // =============================================
        public override EMPLEADO_COSECHA ObtenerPorId(int id)
        {
            const string sql = @"
                SELECT ID_EMPLEADO_COSECHA,
                       ID_EMPLEADO,
                       ID_COSECHA,
                       CANTIDAD_COSECHADA,
                       VALOR_UNITARIO,
                       PRECIO_BRUTO,
                       DEDUCCIONES,
                       PRECIO_NETO,
                       FECHA_TRABAJO,
                       OBSERVACIONES
                FROM EMPLEADO_COSECHA
                WHERE ID_EMPLEADO_COSECHA = :ID";

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
        // OBTENER TODOS LOS EMPLEADOS DE UNA COSECHA
        // =============================================
        public IList<EMPLEADO_COSECHA> ObtenerPorCosecha(int idCosecha)
        {
            const string sql = @"
                SELECT ID_EMPLEADO_COSECHA,
                       ID_EMPLEADO,
                       ID_COSECHA,
                       CANTIDAD_COSECHADA,
                       VALOR_UNITARIO,
                       PRECIO_BRUTO,
                       DEDUCCIONES,
                       PRECIO_NETO,
                       FECHA_TRABAJO,
                       OBSERVACIONES
                FROM EMPLEADO_COSECHA
                WHERE ID_COSECHA = :ID
                ORDER BY FECHA_TRABAJO";

            var lista = new List<EMPLEADO_COSECHA>();

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":ID", idCosecha);
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
        // GUARDAR (INSERT SIMPLE)
        // Normalmente usaremos InsertarOSumar(), pero
        // dejamos Guardar implementado por si acaso.
        // =============================================
        public override string Guardar(EMPLEADO_COSECHA entidad)
        {
            const string sql = @"
                INSERT INTO EMPLEADO_COSECHA
                (ID_EMPLEADO_COSECHA,
                 ID_EMPLEADO,
                 ID_COSECHA,
                 CANTIDAD_COSECHADA,
                 VALOR_UNITARIO,
                 PRECIO_BRUTO,
                 DEDUCCIONES,
                 PRECIO_NETO,
                 FECHA_TRABAJO,
                 OBSERVACIONES)
                VALUES
                (SEQ_EMPLEADO_COSECHA.NEXTVAL,
                 :ID_EMPLEADO,
                 :ID_COSECHA,
                 :CANTIDAD,
                 :VALOR_UNITARIO,
                 :PRECIO_BRUTO,
                 :DEDUCCIONES,
                 :PRECIO_NETO,
                 :FECHA_TRABAJO,
                 :OBS)
                RETURNING ID_EMPLEADO_COSECHA INTO :ID_OUT";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":ID_EMPLEADO", entidad.ID_EMPLEADO);
                cmd.Parameters.Add(":ID_COSECHA", entidad.ID_COSECHA);
                cmd.Parameters.Add(":CANTIDAD", entidad.CANTIDAD_COSECHADA);
                cmd.Parameters.Add(":VALOR_UNITARIO", entidad.VALOR_UNITARIO);
                cmd.Parameters.Add(":PRECIO_BRUTO", entidad.PRECIO_BRUTO);
                cmd.Parameters.Add(":DEDUCCIONES", entidad.DEDUCCIONES);
                cmd.Parameters.Add(":PRECIO_NETO", entidad.PRECIO_NETO);
                cmd.Parameters.Add(":FECHA_TRABAJO", entidad.FECHA_TRABAJO);
                cmd.Parameters.Add(":OBS", (object)entidad.OBSERVACIONES ?? DBNull.Value);

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
        // INSERTAR O SUMAR (LÓGICA A1)
        //
        // - Si NO existe registro para (empleado, cosecha, fecha)
        //   => INSERT normal (la primera vez que trabaja ese día).
        //
        // - Si ya existe registro ese día
        //   => UPDATE sumando la cantidad:
        //      CANTIDAD_COSECHADA = CANTIDAD_COSECHADA + nueva_cantidad
        //
        // La UNIQUE (ID_EMPLEADO, ID_COSECHA, FECHA_TRABAJO) se respeta.
        // =============================================
        public bool InsertarOSumar(EMPLEADO_COSECHA entidad)
        {
            // Asegurarnos de que solo pase la fecha (sin hora)
            DateTime fecha = entidad.FECHA_TRABAJO.Date;

            const string sqlExiste = @"
                SELECT COUNT(*)
                FROM EMPLEADO_COSECHA
                WHERE ID_EMPLEADO = :EMP
                  AND ID_COSECHA  = :COSECHA
                  AND TRUNC(FECHA_TRABAJO) = TRUNC(:FECHA)";

            using (var cn = CrearConexion())
            using (var cmdExiste = new OracleCommand(sqlExiste, cn))
            {
                cmdExiste.Parameters.Add(":EMP", entidad.ID_EMPLEADO);
                cmdExiste.Parameters.Add(":COSECHA", entidad.ID_COSECHA);
                cmdExiste.Parameters.Add(":FECHA", fecha);

                cn.Open();

                int count = Convert.ToInt32(cmdExiste.ExecuteScalar());

                if (count > 0)
                {
                    // Ya existe registro para ese empleado en esa cosecha ese mismo día
                    // => SUMAMOS CANTIDAD_COSECHADA
                    const string sqlUpdate = @"
                        UPDATE EMPLEADO_COSECHA
                        SET CANTIDAD_COSECHADA = CANTIDAD_COSECHADA + :DELTA,
                            -- Ponemos DEDUCCIONES en NULL para que el trigger
                            -- TRG_SetValorUnitario vuelva a recalcular
                            DEDUCCIONES = NULL
                        WHERE ID_EMPLEADO = :EMP
                          AND ID_COSECHA  = :COSECHA
                          AND TRUNC(FECHA_TRABAJO) = TRUNC(:FECHA)";

                    using (var cmdUpd = new OracleCommand(sqlUpdate, cn))
                    {
                        cmdUpd.Parameters.Add(":DELTA", entidad.CANTIDAD_COSECHADA);
                        cmdUpd.Parameters.Add(":EMP", entidad.ID_EMPLEADO);
                        cmdUpd.Parameters.Add(":COSECHA", entidad.ID_COSECHA);
                        cmdUpd.Parameters.Add(":FECHA", fecha);

                        return cmdUpd.ExecuteNonQuery() > 0;
                    }
                }
                else
                {
                    // No existe registro para ese empleado ese día
                    // => INSERT normal (el trigger completará precios)
                    const string sqlInsert = @"
                        INSERT INTO EMPLEADO_COSECHA
                        (ID_EMPLEADO_COSECHA,
                         ID_EMPLEADO,
                         ID_COSECHA,
                         CANTIDAD_COSECHADA,
                         VALOR_UNITARIO,
                         PRECIO_BRUTO,
                         DEDUCCIONES,
                         PRECIO_NETO,
                         FECHA_TRABAJO,
                         OBSERVACIONES)
                        VALUES
                        (SEQ_EMPLEADO_COSECHA.NEXTVAL,
                         :EMP,
                         :COSECHA,
                         :CANTIDAD,
                         :VALOR_UNITARIO,
                         NULL,       -- PRECIO_BRUTO lo calcula el trigger
                         NULL,       -- DEDUCCIONES las calcula el trigger
                         NULL,       -- PRECIO_NETO lo calcula el trigger
                         :FECHA,
                         :OBS)";

                    using (var cmdIns = new OracleCommand(sqlInsert, cn))
                    {
                        cmdIns.Parameters.Add(":EMP", entidad.ID_EMPLEADO);
                        cmdIns.Parameters.Add(":COSECHA", entidad.ID_COSECHA);
                        cmdIns.Parameters.Add(":CANTIDAD", entidad.CANTIDAD_COSECHADA);
                        cmdIns.Parameters.Add(":VALOR_UNITARIO", entidad.VALOR_UNITARIO);
                        cmdIns.Parameters.Add(":FECHA", fecha);
                        cmdIns.Parameters.Add(":OBS", (object)entidad.OBSERVACIONES ?? DBNull.Value);

                        return cmdIns.ExecuteNonQuery() > 0;
                    }
                }
            }
        }

        // =============================================
        // ELIMINAR UN REGISTRO POR ID
        // =============================================
        public override bool Eliminar(int id)
        {
            const string sql = @"
                DELETE FROM EMPLEADO_COSECHA
                WHERE ID_EMPLEADO_COSECHA = :ID";

            using (var cn = CrearConexion())
            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(":ID", id);
                cn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // =============================================
        // MAPEADOR
        // =============================================
        private EMPLEADO_COSECHA Mapear(OracleDataReader dr)
        {
            return new EMPLEADO_COSECHA
            {
                ID_EMPLEADO_COSECHA = Convert.ToInt32(dr["ID_EMPLEADO_COSECHA"]),
                ID_EMPLEADO = Convert.ToInt32(dr["ID_EMPLEADO"]),
                ID_COSECHA = Convert.ToInt32(dr["ID_COSECHA"]),
                CANTIDAD_COSECHADA = Convert.ToDecimal(dr["CANTIDAD_COSECHADA"]),
                VALOR_UNITARIO = dr["VALOR_UNITARIO"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(dr["VALOR_UNITARIO"]),
                PRECIO_BRUTO = dr["PRECIO_BRUTO"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(dr["PRECIO_BRUTO"]),
                DEDUCCIONES = dr["DEDUCCIONES"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(dr["DEDUCCIONES"]),
                PRECIO_NETO = dr["PRECIO_NETO"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(dr["PRECIO_NETO"]),
                FECHA_TRABAJO = Convert.ToDateTime(dr["FECHA_TRABAJO"]),
                OBSERVACIONES = dr["OBSERVACIONES"] == DBNull.Value
                    ? null
                    : dr["OBSERVACIONES"].ToString()
            };
        }
    }
}
