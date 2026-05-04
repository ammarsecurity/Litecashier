using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantPOS.Models;

namespace RestaurantPOS.Models.Restaurant
{
    /// <summary>إعدادات مخطط أرضية المطعم (خلفية، مناطق مرسومة) — صف لكل مستخدم تجاري ومفتاح مخطط (طابق/بار…).</summary>
    public class RestaurantLayoutSettings : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }

        /// <summary>فصل مخططات متعددة؛ فارغ = المخطط الافتراضي القديم؛ غالباً يطابق Table.Zone للطاولات على ذلك الطابق.</summary>
        [MaxLength(128)]
        public string PlanKey { get; set; } = "";

        /// <summary>اسم ملف الصورة المحفوظ تحت wwwroot/Images (مثل صور الأصناف).</summary>
        public string? FloorPlanImageFileName { get; set; }

        /// <summary>لون خلفية اللوحة عند عدم استخدام صورة (#RRGGBB).</summary>
        public string? BackgroundColor { get; set; }

        /// <summary>JSON: مصفوفة مناطق مستطيلة [{ "name","x","y","w","h","color" }] بإحداثيات معيّرة 0–1.</summary>
        public string? ZonesJson { get; set; }
    }
}
