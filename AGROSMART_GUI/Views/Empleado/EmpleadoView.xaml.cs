using AGROSMART_BLL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AGROSMART_GUI.Views.Empleado
{
    /// <summary>
    /// Lógica de interacción para EmpleadoView.xaml
    /// </summary>
    public partial class EmpleadoView : Window
    {
        private readonly int _idEmpleado;
        private readonly string _nombreEmpleado;

        public EmpleadoView(int idEmpleadoActual, string nombreCompleto = null)
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;

           
            this.WindowStyle = WindowStyle.SingleBorderWindow;

           
            this.ResizeMode = ResizeMode.CanResize;
            _idEmpleado = idEmpleadoActual;
            _nombreEmpleado = nombreCompleto;

            if (!string.IsNullOrWhiteSpace(_nombreEmpleado))
                txtUserName.Text = _nombreEmpleado;

            
            MenuListBox.SelectedIndex = 0;

           
            
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuListBox.SelectedItem is ListBoxItem item)
            {
                string tag = item.Tag?.ToString();

                switch (tag)
                {
                    case "🏠":
                        EmpleadoFrame.Navigate(new InicioEmpleadoPage(_idEmpleado, _nombreEmpleado));
                        break;
                    case "📋":
                        EmpleadoFrame.Navigate(new MisTareasPage(_idEmpleado));
                        break;
                    case "📊":
                        EmpleadoFrame.Navigate(new ProgresoPage(_idEmpleado));
                        break;
                    case "👤":
                        EmpleadoFrame.Navigate(new PerfilPage(_idEmpleado));
                        break;
                    case "❓":
                        EmpleadoFrame.Navigate(new AyudaPage());
                        break;
                }
            }
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
