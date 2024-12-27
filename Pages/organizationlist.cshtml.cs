using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]

    public class OrgResponseModel
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_data")]
        public List<ResponseData> ResponseData { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }
    }

    public class ResponseData
    {
        [JsonPropertyName("organization_id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("cp_email_address")]
        public string CpEmailAddress { get; set; }

        [JsonPropertyName("cp_phone_number")]
        public List<string> CpPhoneNumber { get; set; }
    }

    public class organizationlistModel : PageModel
    {
        public OrgResponseModel OrgResponse { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public organizationlistModel(IHttpClientFactory httpClientFactory,ILogger<ErrorModel> logger, IOptions<AppSettings> appSettings)
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
                var response = await client.GetAsync($"{apiUrl}/api/organization/list_organization");
                response.EnsureSuccessStatusCode(); // This will throw an exception if the status code is not successful

                var responseContent = await response.Content.ReadAsStringAsync();
                OrgResponse = JsonSerializer.Deserialize<OrgResponseModel>(responseContent);
            }

            return Page();
        }



    }

}
