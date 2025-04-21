using System.ComponentModel.DataAnnotations;

namespace ClientWebApp.Models.DatabaseModels
{
    public class AccessLog
    {
        [Key]
        public Guid Id { get; set; }
        public string IPAddress { get; set; }
        public string UserEmail { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
    }
}
