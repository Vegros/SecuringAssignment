using ClientWebApp.Data;
using ClientWebApp.Models;
using ClientWebApp.Models.DatabaseModels;
using ClientWebApp.Repositories;
using ClientWebApp.Utlities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientWebApp.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class FileDownloadController : Controller
    {

        ApplicationDbContext _dbContext;
        EncryptionUtility _encryptionUtility;
        LogsRepository _logsRepository;
        public FileDownloadController(ApplicationDbContext dbContext, EncryptionUtility encryptionUtility, LogsRepository logsRepository)
        {
            _dbContext = dbContext;
            _encryptionUtility = encryptionUtility;
            _logsRepository = logsRepository;
        }


        [HttpPost("Download")]
        public async Task<IActionResult> DownloadFile([FromBody] DownloadFileDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request");

            var permission = await _dbContext.AccessPermissions
                .Include(p => p.UploadedFile)
                .FirstOrDefaultAsync(p =>
                    p.LawyerEmail == request.Email &&
                    p.AccessCode == request.AccessCode &&
                    p.UploadedFileId == request.FileId);

            if (permission == null)
                return Unauthorized("Invalid email or access code");

            var filePath = permission.UploadedFile.FilePath;

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on server.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileStream = new MemoryStream(fileBytes);

            var lawyerKeys = _encryptionUtility.GenerateAsymmetricKeys();

            var hybridEncrypted = _encryptionUtility.HybridEncrypt(fileStream, lawyerKeys.PublicKey);

            var serverKeys = _encryptionUtility.GenerateAsymmetricKeys();
            var signature = _encryptionUtility.DigitallySign(hybridEncrypted, serverKeys.PrivateKey);

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

            return Ok(new
            {
                file = base64Encrypted,
                signature = signature,
                serverPublicKey = serverKeys.PublicKey,
                lawyerPrivateKey = lawyerKeys.PrivateKey
            });

        }
    }
}
