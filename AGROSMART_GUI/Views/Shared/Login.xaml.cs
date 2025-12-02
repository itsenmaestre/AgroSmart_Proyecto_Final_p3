using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES;
using AGROSMART_GUI.Views.Admin;
using AGROSMART_GUI.Views.Empleado;
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

namespace AGROSMART_GUI.Views.Shared
{
    /// <summary>
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        private readonly UsuarioService _usuarioService = new UsuarioService();

        public Login()
        {
            InitializeComponent();
            this.Loaded += (s, e) => txbId.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txbId.Text, out int id))
                {
                    MessageBox.Show("El ID debe ser numérico.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string contrasena = txbContra.Password;

                if (string.IsNullOrWhiteSpace(contrasena))
                {
                    MessageBox.Show("La contraseña es obligatoria.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                USUARIO usuario = _usuarioService.Login(id, contrasena);
                if (_usuarioService.EsAdministrador(id))
                {
                    string nombreCompleto = $"{usuario.PRIMER_NOMBRE} {usuario.PRIMER_APELLIDO}";
                    var bienvenida = new BienvenidaPage(id, nombreCompleto, true);
                    bienvenida.Show();
                    Window.GetWindow(this)?.Close();
                }
                else if (_usuarioService.EsEmpleado(id))
                {
                    string nombreCompleto = $"{usuario.PRIMER_NOMBRE} {usuario.PRIMER_APELLIDO}";
                    var bienvenida = new BienvenidaPage(id, nombreCompleto, false);
                    bienvenida.Show();
                    Window.GetWindow(this)?.Close();
                }
                else
                {
                    MessageBox.Show("Usuario sin rol asignado. Contacte al administrador.",
                        "Error de Acceso", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar sesión: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MenuPage());
        }

        // Eventos de focus para ID
        private void TxbId_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D8A6B"));
            }
        }

        private void TxbId_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
            }
        }

        private void TxbId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txbContra.Focus();
            }
        }

        // Eventos de focus para Contraseña
        private void TxbContra_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D8A6B"));
        }

        private void TxbContra_LostFocus(object sender, RoutedEventArgs e)
        {
            PasswordBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
        }

        private void TxbContra_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }
    }
}
