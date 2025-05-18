using ClientWebApp.ActionFilters;
using ClientWebApp.Models;
using ClientWebApp.Models.DatabaseModels;
using ClientWebApp.Repositories;
using ClientWebApp.Utlities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClientWebApp.Controllers
{
    public class FileUploadController : Controller
    {
        PermisionRepository _permisionRepository;
        FileRepository _fileRepository;
        EncryptionUtility _encryptionUtility;
        LogsRepository _logsRepository;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(ILogger<FileUploadController> logger, PermisionRepository permisionRepository, FileRepository fileRepository, EncryptionUtility encryptionUtility, LogsRepository logsRepository)
        {
            _logger = logger;
            _permisionRepository = permisionRepository;
            _fileRepository = fileRepository;
            _encryptionUtility = encryptionUtility;
            _logsRepository = logsRepository;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadFileViewModel model, [FromServices] IWebHostEnvironment host)
        {
            //check if file is empty
            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file.");
                _logger.LogWarning("Upload attempt with empty or null file.");
                return View(model);
            }

            //check if file is a word document
            var extension = Path.GetExtension(model.File.FileName).ToLower();
            if (extension != ".docx")
            {
                ModelState.AddModelError("File", "Only .docx allowed.");
                _logger.LogWarning("Rejected file with invalid extension: {Extension}", extension);
                return View(model);
            }

            byte[] zipHeader = { 0x50, 0x4B, 0x03, 0x04 };

            using var ms = new MemoryStream();
            await model.File.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            for (int i = 0; i < zipHeader.Length; i++)
            {
                if (fileBytes.Length <= i || fileBytes[i] != zipHeader[i])
                {
                    ModelState.AddModelError("File", "The file is not a valid .docx (ZIP format).");
                    _logger.LogWarning("Header check failed for uploaded file {FileName}", model.File.FileName);
                    return View(model);
                }
            }

            ms.Position = 0;
            string fileHash = _encryptionUtility.Hash(ms);
            ms.Position = 0;

            //upload file to secure file folder
            var fileName = Guid.NewGuid() + extension;
            var filePath = Path.Combine(host.WebRootPath, "SecureFiles", fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ms.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save uploaded file to {Path}", filePath);
                ModelState.AddModelError("File", "Error saving the file.");
                return View(model);
            }

            var accessCode = Guid.NewGuid().ToString();
            var uploadedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            //upload file to the database
            var uploadedFile = new UploadedFile
            {
                Id = Guid.NewGuid(),
                FileName = model.File.FileName,
                FilePath = filePath,
                UploadedByEmail = uploadedBy,
                FileHash = fileHash,
                UploadDate = DateTime.UtcNow
            };

            _fileRepository.UploadFile(uploadedFile);
            _logger.LogInformation("File metadata saved for {FileName} by {User}", model.File.FileName, uploadedBy);

            //add permsisions of the file to the database
            var permission = new AccessPermission
            {
                Id = Guid.NewGuid(),
                LawyerEmail = model.LawyerEmail,
                AccessCode = accessCode,
                UploadedFileId = uploadedFile.Id
            };

            _permisionRepository.addPermission(permission);
            _logger.LogInformation("Access permission granted to {Lawyer}", model.LawyerEmail);

            var log = new AccessLog
            {
                Id = Guid.NewGuid(),
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserEmail = uploadedBy,
                Timestamp = DateTime.UtcNow,
                Action = "Upload"
            };

            _logsRepository.addLogs(log);
            _logger.LogInformation("Upload action logged for user {User}", uploadedBy);

            TempData["Message"] = $"File uploaded successfully! Access Code: {accessCode}";
            return RedirectToAction("Upload");
        }


        [ServiceFilter(typeof(DownloadActionFilter))]
        [HttpPost("Download")]
        [Route("Download")]
        public async Task<IActionResult> DownloadFile([FromBody] DownloadFileDTO request)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Download request failed validation.");
                return BadRequest("Invalid request");
            }

            _logger.LogInformation("Received download request for {Email}", request.Email);

            //check permissions 

            var permission = HttpContext.Items["permission"] as AccessPermission;
            var filePath = permission.UploadedFile.FilePath;

            //check that file exists
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("File not found at path: {Path}", filePath);
                return NotFound("File not found on server.");
            }

            _logger.LogInformation("File found: {Path}. Proceeding with encryption.", filePath);

            //encypt the file with hybrid encryption
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            MemoryStream fileStream = new(fileBytes);
            byte[] decryptedBytes = fileStream.ToArray();

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

            //generate server keys
            var serverKeys = _encryptionUtility.GenerateAsymmetricKeys();

            //digital sign the file
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

            //create logs to the database
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

            //update permissions
            _permisionRepository.Downloaded(permission);

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
