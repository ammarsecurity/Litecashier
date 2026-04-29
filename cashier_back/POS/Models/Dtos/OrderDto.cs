using System.ComponentModel.DataAnnotations;

namespace POS.Models.Dtos
{
    public class OrderDto
    {

        public int Id { get; set; }
        public  string? OrderCode { get; set; }
        public List<CustomerOrderItem>? CustomerOrderItem { get; set; }
        public decimal OrderPrice { get; set; }
        public int? ItemsCount { get; set;}
        public DateTime InsertDate { get; set;}
    }
}
