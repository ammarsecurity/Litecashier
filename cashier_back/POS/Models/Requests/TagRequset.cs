using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace POS.Models.Requests
{
    public class TagRequset
    {
        [Required]
        public string? Name { get; set; }
        public bool IsForAll { get; set; }
    }
}
