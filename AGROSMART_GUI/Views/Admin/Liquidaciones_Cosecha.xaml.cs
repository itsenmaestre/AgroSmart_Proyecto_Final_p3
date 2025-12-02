using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES_DTOS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AGROSMART_GUI.Views.Admin
{
    public partial class Liquidaciones_Cosecha : Page
    {
        private readonly LiquidacionService _liquidacionService;
        private List<Liquidación_DTO> _todasLasLiquidaciones = new List<Liquidación_DTO>();
        private List<Liquidación_DTO> _liquidacionesFiltradas = new List<Liquidación_DTO>();

        public Liquidaciones_Cosecha()
        {
            InitializeComponent();
            _liquidacionService = new LiquidacionService();
            Loaded += Liquidaciones_Cosecha_Loaded;
        }

        private void Liquidaciones_Cosecha_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDatosPantalla();
        }

        private void CargarDatosPantalla()
        {
            try
            {
                var coleccion = _liquidacionService.ConsultarTodas();
                _todasLasLiquidaciones = coleccion?.Where(l =>
                    string.Equals(l.EstadoCosecha, "TERMINADA", StringComparison.OrdinalIgnoreCase))
                    .ToList() ?? new List<Liquidación_DTO>();

                _liquidacionesFiltradas = _todasLasLiquidaciones;

                CargarComboCosechas();
                ActualizarVista(_liquidacionesFiltradas);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar las liquidaciones de cosecha:\n" + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CargarComboCosechas()
        {
            var cosechas = _todasLasLiquidaciones
                .GroupBy(l => new { l.IdCosecha, l.NombreCultivo })
                .Select(g => new
                {
                    Display = $"#{g.Key.IdCosecha} - {g.Key.NombreCultivo}",
                    Value = g.Key.IdCosecha
                })
                .OrderBy(x => x.Value)
                .ToList();

            cosechas.Insert(0, new { Display = "Todas las cosechas", Value = 0 });

            cmbCosecha.ItemsSource = cosechas;
            cmbCosecha.SelectedIndex = 0;
        }

        private void ActualizarVista(List<Liquidación_DTO> lista)
        {
            ActualizarResumen(lista);
            ConstruirCosechasTerminadas(lista);
        }

        #region Filtros

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? desde = dpFechaDesde.SelectedDate;
                DateTime? hasta = dpFechaHasta.SelectedDate;
                int idCosechaSeleccionada = cmbCosecha.SelectedValue != null ?
                    (int)cmbCosecha.SelectedValue : 0;

                if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
                {
                    MessageBox.Show(
                        "La fecha 'Desde' no puede ser mayor que la fecha 'Hasta'.",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _liquidacionesFiltradas = _todasLasLiquidaciones.Where(l =>
                {
                    bool cumpleFechaDesde = !desde.HasValue || l.FechaRegistro.Date >= desde.Value.Date;
                    bool cumpleFechaHasta = !hasta.HasValue || l.FechaRegistro.Date <= hasta.Value.Date;
                    bool cumpleCosecha = idCosechaSeleccionada == 0 || l.IdCosecha == idCosechaSeleccionada;
                    return cumpleFechaDesde && cumpleFechaHasta && cumpleCosecha;
                }).ToList();

                ActualizarVista(_liquidacionesFiltradas);

                string mensaje = $"Filtro aplicado: {_liquidacionesFiltradas.Count} liquidaciones encontradas";
                if (desde.HasValue || hasta.HasValue || idCosechaSeleccionada > 0)
                {
                    mensaje += "\n";
                    if (desde.HasValue) mensaje += $"Desde: {desde.Value:dd/MM/yyyy}";
                    if (desde.HasValue && hasta.HasValue) mensaje += " | ";
                    if (hasta.HasValue) mensaje += $"Hasta: {hasta.Value:dd/MM/yyyy}";
                    if (idCosechaSeleccionada > 0)
                        mensaje += $"\nCosecha: #{idCosechaSeleccionada}";
                }

                MessageBox.Show(mensaje, "Filtro Aplicado",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al filtrar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLimpiarFiltro_Click(object sender, RoutedEventArgs e)
        {
            dpFechaDesde.SelectedDate = null;
            dpFechaHasta.SelectedDate = DateTime.Today;
            cmbCosecha.SelectedIndex = 0;
            _liquidacionesFiltradas = _todasLasLiquidaciones;
            ActualizarVista(_liquidacionesFiltradas);
        }

        #endregion

        #region Resumen superior

        private void ActualizarResumen(List<Liquidación_DTO> lista)
        {
            int total = lista.Count;
            decimal totalPagado = lista.Sum(l => l.PagoNeto);
            var gruposPorCosecha = lista.GroupBy(l => l.IdCosecha).ToList();

            decimal promedioPorCosecha = 0;
            if (gruposPorCosecha.Any())
            {
                promedioPorCosecha = gruposPorCosecha.Average(g => g.Sum(x => x.PagoNeto));
            }

            int diasTrabajados = lista.Select(l => l.FechaTrabajo.Date).Distinct().Count();

            txtTotalLiquidaciones.Text = total.ToString();
            txtTotalPagado.Text = "$" + totalPagado.ToString("N0");
            txtPromedio.Text = "$" + promedioPorCosecha.ToString("N0");
            txtDiasTrabajados.Text = diasTrabajados.ToString();
        }

        #endregion

        #region Cosechas Terminadas

        private void ConstruirCosechasTerminadas(List<Liquidación_DTO> lista)
        {
            spCosechasTerminadas.Children.Clear();

            var gruposPorCosecha = lista
                .GroupBy(l => l.IdCosecha)
                .OrderByDescending(g => g.First().FechaRegistro)
                .ToList();

            txtContadorCosechas.Text = $"({gruposPorCosecha.Count} cosechas)";

            if (!gruposPorCosecha.Any())
            {
                var emptyCard = CreateEmptyStateCard("No hay cosechas terminadas con los filtros aplicados.");
                spCosechasTerminadas.Children.Add(emptyCard);
                return;
            }

            foreach (var grupo in gruposPorCosecha)
            {
                var card = CreateCosechaTerminadaCard(grupo);
                spCosechasTerminadas.Children.Add(card);
            }
        }

        private Border CreateEmptyStateCard(string message)
        {
            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24),
                Margin = new Thickness(0, 0, 0, 16),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 20,
                    ShadowDepth = 2,
                    Opacity = 0.08,
                    Direction = 270
                },
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "📭",
                            FontSize = 32,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        new TextBlock
                        {
                            Text = message,
                            Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };
        }

        private Border CreateCosechaTerminadaCard(IGrouping<int, Liquidación_DTO> grupo)
        {
            var primera = grupo.First();
            int idCosecha = grupo.Key;
            string cultivo = primera.NombreCultivo;
            DateTime fechaRegistro = grupo.Min(l => l.FechaRegistro);
            decimal totalBruto = grupo.Sum(l => l.PagoBruto);
            decimal totalNeto = grupo.Sum(l => l.PagoNeto);
            int cantEmpleados = grupo.Select(l => l.IdEmpleado).Distinct().Count();

            // Card principal
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 12),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 20,
                    ShadowDepth = 2,
                    Opacity = 0.08,
                    Direction = 270
                }
            };

            // Header del Expander - DISEÑO CENTRADO Y ORGANIZADO EN TABLA
            var headerGrid = new Grid
            {
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 60
            };

            // Columnas con anchos fijos para alineación perfecta
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });  // ID
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) }); // Cultivo
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Empleados
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Bruto
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Neto
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) }); // Estado

            // ID Badge (Columna 0) - CENTRADO
            var idBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 6, 12, 6),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = new TextBlock
                {
                    Text = $"#{idCosecha}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(idBadge, 0);
            headerGrid.Children.Add(idBadge);

            // Cultivo + Fecha (Columna 1) - ALINEADO A LA IZQUIERDA
            var cultivoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(12, 0, 0, 0)
            };
            cultivoPanel.Children.Add(new TextBlock
            {
                Text = cultivo,
                FontWeight = FontWeights.SemiBold,
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
            });
            cultivoPanel.Children.Add(new TextBlock
            {
                Text = $"📅 {fechaRegistro:dd/MM/yyyy}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetColumn(cultivoPanel, 1);
            headerGrid.Children.Add(cultivoPanel);

            // Empleados (Columna 2) - CENTRADO
            var empleadosPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Orientation = Orientation.Horizontal
            };
            empleadosPanel.Children.Add(new TextBlock
            {
                Text = "👥",
                FontSize = 16,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            empleadosPanel.Children.Add(new TextBlock
            {
                Text = $"{cantEmpleados}",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                VerticalAlignment = VerticalAlignment.Center
            });
            empleadosPanel.Children.Add(new TextBlock
            {
                Text = " emp.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3, 0, 0, 0)
            });
            Grid.SetColumn(empleadosPanel, 2);
            headerGrid.Children.Add(empleadosPanel);

            // Bruto (Columna 3) - CENTRADO
            var brutoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            brutoPanel.Children.Add(new TextBlock
            {
                Text = "Bruto",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Medium
            });
            brutoPanel.Children.Add(new TextBlock
            {
                Text = "$" + totalBruto.ToString("N0"),
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetColumn(brutoPanel, 3);
            headerGrid.Children.Add(brutoPanel);

            // Neto (Columna 4) - CENTRADO
            var netoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            netoPanel.Children.Add(new TextBlock
            {
                Text = "Neto",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Medium
            });
            netoPanel.Children.Add(new TextBlock
            {
                Text = "$" + totalNeto.ToString("N0"),
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetColumn(netoPanel, 4);
            headerGrid.Children.Add(netoPanel);

            // Badge terminada (Columna 5) - CENTRADO
            var estadoBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 7, 14, 7),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "✓ TERMINADA",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(estadoBadge, 5);
            headerGrid.Children.Add(estadoBadge);

            // DataGrid interno
            var dgDetalle = CreateDetalleDataGrid();
            dgDetalle.ItemsSource = grupo.OrderBy(x => x.FechaTrabajo).ThenBy(x => x.Nombre).ToList();

            var detalleContainer = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 12, 0, 0),
                Child = dgDetalle
            };

            // Expander
            var expander = new Expander
            {
                Header = headerGrid,
                Content = detalleContainer,
                IsExpanded = false,
                Padding = new Thickness(20, 12, 20, 12),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            card.Child = expander;
            return card;
        }

        private DataGrid CreateDetalleDataGrid()
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                RowBackground = Brushes.White,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                RowHeaderWidth = 0,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };

            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Empleado",
                Binding = new Binding("Nombre"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Fecha",
                Binding = new Binding("FechaTrabajo") { StringFormat = "dd/MM/yyyy" },
                Width = 100
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Cant.",
                Binding = new Binding("Cantidad") { StringFormat = "N2" },
                Width = 80
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Unidad",
                Binding = new Binding("UnidadMedida"),
                Width = 80
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "V. Unit.",
                Binding = new Binding("ValorUnidad") { StringFormat = "N0" },
                Width = 90
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Bruto",
                Binding = new Binding("PagoBruto") { StringFormat = "$#,##0" },
                Width = 100
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Deduc.",
                Binding = new Binding("Deducciones") { StringFormat = "$#,##0" },
                Width = 100
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Neto",
                Binding = new Binding("PagoNeto") { StringFormat = "$#,##0" },
                Width = 100
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "Observaciones",
                Binding = new Binding("Observaciones"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            return dg;
        }

        #endregion

        #region Exportar PDF

        private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_liquidacionesFiltradas.Any())
                {
                    MessageBox.Show(
                        "No hay datos para exportar.",
                        "Exportar PDF",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Liquidaciones_Cosecha_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        GenerarPDFCompleto(_liquidacionesFiltradas, saveFileDialog.FileName);
                        Mouse.OverrideCursor = null;

                        var resultado = MessageBox.Show(
                            $"✓ PDF generado exitosamente\n\n" +
                            $"📊 {_liquidacionesFiltradas.Count} liquidaciones procesadas\n" +
                            $"🌾 {_liquidacionesFiltradas.GroupBy(l => l.IdCosecha).Count()} cosechas incluidas\n" +
                            $"📁 {System.IO.Path.GetFileName(saveFileDialog.FileName)}\n\n" +
                            $"¿Desea abrir el archivo ahora?",
                            "PDF Generado",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (resultado == MessageBoxResult.Yes)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = saveFileDialog.FileName,
                                    UseShellExecute = true
                                });
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(
                                    $"No se pudo abrir el PDF:\n{ex.Message}\n\n" +
                                    $"Puede abrirlo manualmente desde:\n{saveFileDialog.FileName}",
                                    "Aviso",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Mouse.OverrideCursor = null;
                        MessageBox.Show(
                            $"Error al generar PDF:\n{ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show(
                    $"Error al exportar PDF: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GenerarPDFCompleto(List<Liquidación_DTO> liquidaciones, string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 20, 20, 30, 30);
                var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);

                document.Open();
                AgregarTituloPDF(document, "REPORTE DE LIQUIDACIONES DE COSECHA");
                AgregarResumenPDF(document, liquidaciones);
                AgregarDetalleCosechasPDF(document, liquidaciones);
                AgregarGraficaFinalPDF(document, liquidaciones);
                document.Close();
            }
        }

        private iTextSharp.text.BaseColor VERDE_OSCURO = new iTextSharp.text.BaseColor(46, 125, 50);
        private iTextSharp.text.BaseColor VERDE_MEDIO = new iTextSharp.text.BaseColor(76, 175, 80);
        private iTextSharp.text.BaseColor VERDE_CLARO = new iTextSharp.text.BaseColor(232, 245, 233);

        private void AgregarTituloPDF(iTextSharp.text.Document document, string titulo)
        {
            var titleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 16, VERDE_OSCURO);
            var title = new iTextSharp.text.Paragraph(titulo, titleFont)
            {
                Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                SpacingAfter = 15f
            };
            document.Add(title);

            var dateFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 10, iTextSharp.text.BaseColor.GRAY);
            var date = new iTextSharp.text.Paragraph($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}", dateFont)
            {
                Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                SpacingAfter = 20f
            };
            document.Add(date);
        }

        private void AgregarResumenPDF(iTextSharp.text.Document document, List<Liquidación_DTO> liquidaciones)
        {
            int total = liquidaciones.Count;
            decimal totalPagado = liquidaciones.Sum(l => l.PagoNeto);
            var gruposPorCosecha = liquidaciones.GroupBy(l => l.IdCosecha).ToList();

            decimal promedioPorCosecha = 0;
            if (gruposPorCosecha.Any())
            {
                promedioPorCosecha = gruposPorCosecha.Average(g => g.Sum(x => x.PagoNeto));
            }

            int diasTrabajados = liquidaciones.Select(l => l.FechaTrabajo.Date).Distinct().Count();

            var table = new iTextSharp.text.pdf.PdfPTable(4)
            {
                WidthPercentage = 100,
                SpacingBefore = 10f,
                SpacingAfter = 20f
            };

            AgregarCeldaResumenPDF(table, "TOTAL LIQUIDACIONES", total.ToString(), VERDE_CLARO);
            AgregarCeldaResumenPDF(table, "TOTAL PAGADO", $"${totalPagado:N0}", VERDE_CLARO);
            AgregarCeldaResumenPDF(table, "PROMEDIO POR COSECHA", $"${promedioPorCosecha:N0}", VERDE_CLARO);
            AgregarCeldaResumenPDF(table, "DÍAS TRABAJADOS", diasTrabajados.ToString(), VERDE_CLARO);

            document.Add(table);
        }

        private void AgregarCeldaResumenPDF(iTextSharp.text.pdf.PdfPTable table, string titulo, string valor, iTextSharp.text.BaseColor color)
        {
            var titleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 9, VERDE_OSCURO);
            var valueFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 11, VERDE_OSCURO);

            var cell = new iTextSharp.text.pdf.PdfPCell
            {
                BackgroundColor = color,
                Padding = 8f,
                BorderWidth = 1f,
                BorderColor = VERDE_MEDIO,
                HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER
            };

            var content = new iTextSharp.text.Paragraph
            {
                new iTextSharp.text.Phrase(titulo + "\n", titleFont),
                new iTextSharp.text.Phrase(valor, valueFont)
            };
            content.Alignment = iTextSharp.text.Element.ALIGN_CENTER;

            cell.AddElement(content);
            table.AddCell(cell);
        }

        private void AgregarDetalleCosechasPDF(iTextSharp.text.Document document, List<Liquidación_DTO> liquidaciones)
        {
            var gruposPorCosecha = liquidaciones
                .GroupBy(l => l.IdCosecha)
                .OrderByDescending(g => g.First().FechaRegistro)
                .ToList();

            if (!gruposPorCosecha.Any())
                return;

            document.Add(new iTextSharp.text.Paragraph("DETALLE POR COSECHA",
                iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 12, VERDE_OSCURO))
            {
                SpacingBefore = 10f,
                SpacingAfter = 10f
            });

            foreach (var grupo in gruposPorCosecha)
            {
                var primera = grupo.First();
                int idCosecha = grupo.Key;
                string cultivo = primera.NombreCultivo;
                DateTime fechaRegistro = grupo.Min(l => l.FechaRegistro);
                decimal totalBruto = grupo.Sum(l => l.PagoBruto);
                decimal totalNeto = grupo.Sum(l => l.PagoNeto);
                int cantEmpleados = grupo.Select(l => l.IdEmpleado).Distinct().Count();

                var cosechaTable = new iTextSharp.text.pdf.PdfPTable(6)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 8f,
                    SpacingAfter = 8f
                };

                AgregarCeldaCosechaHeaderPDF(cosechaTable, $"#{idCosecha}", VERDE_CLARO, VERDE_OSCURO);
                AgregarCeldaCosechaHeaderPDF(cosechaTable, cultivo + $"\n{fechaRegistro:dd/MM/yyyy}", iTextSharp.text.BaseColor.WHITE, VERDE_OSCURO);
                AgregarCeldaCosechaHeaderPDF(cosechaTable, $"{cantEmpleados}\nempleados", iTextSharp.text.BaseColor.WHITE, VERDE_OSCURO);
                AgregarCeldaCosechaHeaderPDF(cosechaTable, $"Bruto\n${totalBruto:N0}", iTextSharp.text.BaseColor.WHITE, VERDE_OSCURO);
                AgregarCeldaCosechaHeaderPDF(cosechaTable, $"Neto\n${totalNeto:N0}", iTextSharp.text.BaseColor.WHITE, VERDE_OSCURO);
                AgregarCeldaCosechaHeaderPDF(cosechaTable, "✓ TERMINADA", VERDE_CLARO, VERDE_OSCURO);

                document.Add(cosechaTable);
                AgregarDetalleLiquidacionesPDF(document, grupo.ToList());
            }
        }

        private void AgregarCeldaCosechaHeaderPDF(iTextSharp.text.pdf.PdfPTable table, string texto, iTextSharp.text.BaseColor backgroundColor, iTextSharp.text.BaseColor textColor)
        {
            var font = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 9, textColor);

            var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(texto, font))
            {
                BackgroundColor = backgroundColor,
                Padding = 8f,
                BorderWidth = 1f,
                BorderColor = VERDE_MEDIO,
                HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER
            };
            table.AddCell(cell);
        }

        private void AgregarDetalleLiquidacionesPDF(iTextSharp.text.Document document, List<Liquidación_DTO> liquidacionesCosecha)
        {
            var table = new iTextSharp.text.pdf.PdfPTable(9)
            {
                WidthPercentage = 100,
                SpacingBefore = 5f,
                SpacingAfter = 15f
            };

            string[] headers = { "Empleado", "Fecha", "Cant.", "Unidad", "V. Unit.", "Bruto", "Deduc.", "Neto", "Observaciones" };
            foreach (string header in headers)
            {
                var headerCell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(header,
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 7, iTextSharp.text.BaseColor.WHITE)))
                {
                    BackgroundColor = VERDE_OSCURO,
                    Padding = 4f,
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER
                };
                table.AddCell(headerCell);
            }

            foreach (var liquidacion in liquidacionesCosecha.OrderBy(l => l.FechaTrabajo).ThenBy(l => l.Nombre))
            {
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(liquidacion.Nombre ?? "",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(liquidacion.FechaTrabajo.ToString("dd/MM/yyyy"),
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(liquidacion.Cantidad.ToString("N2"),
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(liquidacion.UnidadMedida ?? "",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"${liquidacion.ValorUnidad:N0}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"${liquidacion.PagoBruto:N0}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"${liquidacion.Deducciones:N0}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"${liquidacion.PagoNeto:N0}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
                table.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(liquidacion.Observaciones ?? "",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 7))));
            }

            document.Add(table);
        }

        private class CosechaData
        {
            public int IdCosecha { get; set; }
            public string Cultivo { get; set; }
            public decimal Bruto { get; set; }
            public decimal Neto { get; set; }
            public decimal Diferencia { get; set; }
        }

        private void AgregarGraficaFinalPDF(iTextSharp.text.Document document, List<Liquidación_DTO> liquidaciones)
        {
            try
            {
                document.Add(new iTextSharp.text.Paragraph("ANÁLISIS COMPARATIVO - PAGO BRUTO VS NETO POR COSECHA",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 12, iTextSharp.text.BaseColor.DARK_GRAY))
                {
                    SpacingBefore = 20f,
                    SpacingAfter = 10f
                });

                var datos = liquidaciones
                    .GroupBy(l => l.IdCosecha)
                    .Select(g => new CosechaData
                    {
                        IdCosecha = g.Key,
                        Cultivo = g.First().NombreCultivo,
                        Bruto = g.Sum(x => x.PagoBruto),
                        Neto = g.Sum(x => x.PagoNeto),
                        Diferencia = g.Sum(x => x.PagoBruto) - g.Sum(x => x.PagoNeto)
                    })
                    .OrderByDescending(x => x.Bruto)
                    .Take(6)
                    .ToList();

                if (!datos.Any())
                    return;

                var chartImage = CrearGraficaVisual(datos);
                if (chartImage != null)
                {
                    chartImage.ScaleToFit(500f, 300f);
                    chartImage.Alignment = iTextSharp.text.Image.ALIGN_CENTER;
                    document.Add(chartImage);
                }

                AgregarTablaDatosGrafica(document, datos);

                var leyenda = new iTextSharp.text.Paragraph(
                    "Esta gráfica muestra la comparación entre el pago bruto y neto por cosecha. " +
                    "La diferencia representa el total de deducciones aplicadas.",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 9, iTextSharp.text.BaseColor.GRAY))
                {
                    Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                    SpacingBefore = 10f,
                    SpacingAfter = 20f
                };
                document.Add(leyenda);
            }
            catch (Exception ex)
            {
                document.Add(new iTextSharp.text.Paragraph("No se pudo generar la gráfica: " + ex.Message,
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 9, iTextSharp.text.BaseColor.RED)));
            }
        }

        private iTextSharp.text.Image CrearGraficaVisual(List<CosechaData> datos)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var document = new iTextSharp.text.Document(new iTextSharp.text.Rectangle(600, 400));
                    var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms);
                    document.Open();

                    var contentByte = writer.DirectContent;

                    float margin = 50f;
                    float chartWidth = 500f;
                    float chartHeight = 250f;
                    float xStart = margin;
                    float yStart = 100f;

                    contentByte.SetColorFill(iTextSharp.text.BaseColor.WHITE);
                    contentByte.Rectangle(xStart, yStart, chartWidth, chartHeight);
                    contentByte.Fill();

                    contentByte.SetColorStroke(iTextSharp.text.BaseColor.LIGHT_GRAY);
                    contentByte.Rectangle(xStart, yStart, chartWidth, chartHeight);
                    contentByte.Stroke();

                    decimal maxValor = Math.Max(datos.Max(x => x.Bruto), datos.Max(x => x.Neto));
                    if (maxValor == 0) maxValor = 1;

                    var colorBruto = new iTextSharp.text.BaseColor(65, 105, 225);
                    var colorNeto = new iTextSharp.text.BaseColor(34, 139, 34);
                    var colorEjes = new iTextSharp.text.BaseColor(100, 100, 100);

                    contentByte.SetColorStroke(colorEjes);
                    contentByte.SetLineWidth(1f);

                    contentByte.MoveTo(xStart, yStart);
                    contentByte.LineTo(xStart, yStart + chartHeight);
                    contentByte.Stroke();

                    contentByte.MoveTo(xStart, yStart);
                    contentByte.LineTo(xStart + chartWidth, yStart);
                    contentByte.Stroke();

                    float barGroupWidth = chartWidth / datos.Count;
                    float barWidth = barGroupWidth * 0.3f;
                    float espacioEntreBarras = barWidth * 0.2f;

                    for (int i = 0; i < datos.Count; i++)
                    {
                        float xBase = xStart + (i * barGroupWidth) + (barGroupWidth - (2 * barWidth + espacioEntreBarras)) / 2;

                        float alturaBruto = (float)((double)datos[i].Bruto / (double)maxValor * (double)chartHeight);
                        float xBruto = xBase;
                        float yBruto = yStart;
                        contentByte.SetColorFill(colorBruto);
                        contentByte.Rectangle(xBruto, yBruto, barWidth, alturaBruto);
                        contentByte.Fill();

                        float alturaNeto = (float)((double)datos[i].Neto / (double)maxValor * (double)chartHeight);
                        float xNeto = xBase + barWidth + espacioEntreBarras;
                        float yNeto = yStart;
                        contentByte.SetColorFill(colorNeto);
                        contentByte.Rectangle(xNeto, yNeto, barWidth, alturaNeto);
                        contentByte.Fill();

                        contentByte.SetColorFill(iTextSharp.text.BaseColor.BLACK);
                        contentByte.BeginText();
                        contentByte.SetFontAndSize(iTextSharp.text.pdf.BaseFont.CreateFont(), 7);

                        string textoBruto = $"${datos[i].Bruto:N0}";
                        contentByte.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER,
                            textoBruto, xBruto + barWidth / 2, yBruto + alturaBruto + 8, 0);

                        string textoNeto = $"${datos[i].Neto:N0}";
                        contentByte.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER,
                            textoNeto, xNeto + barWidth / 2, yNeto + alturaNeto + 8, 0);

                        string etiqueta = $"C#{datos[i].IdCosecha}";
                        contentByte.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER,
                            etiqueta, xBase + barGroupWidth / 2, yStart - 20, 0);

                        contentByte.EndText();
                    }

                    float leyendaY = yStart + chartHeight + 30;
                    float leyendaX = xStart + 50;

                    contentByte.SetColorFill(colorBruto);
                    contentByte.Rectangle(leyendaX, leyendaY, 10, 10);
                    contentByte.Fill();
                    contentByte.BeginText();
                    contentByte.SetColorFill(iTextSharp.text.BaseColor.BLACK);
                    contentByte.SetFontAndSize(iTextSharp.text.pdf.BaseFont.CreateFont(), 8);
                    contentByte.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT,
                        "Pago Bruto", leyendaX + 15, leyendaY + 2, 0);
                    contentByte.EndText();

                    contentByte.SetColorFill(colorNeto);
                    contentByte.Rectangle(leyendaX + 100, leyendaY, 10, 10);
                    contentByte.Fill();
                    contentByte.BeginText();
                    contentByte.SetColorFill(iTextSharp.text.BaseColor.BLACK);
                    contentByte.SetFontAndSize(iTextSharp.text.pdf.BaseFont.CreateFont(), 8);
                    contentByte.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_LEFT,
                        "Pago Neto", leyendaX + 115, leyendaY + 2, 0);
                    contentByte.EndText();

                    contentByte.BeginText();
                    contentByte.SetFontAndSize(iTextSharp.text.pdf.BaseFont.CreateFont(), 7);
                    contentByte.ShowTextAligned(iTextSharp.text.pdf.PdfContentByte.ALIGN_CENTER,
                        "COMPARACIÓN PAGO BRUTO VS NETO POR COSECHA", xStart + chartWidth / 2, yStart + chartHeight + 50, 0);
                    contentByte.EndText();

                    document.Close();
                    return iTextSharp.text.Image.GetInstance(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al crear gráfica visual: " + ex.Message);
                return null;
            }
        }

        private void AgregarTablaDatosGrafica(iTextSharp.text.Document document, List<CosechaData> datos)
        {
            var tablaDatos = new iTextSharp.text.pdf.PdfPTable(5)
            {
                WidthPercentage = 100,
                SpacingBefore = 15f,
                SpacingAfter = 10f
            };

            string[] headers = { "Cosecha", "Cultivo", "Bruto", "Neto", "Deducciones" };
            foreach (string header in headers)
            {
                tablaDatos.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(header,
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 8, iTextSharp.text.BaseColor.WHITE)))
                {
                    BackgroundColor = new iTextSharp.text.BaseColor(79, 129, 189),
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER,
                    Padding = 5f
                });
            }

            foreach (var dato in datos)
            {
                tablaDatos.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"#{dato.IdCosecha}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 8)))
                {
                    Padding = 4f,
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER
                });

                tablaDatos.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(dato.Cultivo ?? "",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 8)))
                {
                    Padding = 4f
                });

                tablaDatos.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"${dato.Bruto:N0}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 8)))
                {
                    Padding = 4f,
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT
                });

                tablaDatos.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"${dato.Neto:N0}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 8)))
                {
                    Padding = 4f,
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT
                });

                tablaDatos.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"${dato.Diferencia:N0}",
                    iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 8)))
                {
                    Padding = 4f,
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT
                });
            }

            document.Add(tablaDatos);
        }

        #endregion
    }
}