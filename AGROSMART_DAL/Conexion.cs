using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

using System.Threading.Tasks;

namespace AGROSMART_DAL
{
    public static class Conexion
    {
        private static string Cadena =>
            ConfigurationManager.ConnectionStrings["ConexionAgroSmart"]?.ConnectionString
            ?? throw new InvalidOperationException(
                "Falta la cadena 'ConexionAgroSmart' en el App.config del proyecto de inicio (AGROSMART_GUI).");

        public static OracleConnection CrearConexion()
        {
            return new OracleConnection(Cadena);
        }

        public static async Task<bool> ProbarAsync()
        {
            using (OracleConnection cn = CrearConexion())
            {
                await cn.OpenAsync();

                using (OracleCommand cmd = new OracleCommand("SELECT 1 FROM DUAL", cn))
                {
                    object result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result) == 1;
                }
            }
        }
    }
}
