using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]

    public class BranchResponseModel
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_data")]
        public List<BranchResponseData> ResponseData { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }
    }

    public class BranchResponseData
    {
        [JsonPropertyName("branch_id")]
        public string BranchId { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("closing_time")]
        public int ClosingTime { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("opening_time")]
        public int OpeningTime { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }
    }


    public class branchlistModel : PageModel
    {
        public BranchResponseModel BranchResponse { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public branchlistModel(IHttpClientFactory httpClientFactory, ILogger<ErrorModel> logger, IOptions<AppSettings> appSettings)
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
                return RedirectToPage("/signin");
            }

            using (var client = new HttpClient())
            {

                accessToken = HttpContext.Session.GetString("SessionToken");

                client.DefaultRequestHeaders.Add("x-session-key", accessToken);
                var apiUrl = $"{_appSettings.BaseUrl}";
                var response = await client.GetAsync($"{apiUrl}/api/branch/list_branchs");
                response.EnsureSuccessStatusCode(); // This will throw an exception if the status code is not successful

                var responseContent = await response.Content.ReadAsStringAsync();
                BranchResponse = JsonSerializer.Deserialize<BranchResponseModel>(responseContent);
            }

            return Page();
        }
    }

}
