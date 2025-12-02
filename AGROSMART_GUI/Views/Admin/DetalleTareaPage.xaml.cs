using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AGROSMART_GUI.Views.Admin
{
    public partial class DetalleTareaPage : Page
    {
        private readonly DetalleTareaService _detalleService;
        private readonly InsumoService _insumoService;
        private readonly TareaService _tareaService;
        private readonly int _idAdmin;
        private List<DETALLE_TAREA> _listaDetalles;

        public DetalleTareaPage(int idAdmin)
        {
            InitializeComponent();
            _idAdmin = idAdmin;
            _detalleService = new DetalleTareaService();
            _insumoService = new InsumoService();
            _tareaService = new TareaService();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CargarCombos();
            CargarDetallesTarea();
        }

        private void CargarCombos()
        {
            try
            {
                // Cargar tareas disponibles
                var tareas = _tareaService.Consultar();
                cboTarea.ItemsSource = tareas;
                cboTarea.DisplayMemberPath = "TIPO_ACTIVIDAD";
                cboTarea.SelectedValuePath = "ID_TAREA";

                // Cargar solo insumos CONSUMIBLES con stock disponible
                var insumos = _insumoService.Consultar()
                    .Where(i => i.TIPO == "CONSUMIBLE" && i.STOCK_ACTUAL > 0)
                    .ToList();

                cboInsumo.ItemsSource = insumos;
                cboInsumo.DisplayMemberPath = "NOMBRE";
                cboInsumo.SelectedValuePath = "ID_INSUMO";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar combos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarDetallesTarea()
        {
            try
            {
                _listaDetalles = _detalleService.Consultar().ToList();

                if (_listaDetalles == null || !_listaDetalles.Any())
                {
                    spFilasDetalle.Children.Clear();
                    var mensajeVacio = new TextBlock
                    {
                        Text = "No hay detalles de tarea registrados",
                        FontSize = 14,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 30, 0, 30)
                    };
                    spFilasDetalle.Children.Add(mensajeVacio);
                    return;
                }

                ActualizarTabla();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalles: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarTabla()
        {
            spFilasDetalle.Children.Clear();

            foreach (var detalle in _listaDetalles)
            {
                spFilasDetalle.Children.Add(CrearFilaDetalle(detalle));
            }
        }

        private Border CrearFilaDetalle(DETALLE_TAREA detalle)
        {
            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 15, 20, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Obtener información de tarea e insumo
            var tarea = _tareaService.ObtenerPorId(detalle.ID_TAREA);
            var insumo = _insumoService.ObtenerPorId(detalle.ID_INSUMO);

            // Columna 0: Tarea
            var txtTarea = CrearTextoCelda(tarea?.TIPO_ACTIVIDAD ?? "N/A", 0);
            txtTarea.FontWeight = FontWeights.SemiBold;
            grid.Children.Add(txtTarea);

            // Columna 1: Insumo
            grid.Children.Add(CrearTextoCelda(insumo?.NOMBRE ?? "N/A", 1));

            // Columna 2: Cantidad Usada
            grid.Children.Add(CrearTextoCelda($"{detalle.CANTIDAD_USADA:N2} {insumo?.UNIDAD_MEDIDA ?? ""}", 2));

            // Columna 3: Costo Unitario
            grid.Children.Add(CrearTextoCelda($"${insumo?.COSTO_UNITARIO:N0}", 3));

            // Columna 4: Costo Total
            decimal costoTotal = detalle.CANTIDAD_USADA * (insumo?.COSTO_UNITARIO ?? 0);
            var txtTotal = CrearTextoCelda($"${costoTotal:N0}", 4);
            txtTotal.FontWeight = FontWeights.Bold;
            txtTotal.Foreground = Brushes.Green;
            grid.Children.Add(txtTotal);

            // Columna 5: Acciones
            var stackAcciones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(stackAcciones, 5);

            var btnEliminar = new Button
            {
                Content = "🗑️",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 14,
                Padding = new Thickness(5),
                ToolTip = "Eliminar detalle",
                Tag = detalle
            };
            btnEliminar.Click += BtnEliminarDetalle_Click;

            stackAcciones.Children.Add(btnEliminar);
            grid.Children.Add(stackAcciones);

            border.Child = grid;
            return border;
        }

        private TextBlock CrearTextoCelda(string texto, int columna)
        {
            var textBlock = new TextBlock
            {
                Text = texto,
                FontSize = 13,
                Foreground = Brushes.DarkSlateGray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(10, 8, 10, 8)
            };
            Grid.SetColumn(textBlock, columna);
            return textBlock;
        }

        private void BtnAgregarDetalle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones básicas
                if (cboTarea.SelectedValue == null)
                {
                    MessageBox.Show("Debe seleccionar una tarea.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cboInsumo.SelectedValue == null)
                {
                    MessageBox.Show("Debe seleccionar un insumo.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtCantidad.Text, out decimal cantidad) || cantidad <= 0)
                {
                    MessageBox.Show("La cantidad debe ser un número mayor a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int idTarea = (int)cboTarea.SelectedValue;
                int idInsumo = (int)cboInsumo.SelectedValue;

                // Verificar stock ANTES de guardar
                var insumo = _insumoService.ObtenerPorId(idInsumo);
                if (insumo == null)
                {
                    MessageBox.Show("Error: No se encontró el insumo seleccionado.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (insumo.STOCK_ACTUAL < cantidad)
                {
                    MessageBox.Show(
                        $"⚠️ STOCK INSUFICIENTE\n\n" +
                        $"Insumo: {insumo.NOMBRE}\n" +
                        $"Stock disponible: {insumo.STOCK_ACTUAL:N2} {insumo.UNIDAD_MEDIDA}\n" +
                        $"Cantidad solicitada: {cantidad:N2} {insumo.UNIDAD_MEDIDA}\n\n" +
                        $"Faltante: {(cantidad - insumo.STOCK_ACTUAL):N2} {insumo.UNIDAD_MEDIDA}",
                        "Stock Insuficiente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Stop);
                    return;
                }

                // Confirmación con detalles
                var confirmar = MessageBox.Show(
                    $"¿Confirma el registro de este detalle?\n\n" +
                    $"Insumo: {insumo.NOMBRE}\n" +
                    $"Cantidad a usar: {cantidad:N2} {insumo.UNIDAD_MEDIDA}\n" +
                    $"Costo: ${(cantidad * insumo.COSTO_UNITARIO):N0}\n\n" +
                    $"Stock actual: {insumo.STOCK_ACTUAL:N2} {insumo.UNIDAD_MEDIDA}\n" +
                    $"Stock después: {(insumo.STOCK_ACTUAL - cantidad):N2} {insumo.UNIDAD_MEDIDA}",
                    "Confirmar registro",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmar != MessageBoxResult.Yes)
                    return;

                var nuevoDetalle = new DETALLE_TAREA
                {
                    ID_TAREA = idTarea,
                    ID_INSUMO = idInsumo,
                    CANTIDAD_USADA = cantidad
                };

                // Bloquear interfaz para evitar doble clic
                btnAgregarDetalle.IsEnabled = false;
                this.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                try
                {
                    // GUARDAR UNA SOLA VEZ
                    string resultado = _detalleService.Guardar(nuevoDetalle);

                    if (resultado == "OK")
                    {
                        // Verificar que realmente se descontó
                        var insumoActualizado = _insumoService.ObtenerPorId(idInsumo);
                        decimal stockFinal = insumoActualizado.STOCK_ACTUAL;
                        decimal descuento = insumo.STOCK_ACTUAL - stockFinal;

                        MessageBox.Show(
                            $"✅ DETALLE REGISTRADO EXITOSAMENTE\n\n" +
                            $"Insumo: {insumo.NOMBRE}\n" +
                            $"Cantidad solicitada: {cantidad:N2} {insumo.UNIDAD_MEDIDA}\n" +
                            $"Cantidad descontada: {descuento:N2} {insumo.UNIDAD_MEDIDA}\n" +
                            $"Costo agregado: ${(cantidad * insumo.COSTO_UNITARIO):N0}\n\n" +
                            $"Stock anterior: {insumo.STOCK_ACTUAL:N2} {insumo.UNIDAD_MEDIDA}\n" +
                            $"Stock actual: {stockFinal:N2} {insumo.UNIDAD_MEDIDA}\n\n" +
                            (descuento != cantidad ? $"⚠️ ADVERTENCIA: La cantidad descontada ({descuento:N2}) no coincide con la solicitada ({cantidad:N2})" : "✓ Descuento correcto"),
                            "Éxito",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        LimpiarCampos();
                        CargarDetallesTarea();
                        CargarCombos();
                    }
                    else
                    {
                        MessageBox.Show($"❌ ERROR AL GUARDAR\n\n{resultado}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        $"⚠️ OPERACIÓN NO PERMITIDA\n\n{ex.Message}",
                        "Error de validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Stop);
                    CargarCombos();
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(
                        $"⚠️ DATOS INVÁLIDOS\n\n{ex.Message}",
                        "Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"❌ ERROR INESPERADO\n\n{ex.Message}\n\nDetalles: {ex.StackTrace}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    btnAgregarDetalle.IsEnabled = true;
                    this.IsEnabled = true;
                    Mouse.OverrideCursor = null;
                }
            }
            catch (Exception ex)
            {
                btnAgregarDetalle.IsEnabled = true;
                this.IsEnabled = true;
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Error crítico: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEliminarDetalle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DETALLE_TAREA detalle)
            {
                try
                {
                    var insumo = _insumoService.ObtenerPorId(detalle.ID_INSUMO);
                    var tarea = _tareaService.ObtenerPorId(detalle.ID_TAREA);

                    decimal costoDetalle = detalle.CANTIDAD_USADA * (insumo?.COSTO_UNITARIO ?? 0);

                    var resultado = MessageBox.Show(
                        $"¿Está seguro de eliminar este detalle?\n\n" +
                        $"Tarea: {tarea?.TIPO_ACTIVIDAD}\n" +
                        $"Insumo: {insumo?.NOMBRE}\n" +
                        $"Cantidad: {detalle.CANTIDAD_USADA:N2} {insumo?.UNIDAD_MEDIDA}\n" +
                        $"Costo: ${costoDetalle:N0}\n\n" +
                        $"• Se devolverá el stock al inventario\n" +
                        $"• Se restará el costo del gasto de la tarea",
                        "Confirmar eliminación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        // Capturar stock antes de eliminar
                        decimal stockAntes = insumo.STOCK_ACTUAL;

                        Mouse.OverrideCursor = Cursors.Wait;

                        bool exito = _detalleService.Eliminar(detalle);

                        Mouse.OverrideCursor = null;

                        if (exito)
                        {
                            // Verificar stock después de eliminar
                            var insumoActualizado = _insumoService.ObtenerPorId(detalle.ID_INSUMO);
                            decimal stockDespues = insumoActualizado.STOCK_ACTUAL;
                            decimal devolucion = stockDespues - stockAntes;

                            MessageBox.Show(
                                $"✅ Detalle eliminado exitosamente.\n\n" +
                                $"Stock anterior: {stockAntes:N2} {insumo?.UNIDAD_MEDIDA}\n" +
                                $"Stock actual: {stockDespues:N2} {insumo?.UNIDAD_MEDIDA}\n" +
                                $"Cantidad devuelta: {devolucion:N2} {insumo?.UNIDAD_MEDIDA}\n" +
                                $"Gasto actualizado: -${costoDetalle:N0}",
                                "Éxito",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            CargarDetallesTarea();
                            CargarCombos();
                        }
                        else
                        {
                            MessageBox.Show(
                                "No se pudo eliminar el detalle.\n" +
                                "Verifique que el registro existe en la base de datos.",
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
                        $"Error al eliminar el detalle:\n\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CboInsumo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboInsumo.SelectedValue != null)
            {
                int idInsumo = (int)cboInsumo.SelectedValue;
                var insumo = _insumoService.ObtenerPorId(idInsumo);

                if (insumo != null)
                {
                    txtStockDisponible.Text = $"Stock disponible: {insumo.STOCK_ACTUAL:N2} {insumo.UNIDAD_MEDIDA}";
                    txtCostoUnitario.Text = $"Costo: ${insumo.COSTO_UNITARIO:N0} / {insumo.UNIDAD_MEDIDA}";
                }
            }
        }

        private void TxtCantidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cboInsumo.SelectedValue != null && decimal.TryParse(txtCantidad.Text, out decimal cantidad))
            {
                int idInsumo = (int)cboInsumo.SelectedValue;
                var insumo = _insumoService.ObtenerPorId(idInsumo);

                if (insumo != null)
                {
                    decimal costoTotal = cantidad * insumo.COSTO_UNITARIO;
                    txtCostoTotal.Text = $"Costo total: ${costoTotal:N0}";
                }
            }
            else
            {
                txtCostoTotal.Text = "Costo total: $0";
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarCampos();
        }

        private void LimpiarCampos()
        {
            cboTarea.SelectedIndex = -1;
            cboInsumo.SelectedIndex = -1;
            txtCantidad.Clear();
            txtStockDisponible.Text = "Stock disponible: -";
            txtCostoUnitario.Text = "Costo: $0";
            txtCostoTotal.Text = "Costo total: $0";
        }
    }
}