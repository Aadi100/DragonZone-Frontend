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

    public class MemberResponseModel
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_data")]
        public List<ResponseDataMember> ResponseDataMember { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }
    }

    public class ResponseDataMember
    {
        [JsonPropertyName("email_address")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("member_id")]
        public string MemberId { get; set; }

        [JsonPropertyName("membership_level")]
        public int MembershipLevel { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("parent")]
        public string Parent { get; set; }


        [JsonPropertyName("card_id")]
        public string Card_Id { get; set; }


        [JsonPropertyName("organization")]
        public string Organization { get; set; }
    }
    public class memberlistModel : PageModel
    {
        public MemberResponseModel MemberResponse { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public memberlistModel(IHttpClientFactory httpClientFactory, ILogger<ErrorModel> logger, IOptions<AppSettings> appSettings)
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
                var response = await client.GetAsync($"{apiUrl}/api/members/list_members");
                response.EnsureSuccessStatusCode(); // This will throw an exception if the status code is not successful

                var responseContent = await response.Content.ReadAsStringAsync();
                MemberResponse = JsonSerializer.Deserialize<MemberResponseModel>(responseContent);
            }

            return Page();
        }

    }
}
