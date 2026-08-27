using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Jot.Models;
using Jot.Services;

namespace Jot.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly DraftStore _draftStore = new();
    private readonly FileService _fileService = new();
    private readonly DispatcherTimer _autosaveTimer;

    private string? _lastSavedContent;

    private string _content = "";
    public string Content
    {
        get => _content;
        set
        {
            if (Set(ref _content, value))
            {
                Raise(nameof(WordCount));
                Raise(nameof(CharCount));
                Raise(nameof(WordCountText));
                Raise(nameof(SaveStateText));
                Raise(nameof(IsDirtyIndicatorVisible));
                _autosaveTimer.Stop();
                _autosaveTimer.Start();
            }
        }
    }

    private string? _filePath;
    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (Set(ref _filePath, value))
            {
                Raise(nameof(DisplayName));
                Raise(nameof(SaveStateText));
                Raise(nameof(IsDirtyIndicatorVisible));
            }
        }
    }

    public string DisplayName => string.IsNullOrEmpty(FilePath) ? "Untitled" : Path.GetFileName(FilePath);

    public string SaveStateText
    {
        get
        {
            if (string.IsNullOrEmpty(FilePath)) return "Not saved to a file";
            return Content == _lastSavedContent ? "Saved" : "Unsaved changes";
        }
    }

    public int WordCount => Content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    public int CharCount => Content.Length;
    public string WordCountText => $"{WordCount} word{(WordCount == 1 ? "" : "s")} · {CharCount} character{(CharCount == 1 ? "" : "s")}";
    public bool IsDirtyIndicatorVisible => Content.Length > 0 && SaveStateText != "Saved";
    public ScrollBarVisibility HorizontalScrollVisibility => WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

    private bool _wordWrap = true;
    public bool WordWrap
    {
        get => _wordWrap;
        set
        {
            if (Set(ref _wordWrap, value))
            {
                Raise(nameof(TextWrapping));
                Raise(nameof(HorizontalScrollVisibility));
                QueueAutosave();
            }
        }
    }

    public TextWrapping TextWrapping => WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;

    private double _fontSize = 16;
    public double FontSize
    {
        get => _fontSize;
        set
        {
            var clamped = Math.Clamp(value, 11, 32);
            if (Set(ref _fontSize, clamped)) QueueAutosave();
        }
    }

    private bool _findBarOpen;
    public bool FindBarOpen
    {
        get => _findBarOpen;
        set => Set(ref _findBarOpen, value);
    }

    public int RestoredCaretIndex { get; private set; }
    public double RestoredWindowWidth { get; private set; } = 760;
    public double RestoredWindowHeight { get; private set; } = 620;

    public RelayCommand NewCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SaveAsCommand { get; }
    public RelayCommand ToggleFindCommand { get; }
    public RelayCommand IncreaseFontCommand { get; }
    public RelayCommand DecreaseFontCommand { get; }

    public MainViewModel()
    {
        NewCommand = new RelayCommand(NewDocument);
        OpenCommand = new RelayCommand(OpenDocument);
        SaveCommand = new RelayCommand(SaveDocument);
        SaveAsCommand = new RelayCommand(SaveDocumentAs);
        ToggleFindCommand = new RelayCommand(() => FindBarOpen = !FindBarOpen);
        IncreaseFontCommand = new RelayCommand(() => FontSize += 1);
        DecreaseFontCommand = new RelayCommand(() => FontSize -= 1);

        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _autosaveTimer.Tick += (_, _) => { _autosaveTimer.Stop(); PersistDraft(); };

        var draft = _draftStore.Load();
        _content = draft.Content;
        _filePath = draft.FilePath;
        _wordWrap = draft.WordWrap;
        _fontSize = draft.FontSize;
        RestoredCaretIndex = draft.CaretIndex;
        RestoredWindowWidth = draft.WindowWidth;
        RestoredWindowHeight = draft.WindowHeight;

        // if we opened with a saved file the last time around, treat that
        // file's on-disk content as the "saved" baseline for the dirty flag
        if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
        {
            try { _lastSavedContent = _fileService.ReadText(_filePath); }
            catch { _lastSavedContent = null; }
        }
    }

    private void QueueAutosave()
    {
        _autosaveTimer.Stop();
        _autosaveTimer.Start();
    }

    private void NewDocument()
    {
        if (!ConfirmDiscardIfNeeded()) return;
        Content = "";
        FilePath = null;
        _lastSavedContent = null;
        Raise(nameof(SaveStateText));
        Raise(nameof(IsDirtyIndicatorVisible));
        PersistDraft();
    }

    private void OpenDocument()
    {
        if (!ConfirmDiscardIfNeeded()) return;
        var path = _fileService.PickOpenPath();
        if (path == null) return;

        try
        {
            Content = _fileService.ReadText(path);
            FilePath = path;
            _lastSavedContent = Content;
            Raise(nameof(SaveStateText));
            Raise(nameof(IsDirtyIndicatorVisible));
            PersistDraft();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't open that file: {ex.Message}", "Jot", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveDocument()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            SaveDocumentAs();
            return;
        }
        WriteToDisk(FilePath);
    }

    private void SaveDocumentAs()
    {
        var path = _fileService.PickSavePath(DisplayName);
        if (path == null) return;
        FilePath = path;
        WriteToDisk(path);
    }

    private void WriteToDisk(string path)
    {
        try
        {
            _fileService.WriteText(path, Content);
            _lastSavedContent = Content;
            Raise(nameof(SaveStateText));
            Raise(nameof(IsDirtyIndicatorVisible));
            PersistDraft();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't save that file: {ex.Message}", "Jot", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool ConfirmDiscardIfNeeded()
    {
        if (string.IsNullOrEmpty(FilePath) || Content == _lastSavedContent) return true;

        var result = MessageBox.Show(
            $"\"{DisplayName}\" has unsaved changes. Save before continuing?",
            "Jot", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel) return false;
        if (result == MessageBoxResult.Yes) SaveDocument();
        return true;
    }

    // called on every keystroke (debounced) and once more, immediately, on
    // window close so nothing typed in the last few hundred ms is lost
    public void PersistDraft(int caretIndex = 0, double windowWidth = 0, double windowHeight = 0)
    {
        _draftStore.Save(new DraftState
        {
            Content = Content,
            FilePath = FilePath,
            HasUnsavedChanges = Content != _lastSavedContent,
            CaretIndex = caretIndex,
            WordWrap = WordWrap,
            FontSize = FontSize,
            WindowWidth = windowWidth > 0 ? windowWidth : RestoredWindowWidth,
            WindowHeight = windowHeight > 0 ? windowHeight : RestoredWindowHeight,
        });
    }
}
