using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AGROSMART_BLL;
using AGROSMART_ENTITY;
using AGROSMART_ENTITY.ENTIDADES;

namespace AGROSMART_GUI.Views.Admin
{
    public partial class GestionarEmpleadosPage : Page
    {
        private readonly UsuarioService _usuarioService = new UsuarioService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private List<EmpleadoViewModel> _listaEmpleados;
        private EmpleadoViewModel _empleadoSeleccionado;
        private bool _modoEdicion = false;

        public GestionarEmpleadosPage()
        {
            InitializeComponent();
            CargarEmpleados();
        }

        private void CargarEmpleados()
        {
            try
            {
                
                var empleados = _empleadoService.ListarEmpleadosConUsuario();
              

                _listaEmpleados = empleados.Select(e => new EmpleadoViewModel
                {
                    
                    IdUsuario = e.IdUsuario,
                    NombreCompleto = e.NombreCompleto,
                    Email = e.Email ?? "No registrado",
                    Telefono = e.Telefono ?? "No registrado",
                    MontoPorHora = e.MontoPorHora,
                    MontoPorJornal = e.MontoPorJornal,
                    MontoHoraFormatted = $"${e.MontoPorHora:N2}",
                    MontoJornalFormatted = $"${e.MontoPorJornal:N2}"
                }).ToList();

                dgEmpleados.ItemsSource = _listaEmpleados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar empleados: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_listaEmpleados == null) return;

            string busqueda = txtBuscar.Text.ToLower();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                dgEmpleados.ItemsSource = _listaEmpleados;
            }
            else
            {
                var filtrados = _listaEmpleados.Where(emp =>
                    emp.NombreCompleto.ToLower().Contains(busqueda) ||
                    emp.Email.ToLower().Contains(busqueda) ||
                    emp.IdUsuario.ToString().Contains(busqueda) ||
                    emp.Telefono.ToLower().Contains(busqueda)
                ).ToList();

                dgEmpleados.ItemsSource = filtrados;
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarEmpleados();
            OcultarPanelEdicion();
            MessageBox.Show("Lista actualizada correctamente.", "Información",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnNuevoEmpleado_Click(object sender, RoutedEventArgs e)
        {
            _modoEdicion = false;
            _empleadoSeleccionado = null;
            txtTituloEdicion.Text = "Nuevo Empleado";
            btnGuardar.Content = "💾 Crear Empleado";
            LimpiarFormulario();
            MostrarPanelEdicion();
        }

        private void DgEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is EmpleadoViewModel emp)
            {
                _modoEdicion = true;
                _empleadoSeleccionado = emp;
                txtTituloEdicion.Text = "Editar Empleado";
                btnGuardar.Content = "💾 Actualizar";
                CargarDatosEnFormulario(emp);
                MostrarPanelEdicion();
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is EmpleadoViewModel emp)
            {
                var result = MessageBox.Show(
                    $"¿Está seguro de eliminar al empleado '{emp.NombreCompleto}'?\n\nEsta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                try
                {
                    // 🟡 PRIMERO validar con el nuevo método seguro
                    string validacion = _empleadoService.EliminarEmpleado(emp.IdUsuario);

                    if (validacion != "OK")
                    {
                        MessageBox.Show(validacion, "No se puede eliminar",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 🟢 El empleado ya se eliminó, ahora obtener usuario
                    var usuario = _usuarioService.ObtenerPorId(emp.IdUsuario);

                    if (usuario == null)
                    {
                        MessageBox.Show("Empleado eliminado, pero el usuario ya no existe.", "Información",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        CargarEmpleados();
                        OcultarPanelEdicion();
                        return;
                    }

                    // 🔵 Eliminar el usuario
                    bool eliminadoUsr = _usuarioService.Eliminar(usuario);

                    if (eliminadoUsr)
                    {
                        MessageBox.Show("Empleado eliminado exitosamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("El empleado fue eliminado, pero hubo un error al eliminar el usuario.",
                            "Advertencia",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }

                    CargarEmpleados();
                    OcultarPanelEdicion();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }


        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
                return;

            try
            {
                if (_modoEdicion)
                {
                    ActualizarEmpleado();
                }
                else
                {
                    CrearNuevoEmpleado();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CrearNuevoEmpleado()
        {
            try
            {
                
                var nuevoUsuario = new USUARIO
                {
                    ID_USUARIO = int.Parse(txtCedula.Text.Trim()),
                    PRIMER_NOMBRE = txtPrimerNombre.Text.Trim(),
                    SEGUNDO_NOMBRE = string.IsNullOrWhiteSpace(txtSegundoNombre.Text) ? null : txtSegundoNombre.Text.Trim(),
                    PRIMER_APELLIDO = txtPrimerApellido.Text.Trim(),
                    SEGUNDO_APELLIDO = string.IsNullOrWhiteSpace(txtSegundoApellido.Text) ? null : txtSegundoApellido.Text.Trim(),
                    EMAIL = txtEmailEdit.Text.Trim(),
                    TELEFONO = string.IsNullOrWhiteSpace(txtTelefonoEdit.Text) ? null : txtTelefonoEdit.Text.Trim(),
                    CONTRASENA = txtPassword.Password,
                   
                };

                var nuevoEmpleado = new EMPLEADO
                {
                    ID_USUARIO = nuevoUsuario.ID_USUARIO,
                    MONTO_POR_HORA = decimal.Parse(txtMontoHoraEdit.Text),
                    MONTO_POR_JORNAL = decimal.Parse(txtMontoJornalEdit.Text)
                };

               
                string resultado = _usuarioService.RegistrarEmpleado(nuevoUsuario, nuevoEmpleado);

                if (resultado == "OK")
                {
                    MessageBox.Show("Empleado creado exitosamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarEmpleados();
                    OcultarPanelEdicion();
                }
                else
                {
                    MessageBox.Show($"Error al crear empleado: {resultado}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear empleado: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarEmpleado()
        {
            try
            {
                
                var usuario = _usuarioService.ObtenerPorId(_empleadoSeleccionado.IdUsuario);

                if (usuario == null)
                {
                    MessageBox.Show("No se encontró el usuario.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                usuario.PRIMER_NOMBRE = txtPrimerNombre.Text.Trim();
                usuario.SEGUNDO_NOMBRE = string.IsNullOrWhiteSpace(txtSegundoNombre.Text) ? null : txtSegundoNombre.Text.Trim();
                usuario.PRIMER_APELLIDO = txtPrimerApellido.Text.Trim();
                usuario.SEGUNDO_APELLIDO = string.IsNullOrWhiteSpace(txtSegundoApellido.Text) ? null : txtSegundoApellido.Text.Trim();
                usuario.EMAIL = txtEmailEdit.Text.Trim();
                usuario.TELEFONO = string.IsNullOrWhiteSpace(txtTelefonoEdit.Text) ? null : txtTelefonoEdit.Text.Trim();

               
                if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    usuario.CONTRASENA = txtPassword.Password;
                }

               
                bool usuarioActualizado = _usuarioService.Actualizar(usuario);

                if (usuarioActualizado)
                {
                    
                    var empleado = _empleadoService.ObtenerPorId(_empleadoSeleccionado.IdUsuario);

                    if (empleado == null)
                    {
                        MessageBox.Show("No se encontró el empleado.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    empleado.MONTO_POR_HORA = decimal.Parse(txtMontoHoraEdit.Text);
                    empleado.MONTO_POR_JORNAL = decimal.Parse(txtMontoJornalEdit.Text);

                    bool empleadoActualizado = _empleadoService.Actualizar(empleado);

                    if (empleadoActualizado)
                    {
                        MessageBox.Show("Empleado actualizado exitosamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        CargarEmpleados();
                        OcultarPanelEdicion();
                    }
                    else
                    {
                        MessageBox.Show("Usuario actualizado pero error al actualizar datos del empleado.", "Advertencia",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Error al actualizar usuario.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelarEdicion_Click(object sender, RoutedEventArgs e)
        {
            OcultarPanelEdicion();
        }

        private bool ValidarFormulario()
        {
            
            if (!_modoEdicion && string.IsNullOrWhiteSpace(txtCedula.Text))
            {
                MessageBox.Show("La cédula es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCedula.Focus();
                return false;
            }

            if (!_modoEdicion && !int.TryParse(txtCedula.Text, out int cedula))
            {
                MessageBox.Show("La cédula debe ser un número válido.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCedula.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPrimerNombre.Text))
            {
                MessageBox.Show("El primer nombre es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrimerNombre.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPrimerApellido.Text))
            {
                MessageBox.Show("El primer apellido es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrimerApellido.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEmailEdit.Text))
            {
                MessageBox.Show("El email es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmailEdit.Focus();
                return false;
            }

            if (!IsValidEmail(txtEmailEdit.Text))
            {
                MessageBox.Show("El formato del email no es válido.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmailEdit.Focus();
                return false;
            }

            if (!_modoEdicion && string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("La contraseña es obligatoria para nuevos empleados.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtMontoHoraEdit.Text))
            {
                MessageBox.Show("El monto por hora es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMontoHoraEdit.Focus();
                return false;
            }

            if (!decimal.TryParse(txtMontoHoraEdit.Text, out decimal montoHora) || montoHora < 0)
            {
                MessageBox.Show("El monto por hora debe ser un número válido mayor o igual a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMontoHoraEdit.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtMontoJornalEdit.Text))
            {
                MessageBox.Show("El monto por jornal es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMontoJornalEdit.Focus();
                return false;
            }

            if (!decimal.TryParse(txtMontoJornalEdit.Text, out decimal montoJornal) || montoJornal < 0)
            {
                MessageBox.Show("El monto por jornal debe ser un número válido mayor o igual a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMontoJornalEdit.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private void ValidarNumeros(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
            string newText = (sender as TextBox).Text + e.Text;
            e.Handled = !regex.IsMatch(newText);
        }

        private void MostrarPanelEdicion()
        {
            panelEdicion.Visibility = Visibility.Visible;
        }

        private void OcultarPanelEdicion()
        {
            panelEdicion.Visibility = Visibility.Collapsed;
            LimpiarFormulario();
            _empleadoSeleccionado = null;
        }

        private void LimpiarFormulario()
        {
            txtCedula.Clear();
            txtPrimerNombre.Clear();
            txtSegundoNombre.Clear();
            txtPrimerApellido.Clear();
            txtSegundoApellido.Clear();
            txtEmailEdit.Clear();
            txtTelefonoEdit.Clear();
            txtPassword.Clear();
            txtMontoHoraEdit.Clear();
            txtMontoJornalEdit.Clear();
        }

        private void CargarDatosEnFormulario(EmpleadoViewModel emp)
        {
            try
            {
                // Obtener datos completos del usuario
                var usuario = _usuarioService.ObtenerPorId(emp.IdUsuario);

                if (usuario != null)
                {
                    // En modo edición, la cédula no se puede cambiar
                    txtCedula.Text = usuario.ID_USUARIO.ToString();
                    txtCedula.IsEnabled = false; // Deshabilitar en modo edición

                    txtPrimerNombre.Text = usuario.PRIMER_NOMBRE ?? "";
                    txtSegundoNombre.Text = usuario.SEGUNDO_NOMBRE ?? "";
                    txtPrimerApellido.Text = usuario.PRIMER_APELLIDO ?? "";
                    txtSegundoApellido.Text = usuario.SEGUNDO_APELLIDO ?? "";
                    txtEmailEdit.Text = usuario.EMAIL ?? "";
                    txtTelefonoEdit.Text = usuario.TELEFONO ?? "";
                }

                txtMontoHoraEdit.Text = emp.MontoPorHora.ToString("F2");
                txtMontoJornalEdit.Text = emp.MontoPorJornal.ToString("F2");
                txtPassword.Clear(); // No mostramos la contraseña actual
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class EmpleadoViewModel
        {
            public int IdUsuario { get; set; }
            public string NombreCompleto { get; set; }
            public string Email { get; set; }
            public string Telefono { get; set; }
            public decimal MontoPorHora { get; set; }
            public decimal MontoPorJornal { get; set; }
            public string MontoHoraFormatted { get; set; }
            public string MontoJornalFormatted { get; set; }
        }
    }
}
