using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class GenerateCategoriesRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        public int MaxCategories { get; set; } = 15;
        public List<string>? ExistingCategories { get; set; }

        /// <summary>إن وُجد: توليد تصنيفات فرعية تحت هذا القسم الرئيسي فقط (يجب أن يكون الأب بدون أب).</summary>
        public int? ParentTagId { get; set; }
    }
}

