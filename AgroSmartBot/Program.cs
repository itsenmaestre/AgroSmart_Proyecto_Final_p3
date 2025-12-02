using System;
using System.Threading.Tasks;

namespace AgroSmartBot
{
    class Program
    {

        static async Task Main(string[] args)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            Console.WriteLine("=================================");
            Console.WriteLine("   AGROSMART TELEGRAM BOT");
            Console.WriteLine("=================================\n");

            // CONFIGURA TU CADENA DE CONEXIÓN ORACLE AQUÍ
            // Formato 1: Con usuario y contraseña
            string connectionString = "User Id=AGROSMART;Password=agro123;Data Source=LocalHost:1521/xepdb1;";

            // Formato 2: Con TNS
            // string connectionString = "User Id=TU_USUARIO;Password=TU_PASSWORD;Data Source=TU_TNS_NAME;";

            // Ejemplo real:
            // string connectionString = "User Id=AGROSMART;Password=123456;Data Source=localhost:1521/XEPDB1;";

            try
            {
                var botService = new TelegramBotService(connectionString);
                await botService.IniciarBot();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fatal: {ex.Message}");
                Console.WriteLine($"Detalles: {ex.StackTrace}");
                Console.WriteLine("\nPresiona Enter para salir...");
                Console.ReadLine();
            }
        }
    }
}