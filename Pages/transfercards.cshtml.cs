using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using POS.Services;
using static POS.Pages.posModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http;
using System.Text;
using static POS.Pages.addrfcardModel;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class transfercardsModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<transfercardsModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        [BindProperty]
        public ORGInputModel ORG { get; set; }

        public string SelectedOrganizationId { get; set; } // Property to hold the selected person's ID

        public SelectList OrganizationList { get; set; }

        public class ORGInputModel
        {
            [Required]
            public String Name { get; set; }

        }
            public transfercardsModel(ILogger<transfercardsModel> logger, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;
        }


        public class Card
        {
            public string Id { get; set; }

            public string CardId { get; set; }
            public bool Assigned { get; set; }
        }
        public List<Card> Cards { get; set; } = new List<Card>();


        [BindProperty]
        public cardInputModel card { get; set; }

        public class cardInputModel
        {
            [Required]
            public string Organization { get; set; }
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
            return Page();

        }

        [HttpGet]
        public async Task<IActionResult> OnGetCardsAsync(string organizationId)
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);


            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.GetAsync($"{apiUrl}/api/rfid/list_rfcard_ids");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDataCards = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    if (string.IsNullOrEmpty(organizationId))
                    {
                        return new JsonResult(new List<object>());
                    }

                    var filteredCards = responseDataCards
                        .Where(card => card.GetProperty("organization").GetString() == organizationId)
                        .Select(card => new
                        {
                            Id = card.GetProperty("id").GetString(),
                            CardId = card.GetProperty("card_id").GetString(),
                            Assigned = card.GetProperty("assigned").GetBoolean()
                        })
                        .ToList();

                    return new JsonResult(filteredCards);
                }
            }

            return new JsonResult(new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> OnGetBranchesAsync(string organizationId)
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.GetAsync($"{apiUrl}/api/branch/list_branchs_ids");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDataBranch = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    var filteredBranches = responseDataBranch
                        .Where(branch => branch.GetProperty("organization").GetString() == organizationId)
                        .Select(branch => new
                        {
                            Id = branch.GetProperty("id").GetString(),
                            Name = branch.GetProperty("name").GetString()
                        })
                        .ToList();

                    return new JsonResult(filteredBranches.Select(b => new SelectListItem { Value = b.Id, Text = b.Name }));
                }
            }

            return new JsonResult(new List<SelectListItem>());
        }

        [HttpPost]
        public async Task<IActionResult> OnPostSubmit([FromBody] SubmitRequest request)
        {
            if (request?.selectedCardIds == null || !request.selectedCardIds.Any() ||
                string.IsNullOrEmpty(request.organizationId) || string.IsNullOrEmpty(request.branchId))
            {
                return BadRequest("Required fields are missing.");
            }

            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized();
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            var rfcardData = new
            {
                ids = request.selectedCardIds,
                organization = request.organizationId,
                branch = string.IsNullOrEmpty(request.branchId) ? null : request.branchId
            };

            var content = new StringContent(JsonSerializer.Serialize(rfcardData), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/rfid/transfer", content);

            if (response.IsSuccessStatusCode)
            {
                return Page(); 
            }
            else
            {
                _logger.LogError("Error posting card data: {StatusCode}, {ResponseBody}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return StatusCode((int)response.StatusCode, "Error saving the selected cards.");
            }
        }

        public class SubmitRequest
        {
            public List<string> selectedCardIds { get; set; }
            public string organizationId { get; set; } 
            public string branchId { get; set; }
        }


    }

    }
