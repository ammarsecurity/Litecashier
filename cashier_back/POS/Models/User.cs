using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace POS.Models
{
    public class User : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Password { get; set; }
        public required string Username { get; set; }
        public required string Role { get; set; }
        public int InsertByUserId { get; set; }

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
