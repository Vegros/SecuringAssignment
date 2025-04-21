using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientWebApp.Models.DatabaseModels
{
    public class AccessPermission
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string LawyerEmail { get; set; }
        [Required]
        public string AccessCode { get; set; }

        [ForeignKey("UploadedFile")]
        public Guid UploadedFileId { get; set; }
        [Required]
        public UploadedFile UploadedFile { get; set; }
    }
}
