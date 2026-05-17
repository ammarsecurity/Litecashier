using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RestaurantPOS.Models
{
    public class User : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string PhoneNumber { get; set; }
        [JsonIgnore]
        public string Password { get; set; } = string.Empty;
        public required string Username { get; set; }
        public required string Role { get; set; }
        public int InsertByUserId { get; set; }
        public string? Logo { get; set; }
        public string? RestaurantName { get; set; }

        /// <summary>رمز دخول سريع للحساب التجاري (أرقام فقط) — تسجيل دخول بدون هاتف/كلمة مرور</summary>
        [StringLength(20)]
        public string? LoginCode { get; set; }

        /// <summary>JSON array of section keys for Manager role (see SectionDefinitions.AssignableSectionKeys).</summary>
        [StringLength(2000)]
        public string? AllowedSectionsJson { get; set; }

        /// <summary>When true (Manager only), sensitive POS actions are confirmed with this user's LoginCode instead of the commercial account password.</summary>
        public bool CanUseOwnLoginCodeForSensitiveActions { get; set; }

        [JsonIgnore]
        public List<Item>? Items { get; set; }
        [JsonIgnore]
        public List<CustomerOrder>? CustomerOrders { get; set; }
        [JsonIgnore]
        public List<Tag>? Tags { get; set; }
        [JsonIgnore]
        public List<CustomerOrderItem>? CustomerOrderItem { get; set; }
        
    }
}
