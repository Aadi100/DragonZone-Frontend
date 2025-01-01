using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]


    public class UserResponseModel
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_data")]
        public List<UserResponseData> UserResponseData { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }
    }

    public class UserResponseData
    {
        [JsonPropertyName("email_address")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("branch")]
        public string Branch { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }

        [JsonPropertyName("role")]
        [JsonConverter(typeof(RoleNameConverter))]
        public string RoleName { get; set; }  // This now extracts only the role name
    }

    public class RoleNameConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
                {
                    if (doc.RootElement.TryGetProperty("name", out JsonElement nameElement))
                    {
                        return nameElement.GetString();
                    }
                }
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
    public class userlistModel : PageModel
    {
        public UserResponseModel UserResponse { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public userlistModel(IHttpClientFactory httpClientFactory, ILogger<ErrorModel> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;
        }






        public async Task<IActionResult> OnGetAsync()
        {
            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Redirect("/signin");
            }

            using (var client = new HttpClient())
            {

                accessToken = HttpContext.Session.GetString("SessionToken");

                client.DefaultRequestHeaders.Add("x-session-key", accessToken);
                var apiUrl = $"{_appSettings.BaseUrl}";
                var response = await client.GetAsync($"{apiUrl}/api/users/all_users");
                response.EnsureSuccessStatusCode(); // This will throw an exception if the status code is not successful

                var responseContent = await response.Content.ReadAsStringAsync();
                UserResponse = JsonSerializer.Deserialize<UserResponseModel>(responseContent);
            }

            return Page();
        }
    }
}
