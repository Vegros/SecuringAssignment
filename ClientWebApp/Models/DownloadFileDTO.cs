namespace ClientWebApp.Models
{
    public class DownloadFileDTO
    {
        public Guid FileId { get; set; }
        public string Email { get; set; }
        public string AccessCode { get; set; }
        public string lawyerPublicKey { get; set; }
    }

}
