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
    public class addmemberModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<addmemberModel> _logger;
        private readonly SerialPortService _serialPortService;
        private readonly AppSettings _appSettings;

        public addmemberModel(IHttpClientFactory httpClientFactory, ILogger<addmemberModel> logger, SerialPortService serialPortService, IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _serialPortService = serialPortService;
            _appSettings = appSettings.Value;
        }
        [BindProperty]
        public MemberInputModel Member { get; set; }

        public string SelectedrfcardId { get; set; } // Property to hold the selected person's ID
        public SelectList rfcardList { get; set; }
        public SelectList parentList { get; set; }


        public class MemberInputModel
        {
            [Required]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Membership_Level { get; set; }

            [Required]
            [Phone]
            public string Mobile { get; set; }

            [Required]
            public string NIC { get; set; }

            [Required]
            public string Card_id { get; set; }

            [Required]
            public string City { get; set; }

            [Required]
            public string Credit { get; set; }

            [Required]
            public string Reward { get; set; }

            [Required]
            public string Type { get; set; }

            [Required]
            public string Parent { get; set; }

            [Required]
            public string HiddenCardId { get; set; }


        }

        public async Task<IActionResult> OnGetAsync()
        {
            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/signin");
            }

            var cardData = new
            {
                assigned = "False"

            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-session-key", accessToken);
            var apiUrl = $"{_appSettings.BaseUrl}";
            var responserfcard = await client.GetAsync($"{apiUrl}/api/rfid/list_rfcards");

            if (responserfcard.IsSuccessStatusCode)
            {
                var json = await responserfcard.Content.ReadAsStringAsync();


                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDatarfcard = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    var rfcards = responseDatarfcard.Select(rfcards => new
                    {
                        Id = rfcards.GetProperty("card_id").GetString(),
                        card_id = rfcards.GetProperty("id").GetString()
                    }).ToList();
                        
                    rfcardList = new SelectList(rfcards, "card_id", "Id");
                }
            }

            //client.DefaultRequestHeaders.Add("x-session-key", accessToken);
            //var responsemember = await client.GetAsync("http://127.0.0.1:5001/api/members/list_members_id");

            //if (responsemember.IsSuccessStatusCode)
            //{
            //    var json = await responsemember.Content.ReadAsStringAsync();


            //    using (JsonDocument doc = JsonDocument.Parse(json))
            //    {
            //        var responseDatamember = doc.RootElement.GetProperty("response_data").EnumerateArray();

            //        var members = responseDatamember.Select(mem => new
            //        {
            //            Id = mem.GetProperty("member_id").GetString(),
            //            member_id = mem.GetProperty("id").GetString()
            //        }).ToList();

            //        parentList = new SelectList(members, "member_id", "Id");
            //    }
            //}


            return Page();
        }

        [HttpGet]
        public async Task<IActionResult> OnGetReceiveAndFetchCardsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");

            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Add("x-session-key", accessToken);
            }

            string extractedCardUID = string.Empty;
            var receivedData = _serialPortService.GetLatestData();

            if (!string.IsNullOrEmpty(receivedData))
            {
                var cleanedData = receivedData.Replace("&#xD;", "").Replace("&#xA;", "").Trim();
                extractedCardUID = cleanedData.Substring(cleanedData.IndexOf(':') + 1).Replace(" ", "").Trim().ToUpper();

                HttpContext.Session.SetString("ExtractedCardUID", extractedCardUID);
            }

            try
            {
                var apiUrl = $"{_appSettings.BaseUrl}";
                var responseCards = await client.GetAsync($"{apiUrl}/api/rfid/list_rfcard_ids");
                if (responseCards.IsSuccessStatusCode)
                {
                    var json = await responseCards.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var responseDatarfcard = doc.RootElement.GetProperty("response_data").EnumerateArray();
                        var filteredCard = responseDatarfcard
                            .FirstOrDefault(card => card.GetProperty("card_uid").GetString() == extractedCardUID);

                        if (filteredCard.ValueKind != JsonValueKind.Undefined)
                        {
                            var cardId = filteredCard.GetProperty("card_id").GetString();
                            var id = filteredCard.GetProperty("id").GetString(); // Fetch the actual id

                            return new JsonResult(new { card_id = cardId, id = id });  // Return both card_id and id
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching card data.");
            }

            return new JsonResult(new { card_id = (string)null });
        }


        public async Task<IActionResult> OnPostAsync()
        {

            var client = _httpClientFactory.CreateClient();

            var accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            var memberData = new
            {
                name = Member.Name,
                email_address = Member.Email,
                membership_level = Member.Membership_Level,
                phone_number = Member.Mobile,
                nic = Member.NIC,
                card_id = Member.HiddenCardId,
                city = Member.City,
                credit = Member.Credit,
                reward = Member.Reward,
                type = Member.Type,
                //parent = Member.Parent,
            };

            var content = new StringContent(JsonSerializer.Serialize(memberData), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/members/create", content);

            if (response.IsSuccessStatusCode)
            {
                //var responseContent = await response.Content.ReadAsStringAsync();
                //HttpContext.Session.SetString("FullResponse", responseContent);
                //return RedirectToPage("/response");
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
