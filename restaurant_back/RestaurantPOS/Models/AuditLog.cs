using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class AuditLog : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // "Delete" or "Update"

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty; // "Item", "Order", "User", etc.

        [Required]
        public int EntityId { get; set; } // ID of the entity that was modified/deleted

        [StringLength(500)]
        public string? EntityName { get; set; } // Name or description of the entity

        [Column(TypeName = "text")]
        public string? OldValues { get; set; } // JSON string of old values (for updates)

        [Column(TypeName = "text")]
        public string? NewValues { get; set; } // JSON string of new values (for updates)

        [StringLength(1000)]
        public string? Description { get; set; } // Description of what was changed

        [ForeignKey("UserId")]
        public int UserId { get; set; } // User who performed the action
        public User? User { get; set; }

        [ForeignKey("CommercialUserId")]
        public int CommercialUserId { get; set; } // Commercial user this log belongs to
        public User? CommercialUser { get; set; }
    }
}

