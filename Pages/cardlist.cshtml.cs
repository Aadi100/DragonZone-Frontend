using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class cardlistModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<cardlistModel> _logger;
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;

        public cardlistModel(ILogger<cardlistModel> logger, HttpClient httpClient, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClient = httpClient;
            _appSettings = appSettings.Value;
        }

        public List<Member> Members { get; set; } = new List<Member>();

        public async Task OnGetAsync()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            Members = await GetMembersAsync();
        }

        private async Task<List<Member>> GetMembersAsync()
        {
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await _httpClient.GetAsync($"{apiUrl}/members");
            response.EnsureSuccessStatusCode();
            var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<Member>>(responseStream);
        }

        public class Member
        {
            public string Name { get; set; }
            public string Phone { get; set; }
            public string CardNo { get; set; }
        }
    }

}
