using System.Text;
using System.Text.Json;
using ExcelDataReader;

namespace ParserPlayground;

/// <summary>
/// Builds a full structured parse report for A1862604.xls (ExcelDataReader only, no Syncfusion).
/// No DB writes. Returns a single JSON object.
/// </summary>
public static class ExcelFullReportBuilder
{
    private const int ColK = 9, ColM = 12, ColO = 14;
    private static readonly string[] BuildingKeywords = { "menara", "wisma", "pangsapuri", "bangunan", "kompleks", "medan" };
    private static readonly string[] StreetPrefixes = { "jalan", "jln", "lorong", "lg", "persiaran" };
    private static readonly string[] UnitKeywords = { "level", "floor", "unit", "block", "lot" };

    public static string BuildAndReturnJson(string filePath)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = File.ReadAllBytes(filePath);
        using var stream = new MemoryStream(bytes);
        using var reader = ExcelReaderFactory.CreateBinaryReader(stream);
        var dataSet = reader.AsDataSet();

        var report = new Dictionary<string, object?>();

        report["FileInfo"] = BuildFileInfo(filePath, dataSet);
        if (dataSet.Tables.Count > 0)
        {
            var table = dataSet.Tables[0]!;
            report["SheetStructure"] = BuildSheetStructure(table);
            report["ExtractedCoreFields"] = BuildExtractedCoreFields(table);
            report["Materials"] = BuildMaterials(table);
            report["DevicesPackageInfo"] = BuildDevicesPackageInfo(table);
            var (confidence, unhandled) = BuildConfidenceAndUnhandled(table, report);
            report["ConfidenceIndicators"] = confidence;
            report["AnyUnhandledSections"] = unhandled;
        }
        else
        {
            report["SheetStructure"] = null;
            report["ExtractedCoreFields"] = new Dictionary<string, object?>();
            report["Materials"] = new Dictionary<string, object?>();
            report["DevicesPackageInfo"] = new Dictionary<string, object?>();
            report["ConfidenceIndicators"] = new Dictionary<string, object?>();
            report["AnyUnhandledSections"] = new List<object>();
        }

        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }

    private static Dictionary<string, object?> BuildFileInfo(string filePath, System.Data.DataSet dataSet)
    {
        return new Dictionary<string, object?>
        {
            ["FileName"] = Path.GetFileName(filePath),
            ["SheetCount"] = dataSet.Tables.Count,
            ["SheetNames"] = Enumerable.Range(0, dataSet.Tables.Count).Select(i => dataSet.Tables[i]!.TableName).ToList()
        };
    }

    private static Dictionary<string, object?> BuildSheetStructure(System.Data.DataTable table)
    {
        int totalRows = table.Rows.Count;
        int totalCols = table.Columns.Count;
        var first30 = new List<object>();
        for (int r = 0; r < Math.Min(30, totalRows); r++)
        {
            var rowList = new List<object?>();
            for (int c = 0; c < totalCols; c++)
            {
                var v = table.Rows[r][c];
                if (v == null || v == DBNull.Value) rowList.Add(null);
                else if (v is DateTime dt) rowList.Add(dt.ToString("O"));
                else rowList.Add(v.ToString());
            }
            first30.Add(rowList);
        }

        int headerRowIndex = DetectHeaderRow(table);
        var headers = GetHeaders(table, headerRowIndex);
        var columnStats = GetColumnStats(table, headers, headerRowIndex + 1);

        return new Dictionary<string, object?>
        {
            ["TotalRows"] = totalRows,
            ["TotalColumns"] = totalCols,
            ["First30RowsRaw"] = first30,
            ["HeaderRowIndex"] = headerRowIndex,
            ["Headers"] = headers,
            ["ColumnStats"] = columnStats
        };
    }

    private static Dictionary<string, object?> BuildExtractedCoreFields(System.Data.DataTable table)
    {
        var core = new Dictionary<string, object?>
        {
            ["OrderNumber"] = null, ["CustomerName"] = null, ["IC/ID"] = null, ["ContactNumber"] = null,
            ["Email"] = null, ["FullAddress"] = null, ["ParsedBuildingName"] = null, ["Street"] = null,
            ["City"] = null, ["Postcode"] = null, ["State"] = null
        };
        string? fullAddress = null;

        for (int r = 0; r < table.Rows.Count; r++)
        {
            var label = GetCell(table, r, 1);
            if (string.IsNullOrWhiteSpace(label)) continue;
            var labelLower = label.ToLowerInvariant();
            var value = GetValueFromRow(table, r);

            if (labelLower.Contains("reference no") || labelLower.Contains("ref no")) core["OrderNumber"] = value;
            else if (labelLower.Contains("customer name")) core["CustomerName"] = value;
            else if (labelLower.Contains("ic") && (labelLower.Contains("no") || labelLower.Contains("number"))) core["IC/ID"] = value;
            else if (labelLower.Contains("contact no") || labelLower.Contains("contact number") || labelLower.Contains("phone")) core["ContactNumber"] = value;
            else if (labelLower.Contains("email") && !labelLower.Contains("tdc")) core["Email"] = value;
            else if (labelLower.Contains("service address"))
            {
                if (!string.IsNullOrWhiteSpace(value)) { fullAddress = value; core["FullAddress"] = value; }
            }
        }

        if (!string.IsNullOrWhiteSpace(fullAddress))
        {
            var (buildingName, city, postcode) = ParseAddressForBuilding(fullAddress);
            core["ParsedBuildingName"] = buildingName;
            core["Street"] = ExtractStreet(fullAddress);
            core["City"] = city;
            core["Postcode"] = postcode;
            core["State"] = ExtractState(fullAddress);
        }
        else
        {
            core["ParsedBuildingName"] = null;
            core["Street"] = null;
            core["City"] = null;
            core["Postcode"] = null;
            core["State"] = null;
        }

        return core;
    }

    private static Dictionary<string, object?> BuildMaterials(System.Data.DataTable table)
    {
        var raw = new List<object>();
        var toSupply = new List<string>();
        var notProvided = new List<string>();
        var zeroQty = new List<object>();

        if (ColK >= table.Columns.Count)
            return new Dictionary<string, object?> { ["RawMaterialsRows"] = raw, ["MaterialsToSupply"] = toSupply, ["MaterialsNotProvided"] = notProvided, ["ZeroQuantityMaterials"] = zeroQty };

        for (int row = 30; row <= 50 && row < table.Rows.Count; row++)
        {
            var name = table.Rows[row][ColK]?.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name)) continue;
            var addVal = ColM < table.Columns.Count ? table.Rows[row][ColM]?.ToString()?.Trim() : null;
            var notProv = ColO < table.Columns.Count ? table.Rows[row][ColO]?.ToString()?.Trim() : null;

            raw.Add(new { RowIndex = row + 1, Name = name, ADD_or_Qty = addVal, NotProvided = notProv });
            bool toSupplyThis = "X".Equals(addVal, StringComparison.OrdinalIgnoreCase);
            bool notProvidedThis = "X".Equals(notProv, StringComparison.OrdinalIgnoreCase);
            if (toSupplyThis) toSupply.Add(NormalizeMaterialName(name));
            else if (notProvidedThis) notProvided.Add(NormalizeMaterialName(name));
        }

        for (int row = 53; row <= 65 && row < table.Rows.Count; row++)
        {
            var name = table.Rows[row][ColK]?.ToString()?.Trim() ?? "";
            var qtyStr = ColM < table.Columns.Count ? table.Rows[row][ColM]?.ToString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(qtyStr ?? "")) continue;
            raw.Add(new { RowIndex = row + 1, Name = name, Quantity = qtyStr });
            int qty = int.TryParse(qtyStr, out var n) ? n : 0;
            if (qty > 0) toSupply.Add(NormalizeMaterialName(name));
            else if (!string.IsNullOrWhiteSpace(name)) zeroQty.Add(new { Name = NormalizeMaterialName(name), Quantity = qtyStr ?? "0" });
        }

        return new Dictionary<string, object?>
        {
            ["RawMaterialsRows"] = raw,
            ["MaterialsToSupply"] = toSupply,
            ["MaterialsNotProvided"] = notProvided,
            ["ZeroQuantityMaterials"] = zeroQty
        };
    }

    private static Dictionary<string, object?> BuildDevicesPackageInfo(System.Data.DataTable table)
    {
        var info = new Dictionary<string, object?>
        {
            ["PackageName"] = null, ["Speed"] = null, ["DeviceModel"] = null, ["ONUModel"] = null
        };
        var skus = new List<string>();

        for (int r = 0; r < table.Rows.Count; r++)
        {
            var label = GetCell(table, r, 1);
            if (string.IsNullOrWhiteSpace(label)) continue;
            var labelLower = label.ToLowerInvariant();
            var value = GetValueFromRow(table, r);

            if (labelLower.Contains("package")) info["PackageName"] = value;
            else if (labelLower.Contains("bandwidth") || labelLower.Contains("speed")) info["Speed"] = value;
            else if (labelLower.Contains("device") || labelLower.Contains("equipment")) info["DeviceModel"] = value;
            else if (labelLower.Contains("onu") || (value != null && (value.Contains("HG8145") || value.Contains("ONU")))) info["ONUModel"] = value;
            if (value != null && System.Text.RegularExpressions.Regex.IsMatch(value, @"CAE-\d{3}-\d{4}"))
                skus.AddRange(System.Text.RegularExpressions.Regex.Matches(value, @"CAE-\d{3}-\d{4}").Select(m => m.Value));
            if (value != null && System.Text.RegularExpressions.Regex.IsMatch(value, @"PON-[A-Z0-9-]+"))
                skus.AddRange(System.Text.RegularExpressions.Regex.Matches(value, @"PON-[A-Z0-9-]+").Select(m => m.Value));
        }
        info["AnyExtractedSKUs"] = skus.Distinct().ToList();
        return info;
    }

    private static (Dictionary<string, object?> confidence, List<object> unhandled) BuildConfidenceAndUnhandled(System.Data.DataTable table, Dictionary<string, object?> report)
    {
        var missing = new List<string>();
        var layoutWarnings = new List<string>();
        var mappedRows = new HashSet<int>();

        var core = report["ExtractedCoreFields"] as Dictionary<string, object?> ?? new Dictionary<string, object?>();
        if (core.GetValueOrDefault("OrderNumber") == null) missing.Add("OrderNumber");
        if (core.GetValueOrDefault("CustomerName") == null) missing.Add("CustomerName");
        if (core.GetValueOrDefault("ContactNumber") == null) missing.Add("ContactNumber");
        if (core.GetValueOrDefault("FullAddress") == null) missing.Add("FullAddress");

        var sheetStruct = report["SheetStructure"] as Dictionary<string, object?>;
        if (sheetStruct != null && sheetStruct.TryGetValue("HeaderRowIndex", out var hri) && hri is int hi && hi > 3)
            layoutWarnings.Add("HeaderRowIndex is > 3; form-style layout assumed.");

        for (int r = 0; r < Math.Min(30, table.Rows.Count); r++)
        {
            for (int c = 0; c < table.Columns.Count; c++)
            {
                if (!IsEmpty(table.Rows[r][c])) { mappedRows.Add(r); break; }
            }
        }
        for (int r = 30; r <= 65 && r < table.Rows.Count; r++) mappedRows.Add(r);

        var unhandled = new List<object>();
        for (int r = 0; r < table.Rows.Count; r++)
        {
            if (mappedRows.Contains(r)) continue;
            var rowCells = new List<object?>();
            bool any = false;
            for (int c = 0; c < table.Columns.Count; c++)
            {
                var v = table.Rows[r][c];
                if (!IsEmpty(v)) any = true;
                rowCells.Add(v == null || v == DBNull.Value ? null : v.ToString());
            }
            if (any) unhandled.Add(new { RowIndex = r + 1, Cells = rowCells });
        }

        int score = 100 - (missing.Count * 15) - (layoutWarnings.Count * 5);
        if (score < 0) score = 0;

        var confidence = new Dictionary<string, object?>
        {
            ["MissingRequiredFields"] = missing,
            ["LayoutWarnings"] = layoutWarnings,
            ["ExtractionConfidenceScore"] = score
        };
        return (confidence, unhandled);
    }

    private static string? GetCell(System.Data.DataTable table, int row, int col)
    {
        if (row < 0 || row >= table.Rows.Count || col < 0 || col >= table.Columns.Count) return null;
        var v = table.Rows[row][col];
        return (v == null || v == DBNull.Value) ? null : v.ToString()?.Trim();
    }

    private static string? GetValueFromRow(System.Data.DataTable table, int row)
    {
        var r = table.Rows[row];
        for (int c = 2; c < table.Columns.Count; c++)
        {
            var v = r[c];
            if (v != null && v != DBNull.Value)
            {
                var s = v.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }
        return null;
    }

    private static (string? buildingName, string? city, string? postcode) ParseAddressForBuilding(string? addressText)
    {
        if (string.IsNullOrWhiteSpace(addressText)) return (null, null, null);
        var parts = System.Text.RegularExpressions.Regex.Split(addressText, @"[\r\n,]+").Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        string? buildingName = null;
        string? postcode = parts.FirstOrDefault(p => System.Text.RegularExpressions.Regex.IsMatch(p, @"^\d{5}$"));
        string? city = null;
        var postcodeIndex = Array.IndexOf(parts, postcode ?? "");
        if (postcodeIndex >= 0 && postcodeIndex + 1 < parts.Length) city = parts[postcodeIndex + 1];
        else if (parts.Length >= 2) city = parts[parts.Length - 2];

        var priorityKeywords = new (string keyword, int priority)[] { ("menara", 1), ("wisma", 2), ("pangsapuri", 3), ("bangunan", 4), ("kompleks", 5), ("medan", 6) };
        var candidates = new List<(string segment, int priority)>();
        foreach (var part in parts)
        {
            var lower = part.ToLowerInvariant();
            if (UnitKeywords.Any(kw => lower.Contains(kw))) continue;
            if (StreetPrefixes.Any(prefix => lower.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) continue;
            var words = part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (var (keyword, priority) in priorityKeywords)
            {
                if (words.Any(w => w.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    candidates.Add((part, priority));
                    break;
                }
            }
        }
        if (candidates.Count > 0) buildingName = candidates.OrderBy(c => c.priority).First().segment;
        return (buildingName, city, postcode);
    }

    private static string? ExtractStreet(string address)
    {
        var parts = address.Split(',').Select(p => p.Trim()).ToArray();
        foreach (var p in parts)
        {
            var lower = p.ToLowerInvariant();
            if (StreetPrefixes.Any(prefix => lower.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return p;
        }
        return null;
    }

    private static string? ExtractState(string address)
    {
        var knownStates = new[] { "Kuala Lumpur", "Selangor", "Johor", "Penang", "Wilayah Persekutuan" };
        foreach (var state in knownStates)
        {
            if (address.Contains(state, StringComparison.OrdinalIgnoreCase)) return state;
        }
        var parts = address.Split(',').Select(p => p.Trim()).ToArray();
        if (parts.Length >= 1) return parts[^1];
        return null;
    }

    private static string NormalizeMaterialName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
    }

    private static bool IsEmpty(object? v)
    {
        if (v == null || v == DBNull.Value) return true;
        return string.IsNullOrWhiteSpace(v.ToString());
    }

    private static int DetectHeaderRow(System.Data.DataTable table)
    {
        if (table.Rows.Count == 0) return 0;
        for (int r = 0; r < Math.Min(10, table.Rows.Count); r++)
        {
            int nonEmpty = 0;
            for (int c = 0; c < table.Columns.Count; c++)
            {
                if (!IsEmpty(table.Rows[r][c])) nonEmpty++;
            }
            if (nonEmpty >= 2) return r;
        }
        return 0;
    }

    private static List<string> GetHeaders(System.Data.DataTable table, int headerRowIndex)
    {
        var list = new List<string>();
        var row = table.Rows[headerRowIndex];
        for (int c = 0; c < table.Columns.Count; c++)
        {
            var s = row[c]?.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(s)) s = $"Column{c + 1}";
            list.Add(s);
        }
        return list;
    }

    private static Dictionary<string, int> GetColumnStats(System.Data.DataTable table, List<string> headers, int dataStartRow)
    {
        var stats = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var h in headers) stats[h] = 0;
        for (int r = dataStartRow; r < table.Rows.Count; r++)
        {
            for (int c = 0; c < headers.Count && c < table.Columns.Count; c++)
            {
                if (IsEmpty(table.Rows[r][c])) stats[headers[c]]++;
            }
        }
        return stats;
    }
}
