using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GymManagementSystem.Importing;

internal static class ImportFileReader
{
    private const int MaxRows = 10_000;
    private const long MaxFileBytes = 10 * 1024 * 1024;
    private const long MaxExpandedBytes = 25 * 1024 * 1024;

    public static Task<TabularData> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Read(filePath, cancellationToken), cancellationToken);
    }

    private static TabularData Read(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ImportFileException("Select a file to import.");
        }

        FileInfo file = new(filePath);
        if (!file.Exists)
        {
            throw new ImportFileException("The selected file no longer exists.");
        }

        if (file.Length == 0)
        {
            throw new ImportFileException("The selected file is empty.");
        }

        if (file.Length > MaxFileBytes)
        {
            throw new ImportFileException("The selected file is larger than the 10 MB import limit.");
        }

        return file.Extension.ToLowerInvariant() switch
        {
            ".txt" => ReadTxt(file.FullName, cancellationToken),
            ".xlsx" => ReadXlsx(file.FullName, cancellationToken),
            _ => throw new ImportFileException("Only .xlsx and UTF-8 tab-separated .txt files are supported.")
        };
    }

    private static TabularData ReadTxt(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader reader = new(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: false);

            string? headerLine = reader.ReadLine();
            if (headerLine == null || string.IsNullOrWhiteSpace(headerLine))
            {
                throw new ImportFileException("The TXT file must contain a tab-separated header row.");
            }

            headerLine = headerLine.TrimStart('\uFEFF');
            if (!headerLine.Contains('\t'))
            {
                throw new ImportFileException("The TXT header is not tab-separated.");
            }

            List<string> headers = headerLine.Split('\t').Select(header => header.Trim()).ToList();
            List<TabularRow> rows = new();
            int rowNumber = 1;

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string line = reader.ReadLine() ?? string.Empty;
                rowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (rows.Count >= MaxRows)
                {
                    throw new ImportFileException($"Import files may contain at most {MaxRows:N0} data rows.");
                }

                List<string> values = line.Split('\t').ToList();
                rows.Add(new TabularRow
                {
                    RowNumber = rowNumber,
                    Values = values,
                    StructuralError = values.Count == headers.Count
                        ? null
                        : $"Expected {headers.Count} tab-separated values but found {values.Count}."
                });
            }

            return new TabularData { Headers = headers, Rows = rows };
        }
        catch (ImportFileException)
        {
            throw;
        }
        catch (DecoderFallbackException ex)
        {
            throw new ImportFileException("The TXT file is not valid UTF-8.", ex);
        }
        catch (IOException ex)
        {
            throw new ImportFileException("The TXT file could not be read. It may be locked or unavailable.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ImportFileException("Access to the selected TXT file was denied.", ex);
        }
    }

    private static TabularData ReadXlsx(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, leaveOpen: false);

            if (archive.Entries.Sum(entry => entry.Length) > MaxExpandedBytes)
            {
                throw new ImportFileException("The expanded workbook is larger than the 25 MB safety limit.");
            }

            if (archive.Entries.Any(entry =>
                entry.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ImportFileException("Macro-enabled workbook content is not supported.");
            }

            XDocument workbook = LoadXml(GetRequiredEntry(archive, "xl/workbook.xml"));
            XDocument relationships = LoadXml(GetRequiredEntry(archive, "xl/_rels/workbook.xml.rels"));

            XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace relationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace packageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

            XElement? firstSheet = workbook.Descendants(spreadsheet + "sheet").FirstOrDefault();
            string? relationshipId = firstSheet?.Attribute(relationshipsNamespace + "id")?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                throw new ImportFileException("The workbook does not contain a readable worksheet.");
            }

            XElement? relationship = relationships.Descendants(packageRelationships + "Relationship")
                .FirstOrDefault(item => string.Equals(
                    item.Attribute("Id")?.Value,
                    relationshipId,
                    StringComparison.Ordinal));

            string? target = relationship?.Attribute("Target")?.Value;
            if (string.IsNullOrWhiteSpace(target))
            {
                throw new ImportFileException("The first worksheet relationship is missing.");
            }

            Uri baseUri = new("https://local.invalid/xl/workbook.xml");
            string worksheetPath = Uri.UnescapeDataString(new Uri(baseUri, target).AbsolutePath.TrimStart('/'));
            if (!worksheetPath.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ||
                worksheetPath.Contains("..", StringComparison.Ordinal))
            {
                throw new ImportFileException("The workbook contains an invalid worksheet path.");
            }

            List<string> sharedStrings = ReadSharedStrings(archive, spreadsheet);
            XDocument worksheet = LoadXml(GetRequiredEntry(archive, worksheetPath));
            List<XElement> sheetRows = worksheet.Descendants(spreadsheet + "sheetData")
                .Elements(spreadsheet + "row")
                .ToList();

            if (sheetRows.Count == 0)
            {
                throw new ImportFileException("The first worksheet is empty.");
            }

            if (sheetRows.Count - 1 > MaxRows)
            {
                throw new ImportFileException($"Import files may contain at most {MaxRows:N0} data rows.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            List<string> headers = ReadExcelRow(sheetRows[0], sharedStrings, spreadsheet, out string? headerError);
            if (headerError != null)
            {
                throw new ImportFileException($"The header row is invalid: {headerError}");
            }

            List<TabularRow> rows = new();
            foreach (XElement sheetRow in sheetRows.Skip(1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                List<string> values = ReadExcelRow(sheetRow, sharedStrings, spreadsheet, out string? error);
                if (values.All(string.IsNullOrWhiteSpace) && error == null)
                {
                    continue;
                }

                int rowNumber = int.TryParse(sheetRow.Attribute("r")?.Value, out int parsedRowNumber)
                    ? parsedRowNumber
                    : rows.Count + 2;

                rows.Add(new TabularRow
                {
                    RowNumber = rowNumber,
                    Values = values,
                    StructuralError = error
                });
            }

            return new TabularData { Headers = headers, Rows = rows };
        }
        catch (ImportFileException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            throw new ImportFileException("The selected file is not a valid .xlsx workbook.", ex);
        }
        catch (XmlException ex)
        {
            throw new ImportFileException("The workbook contains malformed XML.", ex);
        }
        catch (IOException ex)
        {
            throw new ImportFileException("The workbook could not be read. It may be locked or unavailable.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ImportFileException("Access to the selected workbook was denied.", ex);
        }
    }

    private static List<string> ReadSharedStrings(ZipArchive archive, XNamespace spreadsheet)
    {
        ZipArchiveEntry? entry = FindEntry(archive, "xl/sharedStrings.xml");
        if (entry == null)
        {
            return new List<string>();
        }

        XDocument document = LoadXml(entry);
        return document.Descendants(spreadsheet + "si")
            .Select(item => string.Concat(item.Descendants(spreadsheet + "t").Select(text => text.Value)))
            .ToList();
    }

    private static List<string> ReadExcelRow(
        XElement row,
        List<string> sharedStrings,
        XNamespace spreadsheet,
        out string? error)
    {
        Dictionary<int, string> values = new();
        error = null;
        int fallbackColumn = 0;

        foreach (XElement cell in row.Elements(spreadsheet + "c"))
        {
            int columnIndex = GetColumnIndex(cell.Attribute("r")?.Value) ?? fallbackColumn;
            fallbackColumn = columnIndex + 1;

            if (cell.Element(spreadsheet + "f") != null)
            {
                error = "Spreadsheet formulas are not supported.";
                continue;
            }

            string type = cell.Attribute("t")?.Value ?? string.Empty;
            string rawValue = cell.Element(spreadsheet + "v")?.Value ?? string.Empty;
            string value;

            if (type == "s")
            {
                if (!int.TryParse(rawValue, out int sharedIndex) ||
                    sharedIndex < 0 ||
                    sharedIndex >= sharedStrings.Count)
                {
                    error = "A shared-string cell contains an invalid reference.";
                    value = string.Empty;
                }
                else
                {
                    value = sharedStrings[sharedIndex];
                }
            }
            else if (type == "inlineStr")
            {
                value = string.Concat(cell.Descendants(spreadsheet + "t").Select(text => text.Value));
            }
            else
            {
                value = rawValue;
            }

            values[columnIndex] = value;
        }

        int columnCount = values.Count == 0 ? 0 : values.Keys.Max() + 1;
        return Enumerable.Range(0, columnCount)
            .Select(index => values.TryGetValue(index, out string? value) ? value : string.Empty)
            .ToList();
    }

    private static int? GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return null;
        }

        int index = 0;
        int letters = 0;
        foreach (char character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
            letters++;
        }

        return letters == 0 ? null : index - 1;
    }

    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxExpandedBytes
        };
        using XmlReader reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static ZipArchiveEntry GetRequiredEntry(ZipArchive archive, string path)
    {
        return FindEntry(archive, path) ??
            throw new ImportFileException($"The workbook is missing the required '{path}' part.");
    }

    private static ZipArchiveEntry? FindEntry(ZipArchive archive, string path)
    {
        return archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.FullName, path, StringComparison.OrdinalIgnoreCase));
    }
}
