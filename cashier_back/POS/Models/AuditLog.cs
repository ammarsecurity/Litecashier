using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class AuditLog : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public int EntityId { get; set; }

        [StringLength(500)]
        public string? EntityName { get; set; }

        [Column(TypeName = "text")]
        public string? OldValues { get; set; }

        [Column(TypeName = "text")]
        public string? NewValues { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [ForeignKey("CommercialUserId")]
        public int CommercialUserId { get; set; }
        public User? CommercialUser { get; set; }
    }
}
