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
    /// Lógica de interacción para CultivosPage.xaml
    /// </summary>
    public partial class CultivosPage : Page
    {
        private readonly CultivoService _cultivoService = new CultivoService();
        private readonly int _idAdmin;
        private int? _cultivoEnEdicion = null; // Almacena el ID del cultivo en edición

        public CultivosPage(int idAdmin)
        {
            InitializeComponent();
            _idAdmin = idAdmin;

            // Establecer fecha mínima para siembra (hoy)
            dpFechaSiembra.DisplayDateStart = DateTime.Today;
            dpFechaCosechaEstimada.DisplayDateStart = DateTime.Today;

            CargarCultivos();
        }

        private void CargarCultivos()
        {
            try
            {
                var cultivos = _cultivoService.Consultar();
                var viewModels = cultivos.Select(c => new CultivoViewModel
                {
                    IdCultivo = c.ID_CULTIVO,
                    NombreLote = c.NOMBRE_LOTE,
                    FechaSiembra = c.FECHA_SIEMBRA,
                    FechaCosechaEstimada = c.FECHA_COSECHA_ESTIMADA,
                    DiasRestantes = CalcularDiasRestantes(c.FECHA_COSECHA_ESTIMADA)
                }).ToList();

                dgCultivos.ItemsSource = viewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar cultivos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CalcularDiasRestantes(DateTime fechaCosecha)
        {
            var dias = (fechaCosecha - DateTime.Today).Days;
            if (dias < 0)
                return "Vencido";
            else if (dias == 0)
                return "Hoy";
            else
                return $"{dias} días";
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(txtNombreLote.Text))
                {
                    MessageBox.Show("El nombre del lote es obligatorio.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpFechaSiembra.SelectedDate.HasValue)
                {
                    MessageBox.Show("Debe seleccionar la fecha de siembra.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpFechaCosechaEstimada.SelectedDate.HasValue)
                {
                    MessageBox.Show("Debe seleccionar la fecha estimada de cosecha.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // RN-12: Validar que cosecha > siembra
                if (dpFechaCosechaEstimada.SelectedDate <= dpFechaSiembra.SelectedDate)
                {
                    MessageBox.Show("La fecha de cosecha estimada debe ser posterior a la fecha de siembra.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_cultivoEnEdicion.HasValue)
                {
                    // MODO ACTUALIZACIÓN
                    var cultivoExistente = _cultivoService.ObtenerPorId(_cultivoEnEdicion.Value);

                    if (cultivoExistente != null)
                    {
                        cultivoExistente.NOMBRE_LOTE = txtNombreLote.Text.Trim();
                        cultivoExistente.FECHA_SIEMBRA = dpFechaSiembra.SelectedDate.Value;
                        cultivoExistente.FECHA_COSECHA_ESTIMADA = dpFechaCosechaEstimada.SelectedDate.Value;

                        bool resultado = _cultivoService.Actualizar(cultivoExistente);

                        if (resultado)
                        {
                            MessageBox.Show("Cultivo actualizado exitosamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LimpiarCampos();
                            CargarCultivos();
                        }
                        else
                        {
                            MessageBox.Show("Error al actualizar el cultivo.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    // MODO CREACIÓN
                    var nuevoCultivo = new CULTIVO
                    {
                        NOMBRE_LOTE = txtNombreLote.Text.Trim(),
                        FECHA_SIEMBRA = dpFechaSiembra.SelectedDate.Value,
                        FECHA_COSECHA_ESTIMADA = dpFechaCosechaEstimada.SelectedDate.Value,
                        ID_ADMIN_SUPERVISOR = _idAdmin,
                        ALERTA_N8N = "SIN ALERTA" // Por defecto
                    };

                    string resultado = _cultivoService.Guardar(nuevoCultivo);

                    if (resultado == "OK")
                    {
                        MessageBox.Show("Cultivo registrado exitosamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LimpiarCampos();
                        CargarCultivos();
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
            txtNombreLote.Clear();
            dpFechaSiembra.SelectedDate = null;
            dpFechaCosechaEstimada.SelectedDate = null;
            txtObservaciones.Clear();

            // Resetear modo edición
            _cultivoEnEdicion = null;
            btnGuardar.Content = "💾 Guardar Cultivo";
        }

        private void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CultivoViewModel vm)
            {
                string mensaje = $"ID: {vm.IdCultivo}\n" +
                                $"Nombre: {vm.NombreLote}\n" +
                                $"Fecha Siembra: {vm.FechaSiembra:dd/MM/yyyy}\n" +
                                $"Cosecha Estimada: {vm.FechaCosechaEstimada:dd/MM/yyyy}\n" +
                                $"Días restantes: {vm.DiasRestantes}";

                MessageBox.Show(mensaje, "Detalles del Cultivo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CultivoViewModel vm)
            {
                try
                {
                    var cultivo = _cultivoService.ObtenerPorId(vm.IdCultivo);
                    if (cultivo != null)
                    {
                        // Cargar datos en el formulario
                        txtNombreLote.Text = cultivo.NOMBRE_LOTE;
                        dpFechaSiembra.SelectedDate = cultivo.FECHA_SIEMBRA;
                        dpFechaCosechaEstimada.SelectedDate = cultivo.FECHA_COSECHA_ESTIMADA;

                        // Activar modo edición
                        _cultivoEnEdicion = vm.IdCultivo;
                        btnGuardar.Content = "✏️ Actualizar Cultivo";

                        MessageBox.Show("Datos cargados. Modifique los campos y presione 'Actualizar Cultivo'.", "Modo Edición",
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

        public class CultivoViewModel
        {
            public int IdCultivo { get; set; }
            public string NombreLote { get; set; }
            public DateTime FechaSiembra { get; set; }
            public DateTime FechaCosechaEstimada { get; set; }
            public string DiasRestantes { get; set; }
        }
    }
}