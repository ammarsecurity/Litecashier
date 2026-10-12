namespace POS.Models.Dtos
{
    public class FinancialOperationsReportDto
    {
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal ActualCash { get; set; }
        public decimal SelectedCategoriesTotal { get; set; }
        public decimal NetProfit { get; set; }
        public List<string> SelectedCategories { get; set; } = new();
        public List<ExpenseCategoryAmountDto> SelectedCategoryBreakdown { get; set; } = new();
        public List<ExpenseCategoryAmountDto> ExpensesByCategory { get; set; } = new();
        public FinancialOperationsPeriodDto Period { get; set; } = new();
    }

    public class ExpenseCategoryAmountDto
    {
        public string Category { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
        public bool IsSelected { get; set; }
    }

    public class FinancialOperationsPeriodDto
    {
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }
}
