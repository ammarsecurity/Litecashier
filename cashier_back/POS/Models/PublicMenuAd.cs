using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class PublicMenuAd : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int CommercialUserId { get; set; }

        [Required]
        public string Image { get; set; } = "";

        [StringLength(120)]
        public string? Title { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        [ForeignKey(nameof(CommercialUserId))]
        public User? CommercialUser { get; set; }
    }
}
