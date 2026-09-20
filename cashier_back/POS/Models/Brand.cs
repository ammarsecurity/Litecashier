using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class Brand : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
    }
}
