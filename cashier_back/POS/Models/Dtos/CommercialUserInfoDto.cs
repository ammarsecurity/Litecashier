namespace POS.Models.Dtos
{
    public class CommercialUserInfoDto
    {
        public string StoreName { get; set; } = string.Empty;
        public string? Logo { get; set; }

        /// <summary>Pos | A4</summary>
        public string PrintInvoiceFormat { get; set; } = "Pos";

        public string? FooterCreditText { get; set; }
        public string? FooterCreditPhone { get; set; }
        public string? CartWatermarkLogo { get; set; }
        public int CartWatermarkOpacity { get; set; } = 18;
        public string? DefaultProductImage { get; set; }

        /// <summary>Minimum public-menu order total. 0 means no minimum.</summary>
        public decimal PublicMenuMinOrderAmount { get; set; }
    }

    public class UpdatePublicMenuSettingsRequest
    {
        public decimal PublicMenuMinOrderAmount { get; set; }
    }

    public class UpdatePrintSettingsRequest
    {
        /// <summary>Pos | A4</summary>
        public string PrintInvoiceFormat { get; set; } = "Pos";

        public string? FooterCreditText { get; set; }
        public string? FooterCreditPhone { get; set; }
    }
}
