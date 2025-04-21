using System.ComponentModel.DataAnnotations;

namespace ClientWebApp.Models.DatabaseModels
{
    public class UploadedFile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string FileName { get; set;}

        [Required]
        public string FilePath { get; set; }

        [Required]
        public string UploadedByEmail { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow; 

        [Required]
        public string FileHash { get; set; }
    }
}
