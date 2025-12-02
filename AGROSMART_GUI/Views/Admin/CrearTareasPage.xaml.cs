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
    /// <summary>
    /// Lógica de interacción para CrearTareasPage.xaml
    /// </summary>
    public partial class CrearTareasPage : Page
    {
        private readonly TareaService _tareaService = new TareaService();
        private readonly CultivoService _cultivoService = new CultivoService();
        private readonly int _idAdmin;
        private int? _idTareaEdicion = null;

        public CrearTareasPage(int idAdmin)
        {
            InitializeComponent();
            _idAdmin = idAdmin;

            // Establecer fecha por defecto
            if (dpFechaProgramada != null)
                dpFechaProgramada.SelectedDate = DateTime.Today;

            CargarCultivos();
            CargarTareas();
        }

        private void CargarCultivos()
        {
            try
            {
                var cultivos = _cultivoService.Consultar();
                var cultivosVM = cultivos.Select(c => new
                {
                    IdCultivo = c.ID_CULTIVO,
                    Display = $"#{c.ID_CULTIVO} - {c.NOMBRE_LOTE}"
                }).ToList();

                if (cboCultivo != null)
                    cboCultivo.ItemsSource = cultivosVM;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar cultivos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarTareas()
        {
            try
            {
                var tareas = _tareaService.Consultar();
                var viewModels = tareas.Select(t =>
                {
                    var cultivo = _cultivoService.ObtenerPorId(t.ID_CULTIVO);
                    return new TareaViewModel
                    {
                        IdTarea = t.ID_TAREA,
                        TipoActividad = t.TIPO_ACTIVIDAD,
                        NombreCultivo = cultivo != null ? cultivo.NOMBRE_LOTE : "Desconocido",
                        FechaProgramada = t.FECHA_PROGRAMADA,
                        TiempoTotal = t.TIEMPO_TOTAL_TAREA,
                        Estado = t.ESTADO
                    };
                }).ToList();

                if (dgTareas != null)
                    dgTareas.ItemsSource = viewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar tareas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboRecurrente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Verificar que los controles existan antes de usarlos
            if (cboRecurrente == null || txtFrecuencia == null) return;

            if (cboRecurrente.SelectedItem is ComboBoxItem item)
            {
                string tag = item.Tag?.ToString();
                txtFrecuencia.IsEnabled = (tag == "V");
                if (tag == "F")
                {
                    txtFrecuencia.Text = "0";
                }
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones con verificación de null
                if (cboCultivo == null || cboCultivo.SelectedValue == null)
                {
                    MessageBox.Show("Debe seleccionar un cultivo.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (txtTipoActividad == null || string.IsNullOrWhiteSpace(txtTipoActividad.Text))
                {
                    MessageBox.Show("Debe especificar el tipo de actividad.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpFechaProgramada == null || !dpFechaProgramada.SelectedDate.HasValue)
                {
                    MessageBox.Show("Debe seleccionar la fecha programada.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (txtTiempoTotal == null || !decimal.TryParse(txtTiempoTotal.Text, out decimal tiempo) || tiempo <= 0)
                {
                    MessageBox.Show("El tiempo total debe ser un número válido mayor a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string estado = (cboEstado?.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (string.IsNullOrEmpty(estado))
                {
                    MessageBox.Show("Debe seleccionar un estado.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string esRecurrente = (cboRecurrente?.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "F";

                int frecuencia = 0;
                if (esRecurrente == "V")
                {
                    if (txtFrecuencia == null || !int.TryParse(txtFrecuencia.Text, out frecuencia) || frecuencia <= 0)
                    {
                        MessageBox.Show("Para tareas recurrentes, la frecuencia debe ser mayor a 0.", "Validación",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                decimal costoTransporte = 0;
                if (txtCostoTransporte != null && !string.IsNullOrWhiteSpace(txtCostoTransporte.Text))
                {
                    if (!decimal.TryParse(txtCostoTransporte.Text, out costoTransporte) || costoTransporte < 0)
                    {
                        MessageBox.Show("El costo de transporte debe ser un número válido mayor o igual a 0.", "Validación",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (_idTareaEdicion.HasValue)
                {
                    // Modo edición
                    var tarea = _tareaService.ObtenerPorId(_idTareaEdicion.Value);
                    if (tarea != null)
                    {
                        tarea.ID_CULTIVO = Convert.ToInt32(cboCultivo.SelectedValue);
                        tarea.TIPO_ACTIVIDAD = txtTipoActividad.Text.Trim();
                        tarea.FECHA_PROGRAMADA = dpFechaProgramada.SelectedDate.Value;
                        tarea.TIEMPO_TOTAL_TAREA = tiempo;
                        tarea.ESTADO = estado;
                        tarea.ES_RECURRENTE = esRecurrente;
                        tarea.FRECUENCIA_DIAS = frecuencia;
                        tarea.COSTO_TRANSPORTE = costoTransporte;

                        bool resultado = _tareaService.Actualizar(tarea);
                        if (resultado)
                        {
                            MessageBox.Show("Tarea actualizada exitosamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LimpiarCampos();
                            CargarTareas();
                        }
                        else
                        {
                            MessageBox.Show("No se pudo actualizar la tarea.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    // Modo creación
                    var nuevaTarea = new TAREA
                    {
                        ID_CULTIVO = Convert.ToInt32(cboCultivo.SelectedValue),
                        ID_ADMIN_CREADOR = _idAdmin,
                        TIPO_ACTIVIDAD = txtTipoActividad.Text.Trim(),
                        FECHA_PROGRAMADA = dpFechaProgramada.SelectedDate.Value,
                        TIEMPO_TOTAL_TAREA = tiempo,
                        ESTADO = estado,
                        ES_RECURRENTE = esRecurrente,
                        FRECUENCIA_DIAS = frecuencia,
                        COSTO_TRANSPORTE = costoTransporte
                    };

                    string resultado = _tareaService.Guardar(nuevaTarea);

                    if (resultado == "OK")
                    {
                        MessageBox.Show("Tarea creada exitosamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LimpiarCampos();
                        CargarTareas();
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
            _idTareaEdicion = null;

            if (cboCultivo != null) cboCultivo.SelectedIndex = -1;
            if (txtTipoActividad != null) txtTipoActividad.Clear();
            if (dpFechaProgramada != null) dpFechaProgramada.SelectedDate = DateTime.Today;
            if (txtTiempoTotal != null) txtTiempoTotal.Text = "8";
            if (cboEstado != null) cboEstado.SelectedIndex = 0;
            if (cboRecurrente != null) cboRecurrente.SelectedIndex = 0;
            if (txtFrecuencia != null)
            {
                txtFrecuencia.Text = "0";
                txtFrecuencia.IsEnabled = false;
            }
            if (txtCostoTransporte != null) txtCostoTransporte.Text = "0.00";
            if (btnGuardar != null) btnGuardar.Content = "💾 Crear Tarea";
        }

        private void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is TareaViewModel vm)
            {
                string mensaje = $"ID: {vm.IdTarea}\n" +
                               $"Actividad: {vm.TipoActividad}\n" +
                               $"Cultivo: {vm.NombreCultivo}\n" +
                               $"Fecha: {vm.FechaProgramada:dd/MM/yyyy}\n" +
                               $"Tiempo: {vm.TiempoTotal:N2} horas\n" +
                               $"Estado: {vm.Estado}";

                MessageBox.Show(mensaje, "Detalles de la Tarea",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is TareaViewModel vm)
            {
                try
                {
                    var tarea = _tareaService.ObtenerPorId(vm.IdTarea);
                    if (tarea != null)
                    {
                        _idTareaEdicion = tarea.ID_TAREA;

                        if (cboCultivo != null) cboCultivo.SelectedValue = tarea.ID_CULTIVO;
                        if (txtTipoActividad != null) txtTipoActividad.Text = tarea.TIPO_ACTIVIDAD;
                        if (dpFechaProgramada != null) dpFechaProgramada.SelectedDate = tarea.FECHA_PROGRAMADA;
                        if (txtTiempoTotal != null) txtTiempoTotal.Text = tarea.TIEMPO_TOTAL_TAREA.ToString("0.##");

                        // Seleccionar estado
                        if (cboEstado != null)
                        {
                            foreach (ComboBoxItem item in cboEstado.Items)
                            {
                                if (item.Content.ToString() == tarea.ESTADO)
                                {
                                    cboEstado.SelectedItem = item;
                                    break;
                                }
                            }
                        }

                        // Seleccionar recurrencia
                        if (cboRecurrente != null)
                        {
                            foreach (ComboBoxItem item in cboRecurrente.Items)
                            {
                                if (item.Tag?.ToString() == tarea.ES_RECURRENTE)
                                {
                                    cboRecurrente.SelectedItem = item;
                                    break;
                                }
                            }
                        }

                        if (txtFrecuencia != null) txtFrecuencia.Text = tarea.FRECUENCIA_DIAS.ToString();
                        if (txtCostoTransporte != null) txtCostoTransporte.Text = tarea.COSTO_TRANSPORTE.ToString("0.00");

                        if (btnGuardar != null) btnGuardar.Content = "💾 Actualizar Tarea";

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

        public class TareaViewModel
        {
            public int IdTarea { get; set; }
            public string TipoActividad { get; set; }
            public string NombreCultivo { get; set; }
            public DateTime FechaProgramada { get; set; }
            public decimal TiempoTotal { get; set; }
            public string Estado { get; set; }
        }
    }
}
