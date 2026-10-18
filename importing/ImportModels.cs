using System.IO;

namespace GymManagementSystem.Importing;

public enum ImportDataType
{
    Members,
    SubscriptionPlans
}

public enum ImportRowStatus
{
    Valid,
    Invalid,
    Duplicate,
    Imported,
    Failed
}

public sealed class ImportRow
{
    public int RowNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Age { get; init; } = string.Empty;
    public string Weight { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ImportRowStatus Status { get; internal set; }
    public string Reason { get; internal set; } = string.Empty;
}

public sealed class ImportPreview
{
    public required string FileName { get; init; }
    public required ImportDataType DataType { get; init; }
    public required List<ImportRow> Rows { get; init; }

    public int TotalCount => Rows.Count;
    public int ValidCount => Rows.Count(row => row.Status == ImportRowStatus.Valid);
    public int InvalidCount => Rows.Count(row => row.Status == ImportRowStatus.Invalid);
    public int DuplicateCount => Rows.Count(row => row.Status == ImportRowStatus.Duplicate);
    public int ImportedCount => Rows.Count(row => row.Status == ImportRowStatus.Imported);
    public int FailedCount => Rows.Count(row => row.Status == ImportRowStatus.Failed);
    public int SkippedCount => InvalidCount + DuplicateCount + FailedCount;
}

public sealed class ImportSummary
{
    public int ImportedCount { get; init; }
    public int FailedCount { get; init; }
    public int SkippedCount { get; init; }
}

internal sealed class TabularData
{
    public required List<string> Headers { get; init; }
    public required List<TabularRow> Rows { get; init; }
}

internal sealed class TabularRow
{
    public int RowNumber { get; init; }
    public required List<string> Values { get; init; }
    public string? StructuralError { get; init; }
}

public sealed class ImportFileException : Exception
{
    public ImportFileException(string message)
        : base(message)
    {
    }

    public ImportFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
