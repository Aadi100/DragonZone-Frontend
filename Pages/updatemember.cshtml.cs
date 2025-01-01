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
    public class updatememberModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<updatememberModel> _logger;
        private readonly AppSettings _appSettings;

        public updatememberModel(IHttpClientFactory httpClientFactory, ILogger<updatememberModel> logger, IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
        }
        [BindProperty]
        public UpdateMemberInputModel UpdateMember { get; set; }

        public class UpdateMemberInputModel
        {
            [Required]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }

            
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var memberValue = HttpContext.Request.Cookies["MemberValue"];

            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.GetAsync($"{apiUrl}/api/users/{memberValue}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseData = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    var Id = responseData
                        .First()
                        .GetProperty("id")
                        .GetString();


                    HttpContext.Response.Cookies.Append("MemberId", Id, new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(30) 
                    });

                    Console.WriteLine($"Extracted ID: {Id}");
                }
            }
            else
            {
                Console.WriteLine($"API request failed with status code: {response.StatusCode}");
            }

            return Page();
        }

        

        public async Task<IActionResult> OnPostAsync()
        {
            if (!HttpContext.Request.Cookies.TryGetValue("MemberId", out var ID) || string.IsNullOrEmpty(ID))
            {
                _logger.LogError("Member ID is missing from cookies.");
                ModelState.AddModelError(string.Empty, "Member ID is missing.");
                return Page();
            }

            var client = _httpClientFactory.CreateClient();

            var memberData = new
            {
                id = ID,
                name = UpdateMember.Name,
                email_address = UpdateMember.Email,
                password = UpdateMember.Password,

            };

            var content = new StringContent(JsonSerializer.Serialize(memberData), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PutAsync($"{apiUrl}//api/users/update", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                //HttpContext.Session.SetString("FullResponse", responseContent);
                //return Redirect("/response");
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
