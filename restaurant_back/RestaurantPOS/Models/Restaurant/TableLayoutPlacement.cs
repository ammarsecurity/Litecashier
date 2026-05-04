using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestaurantPOS.Models;

namespace RestaurantPOS.Models.Restaurant
{
    /// <summary>موضع طاولة على مخطط محدد (البار، طابق، …) يطابق عادة Table.Zone لذلك المخطط.</summary>
    public class TableLayoutPlacement : BaseEntity
    {
        public int Id { get; set; }

        public int TableId { get; set; }
        public Table? Table { get; set; }

        /// <summary>مفتاح المخطط؛ يطابق اسم الموقع/الطابق عند الطاولات (نفس قيمة Zone).</summary>
        [MaxLength(128)]
        public string PlanKey { get; set; } = "";

        public double LayoutPosX { get; set; }
        public double LayoutPosY { get; set; }
    }
}
