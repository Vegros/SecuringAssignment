using ClientWebApp.Data;
using ClientWebApp.Models;
using ClientWebApp.Models.DatabaseModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace ClientWebApp.Controllers
{
    [Authorize]
    public class FileUploadController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public FileUploadController(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
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
            if (extension != ".docx" && extension != ".pdf")
            {
                ModelState.AddModelError("File", "Only .docx or .pdf allowed.");
                return View(model);
            }

            var fileName = Guid.NewGuid() + extension;
            var filePath = Path.Combine(host.WebRootPath, "SecureFiles", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var accessCode = Guid.NewGuid().ToString();
            var uploadedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            var uploadedFile = new UploadedFile
            {
                Id = Guid.NewGuid(),
                FileName = model.File.FileName,
                FilePath = filePath,
                UploadedByEmail = uploadedBy,
                FileHash = "", // Hashing to be added
                UploadDate = DateTime.UtcNow
            };

            _dbcontext.Files.Add(uploadedFile);

            var permission = new AccessPermission
            {
                Id = Guid.NewGuid(),
                LawyerEmail = model.LawyerEmail,
                AccessCode = accessCode,
                UploadedFileId = uploadedFile.Id
            };

            _dbcontext.AccessPermissions.Add(permission);
            await _dbcontext.SaveChangesAsync(); // ✅ Use await

            TempData["Message"] = $"File uploaded successfully! Access Code: {accessCode}";
            return RedirectToAction("Upload");
        }

      


    }
}
