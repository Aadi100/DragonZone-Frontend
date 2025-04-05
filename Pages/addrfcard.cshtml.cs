using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using POS.Services;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace POS.Pages
{
    public class addrfcardModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<addrfcardModel> _logger;
        private readonly SerialPortService _serialPortService;
        private readonly AppSettings _appSettings;

        [BindProperty]
        public string ReceivedData { get; private set; }

        public string cleanedData { get; private set; }
        public string ExtractedcardUID { get; private set; }

        public SelectList BranchList { get; set; }
        public SelectList OrganizationList { get; set; }

        public addrfcardModel(IHttpClientFactory httpClientFactory, ILogger<addrfcardModel> logger, SerialPortService serialPortService, IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _serialPortService = serialPortService;
            _appSettings = appSettings.Value;
        }

        [BindProperty]
        public rfcardInputModel RF { get; set; }

        public class rfcardInputModel
        {
            [Required]
            public string Organization { get; set; }

            [Required]
            public string UID { get; set; }

            [Required]
            public string Branch { get; set; }

            [Required]
            public string Assigned { get; set; }

            [Required]
            public int Credit { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");

            if (string.IsNullOrEmpty(accessToken))
            {
                return Redirect("/signin");
            }

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);
            var apiUrl = $"{_appSettings.BaseUrl}";
            var responseOrg = await client.GetAsync($"{apiUrl}/api/organization/list_organization");

            if (responseOrg.IsSuccessStatusCode)
            {
                var json = await responseOrg.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDataOrg = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    var orgs = responseDataOrg.Select(org => new
                    {
                        Id = org.GetProperty("id").GetString(),
                        Name = org.GetProperty("name").GetString()
                    }).ToList();

                    OrganizationList = new SelectList(orgs, "Id", "Name");
                }
            }

            BranchList = new SelectList(new List<SelectListItem>());



            return Page();
        }



        [HttpGet]
        public async Task<JsonResult> OnGetReceiveDataAsync()
        {
            ReceivedData = _serialPortService.GetLatestData();
            if (!string.IsNullOrEmpty(ReceivedData))
            {
                cleanedData = ReceivedData.Replace("&#xD;", "").Replace("&#xA;", "").Trim();
                ExtractedcardUID = cleanedData.Substring(cleanedData.IndexOf(':') + 1).Replace(" ", "").Trim().ToUpper();
            }

            return new JsonResult(new { extractedCardUID = ExtractedcardUID });
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

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            var rfcardData = new
            {
                card_uid = RF.UID,
                branch = RF.Branch,
                organization = RF.Organization,
                assigned = RF.Assigned,
                credit = RF.Credit
            };

            var content = new StringContent(JsonSerializer.Serialize(rfcardData), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/rfid/create", content);

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
