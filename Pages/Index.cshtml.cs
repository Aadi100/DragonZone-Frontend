using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using POS.Services;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;


namespace POS.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;


        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;

        }
        public DashboardData Dashboard { get; set; }


        public async Task<IActionResult> OnGetAsync()
        {

            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/signin");
            }

            Dashboard = new DashboardData
            {
                TotalGames = await GetTotalGamesAsync(),
                TotalUsers = await GetTotalUsersAsync(),
                TotalBranches = await GetTotalBranchesAsync(),
                //TotalClients = await GetTotalClientsAsync(),
                TotalCards = await GetTotalCardsAsync(),
                TotalOrganizations = await GetTotalOrganizationsAsync(),

            };

            return Page();
            // Handle GET request if needed
        }

        private async Task<int> GetTotalGamesAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_appSettings.BaseUrl}";

            var response = await client.GetAsync($"{apiUrl}/api/gameunit/list_gameunits");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonResponse);

            if (apiResponse.ResponseCode == 200)
            {
                return apiResponse.ResponseData.Count; // Count the number of games
            }

            return 0; // Return 0 if the response is not successful
        }

        private async Task<int> GetTotalUsersAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_appSettings.BaseUrl}";

            var response = await client.GetAsync($"{apiUrl}/api/users/all_users");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<UserApiResponse>(jsonResponse);

            if (apiResponse.ResponseCode == 200)
            {
                return apiResponse.ResponseData.Count; // Count the number of users
            }

            return 0; // Return 0 if the response is not successful
        }

        private async Task<int> GetTotalCardsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_appSettings.BaseUrl}";

            var response = await client.GetAsync($"{apiUrl}/api/rfid/list_rfcards");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<CardApiResponse>(jsonResponse);

            if (apiResponse.ResponseCode == 200)
            {
                return apiResponse.ResponseData.Count; // Count the number of cards
            }

            return 0; // Return 0 if the response is not successful
        }

        private async Task<int> GetTotalOrganizationsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_appSettings.BaseUrl}";

            var response = await client.GetAsync($"{apiUrl}/api/organization/list_organization");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<OrganizationApiResponse>(jsonResponse);

            if (apiResponse.ResponseCode == 200)
            {
                return apiResponse.ResponseData.Count; // Count the number of organizations
            }

            return 0; // Return 0 if the response is not successful
        }

        private async Task<int> GetTotalBranchesAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_appSettings.BaseUrl}";

            var response = await client.GetAsync($"{apiUrl}/api/branch/list_branchs");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<BranchApiResponse>(jsonResponse);

            if (apiResponse.ResponseCode == 200)
            {
                return apiResponse.ResponseData.Count; // Count the number of branches
            }

            return 0; // Return 0 if the response is not successful
        }

        //private async Task<int> GetTotalClientsAsync()
        //{
        //    var client = _httpClientFactory.CreateClient();
        //var apiUrl = $"{_appSettings.BaseUrl}";

        //    var response = await client.GetAsync($"{apiUrl}/api/rfid/list_rfcards");
        //    response.EnsureSuccessStatusCode();

        //    var jsonResponse = await response.Content.ReadAsStringAsync();
        //    var apiResponse = JsonSerializer.Deserialize<ClientApiResponse>(jsonResponse);

        //    if (apiResponse.ResponseCode == 200)
        //    {
        //        return apiResponse.ResponseData.Count; // Count the number of clients
        //    }

        //    return 0; // Return 0 if the response is not successful
        //}


        public class DashboardData
        {
            public int TotalGames { get; set; }
            public int TotalUsers { get; set; }
            public int TotalEmployees { get; set; }
            public int TotalCards { get; set; }
            public int TotalBranches { get; set; } // Added property for total branches
            public int TotalClients { get; set; }

            public int TotalOrganizations { get; set; }
            public decimal TotalSaleAmount { get; set; }
            public int Customers { get; set; }
            public int Cards { get; set; }
            public int PurchaseInvoices { get; set; }
            public int SalesInvoices { get; set; }
        }

        public class ApiResponse
        {
            [JsonPropertyName("response_code")]
            public int ResponseCode { get; set; }

            [JsonPropertyName("response_data")]
            public List<GameData> ResponseData { get; set; }

            [JsonPropertyName("response_message")]
            public string ResponseMessage { get; set; }
        }

        public class GameData
        {
            public string Branch { get; set; }
            public decimal Cost { get; set; }
            public string GameId { get; set; }
            public string Name { get; set; }
            public string Organization { get; set; }
            public string Type { get; set; }
            public string UnitStatus { get; set; }
        }

        public class UserApiResponse
        {
            [JsonPropertyName("response_code")]
            public int ResponseCode { get; set; }
            
            [JsonPropertyName("response_data")]

            public List<UserData> ResponseData { get; set; }
            
            [JsonPropertyName("response_message")]

            public string ResponseMessage { get; set; }
        }

        public class UserData
        {
            public string UserId { get; set; }
            public string Username { get; set; }
            public string Status { get; set; }
        }


        public class CardApiResponse
        {
            [JsonPropertyName("response_code")]
            public int ResponseCode { get; set; }

            [JsonPropertyName("response_data")]

            public List<UserData> ResponseData { get; set; }

            [JsonPropertyName("response_message")]

            public string ResponseMessage { get; set; }
        }

        public class CardData
        {
            public string CardId { get; set; }
            public string CardName { get; set; }
            public string Status { get; set; }
        }

        public class BranchApiResponse
        {
            [JsonPropertyName("response_code")]
            public int ResponseCode { get; set; }

            [JsonPropertyName("response_data")]
            public List<GameData> ResponseData { get; set; }

            [JsonPropertyName("response_message")]
            public string ResponseMessage { get; set; }
        }

        public class BranchData
        {
            public string BranchId { get; set; }
            public string BranchName { get; set; }
            public string Status { get; set; }
        }

        public class ClientApiResponse
        {
            [JsonPropertyName("response_code")]
            public int ResponseCode { get; set; }

            [JsonPropertyName("response_data")]
            public List<GameData> ResponseData { get; set; }

            [JsonPropertyName("response_message")]
            public string ResponseMessage { get; set; }
        }

        public class ClientData
        {
            public string ClientId { get; set; }
            public string ClientName { get; set; }
            public string Status { get; set; }
        }

        public class OrganizationApiResponse
        {
            [JsonPropertyName("response_code")]
            public int ResponseCode { get; set; }

            [JsonPropertyName("response_data")]
            public List<GameData> ResponseData { get; set; }

            [JsonPropertyName("response_message")]
            public string ResponseMessage { get; set; }
        }

        public class OrganizationData
        {
            public string OrganizationId { get; set; }
            public string OrganizationName { get; set; }
            public string Status { get; set; }
        }
    }
}
