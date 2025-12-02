using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Timers;
using Oracle.ManagedDataAccess.Client;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScottPlot;

namespace AgroSmartBot
{
    public class TelegramBotServicePDF
    {
        private readonly string _connectionString;
        public readonly ConfiguracionEmail _configEmail;
        private Timer _timerReporteSemanal;



        public TelegramBotServicePDF(string connectionString, ConfiguracionEmail configEmail)
        {
            _connectionString = connectionString;
            _configEmail = configEmail;
        }
        public void IniciarReportesProgramados()
        {
            _timerReporteSemanal = new Timer();
            _timerReporteSemanal.Elapsed += async (sender, e) => await VerificarYEnviarReporteSemanal();
            _timerReporteSemanal.Interval = 60000; // Cada minuto
            _timerReporteSemanal.AutoReset = true;
            _timerReporteSemanal.Enabled = true;

            Console.WriteLine("✅ Sistema de reportes automáticos iniciado");
            Console.WriteLine("📅 Se enviará todos los LUNES a las 8:00 AM");
        }

        public void DetenerReportesProgramados()
        {
            _timerReporteSemanal?.Stop();
            _timerReporteSemanal?.Dispose();
        }

        // ====================================================================
        // VERIFICAR Y ENVIAR REPORTE SEMANAL AUTOMÁTICO
        // ====================================================================

        private async Task VerificarYEnviarReporteSemanal()
        {
            DateTime ahora = DateTime.Now;

            if (ahora.DayOfWeek == DayOfWeek.Monday &&
                ahora.Hour == 8 &&
                ahora.Minute == 0)
            {
                Console.WriteLine($"⏰ Iniciando envío automático - {ahora:dd/MM/yyyy HH:mm}");

                try
                {
                    await GenerarYEnviarReportePDFAutomatico();
                    Console.WriteLine("✅ Reporte semanal enviado exitosamente");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error en envío automático: {ex.Message}");
                }
            }
        }

        // ====================================================================
        // GENERAR Y ENVIAR REPORTE PDF AUTOMÁTICO
        // ====================================================================

        private async Task GenerarYEnviarReportePDFAutomatico()
        {
            string filePath = null;

            try
            {
                DateTime fechaFin = DateTime.Now;
                DateTime fechaInicio = fechaFin.AddDays(-7);

                var datosReporte = await ObtenerDatosCompletosReporte(fechaInicio, fechaFin);

                string graficaPie = GenerarGraficaPie(datosReporte.GastosPorCategoria);
                string graficaBarra = GenerarGraficaBarra(datosReporte.Top5Tareas);

                string fileName = $"Reporte_AgroSmart_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                filePath = Path.Combine(Path.GetTempPath(), fileName);

                GenerarReportePDF(datosReporte, graficaPie, graficaBarra, filePath);

                await EnviarEmailConAdjunto(
                    _configEmail.EmailDestino,
                    "Reporte Semanal Automático AgroSmart",
                    GenerarCuerpoEmailAutomatico(fechaInicio, fechaFin),
                    filePath,
                    fileName);

                Console.WriteLine($"📧 Reporte enviado a: {_configEmail.EmailDestino}");
            }
            finally
            {
                LimpiarArchivos(filePath);
            }
        }

        // ====================================================================
        // GENERAR REPORTE PDF MANUAL (llamado desde Telegram)
        // ====================================================================

        public async Task<ResultadoReporte> GenerarReportePDFManual()
        {
            string filePath = null;
            string graficaPie = null;
            string graficaBarra = null;

            try
            {
                DateTime fechaFin = DateTime.Now;
                DateTime fechaInicio = fechaFin.AddDays(-7);

                var datosReporte = await ObtenerDatosCompletosReporte(fechaInicio, fechaFin);

                graficaPie = GenerarGraficaPie(datosReporte.GastosPorCategoria);
                graficaBarra = GenerarGraficaBarra(datosReporte.Top5Tareas);

                string fileName = $"Reporte_AgroSmart_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                filePath = Path.Combine(Path.GetTempPath(), fileName);

                GenerarReportePDF(datosReporte, graficaPie, graficaBarra, filePath);

                bool emailEnviado = false;
                try
                {
                    await EnviarEmailConAdjunto(
                        _configEmail.EmailDestino,
                        "Reporte Semanal AgroSmart",
                        GenerarCuerpoEmail(fechaInicio, fechaFin),
                        filePath,
                        fileName);

                    emailEnviado = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando email: {ex.Message}");
                }

                return new ResultadoReporte
                {
                    Exitoso = true,
                    RutaArchivo = filePath,
                    NombreArchivo = fileName,
                    EmailEnviado = emailEnviado,
                    DatosReporte = datosReporte,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                };
            }
            catch (Exception ex)
            {
                LimpiarArchivos(filePath, graficaPie, graficaBarra);

                return new ResultadoReporte
                {
                    Exitoso = false,
                    MensajeError = ex.Message
                };
            }
        }

        // ====================================================================
        // OBTENER DATOS COMPLETOS DE LA BASE DE DATOS
        // ====================================================================

        private async Task<DatosReportePDF> ObtenerDatosCompletosReporte(DateTime fechaInicio, DateTime fechaFin)
        {
            var datos = new DatosReportePDF
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                GastosPorCategoria = new List<GastoCategoria>(),
                Top5Tareas = new List<TareaTop>(),
                TareasDetalle = new List<TareaDetalle>(),
                InsumosDetalle = new List<InsumoDetalle>(),
                LotesTrabajados = new List<LoteTrabajado>(),
                CosechasSemana = new List<CosechaDetalle>(),
                CultivosTrabajados = new List<CultivoTrabajado>()


            };


            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1. TOTAL DE GASTOS
                datos.TotalGastos = await ObtenerTotalGastos(conn, fechaInicio, fechaFin);

                // 2. NÚMERO DE TAREAS
                datos.NumeroTareas = await ObtenerNumeroTareas(conn, fechaInicio, fechaFin);

                // 3. TAREA MÁS COSTOSA
                (datos.TareaMasCostosa, datos.CostoTareaMasCostosa) =
                    await ObtenerTareaMasCostosa(conn, fechaInicio, fechaFin);

                // 4. INSUMO MÁS USADO
                (datos.InsumoMasUsado, datos.CantidadInsumoUsado) =
                    await ObtenerInsumoMasUsado(conn, fechaInicio, fechaFin);

                // 5. GASTOS POR CATEGORÍA
                datos.GastosPorCategoria = await ObtenerGastosPorCategoria(conn, fechaInicio, fechaFin, datos.TotalGastos);

                // 6. TOP 5 TAREAS
                datos.Top5Tareas = await ObtenerTop5Tareas(conn, fechaInicio, fechaFin);

                // 7. DETALLE DE TAREAS
                datos.TareasDetalle = await ObtenerDetalleTareas(conn, fechaInicio, fechaFin);

                // 8. DETALLE DE INSUMOS
                datos.InsumosDetalle = await ObtenerDetalleInsumos(conn);
                datos.LotesTrabajados = await ObtenerLotesTrabajados(conn, fechaInicio, fechaFin);
                datos.CosechasSemana = await ObtenerCosechasSemana(conn, fechaInicio, fechaFin);
                datos.CultivosTrabajados = await ObtenerCultivosTrabajados(conn, fechaInicio, fechaFin);

            }
            return datos;
        }




        // Métodos auxiliares para consultas SQL

        private async Task<decimal> ObtenerTotalGastos(OracleConnection conn, DateTime fi, DateTime ff)
        {
            string sql = "SELECT NVL(SUM(GASTO_TOTAL), 0) FROM TAREA WHERE FECHA_PROGRAMADA BETWEEN :fi AND :ff";
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;
                return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
            }
        }

        private async Task<int> ObtenerNumeroTareas(OracleConnection conn, DateTime fi, DateTime ff)
        {
            string sql = "SELECT COUNT(*) FROM TAREA WHERE FECHA_PROGRAMADA BETWEEN :fi AND :ff";
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        private async Task<(string nombre, decimal costo)> ObtenerTareaMasCostosa(OracleConnection conn, DateTime fi, DateTime ff)
        {
            string sql = @"
                SELECT TIPO_ACTIVIDAD, GASTO_TOTAL FROM TAREA 
                WHERE FECHA_PROGRAMADA BETWEEN :fi AND :ff 
                AND GASTO_TOTAL = (SELECT MAX(GASTO_TOTAL) FROM TAREA WHERE FECHA_PROGRAMADA BETWEEN :fi AND :ff)
                AND ROWNUM = 1";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return (
                            reader.IsDBNull(0) ? "N/A" : reader.GetString(0),
                            reader.IsDBNull(1) ? 0 : reader.GetDecimal(1)
                        );
                    }
                }
            }

            return ("N/A", 0);
        }

        private async Task<(string nombre, decimal cantidad)> ObtenerInsumoMasUsado(OracleConnection conn, DateTime fi, DateTime ff)
        {
            string sql = @"
                SELECT i.NOMBRE, SUM(dt.CANTIDAD_USADA) as TOTAL
                FROM DETALLE_TAREA dt
                INNER JOIN INSUMO i ON dt.ID_INSUMO = i.ID_INSUMO
                INNER JOIN TAREA t ON dt.ID_TAREA = t.ID_TAREA
                WHERE t.FECHA_PROGRAMADA BETWEEN :fi AND :ff
                GROUP BY i.NOMBRE
                ORDER BY TOTAL DESC
                FETCH FIRST 1 ROW ONLY";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return (
                            reader.IsDBNull(0) ? "N/A" : reader.GetString(0),
                            reader.IsDBNull(1) ? 0 : reader.GetDecimal(1)
                        );
                    }
                }
            }

            return ("N/A", 0);
        }

        private async Task<List<GastoCategoria>> ObtenerGastosPorCategoria(
            OracleConnection conn, DateTime fi, DateTime ff, decimal totalGastos)
        {
            var categorias = new List<GastoCategoria>();

            // Insumos
            string sqlInsumos = @"
                SELECT NVL(SUM(dt.CANTIDAD_USADA * i.COSTO_UNITARIO), 0)
                FROM DETALLE_TAREA dt
                INNER JOIN INSUMO i ON dt.ID_INSUMO = i.ID_INSUMO
                INNER JOIN TAREA t ON dt.ID_TAREA = t.ID_TAREA
                WHERE t.FECHA_PROGRAMADA BETWEEN :fi AND :ff";

            using (var cmd = new OracleCommand(sqlInsumos, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;
                decimal monto = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                categorias.Add(new GastoCategoria
                {
                    Nombre = "Insumos",
                    Monto = monto,
                    Porcentaje = totalGastos > 0 ? (monto / totalGastos) * 100 : 0
                });
            }

            // Personal
            string sqlPersonal = @"
                SELECT NVL(SUM(at.HORAS_TRABAJADAS * e.MONTO_POR_HORA), 0)
                FROM ASIGNACION_TAREA at
                INNER JOIN EMPLEADO e ON at.ID_EMPLEADO = e.ID_USUARIO
                INNER JOIN TAREA t ON at.ID_TAREA = t.ID_TAREA
                WHERE t.FECHA_PROGRAMADA BETWEEN :fi AND :ff";

            using (var cmd = new OracleCommand(sqlPersonal, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;
                decimal monto = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                categorias.Add(new GastoCategoria
                {
                    Nombre = "Personal",
                    Monto = monto,
                    Porcentaje = totalGastos > 0 ? (monto / totalGastos) * 100 : 0
                });
            }

            // Transporte
            string sqlTransporte = @"
                SELECT NVL(SUM(COSTO_TRANSPORTE), 0)
                FROM TAREA
                WHERE FECHA_PROGRAMADA BETWEEN :fi AND :ff";

            using (var cmd = new OracleCommand(sqlTransporte, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;
                decimal monto = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                categorias.Add(new GastoCategoria
                {
                    Nombre = "Transporte",
                    Monto = monto,
                    Porcentaje = totalGastos > 0 ? (monto / totalGastos) * 100 : 0
                });
            }

            return categorias;
        }

        private async Task<List<TareaTop>> ObtenerTop5Tareas(OracleConnection conn, DateTime fi, DateTime ff)
        {
            var tareas = new List<TareaTop>();

            string sql = @"
                SELECT * FROM (
                    SELECT TIPO_ACTIVIDAD, GASTO_TOTAL
                    FROM TAREA
                    WHERE FECHA_PROGRAMADA BETWEEN :fi AND :ff
                    AND GASTO_TOTAL > 0
                    ORDER BY GASTO_TOTAL DESC
                ) WHERE ROWNUM <= 5";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tareas.Add(new TareaTop
                        {
                            Nombre = reader.GetString(0),
                            Costo = reader.GetDecimal(1)
                        });
                    }
                }
            }

            return tareas;
        }

        private async Task<List<TareaDetalle>> ObtenerDetalleTareas(OracleConnection conn, DateTime fi, DateTime ff)
        {
            var tareas = new List<TareaDetalle>();

            string sql = @"
        SELECT t.ID_TAREA, t.TIPO_ACTIVIDAD, c.NOMBRE_LOTE, t.FECHA_PROGRAMADA,
               t.TIEMPO_TOTAL_TAREA, t.ESTADO, t.COSTO_TRANSPORTE, t.GASTO_TOTAL
        FROM TAREA t
        LEFT JOIN CULTIVO c ON t.ID_CULTIVO = c.ID_CULTIVO
        WHERE t.FECHA_PROGRAMADA BETWEEN :fi AND :ff
        ORDER BY t.FECHA_PROGRAMADA DESC";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tareas.Add(new TareaDetalle
                        {
                            Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),                // ID_TAREA
                            TipoActividad = reader.IsDBNull(1) ? "Sin especificar" : reader.GetString(1), // TIPO_ACTIVIDAD
                            Cultivo = reader.IsDBNull(2) ? "Sin lote" : reader.GetString(2),       // NOMBRE_LOTE
                            FechaProgramada = reader.IsDBNull(3) ? DateTime.Now : reader.GetDateTime(3), // FECHA_PROGRAMADA
                            TiempoTotal = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),          // TIEMPO_TOTAL_TAREA
                            Estado = reader.IsDBNull(5) ? "Pendiente" : reader.GetString(5),      // ESTADO
                            CostoTransporte = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),      // COSTO_TRANSPORTE
                            GastoTotal = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7)            // GASTO_TOTAL
                        });
                    }
                }
            }

            return tareas;
        }

        private async Task<List<InsumoDetalle>> ObtenerDetalleInsumos(OracleConnection conn)
        {
            var insumos = new List<InsumoDetalle>();

            string sql = @"
                SELECT ID_INSUMO, NOMBRE, TIPO, STOCK_ACTUAL, UNIDAD_MEDIDA, COSTO_UNITARIO
                FROM INSUMO
                ORDER BY NOMBRE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        insumos.Add(new InsumoDetalle
                        {
                            Id = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            Tipo = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            StockActual = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                            UnidadMedida = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            CostoUnitario = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5)
                        });
                    }
                }
            }

            return insumos;
        }

        // CONTINÚA EN LA PARTE 2...

        // ============================================================================
        // PARTE 2 DE ReportesPDFService.cs
        // Agregar estos métodos a la clase ReportesPDFService
        // ============================================================================

        // ====================================================================
        // GENERAR GRÁFICAS
        // ====================================================================

        private string GenerarGraficaPie(List<GastoCategoria> gastos)
        {
            try
            {
                var plt = new ScottPlot.Plot();

                double[] values = gastos.Select(g => (double)g.Monto).ToArray();
                string[] labels = gastos.Select(g => $"{g.Nombre} ({g.Porcentaje:F1}%)").ToArray();

                var pie = plt.AddPie(values);
                pie.SliceLabels = labels;
                pie.ShowLabels = true;

                string path = Path.Combine(Path.GetTempPath(), $"grafica_gastos_{Guid.NewGuid()}.png");
                plt.SaveFig(path);

                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }


        private string GenerarGraficaBarra(List<TareaTop> tareas)
        {
            try
            {
                if (tareas == null || tareas.Count == 0)
                    return null;

                var plt = new ScottPlot.Plot(600, 400);

                double[] values = tareas.Select(t => (double)t.Costo).ToArray();
                string[] labels = tareas.Select(t => t.Nombre).ToArray();

                var bar = plt.AddBar(values);
                plt.XTicks(Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);
                plt.XAxis.TickLabelStyle(rotation: 45);
                plt.Title("Top 5 Tareas por Costo Total", size: 18, bold: true);
                plt.YLabel("Costo (COP $)");

                string path = Path.Combine(Path.GetTempPath(), $"grafica_barra_{Guid.NewGuid()}.png");
                plt.SaveFig(path);

                return path;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generando gráfica barra: {ex.Message}");
                return null;
            }
        }

        // ====================================================================
        // GENERAR PDF CON QUESTPDF
        // ====================================================================

        private void GenerarReportePDF(DatosReportePDF datos, string graficaPie, string graficaBarra, string outputPath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(c => ComposeContent(c, datos, graficaPie, graficaBarra));
                    page.Footer().Element(ComposeFooter);
                });
            })
            .GeneratePdf(outputPath);
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("AGROSMART")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    column.Item().Text("Reporte Semanal de Actividades e Insumos")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);
            });
        }

        // ============================================================================
        // REEMPLAZAR TODO EL MÉTODO ComposeContent CON ESTE:
        // ============================================================================

        private void ComposeContent(IContainer container, DatosReportePDF datos, string graficaPie, string graficaBarra)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                // PERÍODO
                column.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text($"📅 Período: {datos.FechaInicio:dd/MM/yyyy} al {datos.FechaFin:dd/MM/yyyy}")
                        .FontSize(12)
                        .Bold();
                });

                // INDICADORES CLAVE
                column.Item().PaddingTop(10).Text("Indicadores Clave de la Semana")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("💰 Total de Gastos").FontSize(11).Bold();
                        col.Item().Text($"${datos.TotalGastos:N0} COP")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Red.Darken1);

                        col.Item().PaddingTop(10).Text("📋 Número de Tareas").FontSize(11).Bold();
                        col.Item().Text($"{datos.NumeroTareas}")
                            .FontSize(18)
                            .Bold();
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("💸 Tarea Más Costosa").FontSize(11).Bold();
                        col.Item().Text(datos.TareaMasCostosa ?? "N/A")
                            .FontSize(12)
                            .FontColor(Colors.Blue.Medium);
                        col.Item().Text($"${datos.CostoTareaMasCostosa:N0}")
                            .FontSize(14)
                            .Bold();

                        col.Item().PaddingTop(10).Text("📦 Insumo Más Usado").FontSize(11).Bold();
                        col.Item().Text(datos.InsumoMasUsado ?? "N/A")
                            .FontSize(12);
                        col.Item().Text($"{datos.CantidadInsumoUsado:N2} unidades")
                            .FontSize(11);
                    });
                });

                // ========== ⭐ NUEVA SECCIÓN: CULTIVOS TRABAJADOS ==========
                column.Item().PageBreak();
                column.Item().Text("🌱 Cultivos Trabajados en la Semana")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Green.Darken2);

                if (datos.CultivosTrabajados != null && datos.CultivosTrabajados.Count > 0)
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Lote").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Tipo Cultivo").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Green.Medium).Padding(5).Text("F. Siembra").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Tareas").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Gasto").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Cosecha").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        int contador = 0;
                        foreach (var cultivo in datos.CultivosTrabajados)
                        {
                            var bgColor = contador % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cultivo.NombreLote).FontSize(8);

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cultivo.TipoCultivo).FontSize(8);

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cultivo.FechaSiembra.ToString("dd/MM/yy")).FontSize(8);

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cultivo.NumeroTareasRealizadas.ToString()).FontSize(8).AlignCenter();

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text($"${cultivo.GastoTotal:N0}").FontSize(8).AlignRight();

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cultivo.TuvoCosecha ? "✅ SÍ" : "❌ NO")
                                .FontSize(8)
                                .AlignCenter()
                                .FontColor(cultivo.TuvoCosecha ? Colors.Green.Darken1 : Colors.Red.Darken1);

                            contador++;
                        }
                    });
                }
                else
                {
                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                        .Text("No se trabajó en ningún cultivo durante este período.")
                        .FontSize(10)
                        .Italic();
                }

                // ========== ⭐ NUEVA SECCIÓN: COSECHAS ==========
                if (datos.CosechasSemana != null && datos.CosechasSemana.Count > 0)
                {
                    column.Item().PaddingTop(20).Text("🌾 Cosechas Realizadas")
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Orange.Darken2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Lote").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Fecha Cosecha").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Cantidad").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Calidad").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        int contador = 0;
                        foreach (var cosecha in datos.CosechasSemana)
                        {
                            var bgColor = contador % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cosecha.NombreLote).FontSize(8);

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cosecha.FechaCosecha.ToString("dd/MM/yyyy")).FontSize(8);

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text($"{cosecha.CantidadObtenida:N2} {cosecha.UnidadMedida}").FontSize(8);

                            table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(cosecha.Calidad).FontSize(8).AlignCenter();

                            contador++;
                        }
                    });
                }
                else
                {
                    column.Item().PaddingTop(20).Text("🌾 Cosechas Realizadas")
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.Orange.Darken2);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                        .Text("No hubo cosechas durante este período.")
                        .FontSize(10)
                        .Italic();
                }

                // TABLA DE GASTOS
                column.Item().PageBreak();
                column.Item().PaddingTop(10).Text("Resumen de Gastos por Categoría")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("Categoría").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("Monto (COP)").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("% del Total").Bold().FontColor(Colors.White);
                    });

                    foreach (var gasto in datos.GastosPorCategoria)
                    {
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                            .Text(gasto.Nombre);

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                            .Text($"${gasto.Monto:N0}").AlignRight();

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                            .Text($"{gasto.Porcentaje:F2}%").AlignCenter();
                    }

                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).Padding(8)
                        .Text("TOTAL GENERAL").Bold();

                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).Padding(8)
                        .Text($"${datos.TotalGastos:N0}").Bold().AlignRight();

                    table.Cell().Background(Colors.Grey.Lighten3).Border(1).Padding(8)
                        .Text("100.00%").Bold().AlignCenter();
                });

                // GRÁFICAS
                if (!string.IsNullOrEmpty(graficaPie) && File.Exists(graficaPie))
                {
                    column.Item().PaddingTop(20).Text("Distribución de Gastos")
                        .FontSize(14)
                        .Bold();
                    column.Item().Height(250).Image(graficaPie);
                }

                column.Item().PageBreak();

                if (!string.IsNullOrEmpty(graficaBarra) && File.Exists(graficaBarra))
                {
                    column.Item().PaddingTop(20).Text("Top 5 Tareas")
                        .FontSize(14)
                        .Bold();
                    column.Item().Height(250).Image(graficaBarra);
                }

                // ========== ⭐ NUEVA SECCIÓN: DETALLE POR LOTES ==========
                column.Item().PageBreak();
                column.Item().Text("📍 Detalle de Trabajo por Lotes")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Purple.Darken2);

                if (datos.LotesTrabajados != null && datos.LotesTrabajados.Count > 0)
                {
                    foreach (var lote in datos.LotesTrabajados.Take(5))
                    {
                        // Encabezado del lote
                        column.Item().PaddingTop(15).Background(Colors.Purple.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"🌱 {lote.NombreLote}")
                                    .FontSize(13)
                                    .Bold()
                                    .FontColor(Colors.Purple.Darken2);

                                col.Item().Text($"Tipo: {lote.TipoCultivo} | Tareas: {lote.NumeroTareas} | Gasto: ${lote.GastoTotal:N0}")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });

                        // Tareas del lote
                        foreach (var tarea in lote.Tareas.Take(3))
                        {
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(tareaCol =>
                            {
                                tareaCol.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"📋 {tarea.TipoActividad}")
                                        .FontSize(11)
                                        .Bold();

                                    row.ConstantItem(120).AlignRight().Text($"${tarea.GastoTotal:N0}")
                                        .FontSize(11)
                                        .Bold()
                                        .FontColor(Colors.Red.Darken1);
                                });

                                tareaCol.Item().Text($"Fecha: {tarea.FechaProgramada:dd/MM/yyyy} | Estado: {tarea.Estado}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);

                                // Insumos
                                if (tarea.InsumosUtilizados != null && tarea.InsumosUtilizados.Count > 0)
                                {
                                    tareaCol.Item().PaddingTop(5).Text("📦 Insumos utilizados:")
                                        .FontSize(9)
                                        .Bold();

                                    tareaCol.Item().PaddingLeft(10).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                        });

                                        foreach (var insumo in tarea.InsumosUtilizados)
                                        {
                                            table.Cell().Padding(2).Text(insumo.NombreInsumo).FontSize(8);
                                            table.Cell().Padding(2).Text($"{insumo.CantidadUsada:N2} {insumo.UnidadMedida}").FontSize(8).AlignRight();
                                            table.Cell().Padding(2).Text($"${insumo.CostoTotal:N0}").FontSize(8).AlignRight();
                                        }
                                    });
                                }
                                else
                                {
                                    tareaCol.Item().PaddingTop(5).Text("• Sin insumos registrados")
                                        .FontSize(8)
                                        .Italic()
                                        .FontColor(Colors.Grey.Medium);
                                }
                            });
                        }

                        if (lote.Tareas.Count > 3)
                        {
                            column.Item().Padding(5).Text($"... y {lote.Tareas.Count - 3} tareas más")
                                .FontSize(9)
                                .Italic();
                        }
                    }
                }

                // INVENTARIO (existente)
                column.Item().PageBreak();
                column.Item().Text("Inventario de Insumos")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("ID").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Nombre").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Tipo").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Stock").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Colors.Orange.Medium).Padding(5).Text("Costo").Bold().FontColor(Colors.White).FontSize(9);
                    });

                    int contador = 0;
                    foreach (var insumo in datos.InsumosDetalle.Take(25))
                    {
                        var bgColor = contador % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                            .Text(insumo.Id.ToString()).FontSize(8);
                        table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                            .Text(insumo.Nombre).FontSize(8);
                        table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                            .Text(insumo.Tipo).FontSize(8);
                        table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                            .Text($"{insumo.StockActual:N2} {insumo.UnidadMedida}").FontSize(8);
                        table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                            .Text($"${insumo.CostoUnitario:N0}").FontSize(8).AlignRight();

                        contador++;
                    }
                });

                // ANÁLISIS IA MEJORADO
                column.Item().PageBreak();
                column.Item().Text("Análisis IA (AgroBot)")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().Border(1).BorderColor(Colors.Blue.Lighten2).Background(Colors.Blue.Lighten5)
                    .Padding(15).Text(GenerarAnalisisIAMejorado(datos))
                    .FontSize(10)
                    .LineHeight(1.5f);
            });
        }

        private string GenerarAnalisisIA(DatosReportePDF datos)
        {
            var analisis = new System.Text.StringBuilder();

            analisis.AppendLine("🤖 Análisis Automático del Período\n");
            analisis.AppendLine("Basado en los datos recopilados:\n");

            if (datos.TotalGastos > 0)
            {
                var categoriaMax = datos.GastosPorCategoria.OrderByDescending(g => g.Monto).FirstOrDefault();
                analisis.AppendLine($"• La categoría con mayor gasto fue '{categoriaMax?.Nombre}' representando el {categoriaMax?.Porcentaje:F1}% del total.");
            }

            analisis.AppendLine($"• Se completaron {datos.NumeroTareas} tareas durante el período.");

            if (!string.IsNullOrEmpty(datos.TareaMasCostosa))
            {
                analisis.AppendLine($"• La actividad '{datos.TareaMasCostosa}' fue la más costosa con ${datos.CostoTareaMasCostosa:N0}.");
            }

            if (!string.IsNullOrEmpty(datos.InsumoMasUsado))
            {
                analisis.AppendLine($"• El insumo más utilizado fue '{datos.InsumoMasUsado}' con {datos.CantidadInsumoUsado:N2} unidades.");
            }

            analisis.AppendLine("\nRecomendaciones:");
            analisis.AppendLine("• Revisar los costos de transporte si superan el 15% del gasto total.");
            analisis.AppendLine("• Verificar el stock de los insumos más utilizados.");
            analisis.AppendLine("• Evaluar la eficiencia de las tareas más costosas.");

            return analisis.ToString();
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Row(row =>
            {
                row.RelativeItem().AlignLeft()
                    .Text(text =>
                    {
                        text.Span($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Medium);
                    });

                row.RelativeItem().AlignCenter()
                    .Text(text =>
                    {
                        text.Span("AgroSmart Bot - Reporte Automático")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Medium);
                    });

                row.RelativeItem().AlignRight()
                    .Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        }

        // ====================================================================
        // ENVÍO DE EMAIL
        // ====================================================================

        private async Task EnviarEmailConAdjunto(string destinatario, string asunto, string cuerpoHtml, string rutaArchivo, string nombreArchivo)
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(_configEmail.EmailRemitente, "AgroSmart Bot");
                mail.To.Add(destinatario);
                mail.Subject = asunto;
                mail.Body = cuerpoHtml;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.High;

                Attachment attachment = new Attachment(rutaArchivo, MediaTypeNames.Application.Pdf);
                ContentDisposition disposition = attachment.ContentDisposition;
                disposition.FileName = nombreArchivo;
                mail.Attachments.Add(attachment);

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(_configEmail.EmailRemitente, _configEmail.PasswordApp);
                    smtp.EnableSsl = true;
                    smtp.Timeout = 30000;

                    await smtp.SendMailAsync(mail);
                }
            }
        }

        private string GenerarCuerpoEmail(DateTime fechaInicio, DateTime fechaFin)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0066cc; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .info-box {{ background-color: white; padding: 15px; margin: 10px 0; border-left: 4px solid #0066cc; }}
        .footer {{ background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🌱 AgroSmart</h1>
        <h2>Reporte Semanal PDF</h2>
    </div>
    <div class='content'>
        <p>Estimado usuario,</p>
        <p>Adjunto encontrarás el reporte semanal con datos exactos de la base de datos.</p>
        <div class='info-box'>
            <p><strong>📅 Período:</strong> {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}</p>
        </div>
    </div>
    <div class='footer'>
        <p>Generado por AgroSmart Bot</p>
        <p>{DateTime.Now:dd/MM/yyyy HH:mm}</p>
    </div>
</body>
</html>";
        }

        private string GenerarCuerpoEmailAutomatico(DateTime fechaInicio, DateTime fechaFin)
        {
            return GenerarCuerpoEmail(fechaInicio, fechaFin)
                .Replace("Reporte Semanal PDF", "Reporte Semanal Automático PDF");
        }

        // ====================================================================
        // UTILIDADES
        // ====================================================================

        private void LimpiarArchivos(params string[] rutas)
        {
            foreach (var ruta in rutas)
            {
                if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta))
                {
                    try
                    {
                        File.Delete(ruta);
                    }
                    catch
                    {
                        // Ignorar errores al limpiar
                    }
                }
            }
        }

        public void LimpiarRecursos(ResultadoReporte resultado)
        {
            if (resultado != null && !string.IsNullOrEmpty(resultado.RutaArchivo))
            {
                LimpiarArchivos(resultado.RutaArchivo);
            }
        }


        // ========================================================================
        // CLASES DE DATOS
        // ========================================================================

        public class DatosReportePDF
        {
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public decimal TotalGastos { get; set; }
            public int NumeroTareas { get; set; }
            public string TareaMasCostosa { get; set; }
            public decimal CostoTareaMasCostosa { get; set; }
            public string InsumoMasUsado { get; set; }
            public decimal CantidadInsumoUsado { get; set; }
            public List<GastoCategoria> GastosPorCategoria { get; set; }
            public List<TareaTop> Top5Tareas { get; set; }
            public List<TareaDetalle> TareasDetalle { get; set; }
            public List<InsumoDetalle> InsumosDetalle { get; set; }

            public List<LoteTrabajado> LotesTrabajados { get; set; }
            public List<CosechaDetalle> CosechasSemana { get; set; }
            public List<CultivoTrabajado> CultivosTrabajados { get; set; }
        }

        public class GastoCategoria
        {
            public string Nombre { get; set; }
            public decimal Monto { get; set; }
            public decimal Porcentaje { get; set; }
        }

        public class TareaTop
        {
            public string Nombre { get; set; }
            public decimal Costo { get; set; }
        }

        public class TareaDetalle
        {
            public int Id { get; set; }
            public string TipoActividad { get; set; }
            public string Cultivo { get; set; }
            public DateTime FechaProgramada { get; set; }
            public decimal TiempoTotal { get; set; }
            public string Estado { get; set; }
            public decimal CostoTransporte { get; set; }
            public decimal GastoTotal { get; set; }
        }

        public class InsumoDetalle
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public decimal StockActual { get; set; }
            public string UnidadMedida { get; set; }
            public decimal CostoUnitario { get; set; }
        }

        public class ConfiguracionEmail
        {
            public string EmailRemitente { get; set; }
            public string PasswordApp { get; set; }
            public string EmailDestino { get; set; }
        }

        public class ResultadoReporte
        {
            public bool Exitoso { get; set; }
            public string RutaArchivo { get; set; }
            public string NombreArchivo { get; set; }
            public bool EmailEnviado { get; set; }
            public string MensajeError { get; set; }
            public DatosReportePDF DatosReporte { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
        }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        public class LoteTrabajado
        {
            public int IdCultivo { get; set; }
            public string NombreLote { get; set; }
            public string TipoCultivo { get; set; }
            public int NumeroTareas { get; set; }
            public decimal GastoTotal { get; set; }
            public List<TareaConInsumos> Tareas { get; set; }
        }

        public class TareaConInsumos
        {
            public int IdTarea { get; set; }
            public string TipoActividad { get; set; }
            public DateTime FechaProgramada { get; set; }
            public string Estado { get; set; }
            public decimal GastoTotal { get; set; }
            public List<InsumoUtilizado> InsumosUtilizados { get; set; }
        }

        public class InsumoUtilizado
        {
            public string NombreInsumo { get; set; }
            public decimal CantidadUsada { get; set; }
            public string UnidadMedida { get; set; }
            public decimal CostoUnitario { get; set; }
            public decimal CostoTotal { get; set; }
        }

        public class CosechaDetalle
        {
            public int IdCosecha { get; set; }
            public int IdCultivo { get; set; }
            public int IdAdminRegistro { get; set; }
            public DateTime FechaCosecha { get; set; }
            public DateTime FechaRegistro { get; set; }
            public decimal CantidadObtenida { get; set; }
            public string UnidadMedida { get; set; }
            public string Calidad { get; set; }
            public string Observaciones { get; set; }
            // Campos adicionales del JOIN con CULTIVO
            public string NombreLote { get; set; }
        }

        public class CultivoTrabajado
        {
            public int IdCultivo { get; set; }
            public string NombreLote { get; set; }
            public string TipoCultivo { get; set; }
            public DateTime FechaSiembra { get; set; }
            public int NumeroTareasRealizadas { get; set; }
            public decimal GastoTotal { get; set; }
            public bool TuvoCosecha { get; set; }
        }
        private async Task<List<LoteTrabajado>> ObtenerLotesTrabajados(OracleConnection conn, DateTime fi, DateTime ff)
        {
            var lotes = new List<LoteTrabajado>();

            string sqlLotes = @"
        SELECT DISTINCT 
            c.ID_CULTIVO,
            c.NOMBRE_LOTE,
            COUNT(t.ID_TAREA) as NUM_TAREAS,
            NVL(SUM(t.GASTO_TOTAL), 0) as GASTO_TOTAL
        FROM CULTIVO c
        INNER JOIN TAREA t ON c.ID_CULTIVO = t.ID_CULTIVO
        WHERE t.FECHA_PROGRAMADA BETWEEN :fi AND :ff
        GROUP BY c.ID_CULTIVO, c.NOMBRE_LOTE
        ORDER BY GASTO_TOTAL DESC";

            using (var cmd = new OracleCommand(sqlLotes, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var lote = new LoteTrabajado
                        {
                            IdCultivo = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            NombreLote = reader.IsDBNull(1) ? "Sin nombre" : reader.GetString(1),
                            TipoCultivo = "Cultivo General",
                            NumeroTareas = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                            GastoTotal = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                            Tareas = new List<TareaConInsumos>()
                        };

                        lote.Tareas = await ObtenerTareasConInsumos(conn, lote.IdCultivo, fi, ff);
                        lotes.Add(lote);
                    }
                }
            }

            return lotes;
        }

        private async Task<List<TareaConInsumos>> ObtenerTareasConInsumos(
     OracleConnection conn, int idCultivo, DateTime fi, DateTime ff)
        {
            var tareas = new List<TareaConInsumos>();

            string sqlTareas = @"
        SELECT 
            ID_TAREA,
            TIPO_ACTIVIDAD,
            FECHA_PROGRAMADA,
            ESTADO,
            GASTO_TOTAL
        FROM TAREA
        WHERE ID_CULTIVO = :idCultivo
        AND FECHA_PROGRAMADA BETWEEN :fi AND :ff
        ORDER BY FECHA_PROGRAMADA DESC";

            using (var cmd = new OracleCommand(sqlTareas, conn))
            {
                cmd.Parameters.Add("idCultivo", OracleDbType.Int32).Value = idCultivo;
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tarea = new TareaConInsumos
                        {
                            IdTarea = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            TipoActividad = reader.IsDBNull(1) ? "Sin especificar" : reader.GetString(1),
                            FechaProgramada = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2),
                            Estado = reader.IsDBNull(3) ? "Pendiente" : reader.GetString(3),
                            GastoTotal = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                            InsumosUtilizados = new List<InsumoUtilizado>()
                        };

                        tarea.InsumosUtilizados = await ObtenerInsumosUtilizados(conn, tarea.IdTarea);
                        tareas.Add(tarea);
                    }
                }
            }

            return tareas;
        }

        private async Task<List<InsumoUtilizado>> ObtenerInsumosUtilizados(OracleConnection conn, int idTarea)
        {
            var insumos = new List<InsumoUtilizado>();

            string sql = @"
        SELECT 
            i.NOMBRE,
            dt.CANTIDAD_USADA,
            i.UNIDAD_MEDIDA,
            i.COSTO_UNITARIO,
            (dt.CANTIDAD_USADA * i.COSTO_UNITARIO) as COSTO_TOTAL
        FROM DETALLE_TAREA dt
        INNER JOIN INSUMO i ON dt.ID_INSUMO = i.ID_INSUMO
        WHERE dt.ID_TAREA = :idTarea
        ORDER BY COSTO_TOTAL DESC";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("idTarea", OracleDbType.Int32).Value = idTarea;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        insumos.Add(new InsumoUtilizado
                        {
                            NombreInsumo = reader.IsDBNull(0) ? "Sin nombre" : reader.GetString(0),
                            CantidadUsada = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                            UnidadMedida = reader.IsDBNull(2) ? "N/A" : reader.GetString(2),
                            CostoUnitario = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                            CostoTotal = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)
                        });
                    }
                }
            }

            return insumos;
        }


        private async Task<List<CosechaDetalle>> ObtenerCosechasSemana(OracleConnection conn, DateTime fi, DateTime ff)
        {
            var cosechas = new List<CosechaDetalle>();

            string sql = @"
        SELECT 
            co.ID_COSECHA,
            co.ID_CULTIVO,
            co.ID_ADMIN_REGISTRO,   
            co.FECHA_COSECHA,
            co.FECHA_REGISTRO,
            co.CANTIDAD_OBTENIDA,
            co.UNIDAD_MEDIDA,
            co.CALIDAD,
            co.OBSERVACIONES,
            c.NOMBRE_LOTE
        FROM COSECHA co
        INNER JOIN CULTIVO c ON co.ID_CULTIVO = c.ID_CULTIVO
        WHERE co.FECHA_COSECHA BETWEEN :fi AND :ff
        ORDER BY co.FECHA_COSECHA DESC";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        cosechas.Add(new CosechaDetalle
                        {
                            IdCosecha = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            IdCultivo = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                            IdAdminRegistro = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                            FechaCosecha = reader.IsDBNull(3) ? DateTime.Now : reader.GetDateTime(3),
                            FechaRegistro = reader.IsDBNull(4) ? DateTime.Now : reader.GetDateTime(4),
                            CantidadObtenida = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                            UnidadMedida = reader.IsDBNull(6) ? "N/A" : reader.GetString(6),
                            Calidad = reader.IsDBNull(7) ? "No especificada" : reader.GetString(7),
                            Observaciones = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            NombreLote = reader.IsDBNull(9) ? "Sin nombre" : reader.GetString(9)
                        });
                    }
                }
            }

            return cosechas;
        }


        private async Task<List<CultivoTrabajado>> ObtenerCultivosTrabajados(OracleConnection conn, DateTime fi, DateTime ff)
        {
            var cultivos = new List<CultivoTrabajado>();

            string sql = @"
        SELECT 
            c.ID_CULTIVO,
            c.NOMBRE_LOTE,
            c.FECHA_SIEMBRA,
            COUNT(DISTINCT t.ID_TAREA) as NUM_TAREAS,
            NVL(SUM(t.GASTO_TOTAL), 0) as GASTO_TOTAL,
            CASE WHEN EXISTS (
                SELECT 1 FROM COSECHA co 
                WHERE co.ID_CULTIVO = c.ID_CULTIVO 
                AND co.FECHA_COSECHA BETWEEN :fi AND :ff
            ) THEN 1 ELSE 0 END as TUVO_COSECHA
        FROM CULTIVO c
        LEFT JOIN TAREA t ON c.ID_CULTIVO = t.ID_CULTIVO 
            AND t.FECHA_PROGRAMADA BETWEEN :fi AND :ff
        WHERE EXISTS (
            SELECT 1 FROM TAREA t2 
            WHERE t2.ID_CULTIVO = c.ID_CULTIVO 
            AND t2.FECHA_PROGRAMADA BETWEEN :fi AND :ff
        )
        GROUP BY c.ID_CULTIVO, c.NOMBRE_LOTE, c.FECHA_SIEMBRA
        ORDER BY GASTO_TOTAL DESC";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("fi", OracleDbType.Date).Value = fi;
                cmd.Parameters.Add("ff", OracleDbType.Date).Value = ff;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        cultivos.Add(new CultivoTrabajado
                        {
                            IdCultivo = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            NombreLote = reader.IsDBNull(1) ? "Sin nombre" : reader.GetString(1),
                            TipoCultivo = "Cultivo General",  // Campo no existe en BD
                            FechaSiembra = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2),
                            NumeroTareasRealizadas = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                            GastoTotal = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                            TuvoCosecha = !reader.IsDBNull(5) && reader.GetInt32(5) == 1
                        });
                    }
                }
            }

            return cultivos;
        }

        // ============================================================================
        // AGREGAR ESTE MÉTODO NUEVO (después del método GenerarAnalisisIA existente)
        // ============================================================================

        private string GenerarAnalisisIAMejorado(DatosReportePDF datos)
        {
            var analisis = new System.Text.StringBuilder();

            analisis.AppendLine("🤖 Análisis Automático del Período\n");

            // Análisis de cultivos trabajados
            if (datos.CultivosTrabajados != null && datos.CultivosTrabajados.Count > 0)
            {
                analisis.AppendLine($"📊 RESUMEN DE ACTIVIDAD:");
                analisis.AppendLine($"• Se trabajó en {datos.CultivosTrabajados.Count} lote(s) durante esta semana.");

                var cultivosConCosecha = datos.CultivosTrabajados.Where(c => c.TuvoCosecha).ToList();
                if (cultivosConCosecha.Count > 0)
                {
                    analisis.AppendLine($"• ✅ {cultivosConCosecha.Count} lote(s) tuvieron cosecha:");
                    foreach (var cultivo in cultivosConCosecha)
                    {
                        analisis.AppendLine($"   - {cultivo.NombreLote} ({cultivo.TipoCultivo})");
                    }
                }
                else
                {
                    analisis.AppendLine($"• ❌ No hubo cosechas en esta semana.");
                }

                analisis.AppendLine();
            }

            // Análisis de gastos
            if (datos.TotalGastos > 0)
            {
                analisis.AppendLine($"💰 ANÁLISIS FINANCIERO:");
                analisis.AppendLine($"• Gasto total: ${datos.TotalGastos:N0}");

                var categoriaMax = datos.GastosPorCategoria.OrderByDescending(g => g.Monto).FirstOrDefault();
                if (categoriaMax != null)
                {
                    analisis.AppendLine($"• Mayor gasto: {categoriaMax.Nombre} ({categoriaMax.Porcentaje:F1}%)");
                }

                if (!string.IsNullOrEmpty(datos.TareaMasCostosa))
                {
                    analisis.AppendLine($"• Tarea más costosa: '{datos.TareaMasCostosa}' (${datos.CostoTareaMasCostosa:N0})");
                }

                analisis.AppendLine();
            }

            // Análisis de tareas
            analisis.AppendLine($"📋 TAREAS EJECUTADAS:");
            analisis.AppendLine($"• Total de tareas: {datos.NumeroTareas}");

            if (datos.LotesTrabajados != null && datos.LotesTrabajados.Count > 0)
            {
                var loteConMasTareas = datos.LotesTrabajados.OrderByDescending(l => l.NumeroTareas).FirstOrDefault();
                if (loteConMasTareas != null)
                {
                    analisis.AppendLine($"• Lote más activo: {loteConMasTareas.NombreLote} ({loteConMasTareas.NumeroTareas} tareas)");
                }
            }

            analisis.AppendLine();

            // Recomendaciones
            analisis.AppendLine("💡 RECOMENDACIONES:");

            // Recomendación sobre cosechas
            if (datos.CosechasSemana != null && datos.CosechasSemana.Count > 0)
            {
                analisis.AppendLine($"• Registrar la calidad y cantidad de las {datos.CosechasSemana.Count} cosecha(s) realizadas.");
            }

            // Recomendación sobre gastos
            var gastoTransporte = datos.GastosPorCategoria.FirstOrDefault(g => g.Nombre == "Transporte");
            if (gastoTransporte != null && gastoTransporte.Porcentaje > 15)
            {
                analisis.AppendLine($"• ⚠️ Los costos de transporte ({gastoTransporte.Porcentaje:F1}%) están por encima del 15%. Considere optimizar rutas.");
            }

            // Recomendación sobre insumos
            if (!string.IsNullOrEmpty(datos.InsumoMasUsado))
            {
                analisis.AppendLine($"• Verificar el stock de '{datos.InsumoMasUsado}' (el más utilizado) para evitar desabastecimiento.");
            }

            // Recomendación sobre tareas costosas
            if (!string.IsNullOrEmpty(datos.TareaMasCostosa) && datos.CostoTareaMasCostosa > 0)
            {
                analisis.AppendLine($"• Evaluar la eficiencia de '{datos.TareaMasCostosa}' para identificar oportunidades de optimización.");
            }

            return analisis.ToString();


        }
    }
}











