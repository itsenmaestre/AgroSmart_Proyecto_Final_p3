using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AGROSMART_GUI.Views.Admin
{
    public partial class CosechasPage : Page
    {
        private readonly CosechaService _cosechaService = new CosechaService();
        private readonly CultivoService _cultivoService = new CultivoService();
        private readonly EmpleadoService _empleadoService = new EmpleadoService();
        private readonly EmpleadoCosechaService _empleadoCosechaService = new EmpleadoCosechaService();

        private readonly int _idAdmin;
        private int _idCosechaActual = 0;
        private bool _cosechaGuardada = false;

        private readonly List<EmpleadoCosechaItem> _empleadosCosecha = new List<EmpleadoCosechaItem>();

        public CosechasPage(int idAdmin)
        {
            InitializeComponent();
            _idAdmin = idAdmin;

            dpFechaInicio.SelectedDate = DateTime.Today;
            dpFechaRegistro.SelectedDate = DateTime.Today;
            dpFechaTrabajo.SelectedDate = DateTime.Today;

            CargarCultivos();
            CargarEmpleados();
            CargarCosechas();

            cboCultivo.SelectionChanged += CboCultivo_SelectionChanged;
        }

        #region Carga de combos y listas

        private void CargarCultivos()
        {
            try
            {
                var cultivos = _cultivoService.Consultar();
                var lista = cultivos.Select(c => new
                {
                    IdCultivo = c.ID_CULTIVO,
                    Display = $"#{c.ID_CULTIVO} - {c.NOMBRE_LOTE}"
                }).ToList();

                cboCultivo.ItemsSource = lista;
                cboCultivo.DisplayMemberPath = "Display";
                cboCultivo.SelectedValuePath = "IdCultivo";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar cultivos: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarEmpleados()
        {
            try
            {
                var empleados = _empleadoService.ListarEmpleadosConUsuario();
                var lista = empleados.Select(e => new
                {
                    IdEmpleado = e.IdUsuario,
                    Display = e.NombreCompleto
                }).ToList();

                cboEmpleado.ItemsSource = lista;
                cboEmpleado.DisplayMemberPath = "Display";
                cboEmpleado.SelectedValuePath = "IdEmpleado";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar empleados: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarCosechas()
        {
            try
            {
                var cosechas = _cosechaService.Consultar();

                // ✅ FILTRAR: Solo mostrar cosechas EN_PROCESO
                var cosechasActivas = cosechas
                    .Where(c => c.ESTADO == "EN_PROCESO")
                    .ToList();

                var vm = cosechasActivas.Select(c =>
                {
                    var cultivo = _cultivoService.ObtenerPorId(c.ID_CULTIVO);
                    return new CosechaViewModel
                    {
                        IdCosecha = c.ID_COSECHA,
                        NombreCultivo = cultivo != null ? cultivo.NOMBRE_LOTE : "Cultivo desconocido",
                        FechaInicio = c.FECHA_INICIO,
                        CantidadObtenida = c.CANTIDAD_OBTENIDA,
                        UnidadMedida = c.UNIDAD_MEDIDA,
                        Calidad = c.CALIDAD,
                        Estado = c.ESTADO
                    };
                }).ToList();

                dgCosechas.ItemsSource = vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar cosechas: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Lógica de detección de cosecha existente

        private void CboCultivo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VerificarCosechaActiva();
        }

        private void VerificarCosechaActiva()
        {
            try
            {
                if (cboCultivo.SelectedValue == null)
                {
                    LimpiarFormulario();
                    return;
                }

                int idCultivo = (int)cboCultivo.SelectedValue;

                // Buscar cosecha EN_PROCESO para este cultivo
                var cosechaActiva = _cosechaService.BuscarCosechaActiva(idCultivo);

                if (cosechaActiva != null)
                {
                    // HAY COSECHA ACTIVA - Cargarla
                    _idCosechaActual = cosechaActiva.ID_COSECHA;
                    _cosechaGuardada = true;

                    // Cargar datos en el formulario
                    dpFechaInicio.SelectedDate = cosechaActiva.FECHA_INICIO;
                    dpFechaRegistro.SelectedDate = cosechaActiva.FECHA_REGISTRO;
                    txtCantidad.Text = cosechaActiva.CANTIDAD_OBTENIDA.ToString("0.00");
                    cboUnidad.Text = cosechaActiva.UNIDAD_MEDIDA;

                    // Seleccionar calidad
                    foreach (ComboBoxItem item in cboCalidad.Items)
                    {
                        if (item.Content?.ToString() == cosechaActiva.CALIDAD)
                        {
                            cboCalidad.SelectedItem = item;
                            break;
                        }
                    }

                    txtObservaciones.Text = cosechaActiva.OBSERVACIONES ?? "";

                    // Cargar empleados de esta cosecha
                    CargarEmpleadosDeCosecha(_idCosechaActual);

                    // Bloquear campos que no se pueden cambiar
                    cboCultivo.IsEnabled = false;
                    dpFechaInicio.IsEnabled = false;
                    cboUnidad.IsEnabled = false;

                    // Cambiar texto del botón
                    btnGuardar.Content = "💾  Actualizar Cosecha";

                    // Verificar si está terminada
                    if (cosechaActiva.ESTADO == "TERMINADA")
                    {
                        MessageBox.Show(
                            "⚠️ Esta cosecha ya está TERMINADA.\n\n" +
                            "No se pueden agregar más empleados ni modificar datos.",
                            "Cosecha Finalizada",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        BloquearFormulario();
                    }
                }
                else
                {
                    // NO HAY COSECHA ACTIVA - Formulario vacío para crear nueva
                    _idCosechaActual = 0;
                    _cosechaGuardada = false;

                    LimpiarCamposEmpleado();
                    txtCantidad.Text = "0.00";
                    txtCantidadTotal.Text = "Total: 0.00";

                    // Desbloquear campos
                    cboCultivo.IsEnabled = true;
                    dpFechaInicio.IsEnabled = true;
                    cboUnidad.IsEnabled = true;
                    cboCalidad.IsEnabled = true;
                    txtObservaciones.IsEnabled = true;

                    btnGuardar.Content = "💾  Registrar Cosecha";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al verificar cosecha: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarEmpleadosDeCosecha(int idCosecha)
        {
            try
            {
                var listaBD = _empleadoCosechaService.ObtenerPorCosecha(idCosecha);
                _empleadosCosecha.Clear();

                foreach (var emp in listaBD)
                {
                    string nombre = "Empleado";
                    foreach (var item in cboEmpleado.Items)
                    {
                        dynamic x = item;
                        if (x.IdEmpleado == emp.ID_EMPLEADO)
                        {
                            nombre = x.Display;
                            break;
                        }
                    }

                    _empleadosCosecha.Add(new EmpleadoCosechaItem
                    {
                        IdEmpleado = emp.ID_EMPLEADO,
                        NombreEmpleado = nombre,
                        CantidadCosechada = emp.CANTIDAD_COSECHADA,
                        FechaTrabajo = emp.FECHA_TRABAJO,
                        ValorUnitario = emp.VALOR_UNITARIO,
                        Deducciones = emp.DEDUCCIONES,
                        Observaciones = emp.OBSERVACIONES ?? ""
                    });
                }

                dgEmpleados.ItemsSource = null;
                dgEmpleados.ItemsSource = _empleadosCosecha;
                ActualizarCantidadTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar empleados: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Guardar Cosecha

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones básicas
                if (cboCultivo.SelectedValue == null)
                {
                    MessageBox.Show("Seleccione un cultivo.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpFechaInicio.SelectedDate.HasValue)
                {
                    MessageBox.Show("Seleccione la fecha de inicio de la cosecha.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpFechaRegistro.SelectedDate.HasValue)
                {
                    MessageBox.Show("Seleccione la fecha de registro.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(cboUnidad.Text))
                {
                    MessageBox.Show("Seleccione la unidad de medida.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ComboBoxItem calidadItem = cboCalidad.SelectedItem as ComboBoxItem;
                if (calidadItem == null)
                {
                    MessageBox.Show("Seleccione la calidad.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal total = _empleadosCosecha.Sum(x => x.CantidadCosechada);

                COSECHA cosecha = new COSECHA
                {
                    ID_CULTIVO = (int)cboCultivo.SelectedValue,
                    ID_ADMIN_REGISTRO = _idAdmin,
                    FECHA_INICIO = dpFechaInicio.SelectedDate.Value,
                    FECHA_REGISTRO = dpFechaRegistro.SelectedDate.Value,
                    CANTIDAD_OBTENIDA = total,
                    UNIDAD_MEDIDA = cboUnidad.Text,
                    CALIDAD = calidadItem.Content.ToString(),
                    OBSERVACIONES = txtObservaciones.Text
                };

                if (_idCosechaActual > 0 && _cosechaGuardada)
                {
                    // ACTUALIZAR COSECHA EXISTENTE
                    cosecha.ID_COSECHA = _idCosechaActual;
                    bool ok = _cosechaService.Actualizar(cosecha);

                    if (!ok)
                    {
                        MessageBox.Show("No se pudo actualizar la cosecha.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    MessageBox.Show("✅ Cosecha actualizada correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // CREAR NUEVA COSECHA
                    string resultado = _cosechaService.Guardar(cosecha);
                    _idCosechaActual = Convert.ToInt32(resultado);
                    _cosechaGuardada = true;

                    MessageBox.Show(
                        "✅ Cosecha registrada correctamente.\n\n" +
                        $"ID de Cosecha: {_idCosechaActual}\n" +
                        "Ahora puede agregar empleados día a día.",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Bloquear campos que no se pueden cambiar
                    cboCultivo.IsEnabled = false;
                    dpFechaInicio.IsEnabled = false;
                    cboUnidad.IsEnabled = false;

                    btnGuardar.Content = "💾  Actualizar Cosecha";
                }

                CargarCosechas();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"⚠️ {ex.Message}", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Agregar Empleados

        private void BtnAgregarEmpleado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // VALIDACIÓN CRÍTICA: Debe haber cosecha guardada
                if (_idCosechaActual == 0 || !_cosechaGuardada)
                {
                    MessageBox.Show(
                        "⚠️ Primero debe GUARDAR la cosecha antes de agregar empleados.\n\n" +
                        "Complete los datos de la cosecha y presione 'Registrar Cosecha'.",
                        "Cosecha no guardada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Validar que la cosecha esté EN_PROCESO
                var validacion = _cosechaService.ValidarAgregarEmpleado(_idCosechaActual);
                if (!validacion.valido)
                {
                    MessageBox.Show($"⚠️ {validacion.mensaje}", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validaciones del empleado
                if (cboEmpleado.SelectedValue == null)
                {
                    MessageBox.Show("Seleccione un empleado.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtCantidadEmpleado.Text, out decimal cantidad) || cantidad <= 0)
                {
                    MessageBox.Show("La cantidad debe ser un número mayor a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpFechaTrabajo.SelectedDate.HasValue)
                {
                    MessageBox.Show("Seleccione la fecha de trabajo.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtValorUnitario.Text, out decimal valor) || valor < 0)
                {
                    MessageBox.Show("El valor unitario debe ser un número mayor o igual a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar deducciones
                if (!decimal.TryParse(txtDeducciones.Text, out decimal deducciones) || deducciones < 0)
                {
                    MessageBox.Show("Las deducciones deben ser un número mayor o igual a 0.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar porcentaje de deducción
                if (!decimal.TryParse(txtPorcentajeDeduccion.Text, out decimal porcentaje) || porcentaje < 0 || porcentaje > 100)
                {
                    MessageBox.Show("El porcentaje de deducción debe estar entre 0 y 100.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int idEmpleado = (int)cboEmpleado.SelectedValue;
                DateTime fechaTrabajo = dpFechaTrabajo.SelectedDate.Value.Date;

                // Validar que la fecha de trabajo sea >= fecha de inicio de la cosecha
                if (fechaTrabajo < dpFechaInicio.SelectedDate.Value.Date)
                {
                    MessageBox.Show(
                        $"⚠️ La fecha de trabajo no puede ser anterior al inicio de la cosecha.\n\n" +
                        $"Fecha inicio cosecha: {dpFechaInicio.SelectedDate.Value:dd/MM/yyyy}\n" +
                        $"Fecha de trabajo: {fechaTrabajo:dd/MM/yyyy}",
                        "Fecha inválida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Verificar si ya existe registro para ese empleado en esa fecha
                var existente = _empleadosCosecha.FirstOrDefault(x =>
                    x.IdEmpleado == idEmpleado &&
                    x.FechaTrabajo.Date == fechaTrabajo);

                if (existente != null)
                {
                    var resultado = MessageBox.Show(
                        $"⚠️ El empleado '{existente.NombreEmpleado}' ya tiene un registro para el día {fechaTrabajo:dd/MM/yyyy}.\n\n" +
                        $"Cantidad actual: {existente.CantidadCosechada:N2}\n" +
                        $"Nueva cantidad: {cantidad:N2}\n\n" +
                        "¿Desea SUMAR las cantidades?\n" +
                        $"• Sí = Se sumará (total: {existente.CantidadCosechada + cantidad:N2})\n" +
                        "• No = Se reemplazará",
                        "Registro duplicado",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Cancel)
                        return;

                    if (resultado == MessageBoxResult.Yes)
                    {
                        // SUMAR cantidades
                        existente.CantidadCosechada += cantidad;
                    }
                    else
                    {
                        // REEMPLAZAR cantidad
                        existente.CantidadCosechada = cantidad;
                    }

                    existente.ValorUnitario = valor;
                    existente.Observaciones = txtObservacionesEmpleado.Text ?? "";

                    // Actualizar en BD
                    GuardarEmpleadoEnBD(existente);
                }
                else
                {
                    // NUEVO REGISTRO
                    dynamic empSel = cboEmpleado.SelectedItem;
                    // Calcular deducciones
                    decimal deduccionFinal = deducciones;
                    if (deduccionFinal == 0)
                    {
                        // Si no hay deducción manual, calcular por porcentaje
                        decimal bruto = cantidad * valor;
                        deduccionFinal = bruto * (porcentaje / 100m);
                    }

                    var item = new EmpleadoCosechaItem
                    {
                        IdEmpleado = idEmpleado,
                        NombreEmpleado = empSel.Display,
                        CantidadCosechada = cantidad,
                        ValorUnitario = valor,
                        FechaTrabajo = fechaTrabajo,
                        Observaciones = txtObservacionesEmpleado.Text ?? "",
                        Deducciones = deduccionFinal  // ← NUEVO
                    };

                    _empleadosCosecha.Add(item);

                    // Guardar en BD
                    GuardarEmpleadoEnBD(item);
                }

                // Actualizar DataGrid
                dgEmpleados.ItemsSource = null;
                dgEmpleados.ItemsSource = _empleadosCosecha;
                ActualizarCantidadTotal();

                // Actualizar cantidad total en la cosecha
                _cosechaService.ActualizarCantidadTotal(_idCosechaActual);

                // Recargar historial
                CargarCosechas();

                LimpiarCamposEmpleado();

                MessageBox.Show("✅ Empleado agregado/actualizado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al agregar empleado: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GuardarEmpleadoEnBD(EmpleadoCosechaItem item)
        {
            var empCosecha = new EMPLEADO_COSECHA
            {
                ID_COSECHA = _idCosechaActual,
                ID_EMPLEADO = item.IdEmpleado,
                CANTIDAD_COSECHADA = item.CantidadCosechada,
                FECHA_TRABAJO = item.FechaTrabajo,
                VALOR_UNITARIO = item.ValorUnitario,
                DEDUCCIONES = item.Deducciones,
                OBSERVACIONES = item.Observaciones
            };

            _empleadoCosechaService.RegistrarTrabajo(empCosecha);
        }

        private void BtnEliminarEmpleado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fe = sender as FrameworkElement;
                if (fe?.Tag is EmpleadoCosechaItem item)
                {
                    var resultado = MessageBox.Show(
                        $"¿Está seguro de eliminar este registro?\n\n" +
                        $"Empleado: {item.NombreEmpleado}\n" +
                        $"Fecha: {item.FechaTrabajo:dd/MM/yyyy}\n" +
                        $"Cantidad: {item.CantidadCosechada:N2}",
                        "Confirmar eliminación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        _empleadosCosecha.Remove(item);

                        dgEmpleados.ItemsSource = null;
                        dgEmpleados.ItemsSource = _empleadosCosecha;

                        ActualizarCantidadTotal();

                        MessageBox.Show("✅ Empleado eliminado correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al eliminar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarCantidadTotal()
        {
            decimal total = _empleadosCosecha.Sum(x => x.CantidadCosechada);
            txtCantidad.Text = total.ToString("0.00");
            txtCantidadTotal.Text = $"Total: {total:0.00}";
        }

        private void LimpiarCamposEmpleado()
        {
            cboEmpleado.SelectedIndex = -1;
            txtCantidadEmpleado.Text = "0.00";
            txtValorUnitario.Text = "0";
            txtDeducciones.Text = "0";  // ← NUEVO
            txtPorcentajeDeduccion.Text = "5";  // ← NUEVO
            txtObservacionesEmpleado.Clear();
        }

        #endregion

        #region Limpiar Formulario

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _idCosechaActual = 0;
            _cosechaGuardada = false;
            _empleadosCosecha.Clear();

            cboCultivo.SelectedIndex = -1;
            dpFechaInicio.SelectedDate = DateTime.Today;
            dpFechaRegistro.SelectedDate = DateTime.Today;
            dpFechaTrabajo.SelectedDate = DateTime.Today;

            cboUnidad.SelectedIndex = -1;
            cboCalidad.SelectedIndex = 0;
            txtObservaciones.Clear();
            txtCantidad.Text = "0.00";
            txtCantidadTotal.Text = "Total: 0.00";

            LimpiarCamposEmpleado();

            dgEmpleados.ItemsSource = null;

            // Desbloquear campos
            cboCultivo.IsEnabled = true;
            dpFechaInicio.IsEnabled = true;
            cboUnidad.IsEnabled = true;
            cboCalidad.IsEnabled = true;
            txtObservaciones.IsEnabled = true;

            btnGuardar.Content = "💾  Registrar Cosecha";
        }

        private void BloquearFormulario()
        {
            cboCultivo.IsEnabled = false;
            dpFechaInicio.IsEnabled = false;
            cboUnidad.IsEnabled = false;
            cboCalidad.IsEnabled = false;
            txtObservaciones.IsEnabled = false;
            cboEmpleado.IsEnabled = false;
            txtCantidadEmpleado.IsEnabled = false;
            txtValorUnitario.IsEnabled = false;
            txtObservacionesEmpleado.IsEnabled = false;
            dpFechaTrabajo.IsEnabled = false;
        }

        #endregion

        #region Acciones del Historial

        private void BtnVer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fe = sender as FrameworkElement;
                if (fe?.Tag is CosechaViewModel vm)
                {
                    var cosecha = _cosechaService.ObtenerPorId(vm.IdCosecha);
                    if (cosecha == null) return;

                    var empleados = _empleadoCosechaService.ObtenerPorCosecha(vm.IdCosecha);
                    int numEmpleados = empleados.Count;
                    int diasTrabajo = empleados.Select(x => x.FECHA_TRABAJO.Date).Distinct().Count();

                    string mensaje =
                        $"📋 INFORMACIÓN DE LA COSECHA\n\n" +
                        $"ID: {vm.IdCosecha}\n" +
                        $"Cultivo: {vm.NombreCultivo}\n" +
                        $"Fecha Inicio: {vm.FechaInicio:dd/MM/yyyy}\n" +
                        $"Cantidad: {vm.CantidadObtenida:N2} {vm.UnidadMedida}\n" +
                        $"Calidad: {vm.Calidad}\n" +
                        $"Estado: {vm.Estado}\n\n" +
                        $"👥 Registros de empleados: {numEmpleados}\n" +
                        $"📅 Días trabajados: {diasTrabajo}\n\n";

                    if (cosecha.FECHA_FINALIZACION.HasValue)
                    {
                        mensaje += $"✅ Finalizada el: {cosecha.FECHA_FINALIZACION.Value:dd/MM/yyyy}\n";
                    }

                    if (!string.IsNullOrWhiteSpace(cosecha.OBSERVACIONES))
                    {
                        mensaje += $"\n📝 Observaciones:\n{cosecha.OBSERVACIONES}";
                    }

                    MessageBox.Show(mensaje, "Detalles de la Cosecha",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al ver detalles: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fe = sender as FrameworkElement;
                if (fe?.Tag is CosechaViewModel vm)
                {
                    var cosecha = _cosechaService.ObtenerPorId(vm.IdCosecha);
                    if (cosecha == null) return;

                    if (cosecha.ESTADO == "TERMINADA")
                    {
                        MessageBox.Show(
                            "⚠️ Esta cosecha está TERMINADA.\n\n" +
                            "No se puede editar una cosecha finalizada.",
                            "Cosecha Finalizada",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Cargar datos en el formulario
                    LimpiarFormulario();

                    cboCultivo.SelectedValue = cosecha.ID_CULTIVO;
                    dpFechaInicio.SelectedDate = cosecha.FECHA_INICIO;
                    dpFechaRegistro.SelectedDate = cosecha.FECHA_REGISTRO;
                    txtCantidad.Text = cosecha.CANTIDAD_OBTENIDA.ToString("0.00");
                    cboUnidad.Text = cosecha.UNIDAD_MEDIDA;

                    foreach (ComboBoxItem item in cboCalidad.Items)
                    {
                        if (item.Content?.ToString() == cosecha.CALIDAD)
                        {
                            cboCalidad.SelectedItem = item;
                            break;
                        }
                    }

                    txtObservaciones.Text = cosecha.OBSERVACIONES ?? "";

                    _idCosechaActual = cosecha.ID_COSECHA;
                    _cosechaGuardada = true;

                    CargarEmpleadosDeCosecha(_idCosechaActual);

                    // Bloquear campos que no se pueden cambiar
                    cboCultivo.IsEnabled = false;
                    dpFechaInicio.IsEnabled = false;
                    cboUnidad.IsEnabled = false;

                    btnGuardar.Content = "💾  Actualizar Cosecha";

                    MessageBox.Show(
                        "✅ Cosecha cargada para edición.\n\n" +
                        "Puede modificar la calidad, observaciones y agregar/eliminar empleados.",
                        "Editar Cosecha",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnTerminarCosecha_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fe = sender as FrameworkElement;
                if (fe?.Tag is CosechaViewModel vm)
                {
                    var cosecha = _cosechaService.ObtenerPorId(vm.IdCosecha);
                    if (cosecha == null) return;

                    if (cosecha.ESTADO == "TERMINADA")
                    {
                        MessageBox.Show("Esta cosecha ya está TERMINADA.", "Información",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var resultado = MessageBox.Show(
                        $"⚠️ ¿Está seguro de FINALIZAR esta cosecha?\n\n" +
                        $"Cultivo: {vm.NombreCultivo}\n" +
                        $"Cantidad total: {vm.CantidadObtenida:N2} {vm.UnidadMedida}\n\n" +
                        "⚠️ ADVERTENCIA:\n" +
                        "• Se marcará como TERMINADA\n" +
                        "• NO se podrán agregar más empleados\n" +
                        "• NO se podrá editar la información\n\n" +
                        "Esta acción NO se puede deshacer.",
                        "Finalizar Cosecha",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (resultado != MessageBoxResult.Yes)
                        return;

                    bool ok = _cosechaService.TerminarCosecha(vm.IdCosecha);

                    if (!ok)
                    {
                        MessageBox.Show("No se pudo finalizar la cosecha.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    MessageBox.Show(
                        "✅ Cosecha FINALIZADA correctamente.\n\n" +
                        $"Cantidad total recolectada: {vm.CantidadObtenida:N2} {vm.UnidadMedida}",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Si es la cosecha actual, limpiar formulario
                    if (_idCosechaActual == vm.IdCosecha)
                    {
                        LimpiarFormulario();
                    }

                    CargarCosechas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al finalizar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ViewModels

        public class CosechaViewModel
        {
            public int IdCosecha { get; set; }
            public string NombreCultivo { get; set; }
            public DateTime FechaInicio { get; set; }
            public decimal CantidadObtenida { get; set; }
            public string UnidadMedida { get; set; }
            public string Calidad { get; set; }
            public string Estado { get; set; }
        }

        public class EmpleadoCosechaItem
        {
            public int IdEmpleado { get; set; }
            public string NombreEmpleado { get; set; }
            public decimal CantidadCosechada { get; set; }
            public DateTime FechaTrabajo { get; set; }
            public decimal ValorUnitario { get; set; }
            public string Observaciones { get; set; }
            public decimal Deducciones { get; set; }  // ← NUEVO
        }

        #endregion
    }
}