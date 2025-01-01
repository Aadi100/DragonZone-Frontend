using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using static POS.Pages.SigninModel;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class addgameModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<addgameModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;  

        public addgameModel(ILogger<addgameModel> logger, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;
        }

        [BindProperty]
        public GameInputModel Game { get; set; }
        public SelectList BranchList { get; set; }
        public SelectList OrganizationList { get; set; }
        public class GameInputModel
        {
            [Required]
            public string Name { get; set; }

            [Required]
            public string Type { get; set; }

            [Required]
            public int GameLevel { get; set; }

            [Required]
            [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid cost")]
            public decimal Cost { get; set; }

            [Required]
            public string UnitStatus { get; set; }

            [Required]
            public string Organization { get; set; }

            [Required]
            public string Branch { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Redirect("/signin");
            }

            var client = _httpClientFactory.CreateClient();

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



            BranchList = new SelectList(new List<SelectListItem>());




            // Handle GET request if needed
            return Page();
        }



        [HttpGet]
        public async Task<IActionResult> OnGetBranchesAsync(string organizationId)
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            // Fetch the branches from the API
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.GetAsync($"{apiUrl}/api/branch/list_branchs_ids");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDataBranch = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    // Filter branches based on the selected organizationId
                    var filteredBranches = responseDataBranch
                        .Where(branch => branch.GetProperty("organization").GetString() == organizationId)
                        .Select(branch => new
                        {
                            Id = branch.GetProperty("id").GetString(),
                            Name = branch.GetProperty("name").GetString()
                        })
                        .ToList();

                    // Return filtered branches
                    return new JsonResult(filteredBranches.Select(b => new SelectListItem { Value = b.Id, Text = b.Name }));
                }
            }

            return new JsonResult(new List<SelectListItem>());
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _httpClientFactory.CreateClient();

            var accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);


            var gameData = new
            {
                name =Game.Name,
                type = Game.Type,
                game_level = Game.GameLevel,
                unit_status = Game.UnitStatus,
                cost = Game.Cost,
                branch = Game.Branch,
                organization = Game.Organization,
            };


            var gameJson = JsonSerializer.Serialize(Game);

            var content = new StringContent(JsonSerializer.Serialize(gameData), Encoding.UTF8, "application/json");

            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/gameunit/create", content);

            if (response.IsSuccessStatusCode)
            {
                //var responseContent = await response.Content.ReadAsStringAsync();
                //HttpContext.Session.SetString("FullResponse", responseContent);
                //return RedirectToPage("/response");
                return Page();

            }
            else
            {
                _logger.LogError("Error posting game data: {StatusCode}", response.StatusCode);
                ModelState.AddModelError(string.Empty, "There was an error saving the game.");
                return Page();
            }
        }


    }

}
