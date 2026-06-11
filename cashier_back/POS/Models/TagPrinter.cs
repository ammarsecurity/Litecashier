using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class TagPrinter : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("TagId")]
        public int TagId { get; set; }
        public Tag? Tag { get; set; }

        [ForeignKey("PrinterId")]
        public int PrinterId { get; set; }
        public Printer? Printer { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }
    }
}
