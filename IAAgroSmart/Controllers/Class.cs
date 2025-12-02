using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IAAgroSmart.Controllers
{
    public class Class : ControllerBase
    {

        [HttpGet]
        [Route("envia")]
        public async Task<IActionResult> EnviaAsync(string telefono, string msg)
        {
            string token = "EAAbcTJOEM6ABPxmmmyugnj7DiuoWvTZAQfdylempm7L4Fk3NjFJ8Btj9ew6Ig7lJilfMdCMECnBIbtJPOeo64MBZBMkFqOedL5hF7JirTIbzkd5ZA6CcvqZBCK9XvOwOU0jtsGEUOo2872OUuHDnxYAD2Dw5gdV91q7EzbirmczqxgXA2VJw6mcQ9NA9hKKd6snA86m31g9K5Ec1DO5I6BUoSKyHkuKvSmaeDW3gTVr4ZAgZDZD";
            string idTelefono = "909438658912318";

            using (HttpClient client = new HttpClient())
            {
                var url = $"https://graph.facebook.com/v15.0/{idTelefono}/messages";

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                string body = $@"
        {{
            ""messaging_product"": ""whatsapp"",
            ""recipient_type"": ""individual"",
            ""to"": ""{telefono}"",
            ""type"": ""text"",
            ""text"": {{
                ""body"": ""{msg}""
            }}
        }}";

                var request = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, request);

                string jsonResponse = await response.Content.ReadAsStringAsync();

                return Ok(jsonResponse); // <-- AHORA SWAGGER MUESTRA LA RESPUESTA
            }
        }

    }
}
