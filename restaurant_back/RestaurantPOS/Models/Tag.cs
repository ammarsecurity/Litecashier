using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class Tag : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string?  Name { get; set; }
        public bool IsForAll { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }

        /// <summary>قسم رئيسي؛ null يعني قسم جذر (رئيسي).</summary>
        public int? ParentTagId { get; set; }

        [ForeignKey("ParentTagId")]
        public Tag? Parent { get; set; }

        public ICollection<Tag> Children { get; set; } = new List<Tag>();
    }
}
