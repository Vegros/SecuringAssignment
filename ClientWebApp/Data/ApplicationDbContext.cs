using ClientWebApp.Models.DatabaseModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClientWebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UploadedFile> Files { get; set; }
        public DbSet<AccessPermission> AccessPermissions { get; set; }
        public DbSet<AccessLog> AccessLogs { get; set; }

    }
}
