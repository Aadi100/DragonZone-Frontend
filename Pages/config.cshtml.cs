using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using static POS.Pages.addmemberModel;
using static POS.Pages.SigninModel;
using System.IO.Ports;
using POS.Services;
using static POS.Pages.posModel;

namespace POS.Pages
{
    public class configModel : PageModel
    {
        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //[IgnoreAntiforgeryToken]

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<configModel> _logger;


        [BindProperty]
        public ConfigInputModel config { get; set; }
        public string[] AvailablePorts { get; set; }
        public string SelectedPort { get; set; }

        public class ConfigInputModel
        {
            [Required]
            public string Port { get; set; }

        }



        public configModel(IHttpClientFactory httpClientFactory, ILogger<configModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        public IActionResult OnGet()
        {

            AvailablePorts = SerialPort.GetPortNames();

            var accessToken = HttpContext.Session.GetString("SessionToken");

            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToPage("/signin");
            }
            return Page();

        }

        public IActionResult OnPost()
        {

            SelectedPort = config.Port;

            return RedirectToPage();
        }

    }
}
