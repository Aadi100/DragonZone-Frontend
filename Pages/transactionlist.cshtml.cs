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
using static POS.Pages.transfercardsModel;

namespace POS.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class transactionlistModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<transactionlistModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;

        public string UserRole { get; private set; }
        public string SelectedOrganizationId { get; set; } // Property to hold the selected person's ID
        public SelectList OrganizationList { get; set; }

        public transactionlistModel(ILogger<transactionlistModel> logger, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings.Value;
        }



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

            UserRole = HttpContext.Session.GetString("UserRole");

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
            if (request?.Organization == null || request?.Branch == null)
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

            var Data = new
            {
                organization = request.Organization,
                branch = request.Branch,
                start_date = request.StartDateEpoch,
                end_date = request.EndDateEpoch
            };

            var content = new StringContent(JsonSerializer.Serialize(Data), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/accounts/list_transactions", content);

            List<Transaction> allTransactions = new List<Transaction>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var responseDataTransaction = doc.RootElement.GetProperty("response_data").EnumerateArray();

                    allTransactions = responseDataTransaction
                        .Select(transaction => new Transaction
                        {
                            Id = transaction.GetProperty("id").GetString(),
                            TransactionId = transaction.GetProperty("transaction_id").GetString(),
                            Organization = transaction.GetProperty("organization").GetString(),
                            Branch = transaction.GetProperty("branch").GetString(),
                            Amount = transaction.GetProperty("amount").GetDecimal(),
                            Profit_Admin = transaction.GetProperty("profit_admin").GetDecimal(),
                            Profit_ORG = transaction.GetProperty("profit_org").GetDecimal(),
                            Member = transaction.GetProperty("member").GetString(),
                            Purpose = transaction.GetProperty("purpose").GetString()
                        })
                        .ToList();
                }

                // Return the result as JsonResult
                return new JsonResult(new { allTransactions });
            }
            else
            {
                _logger.LogError("Error posting card data: {StatusCode}, {ResponseBody}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return StatusCode((int)response.StatusCode, "Error saving the selected cards.");
            }
        }

        public class SubmitRequest
        {
            public string Organization { get; set; }
            public string Branch { get; set; }
            public long StartDateEpoch { get; set; }
            public long EndDateEpoch { get; set; }
        }




        [HttpPost]
        public async Task<IActionResult> OnPostInvoice([FromBody] SelectedIdsModel model)
        {
            

            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized();
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-session-key", accessToken);

            var Data = new
            {
                transaction = model.SelectedIds,
                organization = model.Organization,
                branch = model.Branch,
            };

            var content = new StringContent(JsonSerializer.Serialize(Data), Encoding.UTF8, "application/json");
            var apiUrl = $"{_appSettings.BaseUrl}";
            var response = await client.PostAsync($"{apiUrl}/api/invoice/create", content);


            if (response.IsSuccessStatusCode)
            {
                return Page();
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }


        public class SelectedIdsModel
        {
            public List<string> SelectedIds { get; set; }
            public string Organization { get; set; }
            public string Branch { get; set; }
        }

        public class TransactionRequest
        {
            public string Organization { get; set; }
            public string Branch { get; set; }
            public long StartDateEpoch { get; set; }
            public long EndDateEpoch { get; set; }
        }

        // Model for the transaction
        public class Transaction
        {
            public string Id { get; set; }
            public string TransactionId { get; set; }
            public string Organization { get; set; }
            public string Branch { get; set; }
            public decimal Amount { get; set; }
            public decimal Profit_Admin { get; set; }
            public decimal Profit_ORG { get; set; }
            public string Member { get; set; }
            public string Purpose { get; set; }
        }

    }

}
