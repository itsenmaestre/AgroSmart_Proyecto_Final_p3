using AGROSMART_BLL;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace AGROSMART_GUI.Views.Admin
{
    public partial class InicioAdminPage : Page
    {
        private readonly AdminService _adminService = new AdminService();
        private readonly int _idAdmin;
        private readonly string _nombreAdmin;

        public InicioAdminPage(int idAdmin, string nombreAdmin)
        {
            InitializeComponent();
            _idAdmin = idAdmin;
            _nombreAdmin = nombreAdmin;

            CargarDashboard();
        }

        private void CargarDashboard()
        {
            // Configurar saludo personalizado
            var hora = DateTime.Now.Hour;
            string saludo = hora < 12 ? "¡Buenos días" : hora < 18 ? "¡Buenas tardes" : "¡Buenas noches";

            if (!string.IsNullOrWhiteSpace(_nombreAdmin))
            {
                // Obtener solo el primer nombre
                string primerNombre = _nombreAdmin.Split(' ')[0];
                txtSaludo.Text = $"{saludo}, {primerNombre}! 👋";
            }
            else
            {
                txtSaludo.Text = $"{saludo}! 👋";
            }

            // Configurar fecha actual en español
            var cultura = new CultureInfo("es-ES");
            string fechaFormateada = DateTime.Now.ToString("dddd, dd 'de' MMMM yyyy", cultura);
            // Capitalizar primera letra
            txtFecha.Text = char.ToUpper(fechaFormateada[0]) + fechaFormateada.Substring(1);

            // Cargar estadísticas desde la base de datos
            CargarEstadisticas();
        }

        private void CargarEstadisticas()
        {
            try
            {
                var stats = _adminService.ObtenerEstadisticas(_idAdmin);

                // Método auxiliar para obtener valores de forma segura
                int GetStatValue(string key)
                {
                    return stats.ContainsKey(key) ? stats[key] : 0;
                }

                // Asignar valores a las tarjetas de estadísticas
                int empleados = GetStatValue("TotalEmpleados");
                int tareas = GetStatValue("TareasCreadas");
                int cultivos = GetStatValue("CultivosActivos");
                int pendientes = GetStatValue("TareasPendientes");

                txtEmpleados.Text = empleados.ToString();
                txtTareas.Text = tareas.ToString();
                txtCultivos.Text = cultivos.ToString();

                // Para insumos, si tienes la estadística en el servicio
                if (stats.ContainsKey("TotalInsumos"))
                {
                    txtInsumos.Text = GetStatValue("TotalInsumos").ToString();
                }
                else
                {
                    txtInsumos.Text = "0";
                }

                // Si tienes información de insumos bajos en stock
                if (stats.ContainsKey("InsumosBajos"))
                {
                    int insumosBajos = GetStatValue("InsumosBajos");
                    // Aquí podrías actualizar el texto de alerta si lo deseas
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar estadísticas: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Valores por defecto en caso de error
                txtEmpleados.Text = "0";
                txtTareas.Text = "0";
                txtCultivos.Text = "0";
                txtInsumos.Text = "0";
            }
        }

        // ========== EVENTOS DE BOTONES DE ACCESO RÁPIDO ==========

        private void BtnCrearTarea_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage(new CrearTareasPage(_idAdmin));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Crear Tareas: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegistrarEmpleado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage(new GestionarEmpleadosPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Empleados: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAgregarInsumo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage(new InsumosPage(_idAdmin));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Insumos: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegistrarCosecha_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToPage(new CosechasPage(_idAdmin));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Cosechas: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== MÉTODO AUXILIAR PARA NAVEGACIÓN ==========

        private void NavigateToPage(Page page)
        {
            try
            {
                // Buscar el AdminView padre
                var parentWindow = Window.GetWindow(this) as AdminView;
                if (parentWindow != null)
                {
                    // Buscar el Frame de navegación
                    var frame = parentWindow.FindName("AdminFrame") as Frame;
                    if (frame != null)
                    {
                        frame.Navigate(page);
                    }
                    else
                    {
                        MessageBox.Show("No se pudo acceder al Frame de navegación.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Alternativa: usar el NavigationService si existe
                    this.NavigationService?.Navigate(page);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al navegar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}