using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES_DTOS;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AGROSMART_GUI.Views.Admin
{
    /// <summary>
    /// Página para gestionar y visualizar los gastos asociados a las tareas agrícolas con filtros
    /// </summary>
    public partial class GestionGastosPage : Page
    {
        #region Servicios y Colecciones

        private readonly GastosService _gastoService;
        private List<GASTOS_DTO> _listaGastos;
        private List<GASTOS_DTO> _listaGastosFiltrada;

        #endregion

        #region Constructor

        public GestionGastosPage()
        {
            InitializeComponent();
            _gastoService = new GastosService();
            _listaGastos = new List<GASTOS_DTO>();
            _listaGastosFiltrada = new List<GASTOS_DTO>();
        }

        #endregion

        #region Eventos de Página

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CargarAnios();
            await CargarDatosIniciales();
        }

        #endregion

        #region Gestión de Filtros

        /// <summary>
        /// Carga los años disponibles en el combo box
        /// </summary>
        private void CargarAnios()
        {
            const int ANIO_INICIO = 2020;
            int anioActual = DateTime.Now.Year;

            cmbAnio.Items.Clear();

            // Opción "Todos los años"
            var itemTodos = CrearComboBoxItem("Todos los años", 0);
            cmbAnio.Items.Add(itemTodos);

            // Cargar años desde 2020 hasta año actual + 1
            for (int anio = ANIO_INICIO; anio <= anioActual + 1; anio++)
            {
                var item = CrearComboBoxItem(anio.ToString(), anio);
                cmbAnio.Items.Add(item);

                if (anio == anioActual)
                {
                    item.IsSelected = true;
                }
            }

            // Fallback: seleccionar "Todos" si no hay selección
            if (cmbAnio.SelectedItem == null)
            {
                itemTodos.IsSelected = true;
            }
        }

        private ComboBoxItem CrearComboBoxItem(string contenido, int valor)
        {
            return new ComboBoxItem
            {
                Content = contenido,
                Tag = valor
            };
        }

        private void FiltroChanged(object sender, SelectionChangedEventArgs e)
        {
            // Solo aplicar filtros si ya se cargaron los datos
            if (_listaGastos?.Any() ?? false)
            {
                AplicarFiltros();
            }
        }
        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar fechas
            dpFechaDesde.SelectedDate = null;
            dpFechaHasta.SelectedDate = null;
            AplicarFiltros();
        }
        private void FechaRangoChanged(object sender, SelectionChangedEventArgs e)
        {
            // Solo aplicar filtros si ya se cargaron los datos
            if (_listaGastos?.Any() ?? false)
            {
                AplicarFiltros();
            }
        }

        /// <summary>
        /// Aplica los filtros de mes y año a los gastos
        /// </summary>
        private void AplicarFiltros()
        {
            try
            {
                var (mesSeleccionado, anioSeleccionado) = ObtenerValoresFiltros();

                _listaGastosFiltrada = FiltrarGastosPorFecha(mesSeleccionado, anioSeleccionado);

                ActualizarTextoResultados(mesSeleccionado, anioSeleccionado);
                CargarFilasTabla();
                ActualizarTarjetasResumen();
            }
            catch (Exception ex)
            {
                MostrarError("Error al aplicar filtros", ex);
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_listaGastosFiltrada?.Any() ?? true)
                {
                    MessageBox.Show(
                        "No hay datos para exportar. Verifica los filtros aplicados.",
                        "Sin datos",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Abrir diálogo para guardar archivo
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Archivo PDF|*.pdf",
                    Title = "Exportar Gastos a PDF",
                    FileName = $"Gastos_AGROSMART_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportarAPDF(saveDialog.FileName);

                    MessageBox.Show(
                        $"✅ Datos exportados exitosamente\n\nArchivo: {Path.GetFileName(saveDialog.FileName)}",
                        "Exportación Exitosa",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Preguntar si desea abrir el archivo
                    var resultado = MessageBox.Show(
                        "¿Deseas abrir el archivo ahora?",
                        "Abrir archivo",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar datos:\n\n{ex.Message}",
                    "Error de Exportación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportarAPDF(string rutaArchivo)
        {
            try
            {
                using (var fs = new FileStream(rutaArchivo, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Configurar documento
                    var document = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30);
                    var writer = PdfWriter.GetInstance(document, fs);

                    document.Open();

                    // Agregar contenido
                    AgregarEncabezadoPDF(document);
                    AgregarResumenPDF(document);
                    AgregarTablaGastosPDF(document);
                    AgregarPiePaginaPDF(document, writer);

                    document.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al exportar a PDF: {ex.Message}", ex);
            }
        }

        private void AgregarEncabezadoPDF(Document document)
        {
            // Título principal
            var tituloFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.WHITE);
            var titulo = new Paragraph("REPORTE DE GASTOS - AGROSMART", tituloFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20f
            };

            var celdaTitulo = new PdfPCell(titulo)
            {
                BackgroundColor = new BaseColor(0, 168, 89), // #00A859
                Border = Rectangle.NO_BORDER,
                Padding = 12f
            };

            var tablaTitulo = new PdfPTable(1)
            {
                WidthPercentage = 100
            };
            tablaTitulo.AddCell(celdaTitulo);
            document.Add(tablaTitulo);

            // Información de filtros y fecha
            var (mes, anio) = ObtenerValoresFiltros();
            string periodoTexto = mes == 0 && anio == 0 ? "Todos los períodos" :
                                 mes == 0 ? $"Año {anio}" :
                                 anio == 0 ? $"Mes: {ObtenerNombreMes(mes)}" :
                                 $"{ObtenerNombreMes(mes)} {anio}";

            var infoFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var infoTable = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingAfter = 20f
            };

            // Columna izquierda: Período
            var celdaPeriodo = new PdfPCell(new Phrase($"Período: {periodoTexto}", infoFont))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            };

            // Columna derecha: Fecha generación
            var celdaFecha = new PdfPCell(new Phrase($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}", infoFont))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };

            infoTable.AddCell(celdaPeriodo);
            infoTable.AddCell(celdaFecha);
            document.Add(infoTable);
        }

        private void AgregarResumenPDF(Document document)
        {
            // Calcular totales
            decimal totalGastos = _listaGastosFiltrada.Sum(g => g.TotalGasto);
            decimal totalInsumos = _listaGastosFiltrada.Sum(g => g.GastoInsumos);
            decimal totalPersonal = _listaGastosFiltrada.Sum(g => g.PagoEmpleados);
            decimal totalTransporte = _listaGastosFiltrada.Sum(g => g.GastoTransporte);

            // Título del resumen
            var tituloFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var titulo = new Paragraph("RESUMEN DE GASTOS", tituloFont)
            {
                Alignment = Element.ALIGN_LEFT,
                SpacingAfter = 10f
            };
            document.Add(titulo);

            // Tabla de resumen
            var tablaResumen = new PdfPTable(3)
            {
                WidthPercentage = 100,
                SpacingAfter = 20f
            };

            // Encabezados
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
            var headers = new[] { "Categoría", "Monto", "Porcentaje" };

            foreach (var header in headers)
            {
                var celda = new PdfPCell(new Phrase(header, headerFont))
                {
                    BackgroundColor = new BaseColor(200, 230, 201), // #C8E6C9
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8f
                };
                tablaResumen.AddCell(celda);
            }

            // Datos del resumen
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var datosResumen = new[]
            {
        new { Categoria = "💰 TOTAL GENERAL", Monto = totalGastos, Porcentaje = 100.0 },
        new { Categoria = "🌿 Insumos", Monto = totalInsumos, Porcentaje = totalGastos > 0 ? (double)(totalInsumos / totalGastos * 100) : 0 },
        new { Categoria = "👥 Personal", Monto = totalPersonal, Porcentaje = totalGastos > 0 ? (double)(totalPersonal / totalGastos * 100) : 0 },
        new { Categoria = "🚚 Transporte", Monto = totalTransporte, Porcentaje = totalGastos > 0 ? (double)(totalTransporte / totalGastos * 100) : 0 }
    };

            foreach (var dato in datosResumen)
            {
                // Categoría
                tablaResumen.AddCell(new PdfPCell(new Phrase(dato.Categoria, dataFont)) { Padding = 6f });

                // Monto
                tablaResumen.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(dato.Monto), dataFont))
                {
                    Padding = 6f,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                // Porcentaje
                var porcentajeTexto = totalGastos > 0 ? $"{dato.Porcentaje:F1}%" : "0%";
                tablaResumen.AddCell(new PdfPCell(new Phrase(porcentajeTexto, dataFont))
                {
                    Padding = 6f,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
            }

            document.Add(tablaResumen);
        }

        private void AgregarTablaGastosPDF(Document document)
        {
            // Título de la tabla
            var tituloFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var titulo = new Paragraph("DETALLE DE GASTOS POR TAREA", tituloFont)
            {
                Alignment = Element.ALIGN_LEFT,
                SpacingAfter = 10f
            };
            document.Add(titulo);

            // Tabla de gastos
            var tablaGastos = new PdfPTable(7)
            {
                WidthPercentage = 100,
                SpacingAfter = 20f
            };

            // Configurar anchos de columnas
            float[] anchosColumnas = { 2.5f, 1.5f, 2f, 1.5f, 1.5f, 1.5f, 1.5f };
            tablaGastos.SetWidths(anchosColumnas);

            // Encabezados
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
            var headers = new[] { "Tarea", "Fecha", "Cultivo", "Insumos", "Personal", "Transporte", "Total" };

            foreach (var header in headers)
            {
                var celda = new PdfPCell(new Phrase(header, headerFont))
                {
                    BackgroundColor = new BaseColor(47, 82, 51), // #2F5233
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8f
                };
                tablaGastos.AddCell(celda);
            }

            // Datos de gastos (ordenados igual que en la UI)
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var gastosOrdenados = _listaGastosFiltrada.OrderByDescending(g => g.TotalGasto).ToList();

            foreach (var gasto in gastosOrdenados)
            {
                // Tarea
                tablaGastos.AddCell(new PdfPCell(new Phrase(gasto.NombreTarea ?? "N/A", dataFont)) { Padding = 6f });

                // Fecha
                var fechaTexto = (gasto.FechaTarea != DateTime.MinValue)
                    ? gasto.FechaTarea.ToString("dd/MM/yyyy")
                    : "N/A";
                tablaGastos.AddCell(new PdfPCell(new Phrase(fechaTexto, dataFont))
                {
                    Padding = 6f,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });

                // Cultivo
                tablaGastos.AddCell(new PdfPCell(new Phrase(gasto.Cultivo ?? "N/A", dataFont)) { Padding = 6f });

                // Insumos
                tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(gasto.GastoInsumos), dataFont))
                {
                    Padding = 6f,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                // Personal
                tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(gasto.PagoEmpleados), dataFont))
                {
                    Padding = 6f,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                // Transporte
                tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(gasto.GastoTransporte), dataFont))
                {
                    Padding = 6f,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                // Total
                var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(0, 168, 89));
                tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(gasto.TotalGasto), totalFont))
                {
                    Padding = 6f,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
            }

            // Fila de totales
            decimal totalInsumos = _listaGastosFiltrada.Sum(g => g.GastoInsumos);
            decimal totalPersonal = _listaGastosFiltrada.Sum(g => g.PagoEmpleados);
            decimal totalTransporte = _listaGastosFiltrada.Sum(g => g.GastoTransporte);
            decimal totalGeneral = _listaGastosFiltrada.Sum(g => g.TotalGasto);

            var totalFontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);

            // Celda "TOTALES" (ocupa 3 columnas)
            var celdaTotales = new PdfPCell(new Phrase("TOTALES", totalFontBold))
            {
                BackgroundColor = new BaseColor(0, 168, 89),
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Colspan = 3,
                Padding = 8f
            };
            tablaGastos.AddCell(celdaTotales);

            // Total Insumos
            tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(totalInsumos), totalFontBold))
            {
                BackgroundColor = new BaseColor(0, 168, 89),
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Padding = 8f
            });

            // Total Personal
            tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(totalPersonal), totalFontBold))
            {
                BackgroundColor = new BaseColor(0, 168, 89),
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Padding = 8f
            });

            // Total Transporte
            tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(totalTransporte), totalFontBold))
            {
                BackgroundColor = new BaseColor(0, 168, 89),
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Padding = 8f
            });

            // Total General
            tablaGastos.AddCell(new PdfPCell(new Phrase(FormatearMonedaPDF(totalGeneral), totalFontBold))
            {
                BackgroundColor = new BaseColor(0, 168, 89),
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Padding = 8f
            });

            document.Add(tablaGastos);
        }

        private void AgregarPiePaginaPDF(Document document, PdfWriter writer)
        {
            var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8);
            var footer = new Paragraph($"Reporte generado por AGROSMART - Página {writer.PageNumber}", footerFont)
            {
                Alignment = Element.ALIGN_CENTER
            };
            document.Add(footer);
        }

        private string FormatearMonedaPDF(decimal valor)
        {
            return valor.ToString("C0", new CultureInfo("es-CO"));
        }
        private (int mes, int anio) ObtenerValoresFiltros()
        {
            int mes = 0, anio = 0;

            if (cmbMes.SelectedItem is ComboBoxItem itemMes)
                mes = Convert.ToInt32(itemMes.Tag);

            if (cmbAnio.SelectedItem is ComboBoxItem itemAnio)
                anio = Convert.ToInt32(itemAnio.Tag);

            return (mes, anio);
        }

        private List<GASTOS_DTO> FiltrarGastosPorFecha(int mes, int anio)
        {
            DateTime? fechaDesde = dpFechaDesde.SelectedDate;
            DateTime? fechaHasta = dpFechaHasta.SelectedDate;

            return _listaGastos.Where(g =>
                g.FechaTarea != DateTime.MinValue &&
                (g.Estado?.Trim().ToUpper() == "FINALIZADA") &&
                (mes == 0 || g.FechaTarea.Month == mes) &&
                (anio == 0 || g.FechaTarea.Year == anio) &&
                // Filtro por rango de fechas
                (!fechaDesde.HasValue || g.FechaTarea >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || g.FechaTarea <= fechaHasta.Value.AddDays(1)) // Incluye todo el día hasta
            ).ToList();
        }

        private void ActualizarTextoResultados(int mes, int anio)
        {
            int totalFiltradas = _listaGastosFiltrada?.Count ?? 0;
            DateTime? fechaDesde = dpFechaDesde.SelectedDate;
            DateTime? fechaHasta = dpFechaHasta.SelectedDate;

            string textoResultado = "";

            // Verificar si hay filtros de rango de fechas activos
            if (fechaDesde.HasValue || fechaHasta.HasValue)
            {
                if (fechaDesde.HasValue && fechaHasta.HasValue)
                {
                    textoResultado = $"✓ Mostrando {totalFiltradas} tareas entre {fechaDesde:dd/MM/yyyy} y {fechaHasta:dd/MM/yyyy}";
                }
                else if (fechaDesde.HasValue)
                {
                    textoResultado = $"✓ Mostrando {totalFiltradas} tareas desde {fechaDesde:dd/MM/yyyy}";
                }
                else if (fechaHasta.HasValue)
                {
                    textoResultado = $"✓ Mostrando {totalFiltradas} tareas hasta {fechaHasta:dd/MM/yyyy}";
                }
            }
            else if (mes == 0 && anio == 0)
            {
                textoResultado = $"✓ Mostrando {totalFiltradas} tareas finalizadas";
            }
            else
            {
                string textoMes = mes > 0 ? ObtenerNombreMes(mes) : "";
                string textoAnio = anio > 0 ? anio.ToString() : "";
                string periodo = "";

                if (!string.IsNullOrEmpty(textoMes) && !string.IsNullOrEmpty(textoAnio))
                    periodo = $" de {textoMes} {textoAnio}";
                else if (!string.IsNullOrEmpty(textoMes))
                    periodo = $" de {textoMes}";
                else if (!string.IsNullOrEmpty(textoAnio))
                    periodo = $" de {textoAnio}";

                textoResultado = $"✓ Mostrando {totalFiltradas} tareas finalizadas{periodo}";
            }

            txtResultadosFiltro.Text = textoResultado;
        }
        private string ObtenerNombreMes(int mes)
        {
            string[] meses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                              "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
            return mes >= 1 && mes <= 12 ? meses[mes] : "";
        }

        #endregion

        #region Carga de Datos

        /// <summary>
        /// Carga todos los datos necesarios desde la base de datos
        /// </summary>
        private async Task CargarDatosIniciales()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await Task.Run(() =>
                {
                    _listaGastos = _gastoService.ListarGastos() ?? new List<GASTOS_DTO>();
                });

                VerificarDatosCargados();
                AplicarFiltros();

                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MostrarError("Error al cargar los datos", ex);
            }
        }

        /// <summary>
        /// Carga las filas de gastos en la interfaz con los datos filtrados
        /// </summary>
        private void CargarFilasTabla()
        {
            try
            {
                spFilasGastos.Children.Clear();

                if (!_listaGastosFiltrada?.Any() ?? true)
                {
                    MostrarMensajeSinDatos();
                    return;
                }

                // Ordenar por costo total descendente
                var gastosOrdenados = _listaGastosFiltrada
                    .OrderByDescending(g => g.TotalGasto)
                    .ToList();

                foreach (var gasto in gastosOrdenados)
                {
                    var fila = CrearFilaGasto(gasto);
                    spFilasGastos.Children.Add(fila);
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error al cargar las filas de gastos", ex);
            }
        }

        #endregion

        #region Creación de UI

        private Border CrearFilaGasto(GASTOS_DTO gasto)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 14, 12, 14),
                Background = Brushes.White
            };

            border.MouseEnter += (s, e) => border.Background =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FAFB"));
            border.MouseLeave += (s, e) => border.Background = Brushes.White;

            var grid = new Grid();
            // 7 columnas: Tarea, Fecha, Cultivo, Insumos, Personal, Transporte, Total
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }); // Tarea
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Fecha
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }); // Cultivo
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // Insumos
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // Personal
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // Transporte
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Total

            // Columna 0: Tarea
            var txtTarea = CrearTextoCelda(gasto.NombreTarea ?? "N/A", 0);
            txtTarea.FontWeight = FontWeights.SemiBold;
            grid.Children.Add(txtTarea);

            // Columna 1: Fecha
            var fechaTexto = (gasto.FechaTarea != DateTime.MinValue)
                ? gasto.FechaTarea.ToString("dd/MM/yyyy")
                : "N/A";
            grid.Children.Add(CrearTextoCelda(fechaTexto, 1));

            // Columna 2: Cultivo
            grid.Children.Add(CrearTextoCelda(gasto.Cultivo ?? "N/A", 2));

            // Columna 3: Gasto Insumos
            grid.Children.Add(CrearTextoCelda(FormatearMoneda(gasto.GastoInsumos), 3));

            // Columna 4: Pago Empleados
            grid.Children.Add(CrearTextoCelda(FormatearMoneda(gasto.PagoEmpleados), 4));

            // Columna 5: Transporte
            grid.Children.Add(CrearTextoCelda(FormatearMoneda(gasto.GastoTransporte), 5));

            // Columna 6: Total
            var txtTotal = CrearTextoCelda(FormatearMoneda(gasto.TotalGasto), 6);
            txtTotal.FontWeight = FontWeights.Bold;
            txtTotal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859"));
            grid.Children.Add(txtTotal);

            border.Child = grid;
            return border;
        }

        private TextBlock CrearTextoCelda(string texto, int columna)
        {
            var textBlock = new TextBlock
            {
                Text = texto,
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(textBlock, columna);
            return textBlock;
        }

        private void MostrarMensajeSinDatos()
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 60, 0, 60)
            };

            var icono = new TextBlock
            {
                Text = "📊",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var mensaje = new TextBlock
            {
                Text = "No hay gastos registrados para este período",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var submensaje = new TextBlock
            {
                Text = "Intenta seleccionar otro mes o año",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(icono);
            stack.Children.Add(mensaje);
            stack.Children.Add(submensaje);

            spFilasGastos.Children.Add(stack);
        }

        #endregion

        #region Resumen y Tarjetas

        private void ActualizarTarjetasResumen()
        {
            if (!_listaGastosFiltrada?.Any() ?? true)
            {
                MostrarResumenVacio();
                return;
            }

            // Calcular totales de los datos filtrados
            decimal totalGastos = _listaGastosFiltrada.Sum(g => g.TotalGasto);
            decimal totalInsumos = _listaGastosFiltrada.Sum(g => g.GastoInsumos);
            decimal totalPersonal = _listaGastosFiltrada.Sum(g => g.PagoEmpleados);
            decimal totalTransporte = _listaGastosFiltrada.Sum(g => g.GastoTransporte);

            // Actualizar totales
            txtTotalGastos.Text = FormatearMoneda(totalGastos);
            txtTotalInsumos.Text = FormatearMoneda(totalInsumos);
            txtTotalPersonal.Text = FormatearMoneda(totalPersonal);
            txtTotalTransporte.Text = FormatearMoneda(totalTransporte);

            // Calcular porcentajes
            if (totalGastos > 0)
            {
                decimal porcInsumos = (totalInsumos / totalGastos) * 100;
                decimal porcPersonal = (totalPersonal / totalGastos) * 100;
                decimal porcTransporte = (totalTransporte / totalGastos) * 100;

                txtPorcentajeInsumos.Text = $"{porcInsumos:F0}% del total";
                txtPorcentajePersonal.Text = $"{porcPersonal:F0}% del total";
                txtPorcentajeTransporte.Text = $"{porcTransporte:F0}% del total";
            }
            else
            {
                txtPorcentajeInsumos.Text = "0% del total";
                txtPorcentajePersonal.Text = "0% del total";
                txtPorcentajeTransporte.Text = "0% del total";
            }

            // Información adicional
            int numGastos = _listaGastosFiltrada?.Count ?? 0;
            txtVariacionTotal.Text = $"{numGastos} tarea(s) registrada(s)";
        }

        private void MostrarResumenVacio()
        {
            txtTotalGastos.Text = "$0";
            txtTotalInsumos.Text = "$0";
            txtTotalPersonal.Text = "$0";
            txtTotalTransporte.Text = "$0";
            txtPorcentajeInsumos.Text = "0% del total";
            txtPorcentajePersonal.Text = "0% del total";
            txtPorcentajeTransporte.Text = "0% del total";
            txtVariacionTotal.Text = "Sin datos";
        }

        #endregion

        #region Utilidades

        private string FormatearMoneda(decimal valor)
        {
            return valor.ToString("C0", new CultureInfo("es-CO"));
        }

        private void MostrarError(string mensaje, Exception ex)
        {
            MessageBox.Show(
                $"{mensaje}\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void VerificarDatosCargados()
        {
            System.Diagnostics.Debug.WriteLine("=== VERIFICACIÓN DE DATOS CARGADOS ===");
            System.Diagnostics.Debug.WriteLine($"Total Gastos Cargados: {_listaGastos?.Count ?? 0}");

            if (_listaGastos?.Any() ?? false)
            {
                System.Diagnostics.Debug.WriteLine("\n--- ANÁLISIS DE ESTADOS ---");
                var agrupadoPorEstado = _listaGastos.GroupBy(g => g.Estado ?? "NULL");
                foreach (var grupo in agrupadoPorEstado)
                {
                    System.Diagnostics.Debug.WriteLine($"Estado '{grupo.Key}': {grupo.Count()} tareas");
                }

                System.Diagnostics.Debug.WriteLine("\n--- ANÁLISIS DE FECHAS ---");
                var conFecha = _listaGastos.Count(g => g.FechaTarea != DateTime.MinValue);
                var sinFecha = _listaGastos.Count(g => g.FechaTarea == DateTime.MinValue);
                System.Diagnostics.Debug.WriteLine($"Con Fecha: {conFecha}");
                System.Diagnostics.Debug.WriteLine($"Sin Fecha: {sinFecha}");

                if (conFecha > 0)
                {
                    var fechaMin = _listaGastos.Where(g => g.FechaTarea != DateTime.MinValue).Min(g => g.FechaTarea);
                    var fechaMax = _listaGastos.Where(g => g.FechaTarea != DateTime.MinValue).Max(g => g.FechaTarea);
                    System.Diagnostics.Debug.WriteLine($"Rango de fechas: {fechaMin:dd/MM/yyyy} - {fechaMax:dd/MM/yyyy}");
                }

                System.Diagnostics.Debug.WriteLine("\n--- EJEMPLO DE PRIMER GASTO ---");
                var primerGasto = _listaGastos.First();
                System.Diagnostics.Debug.WriteLine($"Tarea: {primerGasto.NombreTarea}");
                System.Diagnostics.Debug.WriteLine($"Estado: '{primerGasto.Estado}'");
                System.Diagnostics.Debug.WriteLine($"Fecha: {primerGasto.FechaTarea}");
                System.Diagnostics.Debug.WriteLine($"Cultivo: {primerGasto.Cultivo}");
                System.Diagnostics.Debug.WriteLine($"Insumos: {primerGasto.GastoInsumos:C}");
                System.Diagnostics.Debug.WriteLine($"Personal: {primerGasto.PagoEmpleados:C}");
                System.Diagnostics.Debug.WriteLine($"Transporte: {primerGasto.GastoTransporte:C}");
                System.Diagnostics.Debug.WriteLine($"Total: {primerGasto.TotalGasto:C}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ NO SE CARGARON GASTOS - Verificar servicio ListarGastos()");
            }

            System.Diagnostics.Debug.WriteLine($"\n--- DESPUÉS DE FILTRAR ---");
            System.Diagnostics.Debug.WriteLine($"Total Gastos Filtrados: {_listaGastosFiltrada?.Count ?? 0}");
        }

        #endregion
    }
}