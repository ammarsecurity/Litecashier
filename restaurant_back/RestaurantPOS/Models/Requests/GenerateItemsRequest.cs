using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class GenerateItemsRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        public int MaxItems { get; set; } = 15;
        public List<ItemInfo>? ExistingItems { get; set; }

        /// <summary>قسم رئيسي من قائمة التصنيفات — عند التحديد تُنشأ الأطباق ضمن هذا المسار فقط.</summary>
        public int? RootTagId { get; set; }

        /// <summary>قسم فرعي إن وُجد تحت <see cref="RootTagId"/>؛ مطلوب إذا كان للرئيسي أبناء.</summary>
        public int? SubTagId { get; set; }
    }

    public class ItemInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
    }
}

