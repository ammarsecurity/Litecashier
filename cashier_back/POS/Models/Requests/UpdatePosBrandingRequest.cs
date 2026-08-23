using Microsoft.AspNetCore.Http;

namespace POS.Models.Requests
{
    public class UpdatePosBrandingRequest
    {
        public IFormFile? CartWatermarkLogo { get; set; }
        public bool ClearCartWatermark { get; set; }
        public int? CartWatermarkOpacity { get; set; }
        public IFormFile? DefaultProductImage { get; set; }
        public bool ClearDefaultProductImage { get; set; }
    }
}
