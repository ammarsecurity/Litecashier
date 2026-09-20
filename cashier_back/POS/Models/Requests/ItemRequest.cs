using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class ItemRequest
    {

        [Required]
        public required string Name { get; set; }

        public IFormFile? Image { get; set; }

        public string? Description { get; set; }
        [Required]
        public decimal SellingPrice { get; set; }
        public decimal PurchasingPrice { get; set; }
        public decimal DisCountPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public int Quantity { get; set; } = 0; // Total / fallback when WarehouseStocksJson omitted
        public int? LowStockAlertQuantity { get; set; }
        /// <summary>Optional expiry date (date-only). Empty form value clears on update.</summary>
        public DateTime? ExpiryDate { get; set; }
        /// <summary>Optional brand id. Empty form value clears on update.</summary>
        public int? BrandId { get; set; }
        public string? Tags { get; set; }
        public string? Code { get; set; }

        /// <summary>JSON array of { warehouseId, quantity }.</summary>
        public string? WarehouseStocksJson { get; set; }
    }
}
