namespace POS.Models.Requests
{
    public class SeedDataRequest
    {
        /// <summary>معرّف المستخدم التجاري المستهدف (يُتجاهل إذا SeedDemoAccounts = true).</summary>
        public int CommercialUserId { get; set; }

        /// <summary>إنشاء حسابات العرض ثم ملء البيانات بالكامل.</summary>
        public bool SeedDemoAccounts { get; set; }

        /// <summary>المواد والأقسام (Tags) فقط دون باقي البيانات.</summary>
        public bool CatalogOnly { get; set; }
    }
}

