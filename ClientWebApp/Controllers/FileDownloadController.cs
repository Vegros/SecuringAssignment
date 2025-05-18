using ClientWebApp.ActionFilters;
using ClientWebApp.Data;
using ClientWebApp.Models;
using ClientWebApp.Models.DatabaseModels;
using ClientWebApp.Repositories;
using ClientWebApp.Utlities;
using Microsoft.AspNetCore.Mvc;
namespace ClientWebApp.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class FileDownloadController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly EncryptionUtility _encryptionUtility;
        private readonly LogsRepository _logsRepository;
        private readonly ILogger<FileDownloadController> _logger;

        public FileDownloadController(
            ApplicationDbContext dbContext,
            EncryptionUtility encryptionUtility,
            LogsRepository logsRepository,
            ILogger<FileDownloadController> logger)
        {
            _dbContext = dbContext;
            _encryptionUtility = encryptionUtility;
            _logsRepository = logsRepository;
            _logger = logger;
        }

        [ServiceFilter(typeof(DownloadActionFilter))]
        [HttpPost("Download")]
        public async Task<IActionResult> DownloadFile([FromBody] DownloadFileDTO request)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Download request failed validation.");
                return BadRequest("Invalid request");
            }

            _logger.LogInformation("Received download request for {Email}", request.Email);

            var permission = HttpContext.Items["permission"] as AccessPermission;
            var filePath = permission.UploadedFile.FilePath;

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("File not found at path: {Path}", filePath);
                return NotFound("File not found on server.");
            }

            _logger.LogInformation("File found: {Path}. Proceeding with encryption.", filePath);

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            MemoryStream fileStream = new(fileBytes);
            byte[] decryptedBytes = fileStream.ToArray(); // before encryption

            MemoryStream hybridEncrypted;
            try
            {
                hybridEncrypted = _encryptionUtility.HybridEncrypt(fileStream, request.lawyerPublicKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hybrid encryption failed.");
                return StatusCode(500, "Encryption error.");
            }

            var serverKeys = _encryptionUtility.GenerateAsymmetricKeys();

            string signature;
            try
            {
                signature = _encryptionUtility.DigitallySign(new MemoryStream(decryptedBytes), serverKeys.PrivateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Digital signing failed.");
                return StatusCode(500, "Signature generation error.");
            }

            hybridEncrypted.Position = 0;
            var base64Encrypted = Convert.ToBase64String(hybridEncrypted.ToArray());

            var log = new AccessLog
            {
                Id = Guid.NewGuid(),
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserEmail = request.Email,
                Timestamp = DateTime.UtcNow,
                Action = "Download"
            };

            _logsRepository.addLogs(log);
            _logger.LogInformation("Download successfully processed and logged for {Email}", request.Email);

            _logger.LogInformation("IP address: {ipaddress}, useremail: {email}, TimeStamp: {time}, action: download", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", request.Email, DateTime.UtcNow);

            permission.IsDownloaded = true;
            _dbContext.Update(permission);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                fileName = permission.UploadedFile.FileName,
                file = base64Encrypted,
                signature = signature,
                serverPublicKey = serverKeys.PublicKey
            });
        }
    }
}
