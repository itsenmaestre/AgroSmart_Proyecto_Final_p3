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

    /// <summary>
    /// Lógica de interacción para ProgresoPage.xaml
    /// </summary>
   

   namespace AGROSMART_GUI.Views.Empleado
    {
        public partial class ProgresoPage : Page
        {
            private readonly AsignacionTareaService _asigService = new AsignacionTareaService();
            private readonly TareaService _tareaService = new TareaService();
            private readonly int _idEmpleado;

            private class TareaItem
            {
                public int Codigo { get; set; }
                public int IdTarea { get; set; }
                public string Estado { get; set; }
                public string FechaProgramada { get; set; }
                public string HorasAcumuladas { get; set; }
            }

            public ProgresoPage(int idEmpleado)
            {
                InitializeComponent();
                _idEmpleado = idEmpleado;
                CargarTareasPendientes();
            }

            private void CargarTareasPendientes()
            {
                try
                {
                    var asignaciones = _asigService.ListarPorEmpleado(_idEmpleado);
                    List<TareaItem> items = new List<TareaItem>();

                    foreach (var a in asignaciones)
                    {
                       
                        if (!string.Equals(a.ESTADO, "FINALIZADA", StringComparison.OrdinalIgnoreCase))
                        {
                            var fecha = _tareaService.ObtenerFechaProgramada(a.ID_TAREA);
                            items.Add(new TareaItem
                            {
                                Codigo = a.ID_TAREA,
                                IdTarea = a.ID_TAREA,
                                Estado = a.ESTADO,
                                FechaProgramada = fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy") : "-",
                                HorasAcumuladas = a.HORAS_TRABAJADAS.HasValue ? a.HORAS_TRABAJADAS.Value.ToString("0.##") : "0"
                            });
                        }
                    }

                    
                    lstTareasPendientes.ItemsSource = items;

                   
                    ActualizarEstadisticas(items);

                    
                    if (items.Count == 0)
                    {
                        lstTareasPendientes.Visibility = Visibility.Collapsed;
                        pnlSinTareas.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        lstTareasPendientes.Visibility = Visibility.Visible;
                        pnlSinTareas.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar tareas: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private void ActualizarEstadisticas(List<TareaItem> items)
            {
                try
                {
                  
                    TxtTotalTareas.Text = items.Count.ToString();

                    
                    int enProceso = items.Count(t => t.Estado.Equals("EN_PROCESO", StringComparison.OrdinalIgnoreCase));
                    int pendientes = items.Count(t => t.Estado.Equals("PENDIENTE", StringComparison.OrdinalIgnoreCase));

                    TxtEnProceso.Text = enProceso.ToString();
                    TxtPendientes.Text = pendientes.ToString();
                }
                catch (Exception ex)
                {
                    
                    System.Diagnostics.Debug.WriteLine($"Error en estadísticas: {ex.Message}");
                }
            }

            private void BtnRegistrarAvance_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    Button btn = sender as Button;
                    if (btn == null) return;

                    if (!int.TryParse(Convert.ToString(btn.Tag), out int idTarea))
                    {
                        MessageBox.Show("No se pudo identificar la tarea.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Abrir ventana de registro
                    RegistrarAvanceView ventana = new RegistrarAvanceView(idTarea, _idEmpleado);
                    bool? resultado = ventana.ShowDialog();

                    if (resultado == true)
                    {
                        // Recargar la lista
                        CargarTareasPendientes();
                        MessageBox.Show("Avance registrado exitosamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
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

