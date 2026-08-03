using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class UpdateStockLineRequest
    {
        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ReceiptNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string NewMaterialName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? UnitType { get; set; }

        /// <summary>إجمالي الكمية المدخلة للدفعة؛ يجب ألا يقل عن إجمالي المسحوب</summary>
        [Range(0.01, double.MaxValue)]
        public decimal? TotalAddedQuantity { get; set; }
    }
}
