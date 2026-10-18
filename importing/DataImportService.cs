using System.Globalization;
using System.IO;

namespace GymManagementSystem.Importing;

public sealed class DataImportService
{
    private static readonly string[] MemberColumns =
        { "Name", "Age", "Weight", "PhoneNumber" };

    private static readonly string[] PlanColumns =
        { "Name", "Price", "Description" };

    private readonly MemberService memberService;
    private readonly SubscriptionPlanService planService;

    public DataImportService(
        MemberService memberService,
        SubscriptionPlanService planService)
    {
        this.memberService = memberService;
        this.planService = planService;
    }

    public async Task<ImportPreview> CreatePreviewAsync(
        string filePath,
        ImportDataType dataType,
        CancellationToken cancellationToken = default)
    {
        TabularData data = await ImportFileReader.ReadAsync(filePath, cancellationToken);
        string[] requiredColumns = dataType == ImportDataType.Members
            ? MemberColumns
            : PlanColumns;
        Dictionary<string, int> columnIndexes = CreateColumnMap(data.Headers, requiredColumns);

        List<ImportRow> rows = dataType == ImportDataType.Members
            ? ValidateMembers(data, columnIndexes)
            : ValidatePlans(data, columnIndexes);

        if (rows.Count == 0)
        {
            throw new ImportFileException("The file contains a header but no data records.");
        }

        return new ImportPreview
        {
            FileName = Path.GetFileName(filePath),
            DataType = dataType,
            Rows = rows
        };
    }

    public ImportSummary ImportValidRows(ImportPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        int imported = 0;
        int failed = 0;

        foreach (ImportRow row in preview.Rows.Where(row => row.Status == ImportRowStatus.Valid))
        {
            try
            {
                if (preview.DataType == ImportDataType.Members)
                {
                    Member member = new(
                        row.Name,
                        int.Parse(row.Age, NumberStyles.Integer, CultureInfo.InvariantCulture),
                        double.Parse(row.Weight, NumberStyles.Float, CultureInfo.InvariantCulture),
                        row.PhoneNumber);
                    memberService.addMember(member);
                }
                else
                {
                    SubscriptionPlan plan = new(
                        row.Name,
                        decimal.Parse(row.Price, NumberStyles.Number, CultureInfo.InvariantCulture),
                        row.Description);
                    planService.addPlan(plan);
                }

                row.Status = ImportRowStatus.Imported;
                row.Reason = "Imported successfully.";
                imported++;
            }
            catch (Exception ex)
            {
                row.Status = ImportRowStatus.Failed;
                row.Reason = ex.Message;
                failed++;
            }
        }

        return new ImportSummary
        {
            ImportedCount = imported,
            FailedCount = failed,
            SkippedCount = preview.InvalidCount + preview.DuplicateCount + failed
        };
    }

    private static Dictionary<string, int> CreateColumnMap(
        List<string> headers,
        string[] requiredColumns)
    {
        if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            throw new ImportFileException("The file does not contain a header row.");
        }

        Dictionary<string, int> indexes = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < headers.Count; index++)
        {
            string header = headers[index].Trim();
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            if (!indexes.TryAdd(header, index))
            {
                throw new ImportFileException($"The header '{header}' appears more than once.");
            }
        }

        List<string> missing = requiredColumns.Where(column => !indexes.ContainsKey(column)).ToList();
        if (missing.Count > 0)
        {
            throw new ImportFileException($"Missing required column(s): {string.Join(", ", missing)}.");
        }

        return indexes;
    }

    private List<ImportRow> ValidateMembers(
        TabularData data,
        Dictionary<string, int> indexes)
    {
        HashSet<string> existingPhones = memberService.getAllMembers()
            .Select(member => member.PhoneNumber)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> filePhones = new(StringComparer.Ordinal);
        List<ImportRow> rows = new();

        foreach (TabularRow source in data.Rows)
        {
            ImportRow row = new()
            {
                RowNumber = source.RowNumber,
                Name = GetValue(source.Values, indexes["Name"]),
                Age = GetValue(source.Values, indexes["Age"]),
                Weight = GetValue(source.Values, indexes["Weight"]),
                PhoneNumber = GetValue(source.Values, indexes["PhoneNumber"])
            };

            if (source.StructuralError != null)
            {
                SetInvalid(row, source.StructuralError);
            }
            else if (TryValidateMember(row, out string reason))
            {
                if (existingPhones.Contains(row.PhoneNumber))
                {
                    SetDuplicate(row, "A member with this phone number already exists.");
                }
                else if (!filePhones.Add(row.PhoneNumber))
                {
                    SetDuplicate(row, "This phone number appears more than once in the file.");
                }
                else
                {
                    row.Status = ImportRowStatus.Valid;
                    row.Reason = "Ready to import.";
                }
            }
            else
            {
                SetInvalid(row, reason);
            }

            rows.Add(row);
        }

        return rows;
    }

    private List<ImportRow> ValidatePlans(
        TabularData data,
        Dictionary<string, int> indexes)
    {
        HashSet<string> existingNames = planService.getAllPlans()
            .Select(plan => plan.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> fileNames = new(StringComparer.OrdinalIgnoreCase);
        List<ImportRow> rows = new();

        foreach (TabularRow source in data.Rows)
        {
            ImportRow row = new()
            {
                RowNumber = source.RowNumber,
                Name = GetValue(source.Values, indexes["Name"]),
                Price = GetValue(source.Values, indexes["Price"]),
                Description = GetValue(source.Values, indexes["Description"])
            };

            if (source.StructuralError != null)
            {
                SetInvalid(row, source.StructuralError);
            }
            else if (TryValidatePlan(row, out string reason))
            {
                if (existingNames.Contains(row.Name))
                {
                    SetDuplicate(row, "A subscription plan with this name already exists.");
                }
                else if (!fileNames.Add(row.Name))
                {
                    SetDuplicate(row, "This plan name appears more than once in the file.");
                }
                else
                {
                    row.Status = ImportRowStatus.Valid;
                    row.Reason = "Ready to import.";
                }
            }
            else
            {
                SetInvalid(row, reason);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static bool TryValidateMember(ImportRow row, out string reason)
    {
        try
        {
            if (!int.TryParse(row.Age, NumberStyles.Integer, CultureInfo.InvariantCulture, out int age))
            {
                reason = "Age must be a whole number.";
                return false;
            }

            if (!double.TryParse(row.Weight, NumberStyles.Float, CultureInfo.InvariantCulture, out double weight) ||
                !double.IsFinite(weight))
            {
                reason = "Weight must be a valid number.";
                return false;
            }

            Member.validateName(row.Name);
            Member.validateAge(age);
            Member.validateWeight(weight);
            Member.validatePhoneNumber(row.PhoneNumber);
            reason = string.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    private static bool TryValidatePlan(ImportRow row, out string reason)
    {
        try
        {
            if (!decimal.TryParse(
                    row.Price,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal price))
            {
                reason = "Price must be a valid number.";
                return false;
            }

            SubscriptionPlan.validateName(row.Name);
            SubscriptionPlan.validatePrice(price);
            SubscriptionPlan.validateDescription(row.Description);
            reason = string.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    private static string GetValue(List<string> values, int index)
    {
        return index >= 0 && index < values.Count ? values[index] : string.Empty;
    }

    private static void SetInvalid(ImportRow row, string reason)
    {
        row.Status = ImportRowStatus.Invalid;
        row.Reason = reason;
    }

    private static void SetDuplicate(ImportRow row, string reason)
    {
        row.Status = ImportRowStatus.Duplicate;
        row.Reason = reason;
    }
}
