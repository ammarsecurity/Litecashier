using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Models.Requests
{
    public class TagRequset
    {
        [Required]
        public string? Name { get; set; }
        public bool IsForAll { get; set; }

        /// <summary>معرّف القسم الأب؛ اتركه فارغاً للقسم الرئيسي.</summary>
        public int? ParentTagId { get; set; }
    }
}
