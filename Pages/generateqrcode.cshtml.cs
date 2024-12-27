using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using QRCoder;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using POS.Services;

namespace POS.Pages
{
    public class generateqrcodeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<generateqrcodeModel> _logger;
        private readonly AppSettings _appSettings;

        [BindProperty]
        public ORGInputModel ORG { get; set; }


        [BindProperty]
        public string SelectedItem { get; set; }

        public List<SelectListItem> Items { get; set; } = new List<SelectListItem>();

        public string QrCodeImage { get; set; }

        public string SelectedOrganizationId { get; set; } 

        public SelectList OrganizationList { get; set; }

        public class ORGInputModel
        {
            [Required]
            public string Organization { get; set; }
        }

        public generateqrcodeModel(IHttpClientFactory httpClientFactory, ILogger<generateqrcodeModel> logger, IOptions<AppSettings> appSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var accessToken = HttpContext.Session.GetString("SessionToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/signin");
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(ORG?.Organization))
            {
                var selectedOrgId = ORG.Organization;
                var api = $"{_appSettings.BaseUrl}";
                var apiUrl = $"{api}/api/users/all";
                var client = _httpClientFactory.CreateClient();
                var accessToken = HttpContext.Session.GetString("SessionToken");
                client.DefaultRequestHeaders.Add("x-session-key", accessToken);

                try
                {
                    var response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonResponse);

                    if (apiResponse?.ResponseCode == 200)
                    {
                        var filteredCards = apiResponse.ResponseData
                            .Where(item => item[2] == selectedOrgId) 
                            .ToList();

                        using (var memoryStream = new MemoryStream())
                        {
                            using (Document pdfDoc = new Document())
                            {
                                PdfWriter.GetInstance(pdfDoc, memoryStream);
                                pdfDoc.Open();

                                foreach (var card in filteredCards)
                                {
                                    var cardName = card[0]; 
                                    var cardUrl = card[1];  

                                    var qrCodeImage = GenerateQrCode(cardUrl);

                                    var qrCodeImageInstance = iTextSharp.text.Image.GetInstance(Convert.FromBase64String(qrCodeImage));
                                    pdfDoc.Add(qrCodeImageInstance);
                                    pdfDoc.Add(new Paragraph(cardName)); 
                                }

                                pdfDoc.Close();
                            }

                            var fileName = "QRCodeCards.pdf";
                            return File(memoryStream.ToArray(), "application/pdf", fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while fetching users or generating PDF.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Please select an organization.");
            }

            return Page();
        }

        private string GenerateQrCode(string url)
        {
            int sizeInInches = 1;
            int dpi = 300; 
            int sizeInPixels = sizeInInches * dpi; 

            using (QRCodeGenerator qrCodeGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrCodeGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap bitmap = qrCode.GetGraphic(15, Color.Black, Color.White, true))
                    {
                        using (Bitmap resizedBitmap = new Bitmap(sizeInPixels, sizeInPixels))
                        {
                            using (Graphics graphics = Graphics.FromImage(resizedBitmap))
                            {
                                graphics.Clear(Color.White);
                                graphics.DrawImage(bitmap, 0, 0, sizeInPixels, sizeInPixels);
                            }

                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                resizedBitmap.Save(memoryStream, ImageFormat.Png);
                                byte[] bitmapBytes = memoryStream.ToArray();
                                return Convert.ToBase64String(bitmapBytes);
                            }
                        }
                    }
                }
            }
        }
    }

    public class ApiResponse
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("response_data")]
        public List<List<string>> ResponseData { get; set; }

        [JsonPropertyName("response_message")]
        public string ResponseMessage { get; set; }
    }
}