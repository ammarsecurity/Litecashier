using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class Tag : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string?  Name { get; set; }
        public bool IsForAll { get; set; }

        [ForeignKey("InsertByUserId")]
        public int InsertByUserId { get; set; }
        public User? User { get; set; }

    }
}
