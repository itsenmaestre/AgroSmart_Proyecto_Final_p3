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
    public class DetalleTareaRepository : BaseRepository<DETALLE_TAREA>
    {
       

        public override IList<DETALLE_TAREA> Consultar()
        {
            const string sql = @"SELECT ID_DETALLE_TAREA, ID_TAREA, ID_INSUMO, CANTIDAD_USADA 
                                FROM DETALLE_TAREA 
                                ORDER BY ID_DETALLE_TAREA DESC";

            List<DETALLE_TAREA> lista = new List<DETALLE_TAREA>();

            try
            {
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
            }
            catch (OracleException oex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Oracle en Consultar: {oex.Number} - {oex.Message}");
                throw new Exception($"Error al consultar detalles de tarea (Oracle {oex.Number}): {oex.Message}", oex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Consultar: {ex.Message}");
                throw new Exception($"Error al consultar detalles de tarea: {ex.Message}", ex);
            }

            return lista;
        }

        public override DETALLE_TAREA ObtenerPorId(int id)
        {
            const string sql = @"SELECT ID_DETALLE_TAREA, ID_TAREA, ID_INSUMO, CANTIDAD_USADA 
                                FROM DETALLE_TAREA 
                                WHERE ID_DETALLE_TAREA = :id";

            try
            {
                using (var cn = CrearConexion())
                using (var cmd = new OracleCommand(sql, cn))
                {
                    cmd.Parameters.Add(":id", OracleDbType.Int32).Value = id;
                    cn.Open();
                    using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow))
                        return dr.Read() ? Mapear(dr) : null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerPorId: {ex.Message}");
                throw new Exception($"Error al obtener detalle por ID: {ex.Message}", ex);
            }
        }

        

        public override bool Actualizar(DETALLE_TAREA entidad)
        {
            var detalleAnterior = ObtenerPorId(entidad.ID_DETALLE_TAREA);
            if (detalleAnterior == null) return false;

            decimal diferencia = entidad.CANTIDAD_USADA - detalleAnterior.CANTIDAD_USADA;

            const string sqlDetalle = @"UPDATE DETALLE_TAREA 
                                       SET CANTIDAD_USADA = :cantidad,
                                           ID_INSUMO = :idInsumo
                                       WHERE ID_DETALLE_TAREA = :id";

            const string sqlAjustarStock = @"UPDATE INSUMO 
                                            SET STOCK_ACTUAL = STOCK_ACTUAL - :diferencia 
                                            WHERE ID_INSUMO = :idInsumo";

            try
            {
                using (var cn = CrearConexion())
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            using (var cmd = new OracleCommand(sqlDetalle, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = entidad.CANTIDAD_USADA;
                                cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;
                                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_DETALLE_TAREA;
                                cmd.ExecuteNonQuery();
                            }

                            if (diferencia != 0 && entidad.ID_INSUMO == detalleAnterior.ID_INSUMO)
                            {
                                using (var cmd = new OracleCommand(sqlAjustarStock, cn))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.Add(":diferencia", OracleDbType.Decimal).Value = diferencia;
                                    cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (entidad.ID_INSUMO != detalleAnterior.ID_INSUMO)
                            {
                                const string sqlDevolver = @"UPDATE INSUMO 
                                                            SET STOCK_ACTUAL = STOCK_ACTUAL + :cantidad 
                                                            WHERE ID_INSUMO = :idInsumo";

                                using (var cmd = new OracleCommand(sqlDevolver, cn))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = detalleAnterior.CANTIDAD_USADA;
                                    cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = detalleAnterior.ID_INSUMO;
                                    cmd.ExecuteNonQuery();
                                }

                                const string sqlDescontar = @"UPDATE INSUMO 
                                                             SET STOCK_ACTUAL = STOCK_ACTUAL - :cantidad 
                                                             WHERE ID_INSUMO = :idInsumo";

                                using (var cmd = new OracleCommand(sqlDescontar, cn))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = entidad.CANTIDAD_USADA;
                                    cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error en Actualizar: {ex.Message}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Actualizar: {ex.Message}");
                return false;
            }
        }



       

        public List<DETALLE_TAREA> ObtenerPorTarea(int idTarea)
        {
            const string sql = @"SELECT ID_DETALLE_TAREA, ID_TAREA, ID_INSUMO, CANTIDAD_USADA 
                                FROM DETALLE_TAREA 
                                WHERE ID_TAREA = :idTarea
                                ORDER BY ID_DETALLE_TAREA";

            List<DETALLE_TAREA> lista = new List<DETALLE_TAREA>();

            try
            {
                using (var cn = CrearConexion())
                using (var cmd = new OracleCommand(sql, cn))
                {
                    cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = idTarea;
                    cn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            lista.Add(Mapear(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerPorTarea: {ex.Message}");
                throw new Exception($"Error al obtener detalles por tarea: {ex.Message}", ex);
            }

            return lista;
        }

        public string RegistrarInsumosConDescuento(int idTarea, List<DETALLE_TAREA> detalles)
        {
            const string sqlInsertDetalle = @"INSERT INTO DETALLE_TAREA 
                                             (ID_DETALLE_TAREA, ID_TAREA, ID_INSUMO, CANTIDAD_USADA) 
                                             VALUES (SEQ_DETALLE_TAREA.NEXTVAL, :idTarea, :idInsumo, :cantidad)";

            const string sqlDescuentoStock = @"UPDATE INSUMO 
                                              SET STOCK_ACTUAL = STOCK_ACTUAL - :cantidad 
                                              WHERE ID_INSUMO = :idInsumo 
                                              AND TIPO = 'CONSUMIBLE'";

            try
            {
                using (var cn = CrearConexion())
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var detalle in detalles)
                            {
                                using (var cmd = new OracleCommand(sqlInsertDetalle, cn))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = idTarea;
                                    cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = detalle.ID_INSUMO;
                                    cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = detalle.CANTIDAD_USADA;
                                    cmd.ExecuteNonQuery();
                                }

                                using (var cmd = new OracleCommand(sqlDescuentoStock, cn))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = detalle.CANTIDAD_USADA;
                                    cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = detalle.ID_INSUMO;
                                    int filasAfectadas = cmd.ExecuteNonQuery();

                                    if (filasAfectadas == 0)
                                    {
                                        transaction.Rollback();
                                        return $"Error: No se pudo descontar stock del insumo ID {detalle.ID_INSUMO}";
                                    }
                                }
                            }

                            transaction.Commit();
                            return "OK";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return $"Error: {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error de conexión: {ex.Message}";
            }
        }

        public bool EliminarPorTarea(int idTarea)
        {
            var detalles = ObtenerPorTarea(idTarea);

            if (detalles == null || detalles.Count == 0)
                return true;

            try
            {
                using (var cn = CrearConexion())
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var detalle in detalles)
                            {
                                const string sqlDevolver = @"UPDATE INSUMO 
                                                            SET STOCK_ACTUAL = STOCK_ACTUAL + :cantidad 
                                                            WHERE ID_INSUMO = :idInsumo";

                                using (var cmd = new OracleCommand(sqlDevolver, cn))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = detalle.CANTIDAD_USADA;
                                    cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = detalle.ID_INSUMO;
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            const string sqlEliminar = "DELETE FROM DETALLE_TAREA WHERE ID_TAREA = :idTarea";
                            using (var cmd = new OracleCommand(sqlEliminar, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = idTarea;
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error en EliminarPorTarea: {ex.Message}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarPorTarea: {ex.Message}");
                return false;
            }
        }

      

        private DETALLE_TAREA Mapear(OracleDataReader dr)
        {
            return new DETALLE_TAREA
            {
                ID_DETALLE_TAREA = Convert.ToInt32(dr["ID_DETALLE_TAREA"]),
                ID_TAREA = Convert.ToInt32(dr["ID_TAREA"]),
                ID_INSUMO = Convert.ToInt32(dr["ID_INSUMO"]),
                CANTIDAD_USADA = Convert.ToDecimal(dr["CANTIDAD_USADA"])
            };
        }
        public override string Guardar(DETALLE_TAREA entidad)
        {
            const string sqlValidarStock = @"SELECT STOCK_ACTUAL, NOMBRE, UNIDAD_MEDIDA 
                                     FROM INSUMO 
                                     WHERE ID_INSUMO = :idInsumo 
                                     AND TIPO = 'CONSUMIBLE'";

            const string sqlDetalle = @"INSERT INTO DETALLE_TAREA 
                               (ID_DETALLE_TAREA, ID_TAREA, ID_INSUMO, CANTIDAD_USADA) 
                               VALUES (SEQ_DETALLE_TAREA.NEXTVAL, :idTarea, :idInsumo, :cantidad)";

            const string sqlDescuentoStock = @"UPDATE INSUMO 
                                      SET STOCK_ACTUAL = STOCK_ACTUAL - :cantidad,
                                          FECHA_ULTIMA_ACTUALIZACION = SYSDATE
                                      WHERE ID_INSUMO = :idInsumo
                                      AND STOCK_ACTUAL >= :cantidad";  // ⭐ VALIDACIÓN CRÍTICA

            const string sqlObtenerCosto = @"SELECT COSTO_UNITARIO 
                                     FROM INSUMO 
                                     WHERE ID_INSUMO = :idInsumo";

            const string sqlActualizarGasto = @"UPDATE TAREA 
                                        SET GASTO_TOTAL = GASTO_TOTAL + :costoDetalle 
                                        WHERE ID_TAREA = :idTarea";

            try
            {
                using (var cn = CrearConexion())
                {
                    cn.Open();

                    System.Diagnostics.Debug.WriteLine("=== INICIANDO GUARDADO ===");
                    System.Diagnostics.Debug.WriteLine($"ID_TAREA: {entidad.ID_TAREA}");
                    System.Diagnostics.Debug.WriteLine($"ID_INSUMO: {entidad.ID_INSUMO}");
                    System.Diagnostics.Debug.WriteLine($"CANTIDAD: {entidad.CANTIDAD_USADA}");

                    using (var transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            // ⭐ VALIDACIÓN 1: Verificar stock actual DENTRO de la transacción
                            decimal stockActual = 0;
                            string nombreInsumo = "";
                            string unidadMedida = "";

                            using (var cmd = new OracleCommand(sqlValidarStock, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;

                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        stockActual = Convert.ToDecimal(reader["STOCK_ACTUAL"]);
                                        nombreInsumo = reader["NOMBRE"].ToString();
                                        unidadMedida = reader["UNIDAD_MEDIDA"].ToString();
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        return "Error: El insumo no existe o no es CONSUMIBLE.";
                                    }
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"Stock actual: {stockActual} {unidadMedida}");

                            // ⭐ VALIDACIÓN 2: Verificar que hay stock suficiente
                            if (stockActual < entidad.CANTIDAD_USADA)
                            {
                                transaction.Rollback();
                                System.Diagnostics.Debug.WriteLine($"⚠️ STOCK INSUFICIENTE - Requerido: {entidad.CANTIDAD_USADA}, Disponible: {stockActual}");
                                return $"STOCK_INSUFICIENTE|{nombreInsumo}|{stockActual}|{unidadMedida}|{entidad.CANTIDAD_USADA}";
                            }

                            // ⭐ VALIDACIÓN 3: Verificar que no quedará negativo
                            decimal stockResultante = stockActual - entidad.CANTIDAD_USADA;
                            if (stockResultante < 0)
                            {
                                transaction.Rollback();
                                System.Diagnostics.Debug.WriteLine($"⚠️ OPERACIÓN BLOQUEADA - Stock resultante sería: {stockResultante}");
                                return $"STOCK_NEGATIVO_BLOQUEADO|{nombreInsumo}|{stockActual}|{unidadMedida}|{entidad.CANTIDAD_USADA}|{stockResultante}";
                            }

                            System.Diagnostics.Debug.WriteLine($"✓ Validación exitosa - Stock resultante: {stockResultante} {unidadMedida}");

                            // Obtener costo
                            decimal costoUnitario = 0;
                            using (var cmd = new OracleCommand(sqlObtenerCosto, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;

                                object result = cmd.ExecuteScalar();
                                if (result == null || result == DBNull.Value)
                                {
                                    transaction.Rollback();
                                    return "Error: El insumo no tiene costo definido.";
                                }
                                costoUnitario = Convert.ToDecimal(result);
                            }

                            decimal costoTotal = entidad.CANTIDAD_USADA * costoUnitario;
                            System.Diagnostics.Debug.WriteLine($"Costo Unitario: {costoUnitario}");
                            System.Diagnostics.Debug.WriteLine($"Costo Total: {costoTotal}");

                            // Insertar detalle
                            using (var cmd = new OracleCommand(sqlDetalle, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = entidad.ID_TAREA;
                                cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;
                                cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = entidad.CANTIDAD_USADA;

                                System.Diagnostics.Debug.WriteLine("Ejecutando INSERT...");
                                int filasInsertadas = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Filas insertadas: {filasInsertadas}");

                                if (filasInsertadas == 0)
                                {
                                    transaction.Rollback();
                                    return "Error: No se insertó ninguna fila en DETALLE_TAREA";
                                }
                            }

                            // ⭐ Descontar stock con validación en WHERE
                            using (var cmd = new OracleCommand(sqlDescuentoStock, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = entidad.CANTIDAD_USADA;
                                cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;

                                System.Diagnostics.Debug.WriteLine("Ejecutando UPDATE stock...");
                                int filasAfectadas = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Filas actualizadas en stock: {filasAfectadas}");

                                // ⭐ Si no se actualizó ninguna fila, significa que el stock cambió
                                if (filasAfectadas == 0)
                                {
                                    transaction.Rollback();
                                    System.Diagnostics.Debug.WriteLine("⚠️ CONCURRENCIA: El stock cambió durante la transacción");
                                    return "CONCURRENCIA|El stock del insumo cambió durante la operación. Intente nuevamente.";
                                }
                            }

                            // Actualizar gasto
                            using (var cmd = new OracleCommand(sqlActualizarGasto, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":costoDetalle", OracleDbType.Decimal).Value = costoTotal;
                                cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = entidad.ID_TAREA;

                                System.Diagnostics.Debug.WriteLine("Ejecutando UPDATE gasto...");
                                int filasAfectadas = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Filas actualizadas en gasto: {filasAfectadas}");

                                if (filasAfectadas == 0)
                                {
                                    transaction.Rollback();
                                    return "Error: No se pudo actualizar el gasto de la tarea.";
                                }
                            }

                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine("=== GUARDADO EXITOSO ===");
                            return "OK";
                        }
                        catch (OracleException oex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine("=== ERROR ORACLE ===");
                            System.Diagnostics.Debug.WriteLine($"Código: ORA-{oex.Number:00000}");
                            System.Diagnostics.Debug.WriteLine($"Mensaje: {oex.Message}");

                            switch (oex.Number)
                            {
                                case 942:
                                    return "Error ORA-00942: La tabla no existe en la base de datos.";
                                case 2289:
                                    return "Error ORA-02289: La secuencia SEQ_DETALLE_TAREA no existe.";
                                case 2291:
                                    return $"Error ORA-02291: La tarea o el insumo no existe.";
                                case 1:
                                    return "Error ORA-00001: Ya existe un registro con estos datos.";
                                default:
                                    return $"Error Oracle ORA-{oex.Number:00000}: {oex.Message}";
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error general: {ex.Message}");
                            return $"Error: {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear conexión: {ex.Message}");
                return $"Error de conexión: {ex.Message}";
            }
        }


        public override bool Eliminar(DETALLE_TAREA entidad)
        {
            const string sqlDetalle = "DELETE FROM DETALLE_TAREA WHERE ID_DETALLE_TAREA = :id";

            const string sqlDevolverStock = @"UPDATE INSUMO 
                                     SET STOCK_ACTUAL = STOCK_ACTUAL + :cantidad,
                                         FECHA_ULTIMA_ACTUALIZACION = SYSDATE
                                     WHERE ID_INSUMO = :idInsumo";

            
            const string sqlObtenerCosto = @"SELECT COSTO_UNITARIO 
                                     FROM INSUMO 
                                     WHERE ID_INSUMO = :idInsumo";

            
            const string sqlActualizarGasto = @"UPDATE TAREA 
                                        SET GASTO_TOTAL = GREATEST(GASTO_TOTAL - :costoDetalle, 0)
                                        WHERE ID_TAREA = :idTarea";

            try
            {
                using (var cn = CrearConexion())
                {
                    cn.Open();

                    System.Diagnostics.Debug.WriteLine("=== INICIANDO ELIMINACIÓN ===");
                    System.Diagnostics.Debug.WriteLine($"ID_DETALLE: {entidad.ID_DETALLE_TAREA}");
                    System.Diagnostics.Debug.WriteLine($"ID_TAREA: {entidad.ID_TAREA}");
                    System.Diagnostics.Debug.WriteLine($"ID_INSUMO: {entidad.ID_INSUMO}");
                    System.Diagnostics.Debug.WriteLine($"CANTIDAD: {entidad.CANTIDAD_USADA}");

                    using (var transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            // 1. NUEVO: Obtener el costo unitario del insumo
                            decimal costoUnitario = 0;
                            using (var cmd = new OracleCommand(sqlObtenerCosto, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;

                                object result = cmd.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    costoUnitario = Convert.ToDecimal(result);
                                }
                            }

                            decimal costoTotal = entidad.CANTIDAD_USADA * costoUnitario;
                            System.Diagnostics.Debug.WriteLine($"Costo Unitario: {costoUnitario}");
                            System.Diagnostics.Debug.WriteLine($"Costo a devolver: {costoTotal}");

                            // 2. Devolver stock al insumo
                            using (var cmd = new OracleCommand(sqlDevolverStock, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":cantidad", OracleDbType.Decimal).Value = entidad.CANTIDAD_USADA;
                                cmd.Parameters.Add(":idInsumo", OracleDbType.Int32).Value = entidad.ID_INSUMO;

                                int filasStock = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Stock devuelto - Filas actualizadas: {filasStock}");
                            }

                            // 3. NUEVO: Actualizar el gasto de la tarea (restando el costo)
                            using (var cmd = new OracleCommand(sqlActualizarGasto, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":costoDetalle", OracleDbType.Decimal).Value = costoTotal;
                                cmd.Parameters.Add(":idTarea", OracleDbType.Int32).Value = entidad.ID_TAREA;

                                int filasGasto = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Gasto actualizado - Filas actualizadas: {filasGasto}");
                            }

                            // 4. Eliminar el detalle
                            using (var cmd = new OracleCommand(sqlDetalle, cn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.Add(":id", OracleDbType.Int32).Value = entidad.ID_DETALLE_TAREA;

                                int filas = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Detalle eliminado - Filas eliminadas: {filas}");

                                if (filas > 0)
                                {
                                    transaction.Commit();
                                    System.Diagnostics.Debug.WriteLine("=== ELIMINACIÓN EXITOSA ===");
                                    return true;
                                }
                                else
                                {
                                    transaction.Rollback();
                                    System.Diagnostics.Debug.WriteLine("No se encontró el detalle para eliminar");
                                    return false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error en Eliminar: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                            throw; // Re-lanzar para que el Service lo capture
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Eliminar (outer): {ex.Message}");
                return false;
            }
        }
    }
}