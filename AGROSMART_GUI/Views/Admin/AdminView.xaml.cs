using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AGROSMART_GUI.Views.Admin
{
    public partial class AdminView : Window
    {
        private readonly int _idAdmin;
        private readonly string _nombreAdmin;

        public AdminView(int idAdmin, string nombreCompleto)
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.CanResize;

            _idAdmin = idAdmin;
            _nombreAdmin = nombreCompleto;

            if (!string.IsNullOrWhiteSpace(_nombreAdmin))
                txtUserName.Text = _nombreAdmin;

            Loaded += AdminView_Loaded;
        }

        private void AdminView_Loaded(object sender, RoutedEventArgs e)
        {
            // Seleccionar inicio por defecto
            SeleccionarItem(btnInicio);
            InicioAdminPage();
        }

        // ============== MANEJO DE CLICS DEL MENÚ ==============

        // Maneja el clic en items del menú principal (sin submenú)
        private void MenuItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                DesseleccionarTodos();
                SeleccionarItem(border);

                string tag = border.Tag.ToString().Split('|')[0];

                NavegararPagina(tag);
            }
        }

        // Maneja el clic en items con submenú (para expandir/colapsar)
        private void ToggleSubMenu_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                string tag = border.Tag?.ToString();

                // Toggle del submenú correspondiente
                if (tag == "Tareas")
                {
                    ToggleSubmenu(subMenuTareas, iconTareas);
                }
                else if (tag == "Finanzas")
                {
                    ToggleSubmenu(subMenuFinanzas, iconFinanzas);
                }
            }
        }

        // Maneja el clic en items del submenú
        private void SubMenuItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                DesseleccionarTodos();
                SeleccionarSubItem(border);

                string tag = border.Tag.ToString().Split('|')[0];

                NavegararPagina(tag);
            }

        }



        // ============== NAVEGACIÓN ==============

        private void NavegararPagina(string pagina)
        {
            switch (pagina)
            {
                case "Inicio":
                    InicioAdminPage();
                    break;
                case "Empleados":
                    CargarPaginaEmpleados();
                    break;
                case "Cultivos":
                    CargarPaginaCultivos();
                    break;
                case "CrearTareas":
                    CargarPaginaCrearTareas();
                    break;
                case "AsignarTarea":
                    CargarPaginaAsignarTarea();
                    break;
                case "DetalleTareas":
                    CargarPaginaDetalleTarea();
                    break;
                case "Insumos":
                    CargarPaginaInsumos();
                    break;
                case "Cosechas":
                    CargarPaginaCosechas();
                    break;
                case "GestionGastos":
                    CargarPaginaGestionGastos();
                    break;

                case "Liquidaciones":
                    LiquidarSueldosPage();
                    break;


            }
        }

        // ============== CARGA DE PÁGINAS ==============

        private void InicioAdminPage()
        {
            try
            {
                var page = new InicioAdminPage(_idAdmin, _nombreAdmin);
                AdminFrame.Navigate(page);
            }
            catch
            {
                MostrarPaginaTemporal("Dashboard",
                    $"Bienvenido {_nombreAdmin}.\nSelecciona una opción del menú lateral.");
            }
        }

        private void LiquidarSueldosPage()
        {
            try
            {
                var page = new Liquidaciones_Cosecha();
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Liquidar Sueldos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaEmpleados()
        {
            try
            {
                var page = new GestionarEmpleadosPage();
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Empleados: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaDetalleTarea()
        {
            try
            {
                var page = new DetalleTareaPage(_idAdmin);
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Detalle Tareas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaInsumos()
        {
            try
            {
                var page = new InsumosPage(_idAdmin);
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Insumos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaCrearTareas()
        {
            try
            {
                var page = new CrearTareasPage(_idAdmin);
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Crear Tareas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaAsignarTarea()
        {
            try
            {
                var page = new AsignarEmpleadosPage(_idAdmin);
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Asignar Tareas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaCultivos()
        {
            try
            {
                var page = new CultivosPage(_idAdmin);
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Cultivos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaCosechas()
        {
            try
            {
                var page = new CosechasPage(_idAdmin);
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Cosechas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPaginaGestionGastos()
        {
            try
            {
                var page = new GestionGastosPage();
                AdminFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar Gestión de Gastos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============== UTILIDADES DE SUBMENÚ ==============

        private void ToggleSubmenu(StackPanel submenu, TextBlock icon)
        {
            if (submenu.Visibility == Visibility.Collapsed)
            {
                submenu.Visibility = Visibility.Visible;
                icon.Text = "▲";
            }
            else
            {
                submenu.Visibility = Visibility.Collapsed;
                icon.Text = "▼";
            }
        }

        // ============== ESTILOS Y SELECCIÓN ==============

        // ============== ESTILOS Y SELECCIÓN ==============

        // Limpia todos los Tags (quita Selected)
        private void DesseleccionarTodos()
        {
            // Items principales
            ResetTag(btnInicio);
            ResetTag(btnEmpleados);
            ResetTag(btnCultivos);
            ResetTag(btnInsumos);
            ResetTag(btnCosechas);
            ResetTag(btnTareas);
            ResetTag(btnFinanzas);

            // Subitems Tareas
            foreach (var child in subMenuTareas.Children)
                if (child is Border b)
                    ResetTag(b);

            // Subitems Finanzas
            foreach (var child in subMenuFinanzas.Children)
                if (child is Border b)
                    ResetTag(b);
        }

        private void ResetTag(Border item)
        {
            if (item.Tag != null)
            {
                string baseTag = item.Tag.ToString().Split('|')[0];
                item.Tag = baseTag;  // se queda solo el nombre
            }
        }

        private void SeleccionarItem(Border item)
        {
            string baseTag = item.Tag.ToString().Split('|')[0];
            item.Tag = $"{baseTag}|Selected";  // activa el trigger del XAML
        }

        private void SeleccionarSubItem(Border item)
        {
            string baseTag = item.Tag.ToString().Split('|')[0];
            item.Tag = $"{baseTag}|Selected";
        }


        // ============== OTROS ==============

        private void MostrarPaginaTemporal(string titulo, string descripcion)
        {
            var page = new Page();
            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(40)
            };

            var title = new TextBlock
            {
                Text = titulo,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkGreen,
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var desc = new TextBlock
            {
                Text = descripcion,
                FontSize = 16,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 400
            };

            stack.Children.Add(title);
            stack.Children.Add(desc);
            page.Content = stack;

            AdminFrame.Navigate(page);
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Está seguro que desea cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }
    }
}