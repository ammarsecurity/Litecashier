using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models;

namespace POS.Services
{
    public class PayrollService
    {
        public const string SalaryExpenseCategory = "رواتب";

        private readonly DbConfig _db;

        public PayrollService(DbConfig db)
        {
            _db = db;
        }

        public static (DateTime start, DateTime end) GetPeriodBounds(int year, int month)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        public static decimal ComputeBaseAmount(Employee employee, int year, int month, decimal workDays)
        {
            return employee.SalaryType switch
            {
                SalaryType.Monthly => Round2(employee.Salary),
                SalaryType.Weekly => Round2(employee.Salary * 4m),
                SalaryType.Daily => Round2(employee.Salary * workDays),
                _ => Round2(employee.Salary)
            };
        }

        public static decimal DefaultWorkDays(int year, int month) =>
            DateTime.DaysInMonth(year, month);

        public static decimal DailyRate(Employee employee)
        {
            return employee.SalaryType switch
            {
                SalaryType.Daily => employee.Salary,
                SalaryType.Weekly => employee.Salary / 7m,
                SalaryType.Monthly => employee.Salary / Math.Max(1, DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month)),
                _ => employee.Salary
            };
        }

        public static decimal DailyRateForPeriod(Employee employee, int year, int month)
        {
            return employee.SalaryType switch
            {
                SalaryType.Daily => employee.Salary,
                SalaryType.Weekly => employee.Salary / 7m,
                SalaryType.Monthly => employee.Salary / Math.Max(1, DateTime.DaysInMonth(year, month)),
                _ => employee.Salary
            };
        }

        public static void RecalculateNet(PayrollLine line)
        {
            line.NetAmount = Round2(
                line.BaseAmount
                + line.OvertimeAmount
                - line.DeductionAmount
                - line.AbsenceAmount
                - line.AdvanceDeducted);
            if (line.NetAmount < 0) line.NetAmount = 0;
        }

        public static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        public async Task<PayrollLine> BuildLineAsync(
            Employee employee,
            int commercialUserId,
            int year,
            int month,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken ct = default)
        {
            var workDays = DefaultWorkDays(year, month);
            var baseAmount = ComputeBaseAmount(employee, year, month, workDays);

            var adjustments = await _db.SalaryAdjustments
                .Where(a => !a.IsDeleted
                    && a.InsertByUserId == commercialUserId
                    && a.EmployeeId == employee.Id
                    && a.Date.Date >= periodStart.Date
                    && a.Date.Date <= periodEnd.Date)
                .ToListAsync(ct);

            decimal overtime = 0, deduction = 0, absence = 0;
            var daily = DailyRateForPeriod(employee, year, month);
            foreach (var adj in adjustments)
            {
                switch (adj.Type)
                {
                    case SalaryAdjustmentType.Overtime:
                        overtime += adj.Amount;
                        break;
                    case SalaryAdjustmentType.Deduction:
                        deduction += adj.Amount;
                        break;
                    case SalaryAdjustmentType.Absence:
                        var absAmt = adj.Amount > 0
                            ? adj.Amount
                            : Round2(daily * adj.AbsenceDays);
                        absence += absAmt;
                        break;
                }
            }

            var openAdvances = await _db.EmployeeAdvances
                .Where(a => !a.IsDeleted
                    && a.InsertByUserId == commercialUserId
                    && a.EmployeeId == employee.Id
                    && !a.IsClosed
                    && a.RemainingAmount > 0)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Id)
                .ToListAsync(ct);

            var grossBeforeAdvance = Round2(baseAmount + overtime - deduction - absence);
            if (grossBeforeAdvance < 0) grossBeforeAdvance = 0;

            var openTotal = openAdvances.Sum(a => a.RemainingAmount);
            var advanceDeducted = Round2(Math.Min(grossBeforeAdvance, openTotal));

            var line = new PayrollLine
            {
                EmployeeId = employee.Id,
                BaseSalarySnapshot = employee.Salary,
                SalaryTypeSnapshot = employee.SalaryType,
                WorkDays = workDays,
                BaseAmount = baseAmount,
                OvertimeAmount = Round2(overtime),
                DeductionAmount = Round2(deduction),
                AbsenceAmount = Round2(absence),
                AdvanceDeducted = advanceDeducted,
                InsertDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow,
                IsDeleted = false
            };
            RecalculateNet(line);
            return line;
        }

        public async Task ApplyAdvanceDeductionsAsync(
            int commercialUserId,
            IEnumerable<PayrollLine> lines,
            CancellationToken ct = default)
        {
            foreach (var line in lines.Where(l => !l.IsDeleted && l.AdvanceDeducted > 0))
            {
                var remainingToDeduct = line.AdvanceDeducted;
                var advances = await _db.EmployeeAdvances
                    .Where(a => !a.IsDeleted
                        && a.InsertByUserId == commercialUserId
                        && a.EmployeeId == line.EmployeeId
                        && !a.IsClosed
                        && a.RemainingAmount > 0)
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.Id)
                    .ToListAsync(ct);

                foreach (var adv in advances)
                {
                    if (remainingToDeduct <= 0) break;
                    var take = Math.Min(adv.RemainingAmount, remainingToDeduct);
                    adv.RemainingAmount = Round2(adv.RemainingAmount - take);
                    remainingToDeduct = Round2(remainingToDeduct - take);
                    if (adv.RemainingAmount <= 0)
                    {
                        adv.RemainingAmount = 0;
                        adv.IsClosed = true;
                    }
                    adv.UpdateDate = DateTime.UtcNow;
                }
            }
        }

        public async Task CreateSalaryExpensesAsync(
            int commercialUserId,
            PayrollRun run,
            IEnumerable<PayrollLine> lines,
            CancellationToken ct = default)
        {
            foreach (var line in lines.Where(l => !l.IsDeleted && l.NetAmount > 0))
            {
                var expense = new Expense
                {
                    Amount = line.NetAmount,
                    Date = run.PaidAt ?? DateTime.UtcNow,
                    Category = SalaryExpenseCategory,
                    Description = $"راتب {run.Year}/{run.Month:D2} — موظف #{line.EmployeeId}",
                    EmployeeId = line.EmployeeId,
                    InsertByUserId = commercialUserId,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };
                _db.Expenses.Add(expense);
                await _db.SaveChangesAsync(ct);
                line.LinkedExpenseId = expense.Id;
                line.UpdateDate = DateTime.UtcNow;
            }
        }
    }
}
