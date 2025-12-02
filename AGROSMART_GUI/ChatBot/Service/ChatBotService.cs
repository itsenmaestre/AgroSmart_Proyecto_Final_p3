using AGROSMART_BLL;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_GUI.Services
{
    /// <summary>
    /// Servicio de ChatBot basado en reglas
    /// Procesa consultas del usuario y devuelve respuestas basadas en datos reales de la BD
    /// </summary>
    public class ChatBotService
    {
        private readonly CultivoService _cultivoService;
        private readonly GastosService _gastosService;
        private readonly TareaService _tareaService;
        private readonly EmpleadoService _empleadoService;

        public ChatBotService()
        {
            _cultivoService = new CultivoService();
            _gastosService = new GastosService();
            _tareaService = new TareaService();
            _empleadoService = new EmpleadoService();
        }

        /// <summary>
        /// Procesa una consulta del usuario y devuelve una respuesta
        /// </summary>
        public async Task<string> ProcesarConsulta(string consulta)
        {
            try
            {
                consulta = consulta.ToLower().Trim();

                // Saludos
                if (ContieneAlgunaPalabra(consulta, "hola", "buenos días", "buenas tardes", "buenas noches", "hey"))
                    return "¡Hola! 👋 ¿En qué puedo ayudarte hoy?";

                // Despedidas
                if (ContieneAlgunaPalabra(consulta, "adiós", "chao", "hasta luego", "nos vemos"))
                    return "¡Hasta luego! 👋 Que tengas un excelente día. Vuelve cuando necesites ayuda.";

                // Agradecimientos
                if (ContieneAlgunaPalabra(consulta, "gracias", "thank you", "muchas gracias"))
                    return "¡De nada! 😊 Estoy aquí para ayudarte siempre que lo necesites.";

                // Consultas sobre CULTIVOS
                if (ContieneAlgunaPalabra(consulta, "cultivo", "siembra", "cosecha", "lote", "hectárea"))
                    return await ConsultarCultivos(consulta);

                // Consultas sobre GASTOS
                if (ContieneAlgunaPalabra(consulta, "gasto", "costo", "dinero", "precio", "pago", "presupuesto"))
                    return await ConsultarGastos(consulta);

                // Consultas sobre TAREAS
                if (ContieneAlgunaPalabra(consulta, "tarea", "pendiente", "hacer", "trabajo", "actividad", "programada"))
                    return await ConsultarTareas(consulta);

                // Consultas sobre EMPLEADOS
                if (ContieneAlgunaPalabra(consulta, "empleado", "personal", "trabajador", "empleados", "mano de obra"))
                    return await ConsultarEmpleados(consulta);

                // Ayuda
                if (ContieneAlgunaPalabra(consulta, "ayuda", "help", "qué puedes", "cómo", "funciones", "que sabes"))
                    return RespuestaAyuda();

                // No entendido
                return "🤔 No estoy seguro de entender tu consulta.\n\n" +
                       "Intenta preguntarme sobre:\n" +
                       "📊 Cultivos activos\n" +
                       "💰 Gastos del mes\n" +
                       "✅ Tareas pendientes\n" +
                       "👥 Información de empleados";
            }
            catch (Exception ex)
            {
                return $"❌ Error al procesar tu consulta:\n{ex.Message}";
            }
        }

        #region Métodos de Consulta

        private async Task<string> ConsultarCultivos(string consulta)
        {
            return await Task.Run(() =>
            {
                var cultivos = _cultivoService.Consultar()?.ToList();

                if (cultivos == null || !cultivos.Any())
                    return "📊 No tienes cultivos registrados en este momento.";

                int totalCultivos = cultivos.Count;
                var cultivosActivos = cultivos.Where(c => c.ALERTA_N8N == "ACTIVO").ToList();
                decimal hectareasTotales = cultivos.Sum(c => c.ID_CULTIVO);

                var sb = new StringBuilder();
                sb.AppendLine($"🌾 Resumen de Cultivos\n");
                sb.AppendLine($"Total: {totalCultivos} cultivos");
                sb.AppendLine($"Activos: {cultivosActivos.Count}");
                sb.AppendLine($"Hectáreas totales: {hectareasTotales} ha\n");

                if (cultivosActivos.Any())
                {
                    sb.AppendLine("Cultivos activos:");
                    foreach (var cultivo in cultivosActivos.Take(5))
                    {
                        sb.AppendLine($"• {cultivo.NOMBRE_LOTE} - {cultivo.ID_CULTIVO} ha");
                    }

                    if (cultivosActivos.Count > 5)
                        sb.Append($"\n... y {cultivosActivos.Count - 5} más");
                }

                return sb.ToString();
            });
        }

        private async Task<string> ConsultarGastos(string consulta)
        {
            return await Task.Run(() =>
            {
                var gastos = _gastosService.ListarGastos();

                if (gastos == null || !gastos.Any())
                    return "💰 No hay gastos registrados.";

                // Si pregunta por el mes actual
                if (ContieneAlgunaPalabra(consulta, "mes", "actual", "este mes", "mensual"))
                {
                    var gastosDelMes = gastos.Where(g =>
                        g.FechaTarea.Month == DateTime.Now.Month &&
                        g.FechaTarea.Year == DateTime.Now.Year).ToList();

                    if (!gastosDelMes.Any())
                        return $"💰 No hay gastos registrados en {DateTime.Now:MMMM yyyy}.";

                    decimal totalMes = gastosDelMes.Sum(g => g.TotalGasto);
                    decimal insumos = gastosDelMes.Sum(g => g.GastoInsumos);
                    decimal personal = gastosDelMes.Sum(g => g.PagoEmpleados);
                    decimal transporte = gastosDelMes.Sum(g => g.GastoTransporte);

                    var sb = new StringBuilder();
                    sb.AppendLine($"💰 Gastos de {DateTime.Now:MMMM yyyy}\n");
                    sb.AppendLine($"Total: {totalMes.ToString("C0", new CultureInfo("es-CO"))}\n");
                    sb.AppendLine($"Desglose:");
                    sb.AppendLine($"🌿 Insumos: {insumos.ToString("C0", new CultureInfo("es-CO"))} ({(insumos / totalMes * 100):F0}%)");
                    sb.AppendLine($"👥 Personal: {personal.ToString("C0", new CultureInfo("es-CO"))} ({(personal / totalMes * 100):F0}%)");
                    sb.AppendLine($"🚚 Transporte: {transporte.ToString("C0", new CultureInfo("es-CO"))} ({(transporte / totalMes * 100):F0}%)");
                    sb.Append($"\n📊 {gastosDelMes.Count} tareas registradas");

                    return sb.ToString();
                }

                // Resumen general
                decimal totalGeneral = gastos.Sum(g => g.TotalGasto);
                decimal insumosTotal = gastos.Sum(g => g.GastoInsumos);
                decimal personalTotal = gastos.Sum(g => g.PagoEmpleados);

                return $"💰 Resumen General de Gastos\n\n" +
                       $"Total acumulado: {totalGeneral.ToString("C0", new CultureInfo("es-CO"))}\n" +
                       $"Insumos: {insumosTotal.ToString("C0", new CultureInfo("es-CO"))}\n" +
                       $"Personal: {personalTotal.ToString("C0", new CultureInfo("es-CO"))}\n" +
                       $"Tareas: {gastos.Count}\n\n" +
                       $"💡 Pregunta \"gastos de este mes\" para el desglose mensual.";
            });
        }

        private async Task<string> ConsultarTareas(string consulta)
        {
            return await Task.Run(() =>
            {
                var tareas = _tareaService.Consultar()?.ToList();

                if (tareas == null || !tareas.Any())
                    return "✅ No hay tareas registradas.";

                // Si pregunta por pendientes
                if (ContieneAlgunaPalabra(consulta, "pendiente", "falta", "por hacer"))
                {
                    var tareasPendientes = tareas
                        .Where(t => t.ESTADO == "PENDIENTE" || t.ESTADO == "EN_PROCESO")
                        .OrderBy(t => t.FECHA_PROGRAMADA)
                        .ToList();

                    if (!tareasPendientes.Any())
                        return "✅ ¡Excelente! No tienes tareas pendientes. 🎉";

                    var sb = new StringBuilder();
                    sb.AppendLine($"✅ Tienes {tareasPendientes.Count} tareas pendientes:\n");

                    foreach (var tarea in tareasPendientes.Take(5))
                    {
                        string emoji = tarea.ESTADO == "PENDIENTE" ? "⏳" : "🔄";
                        string fecha = tarea.FECHA_PROGRAMADA.ToString("dd/MM/yyyy");
                        sb.AppendLine($"{emoji} {tarea.TIPO_ACTIVIDAD} - {fecha}");
                    }

                    if (tareasPendientes.Count > 5)
                        sb.Append($"\n... y {tareasPendientes.Count - 5} más");

                    return sb.ToString();
                }

                // Resumen general
                int pendientes = tareas.Count(t => t.ESTADO == "PENDIENTE");
                int enProceso = tareas.Count(t => t.ESTADO == "EN_PROCESO");
                int finalizadas = tareas.Count(t => t.ESTADO == "FINALIZADA");

                return $"✅ Resumen de Tareas\n\n" +
                       $"⏳ Pendientes: {pendientes}\n" +
                       $"🔄 En Proceso: {enProceso}\n" +
                       $"✓ Finalizadas: {finalizadas}\n\n" +
                       $"Total: {tareas.Count} tareas";
            });
        }

        private async Task<string> ConsultarEmpleados(string consulta)
        {
            return await Task.Run(() =>
            {
                var empleados = _empleadoService.Consultar()?.ToList();

                if (empleados == null || !empleados.Any())
                    return "👥 No hay empleados registrados.";

                decimal promedioHora = empleados.Average(e => e.MONTO_POR_HORA);
                decimal promedioJornal = empleados.Average(e => e.MONTO_POR_JORNAL);
                decimal maxHora = empleados.Max(e => e.MONTO_POR_HORA);
                decimal minHora = empleados.Min(e => e.MONTO_POR_HORA);

                return $"👥 Información de Personal\n\n" +
                       $"Total empleados: {empleados.Count}\n\n" +
                       $"💰 Tarifas promedio:\n" +
                       $"• Por hora: {promedioHora.ToString("C0", new CultureInfo("es-CO"))}\n" +
                       $"• Por jornal: {promedioJornal.ToString("C0", new CultureInfo("es-CO"))}\n\n" +
                       $"📊 Rango tarifas/hora:\n" +
                       $"• Mínima: {minHora.ToString("C0", new CultureInfo("es-CO"))}\n" +
                       $"• Máxima: {maxHora.ToString("C0", new CultureInfo("es-CO"))}";
            });
        }

        private string RespuestaAyuda()
        {
            return "💡 Puedo ayudarte con:\n\n" +
                   "📊 Información de cultivos\n" +
                   "   • \"¿Cuántos cultivos tengo?\"\n" +
                   "   • \"Muéstrame los cultivos activos\"\n\n" +
                   "💰 Análisis de gastos\n" +
                   "   • \"Gastos de este mes\"\n" +
                   "   • \"¿Cuánto he gastado?\"\n\n" +
                   "✅ Tareas pendientes\n" +
                   "   • \"Qué tareas tengo pendientes\"\n" +
                   "   • \"Muéstrame las tareas\"\n\n" +
                   "👥 Datos de empleados\n" +
                   "   • \"Información de empleados\"\n" +
                   "   • \"Cuántos empleados tengo\"\n\n" +
                   "¡Pregúntame lo que necesites! 😊";
        }

        #endregion

        #region Métodos Auxiliares

        private bool ContieneAlgunaPalabra(string texto, params string[] palabras)
        {
            return palabras.Any(p => texto.Contains(p));
        }

        #endregion
    }
}
