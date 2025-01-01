    using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using POS.Services;
using Microsoft.Extensions.Options;



namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]


    public class EmployeeViewModel
    {
        public int Role { get; set; }

    }
    public class addemployeeModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<addemployeeModel> _logger;
        private readonly SerialPortService _serialPortService;
        private readonly AppSettings _appSettings;


        public addemployeeModel(IHttpClientFactory httpClientFactory, ILogger<addemployeeModel> logger, SerialPortService serialPortService, IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _serialPortService = serialPortService;
            _appSettings = appSettings.Value;
        }
        [BindProperty]
        public EmployeeInputModel Employee { get; set; }
        public EmployeeViewModel Emp { get; set; } = new EmployeeViewModel();

        public string ReceivedData { get; private set; }

        public string cleanedData { get; private set; }
        public string ExtractedcardUID { get; private set; }

        public string cardUID { get; private set; }


        public string SelectedRoleId { get; set; }
        public SelectList RoleList { get; set; }

        public string SelectedOrganizationId { get; set; } // Property to hold the selected person's ID

        public SelectList OrganizationList { get; set; }

        public string SelectedBranchId { get; set; } // Property to hold the selected person's ID

        public SelectList BranchList { get; set; }

        public string SelectedrfcardId { get; set; } // Property to hold the selected person's ID

        public SelectList rfcardList { get; set; }

        public SelectList cardList { get; set; }


        public string SelectedManagerId { get; set; } // Property to hold the selected person's ID

        public SelectList ManagerList { get; set; }


        public class EmployeeInputModel
        {
            [Required]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Phone]
            public string Mobile { get; set; }

            [Required]
            public string NIC { get; set; }

            [Required]
            public string Password { get; set; }

            [Required]
            public string Gender { get; set; }

            [Required]
            public string Card_id { get; set; }

            [Required]
            public string City { get; set; }

            [Required]
            public string Role { get; set; }

            [Required]
            public string Manager { get; set; }

            [Required]
            public string Organization { get; set; }

            [Required]
            public string Branch { get; set; }

            [Required]
            public string HiddenCardId { get; set; }




        }




        public async Task<IActionResult> OnGetAsync()
        {

            
            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Redirect("/signin");
            }

            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.GetAsync($"{apiUrl}/api/static-data");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var rolesArray = doc.RootElement.GetProperty("user_roles_and_rights").EnumerateArray();

                    var roles = rolesArray.Select(role => new
                    {
                        Id = role.GetProperty("user_role_id").GetInt32(),
                        Name = role.GetProperty("name").GetString()
                    }).ToList();

                    RoleList = new SelectList(roles, "Id", "Name");
                }
            }


            accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

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


            var responsemanager = await client.GetAsync($"{apiUrl}/api/users/read");

            if (responsemanager.IsSuccessStatusCode)
            {
                var json = await responsemanager.Content.ReadAsStringAsync();


                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDataManager = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    var managers = responseDataManager.Select(managers => new
                    {
                        Id = managers.GetProperty("id").GetString(),
                        Name = managers.GetProperty("name").GetString()
                    }).ToList();

                    ManagerList = new SelectList(managers, "Id", "Name");
                }
            }



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

            }

            return new JsonResult(new { card_id = (string)null });
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

            var employeeData = new
            {
                name = Employee.Name,
                email_address = Employee.Email,
                phone_number = Employee.Mobile,
                nic = Employee.NIC,
                role = Employee.Role,
                password = Employee.Password,
                gender = Employee.Gender,
                card_id = Employee.HiddenCardId,
                city = Employee.City,
                manager = Employee.Manager,
                organization = Employee.Organization,
                branch = Employee.Branch
            };

            var content = new StringContent(JsonSerializer.Serialize(employeeData), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/users/create", content);

            if (response.IsSuccessStatusCode)
            {
                
                return Page();
            }
            else
            {
                //_logger.LogError("Error posting member data: {StatusCode}", response.StatusCode);
                //ModelState.AddModelError(string.Empty, "There was an error saving the member.");
                return Page();
                //return Redirect("/response");
            }
        }


        

    }

}
