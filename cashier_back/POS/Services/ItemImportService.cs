using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models;
using POS.Models.Dtos;

namespace POS.Services
{
    public interface IItemImportService
    {
        Task<ItemImportResultDto> ImportFromExcelAsync(Stream fileStream, int userId, int commercialUserId);
    }

    public class ItemImportService : IItemImportService
    {
        private readonly DbConfig _dbConfig;
        private readonly ILogger<ItemImportService> _logger;

        private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = new[] { "كود المنتج", "code", "product code", "item code", "الكود" },
            ["name"] = new[] { "اسم المنتج", "name", "product name", "item name", "الاسم" },
            ["description"] = new[] { "وصف المنتج", "description", "وصف" },
            ["image"] = new[] { "اسم صورة المنتج", "image", "image name", "صورة" },
            ["tags"] = new[] { "اسم القسم", "tags", "category", "قسم", "القسم" },
            ["sellingPrice"] = new[] { "السعر", "selling price", "price", "سعر البيع" },
            ["disCountPrice"] = new[] { "سعر الخصم", "discount price", "discount", "خصم" },
            ["wholesalePrice"] = new[] { "سعر الجملة", "wholesale price", "wholesale", "جملة" },
        };

        public ItemImportService(DbConfig dbConfig, ILogger<ItemImportService> logger)
        {
            _dbConfig = dbConfig;
            _logger = logger;
        }

        public async Task<ItemImportResultDto> ImportFromExcelAsync(
            Stream fileStream,
            int userId,
            int commercialUserId)
        {
            var result = new ItemImportResultDto();
            var tagCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tagsCreated = 0;

            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                result.RowsWithErrors = 1;
                result.Errors.Add(new ItemImportRowError { RowNumber = 0, Message = "emptyWorksheet" });
                return result;
            }

            var columns = ResolveColumns(worksheet);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow < 2)
            {
                result.RowsWithErrors = 1;
                result.Errors.Add(new ItemImportRowError { RowNumber = 0, Message = "noDataRows" });
                return result;
            }

            var user = await _dbConfig.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            var userInsertByUserId = user?.InsertByUserId ?? userId;

            var existingItems = await _dbConfig.Items
                .Where(x => !x.IsDeleted &&
                    (x.InsertByUserId == commercialUserId ||
                     x.User!.Id == commercialUserId ||
                     x.User.InsertByUserId == commercialUserId))
                .Select(x => new { x.Code, x.Name })
                .ToListAsync();

            var existingCodes = new HashSet<string>(
                existingItems.Select(x => NormalizeKey(x.Code)),
                StringComparer.OrdinalIgnoreCase);
            var existingNames = new HashSet<string>(
                existingItems.Select(x => NormalizeKey(x.Name)),
                StringComparer.OrdinalIgnoreCase);

            var pendingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pendingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var rowNum = 2; rowNum <= lastRow; rowNum++)
            {
                var row = worksheet.Row(rowNum);
                var code = GetCellString(row, columns.Code);
                var name = GetCellString(row, columns.Name);

                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.RowsWithErrors++;
                    result.Errors.Add(new ItemImportRowError { RowNumber = rowNum, Message = "missingProductCode" });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.RowsWithErrors++;
                    result.Errors.Add(new ItemImportRowError { RowNumber = rowNum, Message = "missingProductName" });
                    continue;
                }

                var codeKey = NormalizeKey(code);
                var nameKey = NormalizeKey(name);

                if (existingCodes.Contains(codeKey) || pendingCodes.Contains(codeKey))
                {
                    result.ItemsSkipped++;
                    continue;
                }

                if (existingNames.Contains(nameKey) || pendingNames.Contains(nameKey))
                {
                    result.ItemsSkipped++;
                    continue;
                }

                if (!TryParseDecimal(GetCellString(row, columns.SellingPrice), out var sellingPrice) || sellingPrice < 0)
                {
                    result.RowsWithErrors++;
                    result.Errors.Add(new ItemImportRowError { RowNumber = rowNum, Message = "invalidSellingPrice" });
                    continue;
                }

                var discountRaw = GetCellString(row, columns.DisCountPrice);
                decimal disCountPrice;
                if (string.IsNullOrWhiteSpace(discountRaw) || !TryParseDecimal(discountRaw, out disCountPrice) || disCountPrice <= 0)
                {
                    disCountPrice = sellingPrice;
                }

                var wholesaleRaw = GetCellString(row, columns.WholesalePrice);
                decimal wholesalePrice = 0;
                if (!string.IsNullOrWhiteSpace(wholesaleRaw) && TryParseDecimal(wholesaleRaw, out var parsedWholesale) && parsedWholesale >= 0)
                {
                    wholesalePrice = parsedWholesale;
                }

                var tagName = GetCellString(row, columns.Tags);
                if (!string.IsNullOrWhiteSpace(tagName))
                {
                    var (resolvedTag, tagWasCreated) = await ResolveOrCreateTagAsync(
                        tagName,
                        commercialUserId,
                        userId,
                        userInsertByUserId,
                        tagCache);
                    if (tagWasCreated) tagsCreated++;
                    tagName = resolvedTag;
                }

                var description = GetCellString(row, columns.Description);
                var imageFileName = GetCellString(row, columns.Image);

                var item = new Item
                {
                    Name = name.Trim(),
                    Code = code.Trim(),
                    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    Image = string.IsNullOrWhiteSpace(imageFileName) ? null : imageFileName.Trim(),
                    Tags = string.IsNullOrWhiteSpace(tagName) ? null : tagName.Trim(),
                    SellingPrice = sellingPrice,
                    DisCountPrice = disCountPrice,
                    WholesalePrice = wholesalePrice,
                    PurchasingPrice = 0,
                    Quantity = 0,
                    InsertByUserId = commercialUserId,
                };

                _dbConfig.Items.Add(item);
                pendingCodes.Add(codeKey);
                pendingNames.Add(nameKey);
                result.ItemsCreated++;
            }

            if (result.ItemsCreated > 0 || tagsCreated > 0)
            {
                await _dbConfig.SaveChangesAsync();
            }

            result.TagsCreated = tagsCreated;
            _logger.LogInformation(
                "Item import completed by user {UserId}: created={Created}, skipped={Skipped}, tags={Tags}, errors={Errors}",
                userId, result.ItemsCreated, result.ItemsSkipped, result.TagsCreated, result.RowsWithErrors);

            return result;
        }

        private async Task<(string TagName, bool Created)> ResolveOrCreateTagAsync(
            string tagName,
            int commercialUserId,
            int userId,
            int userInsertByUserId,
            Dictionary<string, string> tagCache)
        {
            var trimmed = tagName.Trim();
            var cacheKey = NormalizeKey(trimmed);

            if (tagCache.TryGetValue(cacheKey, out var cachedName))
            {
                return (cachedName, false);
            }

            var existing = await _dbConfig.Tags
                .Where(x => !x.IsDeleted &&
                    (x.InsertByUserId == commercialUserId ||
                     x.User!.Id == commercialUserId ||
                     x.User.InsertByUserId == commercialUserId))
                .ToListAsync();

            var match = existing.FirstOrDefault(t =>
                string.Equals(t.Name?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                var canonical = match.Name!.Trim();
                tagCache[cacheKey] = canonical;
                return (canonical, false);
            }

            var newTag = new Tag
            {
                Name = trimmed,
                IsForAll = false,
                InsertByUserId = commercialUserId,
            };
            _dbConfig.Tags.Add(newTag);
            await _dbConfig.SaveChangesAsync();
            tagCache[cacheKey] = trimmed;
            return (trimmed, true);
        }

        private sealed class ColumnMap
        {
            public int Code { get; set; } = 1;
            public int Name { get; set; } = 2;
            public int Description { get; set; } = 4;
            public int Image { get; set; } = 5;
            public int Tags { get; set; } = 6;
            public int SellingPrice { get; set; } = 7;
            public int DisCountPrice { get; set; } = 8;
            public int WholesalePrice { get; set; } = 9;
        }

        private static ColumnMap ResolveColumns(IXLWorksheet worksheet)
        {
            var map = new ColumnMap();
            var headerRow = worksheet.Row(1);
            var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 8;
            var foundAny = false;

            for (var col = 1; col <= lastCol; col++)
            {
                var header = NormalizeKey(headerRow.Cell(col).GetString());
                if (string.IsNullOrEmpty(header)) continue;

                if (MatchesHeader(header, "code")) { map.Code = col; foundAny = true; }
                else if (MatchesHeader(header, "name")) { map.Name = col; foundAny = true; }
                else if (MatchesHeader(header, "description")) { map.Description = col; foundAny = true; }
                else if (MatchesHeader(header, "image")) { map.Image = col; foundAny = true; }
                else if (MatchesHeader(header, "tags")) { map.Tags = col; foundAny = true; }
                else if (MatchesHeader(header, "sellingPrice")) { map.SellingPrice = col; foundAny = true; }
                else if (MatchesHeader(header, "disCountPrice")) { map.DisCountPrice = col; foundAny = true; }
                else if (MatchesHeader(header, "wholesalePrice")) { map.WholesalePrice = col; foundAny = true; }
            }

            return foundAny ? map : new ColumnMap();
        }

        private static bool MatchesHeader(string normalizedHeader, string fieldKey)
        {
            if (!HeaderAliases.TryGetValue(fieldKey, out var aliases)) return false;
            return aliases.Any(a => NormalizeKey(a) == normalizedHeader);
        }

        private static string GetCellString(IXLRow row, int columnIndex)
        {
            if (columnIndex <= 0) return "";
            var cell = row.Cell(columnIndex);
            if (cell.IsEmpty()) return "";

            if (cell.DataType == XLDataType.Number)
            {
                return cell.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return cell.GetString().Trim();
        }

        private static bool TryParseDecimal(string? raw, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var cleaned = raw.Trim().Replace(",", "");
            return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out value);
        }

        private static string NormalizeKey(string? value) =>
            (value ?? "").Trim().ToLowerInvariant();
    }
}
