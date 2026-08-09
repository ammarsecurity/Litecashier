namespace POS.Models.Dtos
{
    public class CommercialUserInfoDto
    {
        public string StoreName { get; set; } = string.Empty;
        public string? Logo { get; set; }

        /// <summary>Pos | A4</summary>
        public string PrintInvoiceFormat { get; set; } = "Pos";
    }

    public class UpdatePrintSettingsRequest
    {
        /// <summary>Pos | A4</summary>
        public string PrintInvoiceFormat { get; set; } = "Pos";
    }
}
