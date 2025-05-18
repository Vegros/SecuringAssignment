using ClientWebApp.Models;
using ClientWebApp.Models.DatabaseModels;
using ClientWebApp.Repositories;
using ClientWebApp.Utlities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClientWebApp.Controllers
{
    [Authorize]
    public class FileUploadController : Controller
    {
        PermisionRepository _permisionRepository;
        FileRepository _fileRepository;
        EncryptionUtility _encryptionUtility;
        LogsRepository _logsRepository;
        private readonly ILogger<FileDownloadController> _logger;

        public FileUploadController(ILogger<FileDownloadController> logger, PermisionRepository permisionRepository, FileRepository fileRepository, EncryptionUtility encryptionUtility, LogsRepository logsRepository)
        {
            _logger = logger;
            _permisionRepository = permisionRepository;
            _fileRepository = fileRepository;
            _encryptionUtility = encryptionUtility;
            _logsRepository = logsRepository;
        }


        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadFileViewModel model, [FromServices] IWebHostEnvironment host)
        {
            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file.");
                _logger.LogWarning("Upload attempt with empty or null file.");
                return View(model);
            }

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


    }
}
