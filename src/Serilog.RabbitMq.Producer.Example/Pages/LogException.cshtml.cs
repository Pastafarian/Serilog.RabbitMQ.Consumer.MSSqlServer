using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog.Producer.RabbitMq.Example.Exceptions;

namespace Serilog.Producer.RabbitMq.Example.Pages
{
    public class LogExceptionModel : PageModel
    {
        private readonly ILogger _logger;

        public LogExceptionModel(ILogger logger)
        {
            _logger = logger;
        }

        public void OnGet(string message)
        {
            try
            {
                throw new CustomLoggingException($"Test exception logging - {message}")
                {
                    CustomLoggingMessage = $"My custom logging message - {message}"
                };
            }
            catch (Exception ex)
            {
                for (var i = 0; i < 20; i++)
                {
                    _logger.Error(ex, "Log Error");
                }
            }
        }
    }
}
