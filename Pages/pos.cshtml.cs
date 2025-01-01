using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using static POS.Pages.SigninModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using POS.Services;
using Microsoft.Extensions.Options;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class posModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<posModel> _logger;
        private readonly SerialPortService _serialPortService;
        private readonly AppSettings _appSettings;

        public posModel(IHttpClientFactory httpClientFactory, ILogger<posModel> logger, SerialPortService serialPortService, IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _serialPortService = serialPortService;
            _appSettings = appSettings.Value;
        }

        [BindProperty]
        public PosInputModel POS { get; set; }
        public SelectList memberList { get; set; }

        public class PosInputModel
        {
            [Required]
            public int Amount { get; set; }

            [Required]
            public string Purpose { get; set; }

            [Required]
            public string Card_id { get; set; } // Used for displaying the card_id in the textbox

            [Required]
            public string HiddenCardID { get; set; } // This will store the respective id and be sent in the POST request

            [Required]
            public string Member { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Redirect("/signin");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-session-key", accessToken);
            var apiUrl = $"{_appSettings.BaseUrl}";
            var responsemember = await client.GetAsync($"{apiUrl}/api/members/list_members");

            if (responsemember.IsSuccessStatusCode)
            {
                var json = await responsemember.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDatamember = doc.RootElement.GetProperty("response_data").EnumerateArray();
                    var members = responseDatamember.Select(mem => new
                    {
                        Id = mem.GetProperty("member_id").GetString(),
                        member_id = mem.GetProperty("id").GetString()
                    }).ToList();

                    memberList = new SelectList(members, "member_id", "Id");
                }
            }

            return Page();
        }

        [HttpGet]
        public async Task<IActionResult> OnGetFetchCardUIDAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");

            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Add("x-session-key", accessToken);
            }

            string parsedCardUID = string.Empty;

            var latestCardData = _serialPortService.GetLatestData();
            if (!string.IsNullOrEmpty(latestCardData))
            {
                var cleanedData = latestCardData.Replace("&#xD;", "").Replace("&#xA;", "").Trim();
                parsedCardUID = cleanedData.Substring(cleanedData.IndexOf(':') + 1).Replace(" ", "").Trim().ToUpper();
                HttpContext.Session.SetString("ExtractedCardUID", parsedCardUID);
            }

            try
            {
                var apiUrl = $"{_appSettings.BaseUrl}";
                var cardResponse = await client.GetAsync($"{apiUrl}/api/rfid/list_rfcard_ids");

                if (cardResponse.IsSuccessStatusCode)
                {
                    var json = await cardResponse.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var responseDatarfcard = doc.RootElement.GetProperty("response_data").EnumerateArray();

                        // Find the card where card_uid matches the extracted card UID
                        var matchedCard = responseDatarfcard
                            .FirstOrDefault(card => card.GetProperty("card_uid").GetString() == parsedCardUID);

                        if (matchedCard.ValueKind != JsonValueKind.Undefined)
                        {
                            var matchedCardId = matchedCard.GetProperty("id").GetString();
                            var matchedCardCardId = matchedCard.GetProperty("card_id").GetString(); // Get the card_id (e.g., "CI-2")

                            return new JsonResult(new { card_id = matchedCardCardId, id = matchedCardId }); // Return card_id and id
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
            }

            return new JsonResult(new { card_id = (string)null, id = (string)null });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");
            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            var posData = new
            {
                amount = POS.Amount,
                purpose = POS.Purpose,
                card_id = POS.HiddenCardID,
                //member_id = POS.Member,
            };

            var content = new StringContent(JsonSerializer.Serialize(posData), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/accounts/credit", content);

            if (response.IsSuccessStatusCode)
            {
                return Page();
            }
            else
            {
                _logger.LogError("Error posting member data: {StatusCode}", response.StatusCode);
                ModelState.AddModelError(string.Empty, "There was an error saving the member.");
                return Page();
            }
        }
    }
}
