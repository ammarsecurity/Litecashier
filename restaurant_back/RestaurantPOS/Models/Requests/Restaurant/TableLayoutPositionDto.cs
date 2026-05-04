namespace RestaurantPOS.Models.Requests.Restaurant
{
    public class TableLayoutPositionDto
    {
        public int TableId { get; set; }

        /// <summary>0–1 من عرض لوحة المخطط.</summary>
        public double LayoutPosX { get; set; }

        /// <summary>0–1 من ارتفاع لوحة المخطط.</summary>
        public double LayoutPosY { get; set; }

        /// <summary>تحديث اسم المنطقة النصي عند الإسناد من المخطط (اختياري).</summary>
        public string? Zone { get; set; }
    }
}
