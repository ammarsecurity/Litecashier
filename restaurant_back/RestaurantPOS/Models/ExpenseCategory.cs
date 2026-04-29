using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.Models
{
    public class ExpenseCategory : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // اسم الفئة

        [StringLength(500)]
        public string? Description { get; set; } // الوصف

        [StringLength(50)]
        public string? Color { get; set; } // لون الفئة (للعرض في الواجهة)

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}

