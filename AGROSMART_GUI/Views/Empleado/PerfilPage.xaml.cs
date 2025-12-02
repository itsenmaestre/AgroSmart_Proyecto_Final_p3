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

namespace AGROSMART_GUI.Views.Empleado
{
    /// <summary>
    /// Lógica de interacción para PerfilPage.xaml
    /// </summary>
    public partial class PerfilPage : Page
    {
        private readonly UsuarioService _usuarioService = new UsuarioService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly int _idEmpleado;

        public PerfilPage(int idEmpleado)
        {
            InitializeComponent();
            _idEmpleado = idEmpleado;
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                // Obtener datos del usuario
                USUARIO usuario = _usuarioService.ObtenerPorId(_idEmpleado);
                if (usuario != null)
                {
                    // Nombre completo
                    string nombreCompleto = string.Join(" ", new[]
                    {
                        usuario.PRIMER_NOMBRE,
                        usuario.SEGUNDO_NOMBRE,
                        usuario.PRIMER_APELLIDO,
                        usuario.SEGUNDO_APELLIDO
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    txtNombreCompleto.Text = nombreCompleto;
                    txtId.Text = usuario.ID_USUARIO.ToString();
                    txtEmail.Text = usuario.EMAIL ?? "No registrado";

                    // CORRECCIÓN: Verificar y mostrar el teléfono correctamente
                    if (!string.IsNullOrWhiteSpace(usuario.TELEFONO))
                    {
                        // Si TELEFONO es string
                        txtTelefono.Text = usuario.TELEFONO;
                    }
                    else
                    {
                        txtTelefono.Text = "No registrado";
                    }
                }
                else
                {
                    MostrarError("No se encontró información del usuario.");
                    return;
                }

                // Obtener datos del empleado (tarifas)
                EMPLEADO empleado = _empleadoService.ObtenerPorId(_idEmpleado);
                if (empleado != null)
                {
                    txtMontoHora.Text = empleado.MONTO_POR_HORA > 0
                        ? $"${empleado.MONTO_POR_HORA:N2}"
                        : "No asignado";

                    txtMontoJornal.Text = empleado.MONTO_POR_JORNAL > 0
                        ? $"${empleado.MONTO_POR_JORNAL:N2}"
                        : "No asignado";
                }
                else
                {
                    txtMontoHora.Text = "No disponible";
                    txtMontoJornal.Text = "No disponible";
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar el perfil: {ex.Message}");
            }
        }

        private void MostrarError(string mensaje)
        {
            MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Mostrar valores por defecto
            txtNombreCompleto.Text = "Error al cargar";
            txtId.Text = "-";
            txtEmail.Text = "-";
            txtTelefono.Text = "-";
            txtMontoHora.Text = "-";
            txtMontoJornal.Text = "-";
        }
    }
}
