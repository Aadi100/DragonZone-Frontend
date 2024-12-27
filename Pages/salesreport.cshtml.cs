using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]

    public class salesResponseModel
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_data")]
        public List<salesResponseData> ResponseData { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }
    }


    public class salesResponseData
    {
        [JsonPropertyName("transaction_id")]
        public string ID { get; set; }

        [JsonPropertyName("member")]
        public string Member { get; set; }

        [JsonPropertyName("amount")]
        public int amount { get; set; }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }


        [JsonPropertyName("branch")]
        public string Branch { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }

    }

        public class salesreportModel : PageModel
    {
        public salesResponseModel salesResponse { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public salesreportModel(IHttpClientFactory httpClientFactory, ILogger<ErrorModel> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;
        }



        [BindProperty]
        public ORGInputModel ORG { get; set; }

        public string SelectedOrganizationId { get; set; } // Property to hold the selected person's ID

        public SelectList OrganizationList { get; set; }

        public class ORGInputModel
        {
            [Required]
            public string Organization { get; set; }
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
                var response = await client.GetAsync($"{apiUrl}/api/accounts/list_transactions");
                response.EnsureSuccessStatusCode(); // This will throw an exception if the status code is not successful

                var responseContent = await response.Content.ReadAsStringAsync();
                salesResponse = JsonSerializer.Deserialize<salesResponseModel>(responseContent);
            }

            using (var client = new HttpClient())
            {
                accessToken = HttpContext.Session.GetString("SessionToken");

                client.DefaultRequestHeaders.Add("x-session-key", accessToken);
                var apiUrl = $"{_appSettings.BaseUrl}";
                var responseorg = await client.GetAsync($"{apiUrl}/api/organization/list_organization");

                if (responseorg.IsSuccessStatusCode)
                {
                    var json = await responseorg.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var responseDataOrg = doc.RootElement.GetProperty("response_data").EnumerateArray();

                        var orgs = responseDataOrg.Select(orgs => new
                        {
                            Id = orgs.GetProperty("id").GetString(),
                            Name = orgs.GetProperty("name").GetString()
                        }).ToList();

                        OrganizationList = new SelectList(orgs, "Id", "Name");
                    }
                }
            }


            return Page();
        }



    }
}


