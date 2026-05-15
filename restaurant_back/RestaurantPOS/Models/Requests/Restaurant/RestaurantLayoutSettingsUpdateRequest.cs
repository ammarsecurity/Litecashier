namespace RestaurantPOS.Models.Requests.Restaurant
{
    public class RestaurantLayoutSettingsUpdateRequest
    {
        /// <summary>مفتاح المخطط (نفس تسمية الطابق في الطاولات). فارغ = الافتراضي.</summary>
        public string? PlanKey { get; set; }

        public string? BackgroundColor { get; set; }

        /// <summary>JSON مناطق مستطيلة؛ null = لا تغيير، "" = مسح.</summary>
        public string? ZonesJson { get; set; }

        /// <summary>عند true يُزال ملف صورة الخلفية المحفوظ.</summary>
        public bool ClearFloorPlanImage { get; set; }

        /// <summary>حجم رقاقة الطاولة بالبكسل (32–96); null = لا تغيير.</summary>
        public int? TableChipSizePx { get; set; }
    }
}
