using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.Core;
using Microsoft.Win32;

namespace ManufacturingERP.ViewModels;

public partial class DocumentViewModel : ViewModelBase
{
    private readonly IDocumentManagementService _documentService;
    private readonly IAuthService _auth;
    private readonly INotificationService _notification;
    private readonly DocumentSeeder _documentSeeder;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSeeding;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private Document? _selectedDocument;

    public ObservableCollection<Document> Documents { get; } = new();

    public DocumentViewModel(
        IDocumentManagementService documentService,
        IAuthService auth,
        INotificationService notification,
        DocumentSeeder documentSeeder)
    {
        _documentService = documentService;
        _auth = auth;
        _notification = notification;
        _documentSeeder = documentSeeder;
    }

    public override async Task InitializeAsync()
    {
        await LoadDocumentsAsync();
    }

    private async Task LoadDocumentsAsync()
    {
        IsLoading = true;
        try
        {
            var docs = await _documentService.GetDocumentsAsync();
            Documents.Clear();
            foreach (var d in docs)
                Documents.Add(d);
        }
        catch (Exception ex)
        {
            _notification.ShowError($"Lỗi tải danh sách tài liệu: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UploadDocument()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tài liệu để upload",
            Filter = "Hỗ trợ (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt|Tất cả (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        if (_auth.CurrentUser == null)
        {
            _notification.ShowError("Bạn cần đăng nhập để upload tài liệu.");
            return;
        }

        IsLoading = true;
        try
        {
            var fileSize = new FileInfo(dialog.FileName).Length;
            if (fileSize > 50 * 1024 * 1024)
            {
                _notification.ShowError("File quá lớn. Tối đa 50MB.");
                return;
            }

            var fileName = Path.GetFileName(dialog.FileName);
            await _documentService.UploadDocumentAsync(
                dialog.FileName, fileName,
                description: null, category: null,
                uploadedByUserId: _auth.CurrentUser.UserId);

            _notification.ShowSuccess($"Đã upload tài liệu '{fileName}' thành công.");
            await LoadDocumentsAsync();
        }
        catch (Exception ex)
        {
            _notification.ShowError($"Lỗi upload: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDocument()
    {
        if (SelectedDocument == null)
        {
            _notification.ShowWarning("Vui lòng chọn tài liệu cần xóa.");
            return;
        }

        var result = MessageBox.Show(
            $"Xóa tài liệu '{SelectedDocument.OriginalFileName}'?", "Xác nhận",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            await _documentService.DeleteDocumentAsync(SelectedDocument.DocumentId);
            _notification.ShowSuccess("Đã xóa tài liệu.");
            await LoadDocumentsAsync();
        }
        catch (Exception ex)
        {
            _notification.ShowError($"Lỗi xóa: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ReindexDocument()
    {
        if (SelectedDocument == null) return;

        IsLoading = true;
        try
        {
            await _documentService.ReindexDocumentAsync(SelectedDocument.DocumentId);
            _notification.ShowSuccess("Đã đánh chỉ mục lại tài liệu.");
        }
        catch (Exception ex)
        {
            _notification.ShowError($"Lỗi: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SeedSampleDocuments()
    {
        IsSeeding = true;
        try
        {
            if (_auth.CurrentUser == null)
            {
                _notification.ShowError("Bạn cần đăng nhập.");
                return;
            }
            await _documentSeeder.SeedAsync(_auth.CurrentUser.UserId);
            await LoadDocumentsAsync();
        }
        catch (Exception ex)
        {
            _notification.ShowError($"Lỗi tạo dữ liệu mẫu: {ex.Message}");
        }
        finally
        {
            IsSeeding = false;
        }
    }
}
