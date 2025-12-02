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
    /// Lógica de interacción para AsignarEmpleadosPage.xaml
    /// </summary>
    public partial class AsignarEmpleadosPage : Page
    {
        private readonly AsignacionTareaService _asigService = new AsignacionTareaService();
        private readonly TareaService _tareaService = new TareaService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly int _idAdmin;

        // Variables para paginación
        private List<AsignacionViewModel> _todasLasAsignaciones = new List<AsignacionViewModel>();
        private int _paginaActual = 1;
        private int _registrosPorPagina = 5;
        private string _estadoFiltro = "TODOS";

        public AsignarEmpleadosPage(int idAdmin)
        {
            InitializeComponent();
            _idAdmin = idAdmin;

            // Establecer fecha por defecto
            dpFechaAsignacion.SelectedDate = DateTime.Today;

            CargarDatos();
            CargarAsignaciones();
        }

        private void CargarDatos()
        {
            try
            {
                var tareas = _tareaService.Consultar();
                var tareasVM = tareas.Select(t => new
                {
                    IdTarea = t.ID_TAREA,
                    Display = $"#{t.ID_TAREA} - {t.TIPO_ACTIVIDAD} ({t.ESTADO})"
                }).ToList();
                cboTarea.ItemsSource = tareasVM;

                var empleados = _empleadoService.ListarEmpleadosConUsuario();
                var empleadosVM = empleados.Select(e => new
                {
                    IdUsuario = e.IdUsuario,
                    Display = $"{e.NombreCompleto} - ${e.MontoPorHora:N2}/h"
                }).ToList();
                cboEmpleado.ItemsSource = empleadosVM;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboTarea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboTarea.SelectedValue != null)
            {
                try
                {
                    int idTarea = Convert.ToInt32(cboTarea.SelectedValue);
                    var tarea = _tareaService.ObtenerPorId(idTarea);

                    if (tarea != null)
                    {
                        txtDetallesTarea.Text = $"Actividad: {tarea.TIPO_ACTIVIDAD}\n" +
                                              $"Fecha programada: {tarea.FECHA_PROGRAMADA:dd/MM/yyyy}\n" +
                                              $"Tiempo estimado: {tarea.TIEMPO_TOTAL_TAREA:N2} horas\n" +
                                              $"Estado actual: {tarea.ESTADO}";
                    }
                }
                catch (Exception)
                {
                    txtDetallesTarea.Text = "Error al cargar detalles de la tarea";
                }
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            cboTarea.SelectedIndex = -1;
            cboEmpleado.SelectedIndex = -1;
            dpFechaAsignacion.SelectedDate = DateTime.Today;
            txtPagoAcordado.Text = "0.00";
            txtDetallesTarea.Text = "Selecciona una tarea para ver sus detalles";
        }

        private void BtnAsignar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones
                if (cboTarea.SelectedValue == null)
                {
                    MessageBox.Show("Debe seleccionar una tarea.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cboTarea.Focus();
                    return;
                }

                if (cboEmpleado.SelectedValue == null)
                {
                    MessageBox.Show("Debe seleccionar un empleado.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cboEmpleado.Focus();
                    return;
                }

                if (!dpFechaAsignacion.SelectedDate.HasValue)
                {
                    MessageBox.Show("Debe seleccionar la fecha de asignación.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    dpFechaAsignacion.Focus();
                    return;
                }

                int idTarea = Convert.ToInt32(cboTarea.SelectedValue);
                int idEmpleado = Convert.ToInt32(cboEmpleado.SelectedValue);

                decimal pagoAcordado = 0;
                if (!string.IsNullOrWhiteSpace(txtPagoAcordado.Text))
                {
                    string pagoTexto = txtPagoAcordado.Text.Replace(",", ".");
                    if (!decimal.TryParse(pagoTexto,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out pagoAcordado) || pagoAcordado < 0)
                    {
                        MessageBox.Show("El pago acordado debe ser un número válido mayor o igual a 0.", "Validación",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPagoAcordado.Focus();
                        return;
                    }
                }

                var asignacion = new ASIGNACION_TAREA
                {
                    ID_TAREA = idTarea,
                    ID_EMPLEADO = idEmpleado,
                    ID_ADMIN_ASIGNADOR = _idAdmin,
                    FECHA_ASIGNACION = dpFechaAsignacion.SelectedDate.Value.Date,
                    ESTADO = "PENDIENTE",
                    PAGO_ACORDADO = pagoAcordado > 0 ? pagoAcordado : (decimal?)null
                };

                string resultado = _asigService.Asignar(asignacion);

                if (resultado == "OK")
                {
                    MessageBox.Show("Tarea asignada exitosamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    BtnLimpiar_Click(sender, e);
                    CargarAsignaciones();
                }
                else
                {
                    MessageBox.Show($"Error al asignar: {resultado}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nDetalles: {ex.StackTrace}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarAsignaciones()
        {
            try
            {
                var asignaciones = _asigService.ListarTodas();
                var empleadosInfo = _empleadoService.ListarEmpleadosConUsuario();

                _todasLasAsignaciones = asignaciones.Select(a =>
                {
                    var empleado = empleadosInfo.FirstOrDefault(e => e.IdUsuario == a.ID_EMPLEADO);
                    var tarea = _tareaService.ObtenerPorId(a.ID_TAREA);

                    return new AsignacionViewModel
                    {
                        IdAsignacion = a.ID_ASIG_TAREA,
                        NombreTarea = tarea != null ? $"#{a.ID_TAREA} - {tarea.TIPO_ACTIVIDAD}" : $"Tarea #{a.ID_TAREA}",
                        NombreEmpleado = empleado?.NombreCompleto ?? "Empleado desconocido",
                        FechaAsignacion = a.FECHA_ASIGNACION.Date,
                        Estado = a.ESTADO,
                        PagoAcordado = a.PAGO_ACORDADO ?? 0
                    };
                }).OrderByDescending(x => x.IdAsignacion).ToList();

                // Resetear a la primera página cuando se recarga
                _paginaActual = 1;

                // Aplicar filtro y paginación
                AplicarFiltrosYPaginacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar asignaciones: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltrosYPaginacion()
        {
            // 🔹 Verificar que los controles estén inicializados
            if (dgAsignaciones == null || txtConteoAsignaciones == null)
                return;

            // Aplicar filtro por estado
            var asignacionesFiltradas = _todasLasAsignaciones;

            if (_estadoFiltro != "TODOS")
            {
                asignacionesFiltradas = _todasLasAsignaciones
                    .Where(a => a.Estado == _estadoFiltro)
                    .ToList();
            }

            // Actualizar conteo total
            txtConteoAsignaciones.Text = $"Total: {asignacionesFiltradas.Count} asignaciones";

            // Calcular paginación
            int totalRegistros = asignacionesFiltradas.Count;
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / _registrosPorPagina);

            if (totalPaginas == 0) totalPaginas = 1;
            if (_paginaActual > totalPaginas) _paginaActual = totalPaginas;

            // Obtener registros de la página actual
            var asignacionesPaginadas = asignacionesFiltradas
                .Skip((_paginaActual - 1) * _registrosPorPagina)
                .Take(_registrosPorPagina)
                .ToList();

            // Actualizar DataGrid
            dgAsignaciones.ItemsSource = asignacionesPaginadas;

            // Actualizar controles de paginación
            ActualizarControlesPaginacion(totalRegistros, totalPaginas);
        }

        private void ActualizarControlesPaginacion(int totalRegistros, int totalPaginas)
        {
            // 🔹 Verificar que los controles estén inicializados
            if (txtPaginaActual == null || txtTotalPaginas == null || txtPaginaInfo == null)
                return;

            // Actualizar textos
            txtPaginaActual.Text = _paginaActual.ToString();
            txtTotalPaginas.Text = totalPaginas.ToString();

            int registroInicio = totalRegistros > 0 ? ((_paginaActual - 1) * _registrosPorPagina) + 1 : 0;
            int registroFin = Math.Min(_paginaActual * _registrosPorPagina, totalRegistros);
            txtPaginaInfo.Text = $"Mostrando {registroInicio}-{registroFin} de {totalRegistros}";

            // Habilitar/deshabilitar botones
            if (btnPrimeraPagina != null) btnPrimeraPagina.IsEnabled = _paginaActual > 1;
            if (btnPaginaAnterior != null) btnPaginaAnterior.IsEnabled = _paginaActual > 1;
            if (btnPaginaSiguiente != null) btnPaginaSiguiente.IsEnabled = _paginaActual < totalPaginas;
            if (btnUltimaPagina != null) btnUltimaPagina.IsEnabled = _paginaActual < totalPaginas;
        }

        private void BtnPrimeraPagina_Click(object sender, RoutedEventArgs e)
        {
            _paginaActual = 1;
            AplicarFiltrosYPaginacion();
        }

        private void BtnPaginaAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1)
            {
                _paginaActual--;
                AplicarFiltrosYPaginacion();
            }
        }

        private void BtnPaginaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            int totalPaginas = (int)Math.Ceiling((double)_todasLasAsignaciones.Count / _registrosPorPagina);
            if (_paginaActual < totalPaginas)
            {
                _paginaActual++;
                AplicarFiltrosYPaginacion();
            }
        }

        private void BtnUltimaPagina_Click(object sender, RoutedEventArgs e)
        {
            int totalPaginas = (int)Math.Ceiling((double)_todasLasAsignaciones.Count / _registrosPorPagina);
            _paginaActual = totalPaginas;
            AplicarFiltrosYPaginacion();
        }

        private void CboRegistrosPorPagina_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 🔹 Verificar que el ComboBox esté inicializado y tenga un item seleccionado
            if (cboRegistrosPorPagina?.SelectedItem == null)
                return;

            string valor = (cboRegistrosPorPagina.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (int.TryParse(valor, out int registros))
            {
                _registrosPorPagina = registros;
                _paginaActual = 1; // Volver a la primera página
                AplicarFiltrosYPaginacion();
            }
        }

        private void CboFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 🔹 Verificar que el ComboBox esté inicializado y tenga un item seleccionado
            if (cboFiltroEstado?.SelectedItem == null)
                return;

            string contenido = (cboFiltroEstado.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Extraer solo el estado (después del emoji)
            if (contenido.Contains("Todos"))
            {
                _estadoFiltro = "TODOS";
            }
            else if (contenido.Contains("PENDIENTE"))
            {
                _estadoFiltro = "PENDIENTE";
            }
            else if (contenido.Contains("FINALIZADA"))
            {
                _estadoFiltro = "FINALIZADA";
            }
            else if (contenido.Contains("EN_EJECUCION"))
            {
                _estadoFiltro = "EN_EJECUCION";
            }
            else if (contenido.Contains("CANCELADA"))
            {
                _estadoFiltro = "CANCELADA";
            }

            _paginaActual = 1; // Volver a la primera página al filtrar
            AplicarFiltrosYPaginacion();
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarAsignaciones();
            MessageBox.Show("Lista actualizada correctamente.", "Información",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnVerDetalle_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is AsignacionViewModel vm)
            {
                string mensaje = $"═══════════════════════════════\n" +
                               $"📋 DETALLES DE LA ASIGNACIÓN\n" +
                               $"═══════════════════════════════\n\n" +
                               $"🆔 ID Asignación: {vm.IdAsignacion}\n\n" +
                               $"📝 Tarea: {vm.NombreTarea}\n\n" +
                               $"👤 Empleado: {vm.NombreEmpleado}\n\n" +
                               $"📅 Fecha: {vm.FechaAsignacion:dd/MM/yyyy}\n\n" +
                               $"📊 Estado: {vm.Estado}\n\n" +
                               $"💰 Pago Acordado: ${vm.PagoAcordado:N2}\n" +
                               $"═══════════════════════════════";

                MessageBox.Show(mensaje, "Detalles de la Asignación",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is AsignacionViewModel vm)
            {
                if (vm.Estado == "CANCELADA")
                {
                    MessageBox.Show("Esta asignación ya fue cancelada anteriormente.", "Información",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (vm.Estado == "FINALIZADA")
                {
                    MessageBox.Show("No se puede cancelar una asignación que ya fue finalizada.", "Advertencia",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"¿Está seguro de cancelar la asignación #{vm.IdAsignacion}?\n\n" +
                    $"Tarea: {vm.NombreTarea}\n" +
                    $"Empleado: {vm.NombreEmpleado}",
                    "Confirmar Cancelación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool cancelado = _asigService.CancelarAsignacion(vm.IdAsignacion);
                        if (cancelado)
                        {
                            MessageBox.Show("Asignación cancelada exitosamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            CargarAsignaciones();
                        }
                        else
                        {
                            MessageBox.Show("No se pudo cancelar la asignación.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public class AsignacionViewModel
        {
            public int IdAsignacion { get; set; }
            public string NombreTarea { get; set; }
            public string NombreEmpleado { get; set; }
            public DateTime FechaAsignacion { get; set; }
            public string Estado { get; set; }
            public decimal PagoAcordado { get; set; }
        }
    }
}