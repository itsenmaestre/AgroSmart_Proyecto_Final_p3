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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AGROSMART_GUI.Views.Shared
{
    /// <summary>
    /// Lógica de interacción para BienvenidaPage.xaml
    /// </summary>
    public partial class BienvenidaPage : Window
    {
        private readonly int _idUsuario;
        private readonly string _nombreCompleto;
        private readonly bool _esAdmin;
        private readonly DispatcherTimer timer;

        public BienvenidaPage(int idUsuario, string nombreCompleto, bool esAdmin)
        {
            InitializeComponent();
            // animaciones
            (Resources["LogoPulse"] as Storyboard)?.Begin();
            (Resources["BorderRotate"] as Storyboard)?.Begin();
            (Resources["TitleFadeIn"] as Storyboard)?.Begin();
            (Resources["GlowPulse"] as Storyboard)?.Begin();
            (Resources["Particle1Anim"] as Storyboard)?.Begin();
            (Resources["Particle2Anim"] as Storyboard)?.Begin();
            (Resources["Particle3Anim"] as Storyboard)?.Begin();
            (Resources["Dot1Anim"] as Storyboard)?.Begin();
            (Resources["Dot2Anim"] as Storyboard)?.Begin();
            (Resources["Dot3Anim"] as Storyboard)?.Begin();

            this.WindowState = WindowState.Normal;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _idUsuario = idUsuario;
            _nombreCompleto = nombreCompleto;
            _esAdmin = esAdmin;

            
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.CanResize;

           
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3); 
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            if (_esAdmin)
            {
                var adminWindow = new AdminView(_idUsuario, _nombreCompleto);
                adminWindow.Show();
            }
            else
            {
                var empleadoWindow = new EmpleadoView(_idUsuario, _nombreCompleto);
                empleadoWindow.Show();
            }

            this.Close();
        }
       
    }
}
