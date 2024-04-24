using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog.Producer.RabbitMq.Example.Exceptions;

namespace Serilog.Producer.RabbitMq.Example.Pages
{
    public class AuditExceptionModel : PageModel
    {
        private readonly IAuditLogger _auditLogger;

        public AuditExceptionModel(IAuditLogger auditLogger)
        {
            _auditLogger = auditLogger;
        }

        public void OnGet(string message)
        {
            try
            {
                throw new AuditLoggingException($"Test exception audit logging - {message}")
                {
                    CustomAuditMessage = $"My custom audit message - {message}"
                };
            }
            catch (Exception ex)
            {
                _auditLogger.Error(ex, "Audit Error");
            }
        }
    }
}
