using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Serilog.Producer.RabbitMq.Example.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger _logger;
        private readonly IAuditLogger _auditLogger;

        public IndexModel(ILogger logger, IAuditLogger auditLogger)
        {
            _logger = logger;
            _auditLogger = auditLogger;
        }

        public void OnGet()
        {
            try
            {
                throw new Exception("Test exception logging");
            }
            catch (Exception ex)
            {
                for (var i = 0; i < 200; i++)
                {
                    _logger.Error(ex, "Error");
                }
            }

            try
            {
                throw new Exception("Test exception audit logging");
            }
            catch (Exception ex)
            {
                for (var i = 0; i < 200; i++)
                {
                    _auditLogger.Error(ex, "Audit Error");
                }
            }

        }
    }
}
