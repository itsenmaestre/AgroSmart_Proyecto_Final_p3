using AGROSMART_GUI.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace AGROSMART_GUI.Views
{
    public partial class ChatBotPage : Page
    {
        private readonly ChatBotService _chatService;
        private Border _indicadorEscribiendo;

        public ChatBotPage()
        {
            InitializeComponent();
            _chatService = new ChatBotService();
            MostrarMensajeBienvenida();
        }

        #region Eventos de UI

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Funcionalidad opcional para Page
            // Si el Page está dentro de un Window, puedes mover la ventana padre
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null && e.ChangedButton == MouseButton.Left)
            {
                parentWindow.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Opción 1: Navegar hacia atrás si hay historial
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            // Opción 2: Cerrar la ventana contenedora
            else
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.Close();
                }
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await EnviarMensaje();
        }

        private async void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await EnviarMensaje();
            }
        }

        private async void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string mensaje)
            {
                txtInput.Text = mensaje;
                await EnviarMensaje();
            }
        }

        #endregion

        #region Gestión de Mensajes

        private void MostrarMensajeBienvenida()
        {
            AgregarMensajeBot(
                "¡Hola! 👋 Soy AgroBot, tu asistente virtual.\n\n" +
                "Puedo ayudarte con:\n" +
                "📊 Información de cultivos\n" +
                "💰 Análisis de gastos\n" +
                "✅ Gestión de tareas\n" +
                "👥 Datos de empleados\n\n" +
                "¿En qué puedo ayudarte?");
        }

        private async Task EnviarMensaje()
        {
            string mensaje = txtInput.Text.Trim();

            if (string.IsNullOrEmpty(mensaje))
                return;

            // Mostrar mensaje del usuario
            AgregarMensajeUsuario(mensaje);
            txtInput.Clear();
            txtInput.Focus();

            // Mostrar indicador de escritura
            MostrarEscribiendo();

            try
            {
                // Procesar mensaje
                await Task.Delay(500); // Simular tiempo de procesamiento
                string respuesta = await _chatService.ProcesarConsulta(mensaje);

                // Ocultar indicador y mostrar respuesta
                OcultarEscribiendo();
                AgregarMensajeBot(respuesta);
            }
            catch (Exception ex)
            {
                OcultarEscribiendo();
                AgregarMensajeBot($"❌ Error: {ex.Message}");
            }
        }

        #endregion

        #region UI - Mensajes

        private void AgregarMensajeUsuario(string mensaje)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A859")),
                CornerRadius = new CornerRadius(18, 18, 3, 18),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(50, 5, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 300
            };

            var textBlock = new TextBlock
            {
                Text = mensaje,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                FontSize = 14,
                LineHeight = 20
            };

            border.Child = textBlock;
            spMessages.Children.Add(border);

            AnimarEntrada(border);
            ScrollToBottom();
        }

        private void AgregarMensajeBot(string mensaje)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3, 18, 18, 18),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(5, 5, 50, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 300
            };

            var textBlock = new TextBlock
            {
                Text = mensaje,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")),
                FontSize = 14,
                LineHeight = 20
            };

            border.Child = textBlock;
            spMessages.Children.Add(border);

            AnimarEntrada(border);
            ScrollToBottom();
        }

        private void MostrarEscribiendo()
        {
            _indicadorEscribiendo = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(5, 5, 50, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 60,
                Height = 40
            };

            var textBlock = new TextBlock
            {
                Text = "...",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _indicadorEscribiendo.Child = textBlock;
            spMessages.Children.Add(_indicadorEscribiendo);

            // Animar los puntos suspensivos
            AnimarPuntosEscribiendo(textBlock);
            ScrollToBottom();
        }

        private void AnimarPuntosEscribiendo(TextBlock textBlock)
        {
            var storyboard = new System.Windows.Media.Animation.Storyboard();
            var animation = new System.Windows.Media.Animation.StringAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever
            };

            animation.KeyFrames.Add(new DiscreteStringKeyFrame(".", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            animation.KeyFrames.Add(new DiscreteStringKeyFrame("..", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
            animation.KeyFrames.Add(new DiscreteStringKeyFrame("...", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));

            Storyboard.SetTarget(animation, textBlock);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TextBlock.TextProperty));

            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void OcultarEscribiendo()
        {
            if (_indicadorEscribiendo != null)
            {
                spMessages.Children.Remove(_indicadorEscribiendo);
                _indicadorEscribiendo = null;
            }
        }

        private void AnimarEntrada(UIElement elemento)
        {
            var opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var translateAnimation = new DoubleAnimation
            {
                From = 10,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var transform = new TranslateTransform();
            elemento.RenderTransform = transform;

            elemento.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
            transform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
        }

        private void ScrollToBottom()
        {
            scrollViewer.ScrollToEnd();
        }

        #endregion

        #region Métodos Públicos (Opcional)

        /// <summary>
        /// Limpia todo el historial de conversación
        /// </summary>
        public void LimpiarChat()
        {
            spMessages.Children.Clear();
            MostrarMensajeBienvenida();
        }

        /// <summary>
        /// Envía un mensaje programáticamente
        /// </summary>
        public async Task EnviarMensajeProgramatico(string mensaje)
        {
            txtInput.Text = mensaje;
            await EnviarMensaje();
        }

        #endregion
    }
}