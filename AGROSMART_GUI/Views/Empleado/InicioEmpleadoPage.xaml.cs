using AGROSMART_BLL;
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

namespace AGROSMART_GUI.Views.Empleado
{
    /// <summary>
    /// Lógica de interacción para InicioEmpleadoPage.xaml
    /// </summary>
    public partial class InicioEmpleadoPage : Page
    {
        private readonly AsignacionTareaService _asigService = new AsignacionTareaService();
        private readonly TareaService _tareaService = new TareaService();
        private readonly int _idEmpleado;

        public InicioEmpleadoPage(int idEmpleado, string nombreEmpleado)
        {
            InitializeComponent();
            _idEmpleado = idEmpleado;

            if (!string.IsNullOrWhiteSpace(nombreEmpleado))
                txtBienvenida.Text = $"Hola, {nombreEmpleado}";

           
            txtFechaHoy.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM",
                new System.Globalization.CultureInfo("es-ES"));

            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                // Cargar estadísticas
                int tareasHoy = _tareaService.ContarTareasDeHoy(_idEmpleado);
                int enProgreso = _tareaService.ContarPorEstado(_idEmpleado, "EN_EJECUCION");
                int vencidas = _tareaService.ContarVencidas(_idEmpleado);

                txtTareasHoy.Text = tareasHoy.ToString();
                txtEnProgreso.Text = enProgreso.ToString();
                txtVencidas.Text = vencidas.ToString();

                // Resumen
                var asignaciones = _asigService.ListarPorEmpleado(_idEmpleado);
                txtResumen.Text = $"• Tienes {asignaciones.Count} tareas asignadas en total.\n" +
                                 $"• {tareasHoy} tareas programadas para hoy.\n" +
                                 $"• {enProgreso} tareas actualmente en progreso.\n" +
                                 $"• {vencidas} tareas vencidas que requieren atención.";
            }
            catch (Exception ex)
            {
                txtResumen.Text = $"Error al cargar datos: {ex.Message}";
            }
        }
    }
}
