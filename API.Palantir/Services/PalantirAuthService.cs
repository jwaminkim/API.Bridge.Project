using Newtonsoft.Json;
using System.Text;

namespace API.Palantir.Services
{
    public class PalantirAuthService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly string _env;

        //public PalantirAuthService(HttpClient http, IConfiguration config)
        public PalantirAuthService(IHttpClientFactory factory, IConfiguration config)        
        {
            //_http = http;
            _http = factory.CreateClient("palantir");
            _config = config;
            _env = _config["Palantir:Env"] ?? "Prod";
        }

        public async Task<string> GetTokenAsync()
        {
            string baseKey = $"Palantir:{_env}:Auth";

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                _config[$"{baseKey}:TokenUrl"]
            );

            request.Headers.Add("client_id", _config[$"{baseKey}:ClientId"]);
            request.Headers.Add("client_secret", _config[$"{baseKey}:ClientSecret"]);

            var payload = new
            {
                oauth_grant_type = "CLIENT_CREDENTIALS",
                oauth_client_id = _config[$"{baseKey}:OAuthClientId"],
                oauth_client_secret = _config[$"{baseKey}:OAuthClientSecret"]
            };

            request.Content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            dynamic json = JsonConvert.DeserializeObject(
                await response.Content.ReadAsStringAsync()
            )!;

            return json.access_token;
        }
    }
}
