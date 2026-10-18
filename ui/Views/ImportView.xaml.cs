using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GymManagementSystem.Importing;
using Microsoft.Win32;

namespace GymManagementSystem.UI.Views;

public partial class ImportView : UserControl
{
    private readonly DataImportService importService;
    private readonly Action<ImportDataType, int> importCompleted;
    private readonly Action<string, bool> showStatus;
    private ImportPreview? preview;

    public ImportView(
        DataImportService importService,
        Action<ImportDataType, int> importCompleted,
        Action<string, bool> showStatus)
    {
        InitializeComponent();
        this.importService = importService;
        this.importCompleted = importCompleted;
        this.showStatus = showStatus;
        DataTypeComboBox.SelectedIndex = 0;
        ConfigureColumns(ImportDataType.Members);
    }

    private ImportDataType SelectedDataType =>
        (DataTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "SubscriptionPlans"
            ? ImportDataType.SubscriptionPlans
            : ImportDataType.Members;

    private async void ChooseFileButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = $"Choose {GetDataTypeLabel(SelectedDataType)} import file",
            Filter = "Supported import files (*.xlsx;*.txt)|*.xlsx;*.txt|Excel workbooks (*.xlsx)|*.xlsx|Tab-separated text (*.txt)|*.txt",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        SetBusy(true);
        try
        {
            ImportPreview result = await importService.CreatePreviewAsync(dialog.FileName, SelectedDataType);
            preview = result;
            FileNameText.Text = result.FileName;
            FileNameText.Foreground = (System.Windows.Media.Brush)FindResource("InkBrush");
            FileDetailText.Text = Path.GetExtension(dialog.FileName).ToUpperInvariant().TrimStart('.') + " · Preview ready";
            ConfigureColumns(result.DataType);
            RefreshPreview();
            showStatus($"Validated {result.TotalCount} records from {result.FileName}", false);
        }
        catch (Exception ex) when (ex is ImportFileException or IOException or UnauthorizedAccessException)
        {
            ResetPreview();
            showStatus(ex.Message, true);
            MessageBox.Show(ex.Message, "Unable to read import file", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (preview == null || preview.ValidCount == 0)
        {
            return;
        }

        MessageBoxResult confirmation = MessageBox.Show(
            $"Import {preview.ValidCount} valid {GetDataTypeLabel(preview.DataType)} record(s)?\n\n" +
            $"{preview.InvalidCount + preview.DuplicateCount} invalid or duplicate record(s) will be skipped.",
            "Confirm import",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        ImportSummary summary = importService.ImportValidRows(preview);
        RefreshPreview();

        string message =
            $"Imported: {summary.ImportedCount}\n" +
            $"Skipped or invalid: {summary.SkippedCount}\n" +
            $"Failed during import: {summary.FailedCount}";

        MessageBox.Show(
            message,
            summary.FailedCount == 0 ? "Import complete" : "Import completed with errors",
            MessageBoxButton.OK,
            summary.FailedCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

        showStatus(
            $"Imported {summary.ImportedCount} {GetDataTypeLabel(preview.DataType)} record(s)",
            summary.FailedCount > 0);

        if (summary.ImportedCount > 0)
        {
            importCompleted(preview.DataType, summary.ImportedCount);
        }
    }

    private void DataTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded && PreviewGrid == null)
        {
            return;
        }

        ResetPreview();
        ConfigureColumns(SelectedDataType);
    }

    private void ConfigureColumns(ImportDataType dataType)
    {
        if (PreviewGrid == null)
        {
            return;
        }

        PreviewGrid.Columns.Clear();
        AddColumn("ROW", "RowNumber", 65);
        AddColumn("NAME", "Name", new DataGridLength(1.2, DataGridLengthUnitType.Star));

        if (dataType == ImportDataType.Members)
        {
            AddColumn("AGE", "Age", 70);
            AddColumn("WEIGHT", "Weight", 90);
            AddColumn("PHONE", "PhoneNumber", 125);
        }
        else
        {
            AddColumn("PRICE", "Price", 90);
            AddColumn("DESCRIPTION", "Description", new DataGridLength(1.5, DataGridLengthUnitType.Star));
        }

        AddColumn("STATUS", "Status", 95);
        AddColumn("VALIDATION RESULT", "Reason", new DataGridLength(1.6, DataGridLengthUnitType.Star));
    }

    private void AddColumn(string header, string property, double width)
    {
        AddColumn(header, property, new DataGridLength(width));
    }

    private void AddColumn(string header, string property, DataGridLength width)
    {
        PreviewGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(property),
            Width = width
        });
    }

    private void RefreshPreview()
    {
        if (preview == null)
        {
            ResetCounts();
            return;
        }

        PreviewGrid.ItemsSource = null;
        PreviewGrid.ItemsSource = preview.Rows;
        EmptyState.Visibility = preview.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TotalCountText.Text = preview.TotalCount.ToString();
        ValidCountText.Text = preview.ValidCount.ToString();
        InvalidCountText.Text = preview.InvalidCount.ToString();
        DuplicateCountText.Text = preview.DuplicateCount.ToString();
        ImportedCountText.Text = preview.ImportedCount.ToString();
        SkippedCountText.Text = preview.SkippedCount.ToString();
        ImportButton.IsEnabled = preview.ValidCount > 0;
    }

    private void ResetPreview()
    {
        preview = null;
        if (PreviewGrid != null)
        {
            PreviewGrid.ItemsSource = null;
        }
        if (EmptyState != null)
        {
            EmptyState.Visibility = Visibility.Visible;
        }
        if (FileNameText != null)
        {
            FileNameText.Text = "No file selected";
            FileDetailText.Text = string.Empty;
        }
        if (ImportButton != null)
        {
            ImportButton.IsEnabled = false;
        }
        ResetCounts();
    }

    private void ResetCounts()
    {
        if (TotalCountText == null)
        {
            return;
        }

        TotalCountText.Text = "0";
        ValidCountText.Text = "0";
        InvalidCountText.Text = "0";
        DuplicateCountText.Text = "0";
        ImportedCountText.Text = "0";
        SkippedCountText.Text = "0";
    }

    private void SetBusy(bool isBusy)
    {
        BusyOverlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        ChooseFileButton.IsEnabled = !isBusy;
        DataTypeComboBox.IsEnabled = !isBusy;
    }

    private static string GetDataTypeLabel(ImportDataType dataType)
    {
        return dataType == ImportDataType.Members ? "member" : "subscription plan";
    }
}
