using AGROSMART_BLL;
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
    /// Lógica de interacción para MisTareasPage.xaml
    /// </summary>
    public partial class MisTareasPage : Page
    {
        private readonly AsignacionTareaService _asigService = new AsignacionTareaService();
        private readonly TareaService _tareaService = new TareaService();
        private readonly int _idEmpleado;
        private List<TareaItem> _todasLasTareas = new List<TareaItem>();

        private class TareaItem
        {
            public int Codigo { get; set; }
            public string Nombre { get; set; }
            public string Estado { get; set; }
            public string FechaProgramada { get; set; }
            public string HorasAcumuladas { get; set; }
        }

        public MisTareasPage(int idEmpleado)
        {
            InitializeComponent();
            _idEmpleado = idEmpleado;

            // Configurar ComboBox de filtro
            cmbFiltroEstado.Items.Add("TODOS");
            cmbFiltroEstado.Items.Add("PENDIENTE");
            cmbFiltroEstado.Items.Add("EN_EJECUCION");
            cmbFiltroEstado.Items.Add("FINALIZADA");
            cmbFiltroEstado.SelectedIndex = 0;

            // Agregar eventos manualmente
            cmbFiltroEstado.SelectionChanged += CmbFiltroEstado_SelectionChanged;
            btnLimpiarFiltro.Click += BtnLimpiarFiltro_Click;

            CargarTareas();
        }

        private void CargarTareas()
        {
            try
            {
                var asignaciones = _asigService.ListarPorEmpleado(_idEmpleado);
                _todasLasTareas.Clear();

                int pendientes = 0;
                int enEjecucion = 0;
                int finalizadas = 0;

                foreach (var a in asignaciones)
                {
                    var tarea = _tareaService.ObtenerPorId(a.ID_TAREA);
                    var fecha = _tareaService.ObtenerFechaProgramada(a.ID_TAREA);

                    _todasLasTareas.Add(new TareaItem
                    {
                        Codigo = a.ID_TAREA,
                        Nombre = tarea?.TIPO_ACTIVIDAD ?? "Sin nombre",
                        Estado = a.ESTADO,
                        FechaProgramada = fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy") : "-",
                        HorasAcumuladas = a.HORAS_TRABAJADAS.HasValue ? a.HORAS_TRABAJADAS.Value.ToString("0.##") : "0"
                    });

                    // Contar por estado
                    if (a.ESTADO == "PENDIENTE") pendientes++;
                    else if (a.ESTADO == "EN_EJECUCION") enEjecucion++;
                    else if (a.ESTADO == "FINALIZADA") finalizadas++;
                }

                // Aplicar filtro actual
                AplicarFiltro();

                // Actualizar contadores
                txtTotalTareas.Text = _todasLasTareas.Count.ToString();
                txtPendientes.Text = pendientes.ToString();
                txtEjecucion.Text = enEjecucion.ToString();
                txtFinalizadas.Text = finalizadas.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar tareas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltro()
        {
            if (cmbFiltroEstado.SelectedItem == null)
            {
                lstTareas.ItemsSource = _todasLasTareas;
                return;
            }

            string filtroSeleccionado = cmbFiltroEstado.SelectedItem.ToString();

            if (filtroSeleccionado == "TODOS")
            {
                lstTareas.ItemsSource = _todasLasTareas;
            }
            else
            {
                var tareasFiltradas = _todasLasTareas.Where(t => t.Estado == filtroSeleccionado).ToList();
                lstTareas.ItemsSource = tareasFiltradas;
            }
        }

        private void CmbFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltro();
        }

        private void BtnLimpiarFiltro_Click(object sender, RoutedEventArgs e)
        {
            cmbFiltroEstado.SelectedIndex = 0; // Seleccionar "TODOS"
        }
    }
}




