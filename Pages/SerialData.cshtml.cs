using Microsoft.AspNetCore.Mvc.RazorPages;
using POS.Services;
using Microsoft.Extensions.Logging;

namespace POS.Pages
{
    public class SerialDataModel : PageModel
    {
        private readonly SerialPortService _serialPortService;
        private readonly ILogger<SerialDataModel> _logger;

        public string LatestData { get; private set; }

        public SerialDataModel(SerialPortService serialPortService, ILogger<SerialDataModel> logger)
        {
            _serialPortService = serialPortService;
            _logger = logger;
        }

        public void OnGet()
        {
            LatestData = _serialPortService.GetLatestData() ?? "No data received yet.";
        }
    }
}
