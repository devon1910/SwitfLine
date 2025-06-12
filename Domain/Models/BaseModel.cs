
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class BaseModel
    {
        [Key]
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(1);

        public bool IsActive { get; set; } = true;


    }
}
