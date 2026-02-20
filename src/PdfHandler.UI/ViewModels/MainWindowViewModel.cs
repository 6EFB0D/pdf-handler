// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.UI.Views;

namespace PdfHandler.UI.ViewModels;

/// <summary>
/// メインウィンドウのViewModel
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IPdfService _pdfService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly IPdfSplitService _pdfSplitService;
    private readonly IFavoriteService _favoriteService;
    private readonly ILicenseService _licenseService;
    private readonly IPdfRotateService _pdfRotateService;
    private readonly IWorkFolderService _workFolderService;

    [ObservableProperty]
    private FolderNode? _rootFolder;

    [ObservableProperty]
    private FolderNode? _selectedFolder;

    [ObservableProperty]
    private ObservableCollection<PdfFileInfo> _pdfFiles = new();

    [ObservableProperty]
    private PdfFileInfo? _selectedPdfFile;

    [ObservableProperty]
    private bool _isPreviewVisible = true;

    [ObservableProperty]
    private bool _isThumbnailView = true;

    [ObservableProperty]
    private string _statusText = "準備完了";

    [ObservableProperty]
    private byte[]? _previewImageData;

    [ObservableProperty]
    private int _currentPageNumber = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _zoomPercent = 100;

    // サムネイルサイズ
    [ObservableProperty]
    private int _thumbnailWidth = 120;

    [ObservableProperty]
    private int _thumbnailHeight = 150;

    [ObservableProperty]
    private bool _isSmallThumbnail = false;

    [ObservableProperty]
    private bool _isMediumThumbnail = true;

    [ObservableProperty]
    private bool _isLargeThumbnail = false;

    [ObservableProperty]
    private bool _isExtraLargeThumbnail = false;

    [ObservableProperty]
    private ObservableCollection<FavoriteFolder> _favorites = new();

    [ObservableProperty]
    private string _trialStatusText = string.Empty;

    [ObservableProperty]
    private bool _isTrialExpiringSoon = false;

    // コピー・ペースト用の一時保存
    private List<string> _copiedFilePaths = new();

    public MainWindowViewModel(
        IFileService fileService,
        IPdfService pdfService,
        IPdfMergeService pdfMergeService,
        IPdfSplitService pdfSplitService,
        IFavoriteService favoriteService,
        ILicenseService licenseService,
        IPdfRotateService pdfRotateService,
        IWorkFolderService workFolderService)
    {
        _fileService = fileService;
        _pdfService = pdfService;
        _pdfMergeService = pdfMergeService;
        _pdfSplitService = pdfSplitService;
        _favoriteService = favoriteService;
        _licenseService = licenseService;
        _pdfRotateService = pdfRotateService;
        _workFolderService = workFolderService;

        // 初期化処理を順次実行
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // 1. お気に入りを読み込み（フォルダツリー初期化時に使用）
            await LoadFavoritesAsync();
            
            // 2. ライセンス情報を読み込み
            await InitializeLicenseAsync();
            
            // 3. ワークフォルダを初期化
            await InitializeWorkFolderAsync();
            
            // 4. ドライブ一覧を読み込み
            await InitializeDrivesAsync();
            
            // RootFolderが設定されているか確認
            if (RootFolder == null)
            {
                // フォールバック: 最小限のルートノードを作成
                RootFolder = new FolderNode
                {
                    Path = "",
                    Name = "コンピューター",
                    IsExpanded = true
                };
            }
            
            // 初期化完了をステータスバーに表示
            StatusText = $"初期化完了: {RootFolder.Children.Count}個のフォルダ";
        }
        catch (Exception ex)
        {
            // エラーが発生しても最小限のルートノードは作成
            RootFolder = new FolderNode
            {
                Path = "",
                Name = "コンピューター",
                IsExpanded = true
            };
            
            StatusText = $"初期化エラー: {ex.Message}";
            MessageBox.Show($"初期化中にエラーが発生しました: {ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task InitializeLicenseAsync()
    {
        // ライセンス情報を読み込み
        await _licenseService.LoadLicenseAsync();
        
        // ライセンス検証チェック（30日ごと）
        await CheckLicenseVerificationAsync();
        
        // 試用期間ステータスを更新
        UpdateTrialStatus();
    }

    /// <summary>
    /// ライセンス検証チェック（30日ごと）
    /// </summary>
    private async Task CheckLicenseVerificationAsync()
    {
        try
        {
            var licenseInfo = _licenseService.GetLicenseInfo();
            
            // 試用期間中または買い切り版の場合は検証不要
            if (licenseInfo.Plan == LicensePlan.Trial || licenseInfo.Plan == LicensePlan.StandardPurchased)
            {
                return;
            }
            
            // 検証が必要かチェック
            if (licenseInfo.IsVerificationRequired())
            {
                // 検証を実行
                var verificationResult = await _licenseService.VerifyLicenseAsync();
                
                if (!verificationResult)
                {
                    // 検証に失敗した場合、通知を表示
                    var result = MessageBox.Show(
                        "ライセンスの検証に失敗しました。\n\n" +
                        "インターネット接続を確認してください。\n" +
                        "オフラインの場合は、次回オンライン時に自動的に検証されます。\n\n" +
                        "続けて使用しますか？",
                        "ライセンス検証エラー",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result == MessageBoxResult.No)
                    {
                        // アプリケーションを終了
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    // 検証成功時、最終検証日時を更新
                    licenseInfo.LastVerificationDate = DateTime.Now;
                    licenseInfo.NextVerificationDate = DateTime.Now.AddDays(30);
                    await _licenseService.SaveLicenseAsync(licenseInfo);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ライセンス検証チェックエラー: {ex.Message}");
            // エラーが発生してもアプリケーションは続行
        }
    }

    private async Task InitializeWorkFolderAsync()
    {
        // ワークフォルダが存在しない場合は作成
        if (!_workFolderService.WorkFolderExists())
        {
            await _workFolderService.CreateWorkFolderAsync();
        }
    }

    /// <summary>
    /// 子フォルダを読み込まずにフォルダノードを作成（遅延読み込み用）
    /// </summary>
    private FolderNode CreateFolderNodeWithoutChildren(string path, string name)
    {
        return new FolderNode
        {
            Path = path,
            Name = name,
            IsExpanded = false,
            IsChildrenLoaded = false
        };
    }

    /// <summary>
    /// フォルダノードの子フォルダを遅延読み込み（最初の1階層のみ）
    /// </summary>
    public async Task LoadChildrenAsync(FolderNode node)
    {
        if (node.IsChildrenLoaded || string.IsNullOrEmpty(node.Path))
            return;

        try
        {
            if (!Directory.Exists(node.Path))
                return;

            var dirInfo = new DirectoryInfo(node.Path);
            var subDirs = dirInfo.GetDirectories();
            
            node.Children.Clear();
            foreach (var subDir in subDirs)
            {
                try
                {
                    // 隠しフォルダやシステムフォルダを除外
                    if ((subDir.Attributes & FileAttributes.Hidden) == 0 &&
                        (subDir.Attributes & FileAttributes.System) == 0)
                    {
                        var childNode = CreateFolderNodeWithoutChildren(subDir.FullName, subDir.Name);
                        childNode.Parent = node;
                        node.Children.Add(childNode);
                    }
                }
                catch
                {
                    // アクセス権限エラーなどは無視
                }
            }
            node.IsChildrenLoaded = true;
        }
        catch
        {
            // エラーは無視
        }
    }

    /// <summary>
    /// お気に入り一覧を取得（UIから使用）
    /// </summary>
    public async Task<List<FavoriteFolder>> GetFavoritesAsync()
    {
        return await _favoriteService.GetFavoritesAsync();
    }

    private async Task InitializeDrivesAsync()
    {
        try
        {
            // 仮想ルートノードを作成
            var rootNode = new FolderNode
            {
                Path = "",
                Name = "コンピューター",
                IsExpanded = true
            };

            // 1. ワークフォルダを追加（遅延読み込み）
            try
            {
                var workFolderPath = _workFolderService.GetWorkFolderPath();
                if (Directory.Exists(workFolderPath))
                {
                    var workFolderNode = CreateFolderNodeWithoutChildren(workFolderPath, "📁 Work");
                    rootNode.Children.Add(workFolderNode);
                }
            }
            catch
            {
                // エラーは無視
            }

            // 2. ドキュメントフォルダを追加（遅延読み込み）
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (Directory.Exists(documentsPath))
                {
                    var documentsNode = CreateFolderNodeWithoutChildren(documentsPath, "📄 ドキュメント");
                    rootNode.Children.Add(documentsNode);
                }
            }
            catch
            {
                // エラーは無視
            }

            // 3. ダウンロードフォルダを追加（遅延読み込み）
            try
            {
                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    var downloadsNode = CreateFolderNodeWithoutChildren(downloadsPath, "⬇️ ダウンロード");
                    rootNode.Children.Add(downloadsNode);
                }
            }
            catch
            {
                // エラーは無視
            }

            // 4. お気に入りフォルダを追加
            try
            {
                var favorites = await _favoriteService.GetFavoritesAsync();
                if (favorites.Count > 0)
                {
                    var favoritesNode = new FolderNode
                    {
                        Path = "",
                        Name = "⭐ お気に入り",
                        IsExpanded = false
                    };

                    foreach (var favorite in favorites.OrderBy(f => f.Name))
                    {
                        if (Directory.Exists(favorite.Path))
                        {
                            try
                            {
                                // 遅延読み込み：子フォルダは展開時に読み込む
                                var favoriteFolderNode = CreateFolderNodeWithoutChildren(favorite.Path, favorite.Name);
                                // お気に入りであることを識別するためのタグを追加
                                favoriteFolderNode.Tag = "Favorite";
                                favoritesNode.Children.Add(favoriteFolderNode);
                            }
                            catch
                            {
                                // 個別のお気に入りフォルダの読み込みエラーは無視
                            }
                        }
                    }

                    if (favoritesNode.Children.Count > 0)
                    {
                        rootNode.Children.Add(favoritesNode);
                    }
                }
            }
            catch
            {
                // エラーは無視
            }

            // 5. ドライブ一覧を取得して追加（固定ドライブとネットワークドライブ）
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Network))
                    .Select(d => new { Path = d.RootDirectory.FullName, DriveInfo = d })
                    .ToList();

                foreach (var drive in drives)
                {
                    try
                    {
                        // 遅延読み込み：ドライブの子フォルダは展開時に読み込む
                        var volumeLabel = !string.IsNullOrEmpty(drive.DriveInfo.VolumeLabel) 
                            ? $" ({drive.DriveInfo.VolumeLabel})" 
                            : "";
                        var driveTypeLabel = drive.DriveInfo.DriveType == DriveType.Network ? " [ネットワーク]" : "";
                        var driveNode = CreateFolderNodeWithoutChildren(drive.Path, $"{drive.DriveInfo.Name.TrimEnd('\\')}{volumeLabel}{driveTypeLabel}");
                        rootNode.Children.Add(driveNode);
                    }
                    catch
                    {
                        // アクセスできないドライブは無視
                    }
                }
            }
            catch
            {
                // エラーは無視
            }

            // ルートノードを設定（エラーが発生しても空のルートノードは設定する）
            RootFolder = rootNode;
        }
        catch
        {
            // エラーが発生しても空のルートノードを設定
            RootFolder = new FolderNode
            {
                Path = "",
                Name = "コンピューター",
                IsExpanded = true
            };
        }
    }

    public void UpdateTrialStatus()
    {
        if (_licenseService.IsTrialValid())
        {
            var remainingDays = _licenseService.GetRemainingTrialDays();
            TrialStatusText = $"試用期間: 残り{remainingDays}日";
            IsTrialExpiringSoon = remainingDays <= 3;
            
            // 試用期間終了3日前と1日前に通知
            if (remainingDays == 3)
            {
                MessageBox.Show(
                    "試用期間が残り3日となりました。\n\nライセンスを購入すると、引き続き全機能をご利用いただけます。",
                    "試用期間のお知らせ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else if (remainingDays == 1)
            {
                MessageBox.Show(
                    "試用期間が残り1日となりました。\n\nライセンスを購入すると、引き続き全機能をご利用いただけます。\n\n今すぐ購入しますか？",
                    "試用期間のお知らせ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            }
        }
        else if (!_licenseService.IsLicenseValid())
        {
            TrialStatusText = "試用期間が終了しました";
            IsTrialExpiringSoon = true;
            
            // 試用期間終了時に購入ダイアログを表示
            ShowPurchaseDialog();
        }
        else
        {
            var licenseInfo = _licenseService.GetLicenseInfo();
            TrialStatusText = licenseInfo.Plan switch
            {
                LicensePlan.StandardPurchased => "Standard版（買い切り）",
                LicensePlan.StandardSubscription => "Standard版（サブスクリプション）",
                LicensePlan.Premium => "Premium版",
                LicensePlan.PremiumBYOK => "Premium版（BYOK）",
                _ => "ライセンス情報不明"
            };
            IsTrialExpiringSoon = false;
        }
    }

    private void ShowPurchaseDialog()
    {
        var licenseDialog = new LicenseDialog();
        licenseDialog.Owner = Application.Current.MainWindow;
        licenseDialog.ShowDialog();
        
        // ダイアログを閉じた後にライセンス状態を更新
        UpdateTrialStatus();
    }

    partial void OnSelectedFolderChanged(FolderNode? value)
    {
        if (value != null && !string.IsNullOrEmpty(value.Path))
        {
            try
            {
                _ = LoadPdfFilesAsync(value.Path);
            }
            catch (Exception ex)
            {
                StatusText = $"フォルダ読み込みエラー: {ex.Message}";
                MessageBox.Show($"フォルダの読み込みに失敗しました: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else if (value != null && string.IsNullOrEmpty(value.Path))
        {
            // ルートノードやお気に入りノードなど、パスが空のノードが選択された場合は何もしない
            PdfFiles.Clear();
            StatusText = "フォルダを選択してください";
        }
    }

    partial void OnSelectedPdfFileChanged(PdfFileInfo? value)
    {
        if (value != null)
        {
            _ = LoadPreviewAsync(value.FilePath);
        }
        else
        {
            PreviewImageData = null;
            CurrentPageNumber = 1;
            TotalPages = 1;
        }
    }

    // お気に入り管理
    private async Task LoadFavoritesAsync()
    {
        var favorites = await _favoriteService.GetFavoritesAsync();
        Favorites.Clear();
        foreach (var fav in favorites.OrderBy(f => f.Name))
        {
            Favorites.Add(fav);
        }
    }

    [RelayCommand]
    public async Task AddFavoriteAsync(object? parameter)
    {
        FolderNode? folder = null;
        if (parameter is FolderNode node)
        {
            folder = node;
        }
        else if (SelectedFolder != null)
        {
            folder = SelectedFolder;
        }

        if (folder == null || string.IsNullOrEmpty(folder.Path))
        {
            MessageBox.Show("フォルダを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 既にお気に入りに登録されているか確認
        var favorites = await _favoriteService.GetFavoritesAsync();
        if (favorites.Any(f => f.Path.Equals(folder.Path, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("このフォルダは既にお気に入りに追加されています。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new AddFavoriteDialog(folder.Path);
        if (dialog.ShowDialog() == true)
        {
            var success = await _favoriteService.AddFavoriteAsync(dialog.FavoriteName, dialog.FavoritePath);
            if (success)
            {
                await LoadFavoritesAsync();
                // フォルダツリーを更新してお気に入りを反映
                await RefreshFolderTreeAsync();
                StatusText = "お気に入りに追加しました";
            }
            else
            {
                MessageBox.Show("お気に入りの追加に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    public async Task RemoveFavoriteFromFolderAsync(object? parameter)
    {
        if (parameter is not FolderNode folder || string.IsNullOrEmpty(folder.Path))
        {
            MessageBox.Show("フォルダを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var favorites = await _favoriteService.GetFavoritesAsync();
        var favorite = favorites.FirstOrDefault(f => f.Path.Equals(folder.Path, StringComparison.OrdinalIgnoreCase));
        
        if (favorite == null)
        {
            MessageBox.Show("このフォルダはお気に入りに登録されていません。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"お気に入り '{favorite.Name}' を削除しますか？",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _favoriteService.RemoveFavoriteAsync(folder.Path);
            await LoadFavoritesAsync();
            await RefreshFolderTreeAsync();
            StatusText = "お気に入りを削除しました";
        }
    }

    [RelayCommand]
    public async Task RenameFavoriteAsync(object? parameter)
    {
        if (parameter is not FolderNode folder || string.IsNullOrEmpty(folder.Path))
        {
            MessageBox.Show("フォルダを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var favorites = await _favoriteService.GetFavoritesAsync();
        var favorite = favorites.FirstOrDefault(f => f.Path.Equals(folder.Path, StringComparison.OrdinalIgnoreCase));
        
        if (favorite == null)
        {
            MessageBox.Show("このフォルダはお気に入りに登録されていません。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 名前変更ダイアログを表示
        var dialog = new AddFavoriteDialog(folder.Path);
        dialog.Title = "お気に入りの名前を変更";
        // 既存の名前を設定
        dialog.NameTextBoxControl.Text = favorite.Name;
        dialog.NameTextBoxControl.SelectAll();

        if (dialog.ShowDialog() == true)
        {
            var success = await _favoriteService.RenameFavoriteAsync(folder.Path, dialog.FavoriteName);
            if (success)
            {
                await LoadFavoritesAsync();
                await RefreshFolderTreeAsync();
                StatusText = "お気に入りの名前を変更しました";
            }
            else
            {
                MessageBox.Show("お気に入りの名前変更に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(FavoriteFolder favorite)
    {
        if (favorite == null) return;

        var result = MessageBox.Show(
            $"お気に入り '{favorite.Name}' を削除しますか？",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _favoriteService.RemoveFavoriteAsync(favorite.Path);
            await LoadFavoritesAsync();
            // フォルダツリーを更新してお気に入りを反映
            await RefreshFolderTreeAsync();
            StatusText = "お気に入りを削除しました";
        }
    }

    [RelayCommand]
    private async Task OpenFavoriteAsync(FavoriteFolder favorite)
    {
        if (favorite == null || !Directory.Exists(favorite.Path))
        {
            MessageBox.Show("フォルダが見つかりません。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await LoadFolderAsync(favorite.Path);
    }

    // お気に入り追加後にフォルダツリーを更新（お気に入り部分のみ）
    private async Task RefreshFolderTreeAsync()
    {
        if (RootFolder == null)
        {
            // RootFolderが存在しない場合は全体を初期化
            await InitializeDrivesAsync();
            return;
        }

        // お気に入りノードを探す
        FolderNode? favoritesNode = null;
        foreach (var child in RootFolder.Children)
        {
            if (child.Name == "⭐ お気に入り")
            {
                favoritesNode = child;
                break;
            }
        }

        // お気に入り一覧を取得
        var favorites = await _favoriteService.GetFavoritesAsync();

        if (favorites.Count == 0)
        {
            // お気に入りがなくなった場合は、お気に入りノードを削除
            if (favoritesNode != null)
            {
                RootFolder.Children.Remove(favoritesNode);
            }
            return;
        }

            // お気に入りノードが存在しない場合は作成
        if (favoritesNode == null)
        {
            favoritesNode = new FolderNode
            {
                Path = "",
                Name = "⭐ お気に入り",
                IsExpanded = false
            };
            // お気に入りノードを適切な位置に挿入（ドライブの前に）
            int insertIndex = RootFolder.Children.Count;
            for (int i = 0; i < RootFolder.Children.Count; i++)
            {
                var child = RootFolder.Children[i];
                // ドライブかどうかを判定（パスが存在し、ルートディレクトリの場合）
                if (!string.IsNullOrEmpty(child.Path) && 
                    ((child.Path.Length == 3 && child.Path.EndsWith("\\")) ||
                     (child.Path.Length == 2 && child.Path.EndsWith(":"))))
                {
                    insertIndex = i;
                    break;
                }
            }
            RootFolder.Children.Insert(insertIndex, favoritesNode);
        }

        // 現在のお気に入りノードの展開状態と子ノードの展開状態を保持
        var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selectedPath = SelectedFolder?.Path;
        
        if (favoritesNode.IsExpanded)
        {
            // お気に入りノードが展開されている場合、子ノードの展開状態も保持
            CollectExpandedPaths(favoritesNode, expandedPaths);
        }

        // 既存のお気に入りノードのマップを作成（パスをキーに）
        var existingFavorites = new Dictionary<string, FolderNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var existingChild in favoritesNode.Children.ToList())
        {
            if (!string.IsNullOrEmpty(existingChild.Path))
            {
                existingFavorites[existingChild.Path] = existingChild;
            }
        }

        // お気に入りノードの子ノードを更新
        favoritesNode.Children.Clear();
        foreach (var favorite in favorites.OrderBy(f => f.Name))
        {
            if (Directory.Exists(favorite.Path))
            {
                try
                {
                    FolderNode favoriteFolderNode;
                    if (existingFavorites.TryGetValue(favorite.Path, out var existingNode))
                    {
                        // 既存のノードを使用（展開状態を保持）
                        favoriteFolderNode = existingNode;
                        favoriteFolderNode.Name = favorite.Name; // 名前を更新
                    }
                    else
                    {
                        // 新しいノードを作成
                        favoriteFolderNode = CreateFolderNodeWithoutChildren(favorite.Path, favorite.Name);
                        favoriteFolderNode.Tag = "Favorite";
                    }
                    favoritesNode.Children.Add(favoriteFolderNode);
                }
                catch
                {
                    // エラーは無視
                }
            }
        }

        // 展開状態を復元
        if (favoritesNode.IsExpanded)
        {
            RestoreExpandedPaths(favoritesNode, expandedPaths);
        }

        // RootFolderプロパティを再設定してUIに変更を通知
        // これによりPropertyChangedイベントが発火し、TreeViewが更新される
        var currentRootFolder = RootFolder;
        RootFolder = null;
        RootFolder = currentRootFolder;

        // 選択状態を復元
        if (!string.IsNullOrEmpty(selectedPath))
        {
            SelectAndExpandFolder(RootFolder, selectedPath);
        }
    }

    /// <summary>
    /// 展開されているパスを収集
    /// </summary>
    private void CollectExpandedPaths(FolderNode node, HashSet<string> expandedPaths)
    {
        if (node.IsExpanded && !string.IsNullOrEmpty(node.Path))
        {
            expandedPaths.Add(node.Path);
        }
        foreach (var child in node.Children)
        {
            CollectExpandedPaths(child, expandedPaths);
        }
    }

    /// <summary>
    /// 展開状態を復元
    /// </summary>
    private void RestoreExpandedPaths(FolderNode node, HashSet<string> expandedPaths)
    {
        if (!string.IsNullOrEmpty(node.Path) && expandedPaths.Contains(node.Path))
        {
            node.IsExpanded = true;
        }
        foreach (var child in node.Children)
        {
            RestoreExpandedPaths(child, expandedPaths);
        }
    }

    // フォルダ操作
    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "PDFファイルが含まれるフォルダを選択してください",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadFolderAsync(dialog.FolderName);
        }
    }

    private async Task LoadFolderAsync(string folderPath)
    {
        StatusText = "フォルダを読み込み中...";

        // ドライブツリーが既に存在する場合は、その中でフォルダを選択
        if (RootFolder != null)
        {
            // 選択されたフォルダを自動的に展開＆選択
            SelectAndExpandFolder(RootFolder, folderPath);
            
            // PDFファイルを読み込み
            await LoadPdfFilesAsync(folderPath);
            
            StatusText = $"フォルダを開きました: {folderPath}";
        }
        else
        {
            // ドライブツリーが存在しない場合は初期化
            await InitializeDrivesAsync();
            
            // 選択されたフォルダを自動的に展開＆選択
            if (RootFolder != null)
            {
                SelectAndExpandFolder(RootFolder, folderPath);
                await LoadPdfFilesAsync(folderPath);
                StatusText = $"フォルダを開きました: {folderPath}";
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedFolder != null)
        {
            // 選択されているファイルのパスを保存
            string? selectedFilePath = SelectedPdfFile?.FilePath;
            
            await LoadPdfFilesAsync(SelectedFolder.Path);
            
            // 選択状態を復元
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                var fileToSelect = PdfFiles.FirstOrDefault(f => f.FilePath == selectedFilePath);
                if (fileToSelect != null)
                {
                    SelectedPdfFile = fileToSelect;
                }
            }
            
            StatusText = "更新しました";
        }
    }

    private async Task LoadPdfFilesAsync(string folderPath)
    {
        try
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                PdfFiles.Clear();
                StatusText = "フォルダが見つかりません";
                return;
            }

            StatusText = "PDFファイルを読み込み中...";

            var files = await _fileService.GetPdfFilesAsync(folderPath);
            
            PdfFiles.Clear();
            foreach (var file in files)
            {
                try
                {
                    // ページ数を取得
                    file.PageCount = await _pdfService.GetPageCountAsync(file.FilePath);
                    
                    // サムネイルを生成（1ページ目）
                    file.DisplayPageNumber = 1;
                    file.ThumbnailData = await _pdfService.GenerateThumbnailAsync(file.FilePath, 1);
                    
                    PdfFiles.Add(file);
                }
                catch
                {
                    // 個別のファイルの読み込みエラーは無視して続行
                    file.DisplayPageNumber = 1;
                    PdfFiles.Add(file);
                }
            }

            StatusText = $"{PdfFiles.Count}個のPDFファイル";
        }
        catch (Exception ex)
        {
            PdfFiles.Clear();
            StatusText = $"PDFファイルの読み込みに失敗しました: {ex.Message}";
            MessageBox.Show($"PDFファイルの読み込みに失敗しました: {ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // プレビュー機能
    private async Task LoadPreviewAsync(string filePath)
    {
        StatusText = "プレビューを読み込み中...";

        TotalPages = await _pdfService.GetPageCountAsync(filePath);
        CurrentPageNumber = 1;
        ZoomPercent = 100;
        
        await LoadPageAsync(filePath, CurrentPageNumber);
        
        StatusText = $"プレビュー表示中: {Path.GetFileName(filePath)}";
    }

    private async Task LoadPageAsync(string filePath, int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > TotalPages) return;

        // DPIをズームパーセントから計算（96 DPI = 100%）
        int dpi = (int)(96 * ZoomPercent / 100.0);
        var imageData = await _pdfService.RenderPageAsync(filePath, pageNumber, dpi);
        PreviewImageData = imageData;
        CurrentPageNumber = pageNumber;
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (SelectedPdfFile != null && CurrentPageNumber > 1)
        {
            await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber - 1);
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (SelectedPdfFile != null && CurrentPageNumber < TotalPages)
        {
            await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber + 1);
        }
    }

    [RelayCommand]
    private async Task ZoomInAsync()
    {
        if (ZoomPercent < 200)
        {
            ZoomPercent += 25;
            if (SelectedPdfFile != null)
            {
                await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber);
            }
        }
    }

    [RelayCommand]
    private async Task ZoomOutAsync()
    {
        if (ZoomPercent > 50)
        {
            ZoomPercent -= 25;
            if (SelectedPdfFile != null)
            {
                await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber);
            }
        }
    }

    // サムネイルサイズ変更
    [RelayCommand]
    private void SetThumbnailSize(string size)
    {
        IsSmallThumbnail = false;
        IsMediumThumbnail = false;
        IsLargeThumbnail = false;
        IsExtraLargeThumbnail = false;

        switch (size)
        {
            case "Small":
                ThumbnailWidth = 80;
                ThumbnailHeight = 100;
                IsSmallThumbnail = true;
                break;
            case "Medium":
                ThumbnailWidth = 120;
                ThumbnailHeight = 150;
                IsMediumThumbnail = true;
                break;
            case "Large":
                ThumbnailWidth = 180;
                ThumbnailHeight = 225;
                IsLargeThumbnail = true;
                break;
            case "ExtraLarge":
                ThumbnailWidth = 240;
                ThumbnailHeight = 300;
                IsExtraLargeThumbnail = true;
                break;
        }
    }

    // 表示切替
    [RelayCommand]
    private void TogglePreview()
    {
        IsPreviewVisible = !IsPreviewVisible;
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsThumbnailView = !IsThumbnailView;
    }

    // ファイル操作
    [RelayCommand]
    private async Task DeleteFileAsync()
    {
        if (SelectedPdfFile == null) return;

        var result = MessageBox.Show(
            $"ファイル '{Path.GetFileName(SelectedPdfFile.FilePath)}' を削除しますか？\n\nこの操作は元に戻せません。",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var success = await _fileService.DeleteFileAsync(SelectedPdfFile.FilePath);
            if (success)
            {
                StatusText = "ファイルを削除しました";
                await RefreshAsync();
            }
            else
            {
                MessageBox.Show("ファイルの削除に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "ファイルの削除に失敗";
            }
        }
    }

    // 複数ファイル削除（MainWindow.xaml.csから呼ばれる）
    public async Task DeleteFilesAsync(List<PdfFileInfo> files)
    {
        int successCount = 0;
        int failCount = 0;

        StatusText = $"{files.Count}個のファイルを削除中...";

        foreach (var file in files)
        {
            var success = await _fileService.DeleteFileAsync(file.FilePath);
            if (success)
                successCount++;
            else
                failCount++;
        }

        if (failCount == 0)
        {
            StatusText = $"{successCount}個のファイルを削除しました";
        }
        else
        {
            StatusText = $"{successCount}個削除、{failCount}個失敗";
            MessageBox.Show($"{successCount}個のファイルを削除しました。\n{failCount}個のファイルの削除に失敗しました。",
                "削除結果", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        await RefreshAsync();
    }

    // PDF操作
    [RelayCommand]
    private void MergePdfs()
    {
        // ViewからSelectedItemsを取得する必要があるため、
        // MainWindow.xaml.csでハンドリング
        // このメソッドは使用されない（削除予定）
    }

    // ViewModelから直接呼び出される結合処理
    public async Task MergePdfsWithFilesAsync(List<string> selectedFiles)
    {
        // ライセンスチェック
        if (!_licenseService.CanUseMerge())
        {
            MessageBox.Show(
                "PDF結合機能は有償版の機能です。\n\n1か月の試用期間中は全機能をご利用いただけます。\n試用期間が終了した場合は、ライセンスの購入が必要です。",
                "機能制限",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (selectedFiles.Count < 2)
        {
            MessageBox.Show("結合するには2つ以上のPDFファイルを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // デバッグ: 選択されたファイルの順序を出力
        System.Diagnostics.Debug.WriteLine("=== PDF結合: 選択されたファイルの順序 ===");
        for (int i = 0; i < selectedFiles.Count; i++)
        {
            System.Diagnostics.Debug.WriteLine($"  [{i + 1}] {Path.GetFileName(selectedFiles[i])}");
        }
        System.Diagnostics.Debug.WriteLine("=====================================");

        var dialog = new MergePdfDialog(selectedFiles);
        if (dialog.ShowDialog() == true)
        {
            StatusText = "PDFを結合中...";

            var progress = new Progress<int>(percent =>
            {
                StatusText = $"結合中... {percent}%";
            });

            var success = await _pdfMergeService.MergePdfsAsync(dialog.FilePaths, dialog.OutputPath, progress);
            
            if (success)
            {
                MessageBox.Show($"PDFファイルを結合しました。\n保存先: {dialog.OutputPath}", "完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText = "PDF結合完了";
                
                // 結合先フォルダを開いている場合は更新
                string outputDir = Path.GetDirectoryName(dialog.OutputPath) ?? "";
                if (SelectedFolder != null && SelectedFolder.Path == outputDir)
                {
                    await RefreshAsync();
                }
            }
            else
            {
                MessageBox.Show("PDFの結合に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "PDF結合失敗";
            }
        }
    }

    [RelayCommand]
    private async Task SplitPdfAsync()
    {
        // ライセンスチェック
        if (!_licenseService.CanUseSplit())
        {
            MessageBox.Show(
                "PDF分割機能は有償版の機能です。\n\n1か月の試用期間中は全機能をご利用いただけます。\n試用期間が終了した場合は、ライセンスの購入が必要です。",
                "機能制限",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (SelectedPdfFile == null) return;

        var pageCount = await _pdfService.GetPageCountAsync(SelectedPdfFile.FilePath);
        if (pageCount == 0)
        {
            MessageBox.Show("PDFファイルを開けませんでした。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var dialog = new SplitPdfDialog(SelectedPdfFile.FilePath, pageCount);
        if (dialog.ShowDialog() == true)
        {
            StatusText = "PDFを分割中...";

            var progress = new Progress<int>(percent =>
            {
                StatusText = $"分割中... {percent}%";
            });

            bool success = false;

            switch (dialog.SelectedMode)
            {
                case SplitPdfDialog.SplitMode.Range:
                    success = await _pdfSplitService.SplitByRangesAsync(
                        SelectedPdfFile.FilePath,
                        dialog.Ranges,
                        dialog.OutputFolder,
                        dialog.FileNamePattern,
                        progress);
                    break;

                case SplitPdfDialog.SplitMode.Page:
                    success = await _pdfSplitService.SplitByPageAsync(
                        SelectedPdfFile.FilePath,
                        dialog.OutputFolder,
                        dialog.FileNamePattern,
                        progress);
                    break;

                case SplitPdfDialog.SplitMode.Equal:
                    success = await _pdfSplitService.SplitEquallyAsync(
                        SelectedPdfFile.FilePath,
                        dialog.Parts,
                        dialog.OutputFolder,
                        dialog.FileNamePattern,
                        progress);
                    break;
            }

            if (success)
            {
                MessageBox.Show($"PDFファイルを分割しました。\n保存先: {dialog.OutputFolder}", "完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText = "PDF分割完了";
                
                // 分割先フォルダを開いている場合は更新
                if (SelectedFolder != null && SelectedFolder.Path == dialog.OutputFolder)
                {
                    await RefreshAsync();
                }
            }
            else
            {
                MessageBox.Show("PDFの分割に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "PDF分割失敗";
            }
        }
    }

    /// <summary>
    /// サムネイルの指定ページを読み込む
    /// </summary>
    public async Task LoadThumbnailPageAsync(PdfFileInfo fileInfo, int pageNumber)
    {
        if (fileInfo == null || pageNumber < 1 || pageNumber > fileInfo.PageCount)
            return;

        try
        {
            fileInfo.ThumbnailData = await _pdfService.GenerateThumbnailAsync(
                fileInfo.FilePath, 
                pageNumber, 
                ThumbnailWidth, 
                ThumbnailHeight);
        }
        catch
        {
            // エラーは無視
        }
    }

    /// <summary>
    /// PDFファイルの指定ページを回転（サムネイル用）
    /// </summary>
    public async Task RotatePdfPageAsync(PdfFileInfo fileInfo, int pageNumber, int rotationDegrees)
    {
        // ライセンスチェック
        if (!_licenseService.CanUseRotate())
        {
            MessageBox.Show(
                "PDF回転機能は有償版の機能です。\n\n1か月の試用期間中は全機能をご利用いただけます。\n試用期間が終了した場合は、ライセンスの購入が必要です。",
                "機能制限",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (fileInfo == null) return;

        StatusText = "PDFを回転中...";
        var success = await _pdfRotateService.RotatePageAsync(fileInfo.FilePath, pageNumber, rotationDegrees);
        
        if (success)
        {
            StatusText = $"PDFファイルのページ{pageNumber}を{rotationDegrees}度回転しました";
            
            // サムネイルを更新
            await LoadThumbnailPageAsync(fileInfo, pageNumber);
            
            // プレビューを更新（現在のページを再表示）
            if (SelectedPdfFile == fileInfo && IsPreviewVisible && CurrentPageNumber == pageNumber)
            {
                await LoadPageAsync(fileInfo.FilePath, pageNumber);
            }
            
            // ファイル一覧を更新
            await RefreshAsync();
        }
        else
        {
            StatusText = "PDFの回転に失敗しました";
            MessageBox.Show("PDFの回転に失敗しました。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task RotatePdfAsync(object? parameter)
    {
        // ライセンスチェック
        if (!_licenseService.CanUseRotate())
        {
            MessageBox.Show(
                "PDF回転機能は有償版の機能です。\n\n1か月の試用期間中は全機能をご利用いただけます。\n試用期間が終了した場合は、ライセンスの購入が必要です。",
                "機能制限",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (SelectedPdfFile == null) return;

        // パラメータをintに変換
        int rotationDegrees = 90; // デフォルト値
        if (parameter != null)
        {
            if (parameter is int intValue)
            {
                rotationDegrees = intValue;
            }
            else if (parameter is string stringValue && int.TryParse(stringValue, out int parsedValue))
            {
                rotationDegrees = parsedValue;
            }
        }

        // プレビューで表示しているページ番号を取得（デフォルトは1）
        int pageToRotate = CurrentPageNumber > 0 ? CurrentPageNumber : 1;
        
        // プレビューが表示されていない、またはページ番号が無効な場合は1ページ目を回転
        if (pageToRotate < 1 || pageToRotate > TotalPages)
        {
            pageToRotate = 1;
        }

        StatusText = "PDFを回転中...";
        var success = await _pdfRotateService.RotatePageAsync(SelectedPdfFile.FilePath, pageToRotate, rotationDegrees);
        
        if (success)
        {
            StatusText = $"PDFファイルのページ{pageToRotate}を{rotationDegrees}度回転しました";
            
            // 選択されているファイルのパスを保存
            string? selectedFilePath = SelectedPdfFile?.FilePath;
            
            // ファイル一覧を更新
            await RefreshAsync();
            
            // プレビューを更新（現在のページを再表示）
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                var fileToSelect = PdfFiles.FirstOrDefault(f => f.FilePath == selectedFilePath);
                if (fileToSelect != null)
                {
                    SelectedPdfFile = fileToSelect;
                    if (IsPreviewVisible)
                    {
                        await LoadPageAsync(fileToSelect.FilePath, pageToRotate);
                    }
                }
            }
        }
        else
        {
            StatusText = "PDFの回転に失敗しました";
            MessageBox.Show("PDFの回転に失敗しました。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task RotateAllPagesAsync(object? parameter)
    {
        // ライセンスチェック
        if (!_licenseService.CanUseRotate())
        {
            MessageBox.Show(
                "PDF回転機能は有償版の機能です。\n\n1か月の試用期間中は全機能をご利用いただけます。\n試用期間が終了した場合は、ライセンスの購入が必要です。",
                "機能制限",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (SelectedPdfFile == null)
        {
            MessageBox.Show("PDFファイルを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // パラメータをintに変換
        int rotationDegrees = 90; // デフォルト値
        if (parameter != null)
        {
            if (parameter is int intValue)
            {
                rotationDegrees = intValue;
            }
            else if (parameter is string stringValue && int.TryParse(stringValue, out int parsedValue))
            {
                rotationDegrees = parsedValue;
            }
        }

        try
        {
            StatusText = "PDFを回転中...";
            
            // デバッグ出力
            System.Diagnostics.Debug.WriteLine($"RotateAllPagesAsync開始: {SelectedPdfFile.FilePath}, 角度: {rotationDegrees}");
            System.Console.WriteLine($"RotateAllPagesAsync開始: {SelectedPdfFile.FilePath}, 角度: {rotationDegrees}");
            
            var success = await _pdfRotateService.RotateAllPagesAsync(SelectedPdfFile.FilePath, rotationDegrees);
            
            System.Diagnostics.Debug.WriteLine($"RotateAllPagesAsync結果: {success}");
            System.Console.WriteLine($"RotateAllPagesAsync結果: {success}");
            
            if (success)
            {
                StatusText = $"PDFファイルの全ページを{rotationDegrees}度回転しました";
                
                // 選択されているファイルのパスとページ番号を保存
                string? selectedFilePath = SelectedPdfFile?.FilePath;
                int displayPageNumber = SelectedPdfFile?.DisplayPageNumber ?? 1;
                
                // ファイル一覧を更新
                await RefreshAsync();
                
                // 選択状態を復元してサムネイルとプレビューを更新
                if (!string.IsNullOrEmpty(selectedFilePath))
                {
                    var fileToSelect = PdfFiles.FirstOrDefault(f => f.FilePath == selectedFilePath);
                    if (fileToSelect != null)
                    {
                        SelectedPdfFile = fileToSelect;
                        
                        // サムネイルを更新
                        await LoadThumbnailPageAsync(fileToSelect, displayPageNumber);
                        
                        // プレビューを更新
                        if (IsPreviewVisible)
                        {
                            await LoadPreviewAsync(fileToSelect.FilePath);
                        }
                    }
                }
            }
            else
            {
                StatusText = "PDFの回転に失敗しました";
                var errorMessage = "PDFの回転に失敗しました。\n\n";
                errorMessage += "考えられる原因:\n";
                errorMessage += "・ファイルが他のアプリケーションで開かれている\n";
                errorMessage += "・ファイルへの書き込み権限がない\n";
                errorMessage += "・ディスクの空き容量が不足している\n\n";
                errorMessage += "詳細はデバッグコンソールまたはターミナルを確認してください。";
                MessageBox.Show(errorMessage, "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusText = "PDFの回転に失敗しました";
            
            // 詳細なエラー情報をログに出力
            var errorDetails = $"PDF回転エラー詳細:\n";
            errorDetails += $"メッセージ: {ex.Message}\n";
            if (ex.InnerException != null)
            {
                errorDetails += $"内部例外: {ex.InnerException.Message}\n";
                errorDetails += $"内部例外タイプ: {ex.InnerException.GetType().Name}\n";
            }
            errorDetails += $"例外タイプ: {ex.GetType().Name}\n";
            errorDetails += $"スタックトレース:\n{ex.StackTrace}";
            
            System.Diagnostics.Debug.WriteLine(errorDetails);
            System.Console.WriteLine(errorDetails);
            
            var errorMessage = $"PDFの回転に失敗しました。\n\n";
            errorMessage += $"エラー: {ex.Message}\n\n";
            if (ex.InnerException != null)
            {
                errorMessage += $"内部例外: {ex.InnerException.Message}\n\n";
            }
            errorMessage += "詳細はデバッグコンソールまたはターミナルを確認してください。";
            MessageBox.Show(errorMessage, "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // コピー・ペースト機能
    [RelayCommand]
    private void CopyFiles()
    {
        _copiedFilePaths.Clear();
        
        // 選択されたファイルのパスをコピー（複数選択対応）
        if (SelectedPdfFile != null)
        {
            _copiedFilePaths.Add(SelectedPdfFile.FilePath);
        }

        if (_copiedFilePaths.Count > 0)
        {
            StatusText = _copiedFilePaths.Count == 1 
                ? "ファイルをコピーしました" 
                : $"{_copiedFilePaths.Count}個のファイルをコピーしました";
        }
    }

    // 複数ファイルをコピー（外部から呼び出し用）
    public void CopyFiles(List<PdfFileInfo> files)
    {
        _copiedFilePaths.Clear();
        foreach (var file in files)
        {
            _copiedFilePaths.Add(file.FilePath);
        }
        
        if (_copiedFilePaths.Count > 0)
        {
            StatusText = _copiedFilePaths.Count == 1 
                ? "ファイルをコピーしました" 
                : $"{_copiedFilePaths.Count}個のファイルをコピーしました";
        }
    }

    [RelayCommand]
    private async Task PasteFilesAsync()
    {
        if (_copiedFilePaths.Count == 0 || SelectedFolder == null)
        {
            MessageBox.Show("コピーされたファイルがありません。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var targetFolder = SelectedFolder.Path;
        var copiedCount = 0;

        foreach (var sourcePath in _copiedFilePaths)
        {
            if (!File.Exists(sourcePath)) continue;

            var fileName = Path.GetFileName(sourcePath);
            var targetPath = Path.Combine(targetFolder, fileName);

            // 同名ファイルが存在する場合は番号を付ける
            if (File.Exists(targetPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                int counter = 1;
                do
                {
                    fileName = $"{nameWithoutExt} ({counter}){ext}";
                    targetPath = Path.Combine(targetFolder, fileName);
                    counter++;
                } while (File.Exists(targetPath));
            }

            try
            {
                File.Copy(sourcePath, targetPath, false);
                copiedCount++;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ファイルコピーエラー: {ex.Message}");
            }
        }

        if (copiedCount > 0)
        {
            StatusText = $"{copiedCount}個のファイルを貼り付けました";
            await RefreshAsync();
        }
        else
        {
            MessageBox.Show("ファイルの貼り付けに失敗しました。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // フォルダを再帰的に検索して選択＆展開
    private void SelectAndExpandFolder(FolderNode node, string targetPath)
    {
        // 空のパス（ルートノード）の場合はスキップ
        if (string.IsNullOrEmpty(node.Path))
        {
            node.IsExpanded = true;
            foreach (var child in node.Children)
            {
                SelectAndExpandFolder(child, targetPath);
            }
            return;
        }

        if (node.Path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            // 目的のフォルダを見つけた
            node.IsExpanded = true;
            node.IsSelected = true;
            SelectedFolder = node;
            return;
        }

        // targetPathがこのノードの子孫かチェック
        if (targetPath.StartsWith(node.Path, StringComparison.OrdinalIgnoreCase))
        {
            // このノードを展開
            node.IsExpanded = true;
            
            // 子ノードを再帰的に検索
            foreach (var child in node.Children)
            {
                SelectAndExpandFolder(child, targetPath);
            }
        }
    }
}
