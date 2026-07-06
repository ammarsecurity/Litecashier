namespace POS.Models.Dtos
{
    public class ItemImportResultDto
    {
        public int ItemsCreated { get; set; }
        public int ItemsSkipped { get; set; }
        public int TagsCreated { get; set; }
        public int RowsWithErrors { get; set; }
        public List<ItemImportRowError> Errors { get; set; } = new();
    }

    public class ItemImportRowError
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = "";
    }
}
