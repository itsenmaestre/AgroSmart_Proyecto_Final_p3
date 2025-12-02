using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AGROSMART_GUI.Views.Admin
{
    public partial class InsumosPage : Page
    {
        private readonly InsumoService _insumoService = new InsumoService();
        private readonly int _idAdmin;
        private int? _idInsumoEdicion = null;

        public InsumosPage(int idAdmin)
        {
            InitializeComponent();
            _idAdmin = idAdmin;
            CargarInsumos();
        }

        private void CargarInsumos()
        {
            try
            {
                var insumos = _insumoService.Consultar();
                var viewModels = insumos.Select(i => new InsumoViewModel
                {
                    IdInsumo = i.ID_INSUMO,
                    Nombre = i.NOMBRE,
                    Tipo = i.TIPO,
                    StockActual = i.STOCK_ACTUAL,
                    StockMinimo = i.STOCK_MINIMO,
                    CostoUnitario = i.COSTO_UNITARIO,
                    Unidad = i.UNIDAD_MEDIDA
                }).ToList();

                dgInsumos.ItemsSource = viewModels;

                // 🔍 DIAGNÓSTICO: Mostrar insumos con stock negativo
                var stocksNegativos = viewModels.Where(i => i.StockActual < 0).ToList();
                if (stocksNegativos.Any())
                {
                    string mensaje = "⚠️ ALERTA: Se detectaron stocks negativos:\n\n";
                    foreach (var item in stocksNegativos)
                    {
                        mensaje += $"• {item.Nombre}: {item.StockActual}\n";
                    }
                    mensaje += "\n¿Desea corregir estos valores a 0?";

                    var resultado = MessageBox.Show(mensaje, "Stock Negativo Detectado",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        CorregirStocksNegativos(stocksNegativos);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar insumos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CorregirStocksNegativos(List<InsumoViewModel> stocksNegativos)
        {
            try
            {
                int corregidos = 0;
                foreach (var item in stocksNegativos)
                {
                    if (_insumoService.ActualizarStock(item.IdInsumo, 0))
                    {
                        corregidos++;
                    }
                }

                MessageBox.Show($"Se corrigieron {corregidos} insumos.", "Corrección Completa",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CargarInsumos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al corregir stocks: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre del insumo es obligatorio.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtStockActual.Text, out decimal stockActual) || stockActual < 0)
                {
                    MessageBox.Show("El stock actual debe ser un número válido mayor o igual a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtStockMinimo.Text, out decimal stockMinimo) || stockMinimo < 0)
                {
                    MessageBox.Show("El stock mínimo debe ser un número válido mayor o igual a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtCostoUnitario.Text, out decimal costoUnitario) || costoUnitario <= 0)
                {
                    MessageBox.Show("El costo unitario debe ser un número válido mayor a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string unidad = (cboUnidadMedida.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (string.IsNullOrEmpty(unidad))
                {
                    MessageBox.Show("Debe seleccionar una unidad de medida.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string tipo = (cboTipo.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (string.IsNullOrEmpty(tipo))
                {
                    MessageBox.Show("Debe seleccionar un tipo de insumo.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_idInsumoEdicion.HasValue)
                {
                    // Modo edición
                    var insumo = _insumoService.ObtenerPorId(_idInsumoEdicion.Value);
                    if (insumo != null)
                    {
                        insumo.NOMBRE = txtNombre.Text.Trim();
                        insumo.TIPO = tipo;
                        insumo.STOCK_ACTUAL = stockActual;
                        insumo.STOCK_MINIMO = stockMinimo;
                        insumo.COSTO_UNITARIO = costoUnitario;
                        insumo.UNIDAD_MEDIDA = unidad;

                        bool resultado = _insumoService.Actualizar(insumo);
                        if (resultado)
                        {
                            MessageBox.Show("Insumo actualizado exitosamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LimpiarCampos();
                            CargarInsumos();
                        }
                        else
                        {
                            MessageBox.Show("No se pudo actualizar el insumo.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    // Modo creación
                    var nuevoInsumo = new INSUMO
                    {
                        NOMBRE = txtNombre.Text.Trim(),
                        TIPO = tipo,
                        STOCK_ACTUAL = stockActual,
                        STOCK_MINIMO = stockMinimo,
                        COSTO_UNITARIO = costoUnitario,
                        UNIDAD_MEDIDA = unidad,
                        ID_ADMIN_REGISTRO = _idAdmin
                    };

                    string resultado = _insumoService.Guardar(nuevoInsumo);

                    if (resultado == "OK")
                    {
                        MessageBox.Show("Insumo registrado exitosamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LimpiarCampos();
                        CargarInsumos();
                    }
                    else
                    {
                        MessageBox.Show($"Error al guardar: {resultado}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarCampos();
        }

        private void LimpiarCampos()
        {
            _idInsumoEdicion = null;
            txtNombre.Clear();
            cboTipo.SelectedIndex = 0;
            txtStockActual.Text = "0";
            txtStockMinimo.Text = "0";
            txtCostoUnitario.Text = "0.00";
            txtObservaciones.Clear();
            btnGuardar.Content = "💾 Guardar Insumo";
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is InsumoViewModel vm)
            {
                try
                {
                    var insumo = _insumoService.ObtenerPorId(vm.IdInsumo);
                    if (insumo != null)
                    {
                        _idInsumoEdicion = insumo.ID_INSUMO;
                        txtNombre.Text = insumo.NOMBRE;
                        txtStockActual.Text = insumo.STOCK_ACTUAL.ToString("0.##");
                        txtStockMinimo.Text = insumo.STOCK_MINIMO.ToString("0.##");
                        txtCostoUnitario.Text = insumo.COSTO_UNITARIO.ToString("0.00");

                        // Seleccionar tipo
                        foreach (ComboBoxItem item in cboTipo.Items)
                        {
                            if (item.Content.ToString() == insumo.TIPO)
                            {
                                cboTipo.SelectedItem = item;
                                break;
                            }
                        }

                        // Seleccionar unidad de medida
                        foreach (ComboBoxItem item in cboUnidadMedida.Items)
                        {
                            if (item.Content.ToString() == insumo.UNIDAD_MEDIDA)
                            {
                                cboUnidadMedida.SelectedItem = item;
                                break;
                            }
                        }

                        btnGuardar.Content = "💾 Actualizar Insumo";
                        MessageBox.Show("Datos cargados. Modifique y presione Actualizar.", "Editar",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAjustarStock_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is InsumoViewModel vm)
            {
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Stock actual: {vm.StockActual:N2}\n\nIngrese el nuevo stock:",
                    "Ajustar Stock",
                    vm.StockActual.ToString("0.##"));

                if (!string.IsNullOrWhiteSpace(input))
                {
                    if (decimal.TryParse(input, out decimal nuevoStock) && nuevoStock >= 0)
                    {
                        try
                        {
                            bool resultado = _insumoService.ActualizarStock(vm.IdInsumo, nuevoStock);
                            if (resultado)
                            {
                                MessageBox.Show("Stock actualizado exitosamente.", "Éxito",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                CargarInsumos();
                            }
                            else
                            {
                                MessageBox.Show("No se pudo actualizar el stock.", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Debe ingresar un número válido mayor o igual a 0.", "Validación",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void BtnStockBajo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var insumosStockBajo = _insumoService.ObtenerInsumosConStockBajo();
                if (insumosStockBajo.Count == 0)
                {
                    MessageBox.Show("¡Excelente! No hay insumos con stock bajo.", "Stock Bajo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var viewModels = insumosStockBajo.Select(i => new InsumoViewModel
                {
                    IdInsumo = i.ID_INSUMO,
                    Nombre = i.NOMBRE,
                    Tipo = i.TIPO,
                    StockActual = i.STOCK_ACTUAL,
                    StockMinimo = i.STOCK_MINIMO,
                    CostoUnitario = i.COSTO_UNITARIO,
                    Unidad = i.UNIDAD_MEDIDA
                }).ToList();

                dgInsumos.ItemsSource = viewModels;

                MessageBox.Show($"Se encontraron {insumosStockBajo.Count} insumos con stock bajo.", "Alerta de Stock",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al filtrar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class InsumoViewModel
        {
            public int IdInsumo { get; set; }
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public decimal StockActual { get; set; }
            public decimal StockMinimo { get; set; }
            public decimal CostoUnitario { get; set; }
            public string Unidad { get; set; }
        }
    }
}