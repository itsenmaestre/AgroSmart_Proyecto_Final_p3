using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using static AgroSmartBot.TelegramBotServicePDF;
namespace AgroSmartBot
{
    public class TelegramBotService
    {



        // Contexto solo para autenticación
        private readonly Dictionary<long, string> _contextoLogin = new Dictionary<long, string>();

        private readonly ITelegramBotClient _botClient;
        private readonly string _connectionString;

        // Almacena los usuarios autenticados con su ID de usuario
        private readonly Dictionary<long, string> _usuariosAutenticados = new Dictionary<long, string>();

        // ⭐ NUEVO: Almacena el tipo de rol del usuario (ADMIN o EMPLEADO)
        private readonly Dictionary<long, string> _rolesUsuarios = new Dictionary<long, string>();

        // Almacena el contexto de conversación para asignación de tareas
        private readonly Dictionary<long, string> _contextoAsignacion = new Dictionary<long, string>();

        // Credenciales de administrador
        private const string ADMIN_USERNAME = "ADMIN";
        private const string ADMIN_PASSWORD = "2309";
        private const string BOT_TOKEN = "8485672740:AAERCoqinGyQmJnYdtRExTmyojfOSBjhGgM";

        // ⭐ NUEVO: Constantes para roles
        private const string ROL_ADMIN = "ADMIN";
        private const string ROL_EMPLEADO = "EMPLEADO";
        private readonly TelegramBotServicePDF _reportesPDFService;
        public TelegramBotService(string connectionString)
        {
            _botClient = new TelegramBotClient(BOT_TOKEN);
            _connectionString = connectionString;
            // ⭐ Configurar licencia de EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            // ⭐ NUEVO: Configurar email
            var configEmail = new ConfiguracionEmail
            {
                EmailRemitente = "ericsantiago2223@gmail.com",        // ⚠️ CAMBIAR
                PasswordApp = "mtbe gjma vvsn hiyn",            // ⚠️ CAMBIAR
                EmailDestino = "yukiohatake8@gmail.com"              // ⚠️ CAMBIAR
            };

            // ⭐ NUEVO: Crear instancia del servicio
            _reportesPDFService = new TelegramBotServicePDF(connectionString, configEmail);

            // ⭐ NUEVO: Iniciar reportes automáticos
            _reportesPDFService.IniciarReportesProgramados();
        }

        public async Task IniciarBot()
        {
            using (var cts = new CancellationTokenSource())
            {
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                };

                _botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );

                var me = await _botClient.GetMeAsync();
                Console.WriteLine($"Bot iniciado: @{me.Username}");
                Console.WriteLine("Presiona Enter para detener...");
                Console.ReadLine();

                cts.Cancel();
            }
        }

        // ⭐ NUEVO: Método para verificar si un usuario es empleado
        private async Task<bool> EsEmpleado(string idUsuario)
        {
            if (idUsuario == ADMIN_USERNAME)
                return false;

            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT COUNT(*) FROM EMPLEADO WHERE ID_USUARIO = :idUsuario";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":idUsuario", OracleDbType.Varchar2).Value = idUsuario;
                        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando empleado: {ex.Message}");
                return false;
            }
        }

        // ⭐ NUEVO: Método helper para obtener el rol del usuario autenticado
        private string ObtenerRolUsuario(long chatId)
        {
            if (_rolesUsuarios.ContainsKey(chatId))
                return _rolesUsuarios[chatId];
            return null;
        }

        // ⭐ NUEVO: Método helper para verificar si el usuario actual es admin
        private bool EsAdmin(long chatId)
        {
            return ObtenerRolUsuario(chatId) == ROL_ADMIN;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message == null)
                return;

            var message = update.Message;
            if (message.Text == null)
                return;

            var messageText = message.Text;
            var chatId = message.Chat.Id;
            var userName = message.From?.FirstName ?? "Usuario";

            Console.WriteLine($"Mensaje de {userName}: {messageText}");

            // Verificar si el usuario está autenticado
            if (!_usuariosAutenticados.ContainsKey(chatId))
            {
                if (messageText.StartsWith("/"))
                {
                    if (messageText.ToLower() == "/start")
                    {
                        await SolicitarUsuario(chatId, cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "🔒 Debes autenticarte primero. Usa /start",
                            cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await ProcesarAutenticacion(chatId, messageText, cancellationToken);
                }
                return;
            }

            // Verificar si está en proceso de asignación de tarea
            if (_contextoAsignacion.ContainsKey(chatId))
            {
                await ProcesarAsignacionTarea(chatId, messageText, cancellationToken);
                return;
            }

            // Usuario autenticado - procesar comandos normalmente
            if (messageText.StartsWith("/"))
            {
                await ProcesarComando(chatId, messageText, cancellationToken);
            }
            else
            {
                await ProcesarMensaje(chatId, messageText, userName, cancellationToken);
            }
        }

        private async Task SolicitarUsuario(long chatId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🔐 <b>Bienvenido a AgroBot</b>\n\n" +
                      "👤 Por favor, ingresa tu usuario (ID_USUARIO):\n" +
                      "Ejemplo: 1313, 1066867630, etc.",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private async Task ProcesarAutenticacion(long chatId, string mensaje, CancellationToken cancellationToken)
        {
            // 1. Si NO hay usuario temporal, entonces este mensaje ES el usuario
            if (!_contextoLogin.ContainsKey(chatId))
            {
                _contextoLogin[chatId] = $"AUTH_USER:{mensaje.Trim()}";

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "🔒 Ahora ingresa tu contraseña:",
                    cancellationToken: cancellationToken
                );

                return;
            }

            // 2. Ya tenemos usuario, ahora viene la contraseña
            string contexto = _contextoLogin[chatId];

            if (contexto.StartsWith("AUTH_USER:"))
            {
                string usuario = contexto.Replace("AUTH_USER:", "");
                string password = mensaje.Trim();

                await VerificarCredenciales(chatId, usuario, password, cancellationToken);

                // Limpiar solo el contexto de login
                _contextoLogin.Remove(chatId);
            }
        }

        private async Task VerificarCredenciales(long chatId, string usuario, string password, CancellationToken cancellationToken)
        {
            try
            {
                // ⭐ MODIFICADO: Validar admin y guardar rol
                if (usuario == ADMIN_USERNAME && password == ADMIN_PASSWORD)
                {
                    _usuariosAutenticados[chatId] = ADMIN_USERNAME;
                    _rolesUsuarios[chatId] = ROL_ADMIN; // Guardar rol
                    await EnviarMensajeBienvenida(chatId, ADMIN_USERNAME, cancellationToken);
                    return;
                }

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                SELECT ID_USUARIO, PRIMER_NOMBRE, PRIMER_APELLIDO, CONTRASENA
                FROM USUARIO
                WHERE ID_USUARIO = :usuario";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":usuario", OracleDbType.Varchar2).Value = usuario;

                        using (OracleDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            if (await reader.ReadAsync(cancellationToken))
                            {
                                string passwordDB = reader["CONTRASENA"]?.ToString() ?? "";

                                if (passwordDB == password)
                                {
                                    string nombreCompleto = $"{reader["PRIMER_NOMBRE"]} {reader["PRIMER_APELLIDO"]}";

                                    _usuariosAutenticados[chatId] = usuario;

                                    // ⭐ NUEVO: Verificar si es empleado y guardar rol
                                    bool esEmpleado = await EsEmpleado(usuario);
                                    _rolesUsuarios[chatId] = esEmpleado ? ROL_EMPLEADO : ROL_ADMIN;

                                    await EnviarMensajeBienvenida(chatId, nombreCompleto, cancellationToken);
                                }
                                else
                                {
                                    await _botClient.SendTextMessageAsync(
                                        chatId,
                                        "❌ Contraseña incorrecta. Usa /start para intentar nuevamente.",
                                        cancellationToken: cancellationToken);
                                }
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(
                                    chatId,
                                    "❌ Usuario no encontrado. Usa /start para intentar nuevamente.",
                                    cancellationToken: cancellationToken);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    text: $"❌ Error en autenticación: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ProcesarComando(long chatId, string comando, CancellationToken cancellationToken)
        {
            comando = comando.ToLower();

            // Si el usuario NO está autenticado y NO está intentando iniciar sesión:
            if (!_usuariosAutenticados.ContainsKey(chatId) && comando != "/start")
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "🔐 Debes iniciar sesión primero. Usa /start",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // ⭐ MODIFICADO: Obtener rol del usuario
            bool esAdmin = EsAdmin(chatId);

            switch (comando)
            {
                case "/start":
                    if (!_usuariosAutenticados.ContainsKey(chatId))
                    {
                        await SolicitarUsuario(chatId, cancellationToken);
                    }
                    else
                    {
                        string usuarioAuth = _usuariosAutenticados[chatId];
                        await EnviarMensajeBienvenida(chatId, usuarioAuth, cancellationToken);
                    }
                    break;

                case "/cultivos":
                    await MostrarCultivos(chatId, cancellationToken);
                    break;

                case "/tareas":
                case "/tareasdetalle":
                    await MostrarTareasDetalle(chatId, cancellationToken);
                    break;

                case "/tareasresumen":
                    await MostrarTodasLasTareas(chatId, cancellationToken);
                    break;

                case "/mistareas":
                    await MostrarMisTareas(chatId, cancellationToken);
                    break;

                case "/asignaciones":
                    await MostrarAsignacionesTareasEmpleado(chatId, cancellationToken);
                    break;

                case "/asignartarea":
                    if (esAdmin)
                    {
                        await IniciarAsignacionTarea(chatId, cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "⛔ Solo el administrador puede asignar tareas.",
                            cancellationToken: cancellationToken
                        );
                    }
                    break;

                case "/usuarios":
                    // ⭐ NUEVO: Solo admin puede ver todos los usuarios
                    if (esAdmin)
                    {
                        await MostrarUsuarios(chatId, cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "⛔ Solo el administrador puede ver todos los usuarios.",
                            cancellationToken: cancellationToken
                        );
                    }
                    break;

                case "/empleados":
                    // ⭐ NUEVO: Solo admin puede ver empleados
                    if (esAdmin)
                    {
                        await MostrarEmpleados(chatId, cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "⛔ Solo el administrador puede ver la lista de empleados.",
                            cancellationToken: cancellationToken
                        );
                    }
                    break;

                case "/cosechas":
                    await MostrarCosechas(chatId, cancellationToken);
                    break;

                case "/insumos":
                    await MostrarInsumos(chatId, cancellationToken);
                    break;

                case "/reportepdf":
                    if (esAdmin)
                    {
                        await GenerarReportePDFSemanal(chatId, cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "⛔ Solo el administrador puede generar reportes.",
                            cancellationToken: cancellationToken
                        );
                    }
                    break;

                case "/cerrar":
                    await CerrarSesion(chatId, cancellationToken);
                    break;
                default:
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Comando no reconocido. Usa /ayuda para ver los comandos disponibles.",
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }

        private async Task CerrarSesion(long chatId, CancellationToken cancellationToken)
        {
            _usuariosAutenticados.Remove(chatId);
            _contextoAsignacion.Remove(chatId);
            _rolesUsuarios.Remove(chatId); // ⭐ NUEVO: Limpiar rol

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "👋 Sesión cerrada. Usa /start para volver a iniciar.",
                cancellationToken: cancellationToken);
        }

        private async Task ProcesarMensaje(long chatId, string mensaje, string userName, CancellationToken cancellationToken)
        {
            string mensajeLower = mensaje.ToLower();

            // ⭐ MODIFICADO: Usar el método helper para verificar rol
            bool esAdmin = EsAdmin(chatId);

            // Manejar botones del teclado
            if (mensaje == "📋 Mis Tareas")
            {
                await MostrarMisTareas(chatId, cancellationToken);
                return;
            }
            else if (mensaje == "📋 Asignaciones")
            {
                await MostrarAsignacionesTareasEmpleado(chatId, cancellationToken);
                return;
            }
            else if (mensaje == "✅ Todas las Tareas")
            {
                await MostrarTareasDetalle(chatId, cancellationToken);
                return;
            }
            else if (mensaje == "➕ Asignar Tarea")
            {
                if (esAdmin)
                {
                    await IniciarAsignacionTarea(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⛔ Solo el administrador puede asignar tareas.",
                        cancellationToken: cancellationToken);
                }
                return;
            }
            else if (mensaje == "🌱 Cultivos")
            {
                await MostrarCultivos(chatId, cancellationToken);
                return;
            }
            else if (mensaje == "🌾 Cosechas")
            {
                await MostrarCosechas(chatId, cancellationToken);
                return;
            }
            else if (mensaje == "📦 Insumos")
            {
                await MostrarInsumos(chatId, cancellationToken);
                return;
            }
            else if (mensaje == "👥 Usuarios")
            {
                // ⭐ NUEVO: Validar rol
                if (esAdmin)
                {
                    await MostrarUsuarios(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⛔ Solo el administrador puede ver todos los usuarios.",
                        cancellationToken: cancellationToken);
                }
                return;
            }
            else if (mensaje == "👥 Empleados")
            {
                // ⭐ NUEVO: Validar rol
                if (esAdmin)
                {
                    await MostrarEmpleados(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⛔ Solo el administrador puede ver la lista de empleados.",
                        cancellationToken: cancellationToken);
                }
                return;
            }
            else if (mensaje == "📊 Reporte Excel" || mensaje == "📊 Reporte PDF")
            {
                if (esAdmin)
                {
                    await GenerarReportePDFSemanal(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⛔ Solo el administrador puede generar reportes.",
                        cancellationToken: cancellationToken);
                }
                return;
            }
            else if (mensaje == "🚪 Cerrar Sesión")
            {
                await CerrarSesion(chatId, cancellationToken);
                return;
            }

            // Procesamiento por palabras clave (igual que antes)
            if (ContienePalabras(mensajeLower, "mis tareas", "tareas asignadas", "mis pendientes"))
            {
                await MostrarMisTareas(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "asignaciones", "asignacion tareas", "ver asignaciones"))
            {
                await MostrarAsignacionesTareasEmpleado(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "todas las tareas", "tareas completas", "ver todo"))
            {
                await MostrarTareasDetalle(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "detalle tareas", "tareas detalladas", "info completa tareas"))
            {
                await MostrarTareasDetalle(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "asignar tarea", "nueva tarea", "crear tarea"))
            {
                if (esAdmin)
                {
                    await IniciarAsignacionTarea(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⛔ Solo el administrador puede asignar tareas.",
                        cancellationToken: cancellationToken);
                }
            }
            else if (ContienePalabras(mensajeLower, "cultivo", "cultivos", "sembrado", "plantación", "lote"))
            {
                await MostrarCultivos(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "tarea", "tareas", "pendiente", "actividad"))
            {
                await MostrarTareasDetalle(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "usuario", "usuarios", "empleado", "empleados"))
            {
                if (esAdmin)
                {
                    await MostrarUsuarios(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⛔ Solo el administrador puede ver usuarios y empleados.",
                        cancellationToken: cancellationToken);
                }
            }
            else if (ContienePalabras(mensajeLower, "cosecha", "cosechas", "recolección"))
            {
                await MostrarCosechas(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "insumo", "insumos", "stock", "inventario"))
            {
                await MostrarInsumos(chatId, cancellationToken);
            }
            else if (ContienePalabras(mensajeLower, "hola", "buenos", "hey", "saludos"))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"¡Hola {userName}! 👋 ¿En qué puedo ayudarte hoy?",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await MostrarMenuPrincipal(chatId, cancellationToken);
            }
        }

        // ⭐ MODIFICADO: Menús diferenciados por rol
        private async Task EnviarMensajeBienvenida(long chatId, string nombreUsuario, CancellationToken cancellationToken)
        {
            bool esAdmin = EsAdmin(chatId);

            // Botones diferentes según el rol
            var botones = esAdmin ?
                new[]
                {
                       // MENÚ ADMINISTRADOR
                        new KeyboardButton[] { "🌱 Cultivos", "✅ Todas las Tareas" },
                         new KeyboardButton[] { "📋 Asignaciones", "➕ Asignar Tarea" },
                       new KeyboardButton[] { "🌾 Cosechas", "📦 Insumos" },
                          new KeyboardButton[] { "👥 Usuarios", "👥 Empleados" },
                       new KeyboardButton[] { "📊 Reporte PDF" },
                         new KeyboardButton[] { "🚪 Cerrar Sesión" }

                } :
                new[]
                {
                    // MENÚ EMPLEADO
                    new KeyboardButton[] { "📋 Mis Tareas" },

                    new KeyboardButton[] { "🚪 Cerrar Sesión" }
                };

            var keyboard = new ReplyKeyboardMarkup(botones)
            {
                ResizeKeyboard = true
            };

            string nombreSeguro = EscaparHTML(nombreUsuario);

            // Mensajes diferentes según el rol
            string mensajeBienvenida = esAdmin ?
                "🤖 <b>¡Bienvenido Administrador!</b>\n\n" +
                "✅ Autenticación exitosa\n\n" +
                "Como administrador puedes:\n" +
                "🌱 Ver información de cultivos\n" +
                "✅ Gestionar todas las tareas\n" +
                "📋 Ver asignaciones de tareas a empleados\n" +
                "➕ Asignar tareas a usuarios\n" +
                "🌾 Registro de cosechas\n" +
                "📦 Control de insumos\n" +
                "👥 Gestión de usuarios y empleados\n\n" +
                "Selecciona una opción o escribe tu consulta:" :
                $"🤖 <b>¡Bienvenido {nombreSeguro}!</b>\n\n" +
                "✅ Autenticación exitosa como <b>Empleado</b>\n\n" +
                "Puedes consultar:\n" +
                "📋 Tus tareas asignadas\n" +
                "🌱 Información de cultivos\n" +
                "🌾 Registro de cosechas\n" +
                "📦 Control de insumos\n\n" +
                "Selecciona una opción o escribe tu consulta:";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: mensajeBienvenida,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }


        private async Task MostrarMenuPrincipal(long chatId, CancellationToken cancellationToken)
        {
            bool esAdmin = EsAdmin(chatId);

            var botones = esAdmin ?
                new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("🌱 Cultivos", "cultivos"),
                        InlineKeyboardButton.WithCallbackData("✅ Tareas", "tareas")
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("📋 Asignaciones", "asignaciones"),
                        InlineKeyboardButton.WithCallbackData("➕ Asignar", "asignar")
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("🌾 Cosechas", "cosechas"),
                        InlineKeyboardButton.WithCallbackData("📦 Insumos", "insumos")
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("👥 Usuarios", "usuarios"),
                        InlineKeyboardButton.WithCallbackData("👥 Empleados", "empleados")
                    }
                } :
                new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("📋 Mis Tareas", "mistareas"),
                        InlineKeyboardButton.WithCallbackData("🌱 Cultivos", "cultivos")
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("🌾 Cosechas", "cosechas"),
                        InlineKeyboardButton.WithCallbackData("📦 Insumos", "insumos")
                    }
                };

            var keyboard = new InlineKeyboardMarkup(botones);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "¿Sobre qué tema necesitas información?",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task MostrarAsignacionesTareasEmpleado(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                bool esAdmin = EsAdmin(chatId);

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                        SELECT 
                            ID_ASIG_TAREA,
                            ID_TAREA,
                            ID_EMPLEADO,
                            ID_ADMIN_ASIGNADOR,
                            FECHA_ASIGNACION,
                            HORAS_TRABAJADAS,
                            JORNADAS_TRABAJADAS,
                            PAGO_ACORDADO,
                            ESTADO
                        FROM ASIGNACION_TAREA
                        ORDER BY FECHA_ASIGNACION DESC";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        string titulo = esAdmin ?
                            "📋 <b>ASIGNACIÓN TAREA (TODAS)</b>\n\n" :
                            "📋 <b>ASIGNACIÓN TAREA (MIS ASIGNACIONES)</b>\n\n";

                        string mensaje = titulo;
                        int count = 0;
                        int maxMostrar = 10;
                        string idUsuarioActual = _usuariosAutenticados[chatId];

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            string idEmpleado = reader["ID_EMPLEADO"]?.ToString() ?? "";

                            // Si no es admin, solo mostrar sus propias asignaciones
                            if (!esAdmin && idEmpleado != idUsuarioActual)
                                continue;

                            if (count >= maxMostrar)
                                break;

                            count++;

                            string idAsigTarea = EscaparHTML(reader["ID_ASIG_TAREA"]?.ToString() ?? "");
                            string idTarea = EscaparHTML(reader["ID_TAREA"]?.ToString() ?? "");
                            string idAdminAsignador = EscaparHTML(reader["ID_ADMIN_ASIGNADOR"]?.ToString() ?? "");

                            DateTime? fechaAsignacion = reader["FECHA_ASIGNACION"] != DBNull.Value
                                ? reader.GetDateTime(reader.GetOrdinal("FECHA_ASIGNACION"))
                                : (DateTime?)null;

                            string horasTrabajadas = EscaparHTML(reader["HORAS_TRABAJADAS"]?.ToString() ?? "(null)");
                            string jornadasTrabajadas = EscaparHTML(reader["JORNADAS_TRABAJADAS"]?.ToString() ?? "(null)");
                            string pagoAcordado = EscaparHTML(reader["PAGO_ACORDADO"]?.ToString() ?? "(null)");
                            string estado = EscaparHTML(reader["ESTADO"]?.ToString() ?? "");

                            mensaje += $"<b>📌 Asignación ID: {idAsigTarea}</b>\n";
                            mensaje += $"├ ID Tarea: {idTarea}\n";
                            mensaje += $"├ ID Empleado: {idEmpleado}\n";
                            mensaje += $"├ ID Admin Asignador: {idAdminAsignador}\n";

                            if (fechaAsignacion.HasValue)
                                mensaje += $"├ Fecha Asignación: {fechaAsignacion.Value:dd/MM/yyyy}\n";

                            mensaje += $"├ Horas Trabajadas: {horasTrabajadas}\n";
                            mensaje += $"├ Jornadas Trabajadas: {jornadasTrabajadas}\n";
                            mensaje += $"├ Pago Acordado: ${pagoAcordado}\n";
                            mensaje += $"└ Estado: <b>{estado}</b>\n\n";
                        }

                        if (count == 0)
                        {
                            mensaje = esAdmin ?
                                "📋 No hay asignaciones de tareas registradas." :
                                "📋 No tienes asignaciones de tareas.";
                        }
                        else
                        {
                            mensaje += $"<i>Mostrando {count} asignaciones</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar asignaciones: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        // ⭐ MÉTODO MODIFICADO: Mostrar todas las tareas asignadas al empleado
        private async Task MostrarMisTareas(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                string idUsuario = _usuariosAutenticados[chatId];

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    // ⭐ QUERY MODIFICADA: Eliminar filtro de estado Pendiente
                    string query = @"
                SELECT 
                    t.ID_TAREA,
                    t.ID_CULTIVO,
                    t.TIPO_ACTIVIDAD,
                    t.FECHA_PROGRAMADA,
                    t.TIEMPO_TOTAL_TAREA,
                    t.ESTADO,
                    t.ES_RECURRENTE,
                    t.GASTO_TOTAL,
                    a.FECHA_ASIGNACION,
                    a.ESTADO as ESTADO_ASIGNACION,
                    e.ID_USUARIO
                FROM TAREA t
                INNER JOIN ASIGNACION_TAREA a ON t.ID_TAREA = a.ID_TAREA
                INNER JOIN EMPLEADO e ON a.ID_EMPLEADO = e.ID_USUARIO
                WHERE e.ID_USUARIO = :idUsuario
                ORDER BY t.FECHA_PROGRAMADA DESC";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":idUsuario", OracleDbType.Varchar2).Value = idUsuario;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            string mensaje = "📋 <b>MIS TAREAS ASIGNADAS</b>\n\n";
                            int count = 0;
                            int pendientes = 0;
                            int completadas = 0;

                            while (await reader.ReadAsync(cancellationToken))
                            {
                                count++;
                                string idTarea = EscaparHTML(reader["ID_TAREA"]?.ToString() ?? "");
                                string idCultivo = EscaparHTML(reader["ID_CULTIVO"]?.ToString() ?? "");
                                string tipoActividad = EscaparHTML(reader["TIPO_ACTIVIDAD"]?.ToString() ?? "Sin especificar");
                                DateTime? fechaProgramada = reader["FECHA_PROGRAMADA"] != DBNull.Value
                                    ? reader.GetDateTime(reader.GetOrdinal("FECHA_PROGRAMADA"))
                                    : (DateTime?)null;
                                string tiempoTotal = EscaparHTML(reader["TIEMPO_TOTAL_TAREA"]?.ToString() ?? "No definido");
                                string estado = EscaparHTML(reader["ESTADO"]?.ToString() ?? "Pendiente");
                                string recurrente = EscaparHTML(reader["ES_RECURRENTE"]?.ToString() ?? "No");
                                string gastoTotal = EscaparHTML(reader["GASTO_TOTAL"]?.ToString() ?? "0");

                                DateTime? fechaAsignacion = reader["FECHA_ASIGNACION"] != DBNull.Value
                                    ? reader.GetDateTime(reader.GetOrdinal("FECHA_ASIGNACION"))
                                    : (DateTime?)null;

                                // Contar estados
                                if (estado.ToUpper() == "PENDIENTE" || string.IsNullOrEmpty(estado))
                                    pendientes++;
                                else
                                    completadas++;

                                // Emoji según estado
                                string emojiEstado = estado.ToUpper() == "PENDIENTE" || string.IsNullOrEmpty(estado) ? "⏳" : "✅";
                                string emojiRecurrente = recurrente == "Si" || recurrente == "S" ? "🔄" : "📋";

                                mensaje += $"{emojiEstado} <b>Tarea #{idTarea}</b> {emojiRecurrente}\n";
                                mensaje += $"   📍 Cultivo: #{idCultivo}\n";
                                mensaje += $"   🔧 Actividad: {tipoActividad}\n";
                                if (fechaProgramada.HasValue)
                                    mensaje += $"   📅 Fecha programada: {fechaProgramada.Value:dd/MM/yyyy}\n";
                                if (fechaAsignacion.HasValue)
                                    mensaje += $"   📌 Asignada el: {fechaAsignacion.Value:dd/MM/yyyy}\n";
                                mensaje += $"   ⏱️ Tiempo estimado: {tiempoTotal}\n";
                                mensaje += $"   📊 Estado: <b>{estado}</b>\n";
                                mensaje += $"   💰 Gasto: ${gastoTotal}\n\n";
                            }

                            if (count == 0)
                            {
                                mensaje = "📋 <b>No tienes tareas asignadas actualmente.</b>\n\n";
                                mensaje += "Habla con tu administrador para que te asigne tareas.";
                            }
                            else
                            {
                                mensaje += $"━━━━━━━━━━━━━━━━━━━━\n";
                                mensaje += $"📊 <b>Resumen:</b>\n";
                                mensaje += $"   • Total tareas: {count}\n";
                                mensaje += $"   • ⏳ Pendientes: {pendientes}\n";
                                mensaje += $"   • ✅ Completadas: {completadas}";
                            }

                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: mensaje,
                                parseMode: ParseMode.Html,
                                cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar tus tareas: {ex.Message}\n\n" +
                          $"Detalles: {ex.StackTrace}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task IniciarAsignacionTarea(long chatId, CancellationToken cancellationToken)
        {
            // Validar que esté autenticado
            if (!_usuariosAutenticados.ContainsKey(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Debes iniciar sesión con /start.",
                    cancellationToken: cancellationToken);
                return;
            }

            // ⭐ MODIFICADO: Usar método helper para validar admin
            if (!EsAdmin(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "⛔ Solo el <b>ADMIN</b> puede asignar tareas.",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                return;
            }

            // Si es admin, iniciar el proceso
            _contextoAsignacion[chatId] = "ESPERANDO_ID_TAREA";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "➕ <b>ASIGNAR NUEVA TAREA</b>\n\n" +
                      "Paso 1 de 2\n\n" +
                      "Ingresa el ID de la tarea que deseas asignar:\n" +
                      "(Para cancelar, escribe /cancelar)",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private async Task ProcesarAsignacionTarea(long chatId, string mensaje, CancellationToken cancellationToken)
        {
            if (mensaje.ToLower() == "/cancelar")
            {
                _contextoAsignacion.Remove(chatId);
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Asignación de tarea cancelada.",
                    cancellationToken: cancellationToken);
                return;
            }

            string contexto = _contextoAsignacion[chatId];

            if (contexto == "ESPERANDO_ID_TAREA")
            {
                _contextoAsignacion[chatId] = $"TAREA:{mensaje.Trim()}";
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Paso 2 de 2\n\n" +
                          "Ingresa el ID del empleado (ID_USUARIO) al que deseas asignar la tarea:",
                    cancellationToken: cancellationToken);
            }
            else if (contexto.StartsWith("TAREA:"))
            {
                string idTarea = contexto.Replace("TAREA:", "");
                string idEmpleado = mensaje.Trim();

                await AsignarTareaAEmpleado(chatId, idTarea, idEmpleado, cancellationToken);
                _contextoAsignacion.Remove(chatId);
            }
        }

        private async Task AsignarTareaAEmpleado(long chatId, string idTarea, string idEmpleado, CancellationToken cancellationToken)
        {
            try
            {
                // Obtener quién inició sesión
                if (!_usuariosAutenticados.ContainsKey(chatId))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Debes iniciar sesión con /start",
                        cancellationToken: cancellationToken);
                    return;
                }

                // ⭐ MODIFICADO: Usar método helper
                if (!EsAdmin(chatId))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "⛔ Solo el ADMIN puede asignar tareas.",
                        cancellationToken: cancellationToken);
                    return;
                }

                string usuarioActual = _usuariosAutenticados[chatId];
                string idAdmin = usuarioActual;

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    // Verificar que existe la tarea
                    string queryTarea = "SELECT COUNT(*) FROM TAREA WHERE ID_TAREA = :idTarea";
                    using (OracleCommand cmdTarea = new OracleCommand(queryTarea, conn))
                    {
                        cmdTarea.Parameters.Add(":idTarea", OracleDbType.Varchar2).Value = idTarea;
                        int countTarea = Convert.ToInt32(await cmdTarea.ExecuteScalarAsync(cancellationToken));

                        if (countTarea == 0)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId,
                                "❌ La tarea especificada no existe.",
                                cancellationToken: cancellationToken);
                            return;
                        }
                    }

                    // Verificar que existe el empleado
                    string queryEmpleado = "SELECT COUNT(*) FROM EMPLEADO WHERE ID_USUARIO = :idUsuario";
                    using (OracleCommand cmdEmpleado = new OracleCommand(queryEmpleado, conn))
                    {
                        cmdEmpleado.Parameters.Add(":idUsuario", OracleDbType.Varchar2).Value = idEmpleado;
                        int countEmpleado = Convert.ToInt32(await cmdEmpleado.ExecuteScalarAsync(cancellationToken));

                        if (countEmpleado == 0)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId,
                                "❌ El empleado especificado no existe.",
                                cancellationToken: cancellationToken);
                            return;
                        }
                    }

                    // Obtener el nuevo ID_ASIG_TAREA
                    string queryMaxId = "SELECT NVL(MAX(TO_NUMBER(ID_ASIG_TAREA)), 0) + 1 FROM ASIGNACION_TAREA";
                    string nuevoIdAsignacion;

                    using (OracleCommand cmdMaxId = new OracleCommand(queryMaxId, conn))
                    {
                        object result = await cmdMaxId.ExecuteScalarAsync(cancellationToken);
                        nuevoIdAsignacion = result.ToString();
                    }

                    // Insertar la asignación
                    // Insertar la asignación
                    string queryInsert = @"
    INSERT INTO ASIGNACION_TAREA 
    (ID_ASIG_TAREA, ID_TAREA, ID_EMPLEADO, ID_ADMIN_ASIGNADOR, FECHA_ASIGNACION, ESTADO)
    VALUES (:idAsig, :idTarea, :idEmpleado, :idAdmin, SYSDATE, 'Pendiente')";
                    using (OracleCommand cmdInsert = new OracleCommand(queryInsert, conn))
                    {
                        cmdInsert.Parameters.Add(":idAsig", OracleDbType.Varchar2).Value = nuevoIdAsignacion;
                        cmdInsert.Parameters.Add(":idTarea", OracleDbType.Varchar2).Value = idTarea;
                        cmdInsert.Parameters.Add(":idEmpleado", OracleDbType.Varchar2).Value = idEmpleado;
                        cmdInsert.Parameters.Add(":idAdmin", OracleDbType.Varchar2).Value = idAdmin;

                        int rowsAffected = await cmdInsert.ExecuteNonQueryAsync(cancellationToken);

                        if (rowsAffected > 0)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"✅ <b>Tarea asignada exitosamente</b>\n\n" +
                                      $"📋 Tarea: #{idTarea}\n" +
                                      $"👤 Empleado: #{idEmpleado}\n" +
                                      $"🛂 Administrador: {idAdmin}\n" +
                                      $"📅 Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}",
                                parseMode: ParseMode.Html,
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId,
                                "❌ No se pudo asignar la tarea. Intenta nuevamente.",
                                cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Error al asignar tarea: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MostrarUsuarios(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                        SELECT * FROM (
                            SELECT 
                                ID_USUARIO,
                                PRIMER_NOMBRE,
                                SEGUNDO_NOMBRE,
                                PRIMER_APELLIDO,
                                SEGUNDO_APELLIDO,
                                EMAIL,
                                TELEFONO
                            FROM USUARIO
                            ORDER BY ID_USUARIO
                        ) WHERE ROWNUM <= 10";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        string mensaje = "👥 <b>USUARIOS REGISTRADOS</b>\n\n";
                        int count = 0;

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            count++;
                            string idUsuario = EscaparHTML(reader["ID_USUARIO"]?.ToString() ?? "");
                            string primerNombre = EscaparHTML(reader["PRIMER_NOMBRE"]?.ToString() ?? "");
                            string segundoNombre = EscaparHTML(reader["SEGUNDO_NOMBRE"]?.ToString() ?? "");
                            string primerApellido = EscaparHTML(reader["PRIMER_APELLIDO"]?.ToString() ?? "");
                            string segundoApellido = EscaparHTML(reader["SEGUNDO_APELLIDO"]?.ToString() ?? "");
                            string email = EscaparHTML(reader["EMAIL"]?.ToString() ?? "No registrado");
                            string telefono = EscaparHTML(reader["TELEFONO"]?.ToString() ?? "No registrado");

                            string nombreCompleto = $"{primerNombre} {segundoNombre} {primerApellido} {segundoApellido}".Trim();

                            mensaje += $"👤 <b>{nombreCompleto}</b>\n";
                            mensaje += $"   ID: {idUsuario}\n";
                            mensaje += $"   📧 {email}\n";
                            mensaje += $"   📱 {telefono}\n\n";
                        }

                        if (count == 0)
                        {
                            mensaje = "No hay usuarios registrados.";
                        }
                        else
                        {
                            mensaje += $"<i>Total: {count} usuarios</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar usuarios: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MostrarCultivos(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                        SELECT * FROM (
                            SELECT 
                                ID_CULTIVO,
                                NOMBRE_LOTE,
                                FECHA_SIEMBRA,
                                FECHA_COSECHA_ESTIMADA,
                                ALERTA_N8N
                            FROM CULTIVO
                            ORDER BY FECHA_SIEMBRA DESC
                        ) WHERE ROWNUM <= 10";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        string mensaje = "🌱 <b>CULTIVOS REGISTRADOS</b>\n\n";
                        int count = 0;

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            count++;
                            string idCultivo = EscaparHTML(reader["ID_CULTIVO"]?.ToString() ?? "");
                            string nombreLote = EscaparHTML(reader["NOMBRE_LOTE"]?.ToString() ?? "Sin nombre");
                            DateTime? fechaSiembra = reader["FECHA_SIEMBRA"] != DBNull.Value
                                ? reader.GetDateTime(reader.GetOrdinal("FECHA_SIEMBRA"))
                                : (DateTime?)null;
                            DateTime? fechaCosecha = reader["FECHA_COSECHA_ESTIMADA"] != DBNull.Value
                                ? reader.GetDateTime(reader.GetOrdinal("FECHA_COSECHA_ESTIMADA"))
                                : (DateTime?)null;
                            string alerta = EscaparHTML(reader["ALERTA_N8N"]?.ToString() ?? "Normal");

                            mensaje += $"📊 <b>Cultivo #{idCultivo}</b>\n";
                            mensaje += $"   Lote: {nombreLote}\n";
                            if (fechaSiembra.HasValue)
                                mensaje += $"   Siembra: {fechaSiembra.Value:dd/MM/yyyy}\n";
                            if (fechaCosecha.HasValue)
                                mensaje += $"   Cosecha Est.: {fechaCosecha.Value:dd/MM/yyyy}\n";
                            mensaje += $"   Estado: {alerta}\n\n";
                        }

                        if (count == 0)
                        {
                            mensaje = "No hay cultivos registrados.";
                        }
                        else
                        {
                            mensaje += $"<i>Total: {count} cultivos</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar cultivos: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MostrarTodasLasTareas(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                bool esAdmin = EsAdmin(chatId);

                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                        SELECT * FROM (
                            SELECT 
                                t.ID_TAREA,
                                t.ID_CULTIVO,
                                t.TIPO_ACTIVIDAD,
                                t.FECHA_PROGRAMADA,
                                t.TIEMPO_TOTAL_TAREA,
                                t.ESTADO,
                                t.ES_RECURRENTE,
                                t.GASTO_TOTAL,
                                (SELECT COUNT(*) 
                                 FROM ASIGNACION_TAREA at2 
                                 WHERE at2.ID_TAREA = t.ID_TAREA) as TOTAL_ASIGNADOS
                            FROM TAREA t
                            WHERE t.ESTADO = 'Pendiente' OR t.ESTADO IS NULL
                            ORDER BY t.FECHA_PROGRAMADA ASC
                        ) WHERE ROWNUM <= 15";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        string mensaje = esAdmin ?
                            "✅ <b>TODAS LAS TAREAS PENDIENTES (ADMIN)</b>\n\n" :
                            "✅ <b>TAREAS PENDIENTES DEL SISTEMA</b>\n\n";
                        int count = 0;

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            count++;
                            string idTarea = EscaparHTML(reader["ID_TAREA"]?.ToString() ?? "");
                            string idCultivo = EscaparHTML(reader["ID_CULTIVO"]?.ToString() ?? "");
                            string tipoActividad = EscaparHTML(reader["TIPO_ACTIVIDAD"]?.ToString() ?? "Sin especificar");
                            DateTime? fechaProgramada = reader["FECHA_PROGRAMADA"] != DBNull.Value
                                ? reader.GetDateTime(reader.GetOrdinal("FECHA_PROGRAMADA"))
                                : (DateTime?)null;
                            string tiempoTotal = EscaparHTML(reader["TIEMPO_TOTAL_TAREA"]?.ToString() ?? "No definido");
                            string estado = EscaparHTML(reader["ESTADO"]?.ToString() ?? "Pendiente");
                            string recurrente = EscaparHTML(reader["ES_RECURRENTE"]?.ToString() ?? "No");
                            string gastoTotal = EscaparHTML(reader["GASTO_TOTAL"]?.ToString() ?? "0");
                            int totalAsignados = reader["TOTAL_ASIGNADOS"] != DBNull.Value
                                ? Convert.ToInt32(reader["TOTAL_ASIGNADOS"])
                                : 0;

                            string emoji = recurrente == "Si" || recurrente == "S" ? "🔄" : "📋";

                            mensaje += $"{emoji} <b>Tarea #{idTarea}</b>\n";
                            mensaje += $"   Cultivo: #{idCultivo}\n";
                            mensaje += $"   Actividad: {tipoActividad}\n";
                            if (fechaProgramada.HasValue)
                                mensaje += $"   Fecha: {fechaProgramada.Value:dd/MM/yyyy}\n";
                            mensaje += $"   Tiempo: {tiempoTotal}\n";
                            mensaje += $"   Gasto: ${gastoTotal}\n";
                            mensaje += $"   Estado: {estado}\n";

                            if (esAdmin)
                            {
                                mensaje += $"   👥 Asignada a: {totalAsignados} empleado(s)\n";
                            }

                            mensaje += "\n";
                        }

                        if (count == 0)
                        {
                            mensaje = "✅ ¡No hay tareas pendientes en el sistema!";
                        }
                        else
                        {
                            mensaje += $"<i>Total: {count} tareas pendientes</i>\n\n";
                            mensaje += "<i>Usa /tareasdetalle para ver información completa</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar tareas: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MostrarTareasDetalle(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                        SELECT 
                            ID_TAREA,
                            ID_CULTIVO,
                            ID_ADMIN_CREADOR,
                            TIPO_ACTIVIDAD,
                            FECHA_PROGRAMADA,
                            TIEMPO_TOTAL_TAREA,
                            ESTADO,
                            ES_RECURRENTE,
                            FRECUENCIA_DIAS,
                            COSTO_TRANSPORTE,
                            GASTO_TOTAL
                        FROM TAREA
                        ORDER BY FECHA_PROGRAMADA DESC";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        string mensaje = "📋 <b>DETALLE COMPLETO DE TAREAS</b>\n\n";
                        int count = 0;
                        int maxMostrar = 8;

                        while (await reader.ReadAsync(cancellationToken) && count < maxMostrar)
                        {
                            count++;
                            string idTarea = EscaparHTML(reader["ID_TAREA"]?.ToString() ?? "");
                            string idCultivo = EscaparHTML(reader["ID_CULTIVO"]?.ToString() ?? "");
                            string idAdminCreador = EscaparHTML(reader["ID_ADMIN_CREADOR"]?.ToString() ?? "");
                            string tipoActividad = EscaparHTML(reader["TIPO_ACTIVIDAD"]?.ToString() ?? "");

                            DateTime? fechaProgramada = reader["FECHA_PROGRAMADA"] != DBNull.Value
                                ? reader.GetDateTime(reader.GetOrdinal("FECHA_PROGRAMADA"))
                                : (DateTime?)null;

                            string tiempoTotal = EscaparHTML(reader["TIEMPO_TOTAL_TAREA"]?.ToString() ?? "");
                            string estado = EscaparHTML(reader["ESTADO"]?.ToString() ?? "");
                            string esRecurrente = EscaparHTML(reader["ES_RECURRENTE"]?.ToString() ?? "");
                            string frecuenciaDias = EscaparHTML(reader["FRECUENCIA_DIAS"]?.ToString() ?? "0");
                            string costoTransporte = EscaparHTML(reader["COSTO_TRANSPORTE"]?.ToString() ?? "0");
                            string gastoTotal = EscaparHTML(reader["GASTO_TOTAL"]?.ToString() ?? "0");

                            mensaje += $"<b>📌 Tarea ID: {idTarea}</b>\n";
                            mensaje += $"├ Cultivo: {idCultivo}\n";
                            mensaje += $"├ Creador: {idAdminCreador}\n";
                            mensaje += $"├ Actividad: {tipoActividad}\n";

                            if (fechaProgramada.HasValue)
                                mensaje += $"├ Fecha: {fechaProgramada.Value:dd/MM/yyyy}\n";

                            mensaje += $"├ Tiempo: {tiempoTotal}\n";
                            mensaje += $"├ Estado: {estado}\n";
                            mensaje += $"├ Recurrente: {esRecurrente}\n";
                            mensaje += $"├ Frecuencia: {frecuenciaDias} días\n";
                            mensaje += $"├ Costo Transp.: ${costoTransporte}\n";
                            mensaje += $"└ Gasto Total: <b>${gastoTotal}</b>\n\n";
                        }

                        if (count == 0)
                        {
                            mensaje = "No hay tareas registradas.";
                        }
                        else
                        {
                            mensaje += $"<i>Mostrando {count} tareas</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar detalle de tareas: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MostrarCosechas(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                        SELECT * FROM (
                            SELECT 
                                ID_COSECHA,
                                ID_CULTIVO,
                                FECHA_COSECHA,
                                FECHA_REGISTRO,
                                CANTIDAD_OBTENIDA,
                                UNIDAD_MEDIDA,
                                CALIDAD
                            FROM COSECHA
                            ORDER BY FECHA_COSECHA DESC
                        ) WHERE ROWNUM <= 10";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        string mensaje = "🌾 <b>REGISTRO DE COSECHAS</b>\n\n";
                        int count = 0;

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            count++;
                            string idCosecha = EscaparHTML(reader["ID_COSECHA"]?.ToString() ?? "");
                            string idCultivo = EscaparHTML(reader["ID_CULTIVO"]?.ToString() ?? "");
                            DateTime? fechaCosecha = reader["FECHA_COSECHA"] != DBNull.Value
                                ? reader.GetDateTime(reader.GetOrdinal("FECHA_COSECHA"))
                                : (DateTime?)null;
                            string cantidad = EscaparHTML(reader["CANTIDAD_OBTENIDA"]?.ToString() ?? "0");
                            string unidad = EscaparHTML(reader["UNIDAD_MEDIDA"]?.ToString() ?? "");
                            string calidad = EscaparHTML(reader["CALIDAD"]?.ToString() ?? "Normal");

                            mensaje += $"🌾 <b>Cosecha #{idCosecha}</b>\n";
                            mensaje += $"   Cultivo: #{idCultivo}\n";
                            if (fechaCosecha.HasValue)
                                mensaje += $"   Fecha: {fechaCosecha.Value:dd/MM/yyyy}\n";
                            mensaje += $"   Cantidad: {cantidad} {unidad}\n";
                            mensaje += $"   Calidad: {calidad}\n\n";
                        }

                        if (count == 0)
                        {
                            mensaje = "No hay cosechas registradas.";
                        }
                        else
                        {
                            mensaje += $"<i>Total: {count} cosechas</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar cosechas: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MostrarInsumos(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                SELECT * FROM (
                    SELECT 
                        ID_INSUMO,
                        NOMBRE,
                        TIPO,
                        STOCK_ACTUAL,
                        STOCK_MINIMO,
                        COSTO_UNITARIO,
                        UNIDAD_MEDIDA,
                        FECHA_ULTIMA_ACTUALIZACION
                    FROM INSUMO
                    ORDER BY FECHA_ULTIMA_ACTUALIZACION DESC
                )
                WHERE ROWNUM <= 10";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        string mensaje = "📦 <b>INVENTARIO DE INSUMOS</b>\n\n";
                        int count = 0;

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            count++;

                            string id = EscaparHTML(reader["ID_INSUMO"]?.ToString() ?? "");
                            string nombre = EscaparHTML(reader["NOMBRE"]?.ToString() ?? "Sin nombre");
                            string tipo = EscaparHTML(reader["TIPO"]?.ToString() ?? "N/A");
                            string stockActual = EscaparHTML(reader["STOCK_ACTUAL"]?.ToString() ?? "0");
                            string stockMin = EscaparHTML(reader["STOCK_MINIMO"]?.ToString() ?? "0");
                            string unidad = EscaparHTML(reader["UNIDAD_MEDIDA"]?.ToString() ?? "");
                            string costo = EscaparHTML(reader["COSTO_UNITARIO"]?.ToString() ?? "0");

                            DateTime? fechaActualizacion = reader["FECHA_ULTIMA_ACTUALIZACION"] != DBNull.Value
                                ? reader.GetDateTime(reader.GetOrdinal("FECHA_ULTIMA_ACTUALIZACION"))
                                : (DateTime?)null;

                            mensaje += $"📦 <b>{nombre}</b>\n";
                            mensaje += $"   ID: {id}\n";
                            mensaje += $"   Tipo: {tipo}\n";
                            mensaje += $"   Stock: {stockActual} {unidad}\n";
                            mensaje += $"   Stock mínimo: {stockMin}\n";
                            mensaje += $"   Costo unitario: ${costo}\n";

                            if (fechaActualizacion.HasValue)
                                mensaje += $"   Última actualización: {fechaActualizacion.Value:dd/MM/yyyy}\n";

                            mensaje += "\n";
                        }

                        if (count == 0)
                        {
                            mensaje = "No hay insumos registrados.";
                        }
                        else
                        {
                            mensaje += $"<i>Total: {count} insumos</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar insumos: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MostrarEmpleados(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    string query = @"
                SELECT * FROM (
                    SELECT 
                        e.ID_USUARIO,
                        u.PRIMER_NOMBRE,
                        u.PRIMER_APELLIDO,
                        u.TELEFONO,
                        u.EMAIL,
                        e.MONTO_POR_HORA,
                        e.MONTO_POR_JORNAL
                    FROM EMPLEADO e
                    INNER JOIN USUARIO u ON e.ID_USUARIO = u.ID_USUARIO
                    ORDER BY e.ID_USUARIO DESC
                ) 
                WHERE ROWNUM <= 10";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    using (OracleDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        string mensaje = "👥 <b>EMPLEADOS REGISTRADOS</b>\n\n";
                        int count = 0;

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            count++;

                            string idUsuario = EscaparHTML(reader["ID_USUARIO"]?.ToString() ?? "");
                            string nombre = EscaparHTML(reader["PRIMER_NOMBRE"]?.ToString() ?? "");
                            string apellido = EscaparHTML(reader["PRIMER_APELLIDO"]?.ToString() ?? "");
                            string telefono = EscaparHTML(reader["TELEFONO"]?.ToString() ?? "No registrado");
                            string email = EscaparHTML(reader["EMAIL"]?.ToString() ?? "No registrado");
                            string montoHora = EscaparHTML(reader["MONTO_POR_HORA"]?.ToString() ?? "0");
                            string montoJornal = EscaparHTML(reader["MONTO_POR_JORNAL"]?.ToString() ?? "0");

                            mensaje += $"👤 <b>{nombre} {apellido}</b>\n";
                            mensaje += $"   ID usuario: {idUsuario}\n";
                            mensaje += $"   💲 Pago por hora: ${montoHora}\n";
                            mensaje += $"   💲 Pago por jornal: ${montoJornal}\n";
                            mensaje += $"   📱 {telefono}\n";
                            mensaje += $"   📧 {email}\n\n";
                        }

                        if (count == 0)
                        {
                            mensaje = "No hay empleados registrados.";
                        }
                        else
                        {
                            mensaje += $"<i>Total: {count} empleados</i>";
                        }

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: mensaje,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error al consultar empleados: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            string errorMessage;

            if (exception is ApiRequestException apiRequestException)
            {
                errorMessage = $"Error de API de Telegram:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}";
            }
            else
            {
                errorMessage = exception.ToString();
            }

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private bool ContienePalabras(string texto, params string[] palabras)
        {
            return palabras.Any(palabra => texto.Contains(palabra));
        }

        private string EscaparHTML(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            return texto
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
        private async Task GenerarReportePDFSemanal(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📊 Generando reporte PDF profesional...\n" +
                          "📧 Se enviará automáticamente por correo...\n" +
                          "⏳ Esto puede tomar unos momentos...",
                    cancellationToken: cancellationToken);

                // ⭐ LLAMAR AL SERVICIO DE REPORTES
                var resultado = await _reportesPDFService.GenerarReportePDFManual();

                if (!resultado.Exitoso)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"❌ Error al generar reporte: {resultado.MensajeError}",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Mostrar resumen de datos
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"✅ Datos obtenidos:\n" +
                          $"📋 {resultado.DatosReporte.NumeroTareas} tareas\n" +
                          $"💰 Total gastos: ${resultado.DatosReporte.TotalGastos:N0}\n" +
                          $"📦 {resultado.DatosReporte.InsumosDetalle.Count} insumos\n\n" +
                          $"Enviando reporte...",
                    cancellationToken: cancellationToken);

                // Enviar PDF por Telegram
                using (var fileStream = new FileStream(resultado.RutaArchivo, FileMode.Open, FileAccess.Read))
                {
                    var inputFile = InputFile.FromStream(fileStream, resultado.NombreArchivo);

                    string caption = $"📊 <b>AGROSMART – REPORTE SEMANAL PDF</b>\n\n" +
                                   $"📅 Período: {resultado.FechaInicio:dd/MM/yyyy} - {resultado.FechaFin:dd/MM/yyyy}\n" +
                                   $"💰 Total gastos: <b>${resultado.DatosReporte.TotalGastos:N0}</b>\n" +
                                   $"📋 Tareas realizadas: <b>{resultado.DatosReporte.NumeroTareas}</b>\n" +
                                   $"📦 Insumos: <b>{resultado.DatosReporte.InsumosDetalle.Count}</b>\n" +
                                   $"🕐 Generado: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +
                                   $"✅ Incluye gráficas con datos exactos de la BD\n";

                    if (resultado.EmailEnviado)
                    {
                        caption += $"\n📧 <b>Enviado por email a:</b> {_reportesPDFService._configEmail.EmailDestino}";
                    }

                    await _botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: inputFile,
                        caption: caption,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                }

                // Limpiar archivos temporales
                _reportesPDFService.LimpiarRecursos(resultado);

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅ Reporte PDF generado exitosamente.",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Error: {ex.Message}",
                    cancellationToken: cancellationToken);

                Console.WriteLine($"Error completo: {ex.ToString()}");
            }
        }

    }
}
