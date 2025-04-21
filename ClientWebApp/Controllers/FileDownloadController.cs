using ClientWebApp.Data;
using ClientWebApp.Models;
using ClientWebApp.Models.DatabaseModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientWebApp.Controllers
{
   
    [ApiController]
    [Route("api/[controller]")]
    public class FileDownloadController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public FileDownloadController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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

            var file = permission.UploadedFile;

            if (!System.IO.File.Exists(file.FilePath))
                return NotFound("File not found");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);

            _dbContext.AccessLogs.Add(new AccessLog
            {
                Id = Guid.NewGuid(),
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserEmail = request.Email,
                Timestamp = DateTime.UtcNow,
                Action = "Download"
            });

            await _dbContext.SaveChangesAsync();

            return File(fileBytes, "application/octet-stream", file.FileName);
        }
    }
}
