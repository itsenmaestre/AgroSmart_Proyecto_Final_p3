using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Lógica de interacción para RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        private readonly UsuarioService _usuarioService = new UsuarioService();

        public RegisterPage()
        {
            InitializeComponent();
            this.Loaded += (s, e) => TxbId.Focus();
        }

        private void TxbId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txbNom.Focus();
                e.Handled = true;
            }
        }

        private void TxbNom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txbApel.Focus();
                e.Handled = true;
            }
        }

        private void TxbApel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txbEmail.Focus();
                e.Handled = true;
            }
        }

        private void TxbEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txbTelefono.Focus();
                e.Handled = true;
            }
        }

        private void TxbTelefono_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txbContra.Focus();
                e.Handled = true;
            }
        }

        private void TxbContra_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RegisterButton_Click(sender, e);
                e.Handled = true;
            }
        }

        // Eventos GotFocus
        private void TxbId_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859"));
                border.BorderThickness = new Thickness(2);
            }
        }

        private void TxbNom_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859"));
                border.BorderThickness = new Thickness(2);
            }
        }

        private void TxbApel_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859"));
                border.BorderThickness = new Thickness(2);
            }
        }

        private void TxbEmail_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859"));
                border.BorderThickness = new Thickness(2);
            }
        }

        private void TxbTelefono_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859"));
                border.BorderThickness = new Thickness(2);
            }
        }

        private void TxbContra_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859"));
            PasswordBorder.BorderThickness = new Thickness(2);
        }

        // Eventos LostFocus
        private void TxbId_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
                border.BorderThickness = new Thickness(1.5);
            }
        }

        private void TxbNom_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
                border.BorderThickness = new Thickness(1.5);
            }
        }

        private void TxbApel_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
                border.BorderThickness = new Thickness(1.5);
            }
        }

        private void TxbEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
                border.BorderThickness = new Thickness(1.5);
            }
        }

        private void TxbTelefono_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is Grid grid && grid.Parent is Border border)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
                border.BorderThickness = new Thickness(1.5);
            }
        }

        private void TxbContra_LostFocus(object sender, RoutedEventArgs e)
        {
            PasswordBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
            PasswordBorder.BorderThickness = new Thickness(1.5);
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar ID
                if (!int.TryParse(TxbId.Text, out int id))
                {
                    MessageBox.Show("La identificación debe ser numérica.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar campos obligatorios
                if (string.IsNullOrWhiteSpace(txbNom.Text))
                {
                    MessageBox.Show("El nombre es obligatorio.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txbApel.Text))
                {
                    MessageBox.Show("El apellido es obligatorio.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txbEmail.Text))
                {
                    MessageBox.Show("El email es obligatorio.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar formato de email
                if (!Regex.IsMatch(txbEmail.Text, @"^\S+@\S+\.\S+$"))
                {
                    MessageBox.Show("El formato del email no es válido.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar teléfono
                if (string.IsNullOrWhiteSpace(txbTelefono.Text))
                {
                    MessageBox.Show("El teléfono es obligatorio.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar formato de teléfono (7-15 dígitos)
                if (!Regex.IsMatch(txbTelefono.Text, @"^[0-9]{7,15}$"))
                {
                    MessageBox.Show("El teléfono debe tener entre 7 y 15 dígitos numéricos.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar contraseña
                if (string.IsNullOrWhiteSpace(txbContra.Password))
                {
                    MessageBox.Show("La contraseña es obligatoria.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Crear objetos
                var usuario = new USUARIO
                {
                    ID_USUARIO = id,
                    PRIMER_NOMBRE = txbNom.Text.Trim(),
                    PRIMER_APELLIDO = txbApel.Text.Trim(),
                    EMAIL = txbEmail.Text.Trim(),
                    CONTRASENA = txbContra.Password,
                    TELEFONO = txbTelefono.Text.Trim()
                };

                var empleado = new EMPLEADO
                {
                    ID_USUARIO = id,
                    MONTO_POR_HORA = 0,
                    MONTO_POR_JORNAL = 0
                };

                // Registrar
                string resultado = _usuarioService.RegistrarEmpleado(usuario, empleado);

                if (resultado == "OK")
                {
                    MessageBox.Show(
                        "¡Registro exitoso!\n\nYa puedes iniciar sesión con tus credenciales.",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.Navigate(new Login());
                }
                else
                {
                    MessageBox.Show(resultado, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButon_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}
