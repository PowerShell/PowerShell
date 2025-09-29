namespace NelrockContracting.Services.Services;

public interface IAzureIntegrationService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string caseId);
    Task<bool> SendEmailAsync(string to, string subject, string body, string[]? attachments = null);
    Task<bool> SendSmsAsync(string to, string message);
}

public class AzureIntegrationService : IAzureIntegrationService
{
    private readonly ILogger<AzureIntegrationService> _logger;
    private readonly IConfiguration _configuration;

    public AzureIntegrationService(ILogger<AzureIntegrationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string caseId)
    {
        _logger.LogInformation("Uploading file {FileName} for case {CaseId}", fileName, caseId);
        
        // Mock Azure Blob Storage upload
        await Task.Delay(100);
        
        var containerPath = $"cases/{caseId}/files";
        var blobName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{fileName}";
        var mockUrl = $"https://nelrockfiles.blob.core.windows.net/{containerPath}/{blobName}";
        
        _logger.LogInformation("File uploaded to {Url}", mockUrl);
        return mockUrl;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, string[]? attachments = null)
    {
        _logger.LogInformation("Sending email to {To} with subject {Subject}", to, subject);
        
        // Mock Azure Communication Services email
        await Task.Delay(50);
        
        _logger.LogInformation("Email sent successfully");
        return true;
    }

    public async Task<bool> SendSmsAsync(string to, string message)
    {
        _logger.LogInformation("Sending SMS to {To}", to);
        
        // Mock Azure Communication Services SMS
        await Task.Delay(30);
        
        _logger.LogInformation("SMS sent successfully");
        return true;
    }
}
