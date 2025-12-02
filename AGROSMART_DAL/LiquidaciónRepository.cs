using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using AGROSMART_ENTITY.ENTIDADES_DTOS;
using System.Linq;

namespace AGROSMART_DAL
{
    public class LiquidaciónRepository
    {
        private const string TABLA_BASE = @"
            FROM EMPLEADO_COSECHA ec
            JOIN EMPLEADO e ON ec.ID_EMPLEADO = e.ID_USUARIO
            JOIN USUARIO u ON e.ID_USUARIO = u.ID_USUARIO
            JOIN COSECHA c ON ec.ID_COSECHA = c.ID_COSECHA
            LEFT JOIN CULTIVO cu ON c.ID_CULTIVO = cu.ID_CULTIVO";

        // 🔧 CORRECCIÓN: Se eliminó ec.FECHA_REGISTRO que causaba error ORA-00904
        // Se agregó c.FECHA_COSECHA para cálculos de duración
        private const string COLUMNAS = @"
            ec.ID_EMPLEADO_COSECHA,
            ec.ID_EMPLEADO,
            e.ID_USUARIO,
            u.PRIMER_NOMBRE,
            u.PRIMER_APELLIDO,
            ec.ID_COSECHA,
            ec.CANTIDAD_COSECHADA,
            c.UNIDAD_MEDIDA,
            c.CANTIDAD_OBTENIDA,
            ec.VALOR_UNITARIO,
            ec.PRECIO_BRUTO,
            ec.DEDUCCIONES,
            ec.PRECIO_NETO,
            ec.FECHA_TRABAJO,
            ec.OBSERVACIONES,
            c.FECHA_REGISTRO,
            c.FECHA_INICIO,
            cu.NOMBRE_LOTE AS NOMBRE_CULTIVO,
            NVL(c.ESTADO, 'EN_PROCESO') AS ESTADO";

        /// <summary>
        /// Obtiene todas las liquidaciones (sin filtros)
        /// </summary>
        public List<Liquidación_DTO> ConsultarTodas()
        {
            string sql = $"SELECT {COLUMNAS} {TABLA_BASE} ORDER BY c.FECHA_INICIO DESC";
            return EjecutarConsulta(sql);
        }

        /// <summary>
        /// Obtiene liquidaciones por ID de cosecha
        /// </summary>
        public List<Liquidación_DTO> ConsultarPorCosecha(int idCosecha = 0)
        {
            if (idCosecha == 0)
                return ConsultarTodas();

            string sql = $"SELECT {COLUMNAS} {TABLA_BASE} WHERE ec.ID_COSECHA = :idCosecha ORDER BY c.FECHA_INICIO DESC";
            return EjecutarConsulta(sql, new OracleParameter("idCosecha", idCosecha));
        }

        /// <summary>
        /// Obtiene liquidaciones por ID de empleado
        /// </summary>
        public List<Liquidación_DTO> ConsultarPorEmpleado(int idEmpleado)
        {
            string sql = $"SELECT {COLUMNAS} {TABLA_BASE} WHERE ec.ID_EMPLEADO = :idEmpleado ORDER BY c.FECHA_INICIO DESC";
            return EjecutarConsulta(sql, new OracleParameter("idEmpleado", idEmpleado));
        }

        /// <summary>
        /// Obtiene liquidaciones en un rango de fechas
        /// </summary>
        public List<Liquidación_DTO> ConsultarPorFechas(DateTime inicio, DateTime fin)
        {
            string sql = $@"SELECT {COLUMNAS} {TABLA_BASE} 
    WHERE c.FECHA_INICIO >= TRUNC(:inicio)
    AND c.FECHA_INICIO <= TRUNC(:fin) + 1
    ORDER BY c.FECHA_INICIO DESC";
            return EjecutarConsulta(sql,
                new OracleParameter("inicio", OracleDbType.Date) { Value = inicio },
                new OracleParameter("fin", OracleDbType.Date) { Value = fin });
        }

        /// <summary>
        /// Obtiene liquidaciones por mes y año
        /// </summary>
        public List<Liquidación_DTO> ConsultarPorMesAnio(int mes, int anio)
        {
            string sql = $@"SELECT {COLUMNAS} {TABLA_BASE} 
    WHERE EXTRACT(MONTH FROM c.FECHA_INICIO) = :mes 
    AND EXTRACT(YEAR FROM c.FECHA_INICIO) = :anio 
    ORDER BY c.FECHA_INICIO DESC";
            return EjecutarConsulta(sql,
                new OracleParameter("mes", mes),
                new OracleParameter("anio", anio));
        }

        /// <summary>
        /// Obtiene cultivos que tienen liquidaciones
        /// </summary>
        public List<(int IdCultivo, string NombreCultivo)> ObtenerCultivosConLiquidaciones()
        {
            var lista = new List<(int, string)>();
            string sql = @"
                SELECT DISTINCT c.ID_CULTIVO, cu.NOMBRE_LOTE
                FROM COSECHA c
                JOIN CULTIVO cu ON c.ID_CULTIVO = cu.ID_CULTIVO
                JOIN EMPLEADO_COSECHA ec ON c.ID_COSECHA = ec.ID_COSECHA
                ORDER BY cu.NOMBRE_LOTE";

            using (OracleConnection conn = Conexion.CrearConexion())
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int idCultivo = Convert.ToInt32(dr["ID_CULTIVO"]);
                        string nombre = dr["NOMBRE_LOTE"].ToString();
                        lista.Add((idCultivo, nombre));
                    }
                }
            }
            return lista;
        }

        #region CONSULTAS ANALÍTICAS

        /// <summary>
        /// Calcula el total pagado a un empleado
        /// </summary>
        public decimal TotalPagadoEmpleado(int idEmpleado)
        {
            string sql = @"
                SELECT COALESCE(SUM(NVL(ec.PRECIO_NETO, 0)), 0) AS TOTAL
                FROM EMPLEADO_COSECHA ec
                WHERE ec.ID_EMPLEADO = :idEmpleado";

            using (OracleConnection conn = Conexion.CrearConexion())
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("idEmpleado", idEmpleado));
                    var result = cmd.ExecuteScalar();
                    return result != DBNull.Value && result != null ? Convert.ToDecimal(result) : 0m;
                }
            }
        }

        /// <summary>
        /// Obtiene totales consolidados de liquidaciones filtradas
        /// </summary>
        public (decimal TotalBruto, decimal TotalDeducciones, decimal TotalNeto, int EmpleadosUnicos)
            ObtenerTotales(List<Liquidación_DTO> liquidaciones)
        {
            if (liquidaciones == null || !liquidaciones.Any())
                return (0, 0, 0, 0);

            decimal totalBruto = liquidaciones.Sum(x => x.PagoBruto);
            decimal totalDeduc = liquidaciones.Sum(x => x.Deducciones);
            decimal totalNeto = liquidaciones.Sum(x => x.PagoNeto);
            int empleadosUnicos = liquidaciones.Select(x => x.IdEmpleado).Distinct().Count();

            return (totalBruto, totalDeduc, totalNeto, empleadosUnicos);
        }

        #endregion

        #region MAPEO DE DATOS

        /// <summary>
        /// Ejecuta una consulta y mapea los resultados a Liquidación_DTO
        /// </summary>
        private List<Liquidación_DTO> EjecutarConsulta(string sql, params OracleParameter[] parametros)
        {
            List<Liquidación_DTO> lista = new List<Liquidación_DTO>();

            try
            {
            using (OracleConnection conn = Conexion.CrearConexion())
                {
                    conn.Open();
                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        if (parametros != null && parametros.Length > 0)
                            cmd.Parameters.AddRange(parametros);

                        System.Diagnostics.Debug.WriteLine($"📋 SQL EJECUTADO:\n{sql}\n");

                        using (var dr = cmd.ExecuteReader())
                        {
                            int contador = 0;
                            while (dr.Read())
                            {
                                var dto = MapearDesdeDataReader(dr);
                                if (dto != null)
                                {
                                    lista.Add(dto);
                                    contador++;
                                }
                            }
                            System.Diagnostics.Debug.WriteLine($"✅ Se mapearon {contador} registros correctamente");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR EN CONSULTA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"⚠️ STACK TRACE:\n{ex.StackTrace}");
                throw;
            }

            return lista;
        }

        /// <summary>
        /// Mapea una fila del DataReader a Liquidación_DTO
        /// ✅ Mapea datos de EMPLEADO_COSECHA y COSECHA
        /// </summary>
        private Liquidación_DTO MapearDesdeDataReader(IDataRecord dr)
        {
            try
            {
                decimal cantidad = ObtenerDecimal(dr, "CANTIDAD_COSECHADA");
                decimal valorUnidad = ObtenerDecimal(dr, "VALOR_UNITARIO");
                decimal deducciones = ObtenerDecimal(dr, "DEDUCCIONES");
                decimal precioBruto = ObtenerDecimal(dr, "PRECIO_BRUTO");
                decimal precioNeto = ObtenerDecimal(dr, "PRECIO_NETO");
                DateTime fechaTrabajo = ObtenerDateTime(dr, "FECHA_TRABAJO");
                DateTime fechaInicio = ObtenerDateTime(dr, "FECHA_INICIO");
                string estado = ObtenerString(dr, "ESTADO");

                // 🔧 FALLBACK MEJORADO: Si valor unitario es 0, usar valor por defecto
                if (valorUnidad <= 0)
                {
                    string unidad = ObtenerString(dr, "UNIDAD_MEDIDA").ToUpper();
                    if (unidad.Contains("KG")) valorUnidad = 2500;
                    else if (unidad.Contains("LB")) valorUnidad = 1500;
                    else if (unidad.Contains("TON")) valorUnidad = 50000;
                    else if (unidad.Contains("GAL")) valorUnidad = 5000;
                    else if (unidad.Contains("CAJA")) valorUnidad = 8000;
                    else valorUnidad = 2500;
                }

                // Calcular si los valores de BD son 0
                if (precioBruto <= 0)
                    precioBruto = cantidad * valorUnidad;

                if (deducciones <= 0)
                    deducciones = precioBruto * 0.0m;

                if (precioNeto <= 0)
                    precioNeto = precioBruto - deducciones;

                // 🕒 CÁLCULO DE TIEMPO DE COSECHA
                string tiempoCosecha = "0 días";
                if (fechaInicio != DateTime.MinValue)
                {
                    TimeSpan duracion;
                    if (estado == "TERMINADA")
                    {
                        // Si está terminada, usamos la fecha de trabajo como referencia de fin (aproximado)
                        // O simplemente mostramos los días transcurridos hasta hoy si fue reciente
                        // Como no tenemos FECHA_FIN, usamos la fecha de trabajo actual como referencia
                        duracion = fechaTrabajo - fechaInicio;
                    }
                    else
                    {
                        duracion = DateTime.Now - fechaInicio;
                    }
                    
                    int dias = duracion.Days;
                    if (dias < 0) dias = 0;
                    tiempoCosecha = dias == 1 ? "1 día" : $"{dias} días";
                }

                return new Liquidación_DTO
                {
                    IdLiquidacion = ObtenerInt(dr, "ID_EMPLEADO_COSECHA"),
                    IdEmpleado = ObtenerInt(dr, "ID_EMPLEADO"),
                    IdUsuario = ObtenerInt(dr, "ID_USUARIO"),
                    IdCosecha = ObtenerInt(dr, "ID_COSECHA"),
                    Nombre = $"{ObtenerString(dr, "PRIMER_NOMBRE")} {ObtenerString(dr, "PRIMER_APELLIDO")}",
                    Cantidad = cantidad,
                    UnidadMedida = ObtenerString(dr, "UNIDAD_MEDIDA"),
                    ValorUnidad = valorUnidad,
                    FechaTrabajo = fechaTrabajo,
                    PagoBruto = precioBruto,
                    Deducciones = deducciones,
                    PagoNeto = precioNeto,
                    Observaciones = ObtenerString(dr, "OBSERVACIONES"),
                    FechaRegistro = ObtenerDateTime(dr, "FECHA_REGISTRO"), // Ahora mapea c.FECHA_REGISTRO
                    NombreCultivo = ObtenerString(dr, "NOMBRE_CULTIVO"),
                    EstadoCosecha = estado,
                    TiempoCosecha = tiempoCosecha
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR MAPEANDO FILA: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES

        private int ObtenerInt(IDataRecord dr, string columna)
        {
            try { return dr[columna] != DBNull.Value ? Convert.ToInt32(dr[columna]) : 0; }
            catch { return 0; }
        }

        private decimal ObtenerDecimal(IDataRecord dr, string columna)
        {
            try { return dr[columna] != DBNull.Value ? Convert.ToDecimal(dr[columna]) : 0m; }
            catch { return 0m; }
        }

        private string ObtenerString(IDataRecord dr, string columna)
        {
            try { return dr[columna] != DBNull.Value ? dr[columna].ToString() : string.Empty; }
            catch { return string.Empty; }
        }

        private DateTime ObtenerDateTime(IDataRecord dr, string columna)
        {
            try { return dr[columna] != DBNull.Value ? Convert.ToDateTime(dr[columna]) : DateTime.MinValue; }
            catch { return DateTime.MinValue; }
        }

        #endregion
    }
}