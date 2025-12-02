using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace AGROSMART_GUI.Views.Empleado
{
    /// <summary>
    /// Lógica de interacción para RegistrarAvanceView.xaml
    /// </summary>
    public partial class RegistrarAvanceView : Window
    {
        private readonly AsignacionTareaService _svc = new AsignacionTareaService();
        private ASIGNACION_TAREA _asignacion;
        private readonly TareaService _tareaService = new TareaService();


        public RegistrarAvanceView(ASIGNACION_TAREA seleccionada)
        {
            InitializeComponent();
            _asignacion = seleccionada;
            PrecargarUI();
        }


        public RegistrarAvanceView(int idTarea, int idEmpleado)
        {
            InitializeComponent();

            var asignaciones = _svc.ListarPorEmpleado(idEmpleado);
            _asignacion = asignaciones.FirstOrDefault(a => a.ID_TAREA == idTarea);

            if (_asignacion == null)
            {
                MessageBox.Show("No se encontró la asignación para esta tarea.", "AgroSmart");
                this.Close();
                return;
            }

            PrecargarUI();
        }

        private void PrecargarUI()
        {
            // Encabezados
            txtInfoTarea.Text = "Tarea código: " + _asignacion.ID_TAREA;
            txtFechaTarea.Text = "Fecha programada: -";


            txtHorasTrabajadas.Text = _asignacion.HORAS_TRABAJADAS.HasValue
                ? _asignacion.HORAS_TRABAJADAS.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : "0.00";

            txtJornadasTrabajadas.Text = _asignacion.JORNADAS_TRABAJADAS.HasValue
                ? _asignacion.JORNADAS_TRABAJADAS.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : "0.00";

            if (!string.IsNullOrWhiteSpace(_asignacion.ESTADO))
            {
                foreach (object obj in cboEstado.Items)
                {
                    ComboBoxItem item = obj as ComboBoxItem;
                    if (item != null && string.Equals(Convert.ToString(item.Content), _asignacion.ESTADO, StringComparison.OrdinalIgnoreCase))
                    {
                        cboEstado.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Guarda los cambios del avance y actualiza el estado de la tarea
        /// </summary>
        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar datos
                if (!ValidarDatos())
                    return;

                // Actualizar datos de la asignación
                ActualizarAsignacion();

                // Guardar asignación
                string resultado = await Task.Run(() => _svc.ActualizarAvance(_asignacion));
                bool asignacionActualizada = resultado == "OK";

                if (!asignacionActualizada)
                {
                    MessageBox.Show(
                        "No se pudo actualizar la asignación.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Si el estado es FINALIZADA, actualizar también la TAREA
                if (_asignacion.ESTADO?.ToUpper() == "FINALIZADA")
                {
                    await FinalizarTarea();
                }

                MessageBox.Show(
                    "Avance registrado exitosamente.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarError("Error al guardar el avance", ex);
            }
        }

        /// <summary>
        /// Valida los datos del formulario
        /// </summary>
        private bool ValidarDatos()
        {
            // Validar horas trabajadas
            if (string.IsNullOrWhiteSpace(txtHorasTrabajadas.Text))
            {
                MessageBox.Show(
                    "Debe ingresar las horas trabajadas.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtHorasTrabajadas.Focus();
                return false;
            }

            // Validar jornadas trabajadas
            if (string.IsNullOrWhiteSpace(txtJornadasTrabajadas.Text))
            {
                MessageBox.Show(
                    "Debe ingresar las jornadas trabajadas.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtJornadasTrabajadas.Focus();
                return false;
            }

            // Validar estado
            if (cboEstado.SelectedItem == null)
            {
                MessageBox.Show(
                    "Debe seleccionar un estado.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                cboEstado.Focus();
                return false;
            }

            // Validar que sean números válidos
            try
            {
                ParseNullableDecimal(txtHorasTrabajadas.Text);
                ParseNullableDecimal(txtJornadasTrabajadas.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show(
                    "Las horas y jornadas deben ser números válidos.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Actualiza los datos de la asignación con los valores del formulario
        /// </summary>
        private void ActualizarAsignacion()
        {
            _asignacion.HORAS_TRABAJADAS = ParseNullableDecimal(txtHorasTrabajadas.Text);
            _asignacion.JORNADAS_TRABAJADAS = ParseNullableDecimal(txtJornadasTrabajadas.Text);

            if (cboEstado.SelectedItem is ComboBoxItem itemEstado)
            {
                _asignacion.ESTADO = itemEstado.Content?.ToString();
            }

            // Actualizar fecha de finalización si es FINALIZADA
            if (_asignacion.ESTADO?.ToUpper() == "FINALIZADA" &&
                _asignacion.FECHA_ASIGNACION == null)
            {
                _asignacion.FECHA_ASIGNACION = DateTime.Now;
            }
        }

        /// <summary>
        /// Finaliza la tarea principal y actualiza su estado en la BD
        /// </summary>
        private async Task FinalizarTarea()
        {
            try
            {
                // Obtener la tarea completa
                var tarea = await Task.Run(() =>
                    _tareaService.Consultar()?.FirstOrDefault(t => t.ID_TAREA == _asignacion.ID_TAREA));

                if (tarea == null)
                {
                    MessageBox.Show(
                        "No se encontró la tarea asociada.",
                        "Advertencia",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Actualizar estado y fecha de ejecución
                tarea.ESTADO = "FINALIZADA";

                if (tarea.FECHA_PROGRAMADA == null)
                {
                    tarea.FECHA_PROGRAMADA = DateTime.Now;
                }

                // Calcular tiempo total (convertir horas a minutos)
                if (_asignacion.HORAS_TRABAJADAS.HasValue)
                {
                    tarea.TIEMPO_TOTAL_TAREA = (int)(_asignacion.HORAS_TRABAJADAS.Value * 60);
                }

                // Guardar tarea actualizada
                bool tareaActualizada = await Task.Run(() => _tareaService.Actualizar(tarea));

                if (!tareaActualizada)
                {
                    MessageBox.Show(
                        "Se guardó el avance, pero no se pudo actualizar el estado de la tarea principal.",
                        "Advertencia",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al finalizar la tarea: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private decimal? ParseNullableDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            decimal value;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                return value;

            if (decimal.TryParse(input, out value))
                return value;

            throw new FormatException("Número inválido.");
        }

        private void MostrarError(string mensaje, Exception ex)
        {
            MessageBox.Show(
                $"{mensaje}\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}