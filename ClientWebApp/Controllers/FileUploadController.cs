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
        public FileUploadController(PermisionRepository permisionRepository, FileRepository fileRepository, EncryptionUtility encryptionUtility, LogsRepository logsRepository)
        {
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
                return View(model);
            }

            var extension = Path.GetExtension(model.File.FileName).ToLower();
            if (extension != ".docx")
            {
                ModelState.AddModelError("File", "Only .docx allowed.");
                return View(model);
            }

            using var ms = new MemoryStream();
            await model.File.CopyToAsync(ms);
            string fileHash = _encryptionUtility.Hash(ms);
            ms.Position = 0;

            var fileName = Guid.NewGuid() + extension;
            var filePath = Path.Combine(host.WebRootPath, "SecureFiles", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ms.CopyToAsync(stream);
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

            var permission = new AccessPermission
            {
                Id = Guid.NewGuid(),
                LawyerEmail = model.LawyerEmail,
                AccessCode = accessCode,
                UploadedFileId = uploadedFile.Id
            };

            _permisionRepository.addPermission(permission);

            var log = new AccessLog
            {
                Id = Guid.NewGuid(),
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserEmail = uploadedBy,
                Timestamp = DateTime.UtcNow,
                Action = "Upload"
            };

            _logsRepository.addLogs(log);


            TempData["Message"] = $"File uploaded successfully! Access Code: {accessCode}";
            return RedirectToAction("Upload");
        }




    }
}
