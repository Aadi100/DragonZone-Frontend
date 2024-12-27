using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.IO.Ports;
using static POS.Pages.addemployeeModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    public class addbranchModel : PageModel
    {

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<addbranchModel> _logger;
        private readonly AppSettings _appSettings;

        public addbranchModel(IHttpClientFactory httpClientFactory, ILogger<addbranchModel> logger, IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        [BindProperty]
        public BranchInputModel Branch { get; set; }

        public string SelectedOrganizationId { get; set; } // Property to hold the selected person's ID

        public SelectList OrganizationList { get; set; }

        public string SelectedUserId { get; set; } // Property to hold the selected person's ID

        public SelectList UsersList { get; set; }

        public string SelectedTypeId { get; set; }
        public SelectList TypeList { get; set; }


        public class BranchInputModel
        {
            [Required]
            public string Name { get; set; }

            [Required]
            public string User { get; set; }

            [Required]
            public string City { get; set; }

            [Required]
            public List<string> Game_Type { get; set; }

            [Required]
            public string Opening_Time { get; set; }

            [Required]
            public string Closing_Time { get; set; }

            [Required]
            public string Organization { get; set; }


        }


        [BindProperty]
        public string SerialData { get; set; }


        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                using (SerialPort serialPort = new SerialPort("COM8", 9600))
                {
                    serialPort.Open();
                    SerialData = serialPort.ReadLine();
                    serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from serial port.");
                SerialData = "Error reading from serial port.";
            }
            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/signin");
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

            //var response = await client.GetAsync("http://127.0.0.1:5001/api/static-data");

            //if (response.IsSuccessStatusCode)
            //{
            //    var json = await response.Content.ReadAsStringAsync();
            //    using (JsonDocument doc = JsonDocument.Parse(json))
            //    {
            //        var typesArray = doc.RootElement.GetProperty("game_types").EnumerateArray();

            //        var types = typesArray.Select(types => new
            //        {
            //            Id = types.GetProperty("user_role_id").GetInt32(),
            //            Name = types.GetProperty("name").GetString()
            //        }).ToList();

            //        TypeList = new SelectList(types, "Id", "Name");
            //    }
            //}

            var responseuser = await client.GetAsync($"{apiUrl}/api/users/read");

            if (responseuser.IsSuccessStatusCode)
            {
                var json = await responseuser.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDataUser = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    var users = responseDataUser.Select(users => new
                    {
                        Id = users.GetProperty("id").GetString(),
                        Name = users.GetProperty("name").GetString()
                    }).ToList();

                    UsersList = new SelectList(users, "Id", "Name");
                }
            }



            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {


            var client = _httpClientFactory.CreateClient();

            var accessToken = HttpContext.Session.GetString("SessionToken");

            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            var branchData = new
            {
                name = Branch.Name,
                city = Branch.City,
                users = Branch.User,
                game_types = Branch.Game_Type,
                opening_time = Branch.Opening_Time,
                closing_time = Branch.Closing_Time,
                organization = Branch.Organization,

            };

            var content = new StringContent(JsonSerializer.Serialize(branchData), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/branch/create", content);

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
               // return Page();
                return RedirectToPage("/Index");
            }
        }



    }
}
