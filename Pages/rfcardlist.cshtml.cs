using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]

    public class rfcardResponseModel
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_data")]
        public List<ResponseDatacard> ResponseDatacard { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }
    }

    public class ResponseDatacard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("card_id")]
        public string Card_id { get; set; }

        [JsonPropertyName("assigned")]
        public bool Status { get; set; }


        [JsonPropertyName("branch")]
        public string Branch { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }

    }
    public class rfcardlistModel : PageModel
    {

        public rfcardResponseModel RfcardResponse { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public rfcardlistModel(IHttpClientFactory httpClientFactory, ILogger<ErrorModel> logger, IOptions<AppSettings> appSettings)
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
                var response = await client.GetAsync($"{apiUrl}/api/rfid/list_rfcards");
                response.EnsureSuccessStatusCode(); // This will throw an exception if the status code is not successful

                var responseContent = await response.Content.ReadAsStringAsync();
                RfcardResponse = JsonSerializer.Deserialize<rfcardResponseModel>(responseContent);
            }

            return Page();
        }

    }
}
