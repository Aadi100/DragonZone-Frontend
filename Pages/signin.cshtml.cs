using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    public class SigninModel : PageModel
    {
        private readonly ILogger<SigninModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public SigninModel(ILogger<SigninModel> logger, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class LoginInputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            

            var loginData = new
            {
                email_address = Input.Email,
                password = Input.Password
            };
            var apiUrl = $"{_appSettings.BaseUrl}/api/users/login";
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<ResponseModel>(responseContent);

                string accessToken = responseObject.response_data.access_token;
                string userId = responseObject.response_data.user.id;
                string branch = responseObject.response_data.user.branch;
                string organization = responseObject.response_data.user.organization;
                //string roleName = responseObject.response_data.user.role_name;

                HttpContext.Session.SetString("SessionToken", accessToken);
                HttpContext.Session.SetString("UserId", userId);
                HttpContext.Session.SetString("UserBranch", branch);
                HttpContext.Session.SetString("UserOrganization", organization);
                //HttpContext.Session.SetString("UserRole", roleName);

                if (accessToken != null)
                {
                    return RedirectToPage("/Index");
                }
                return RedirectToPage("/Response");
            }
            else
            {
                ErrorMessage = "Invalid login attempt.";
                return Page();
            }
        }

        public class ResponseModel
        {
            public int response_code { get; set; }
            public ResponseData response_data { get; set; }
            public string response_message { get; set; }
        }

        public class ResponseData
        {
            public string access_token { get; set; }
            public long expiry_time { get; set; }
            public User user { get; set; }
        }

        public class User
        {
            public string id { get; set; }
            public string branch { get; set; }
            public string organization { get; set; }
            public string role_name { get; set; }
        }
    }
}
