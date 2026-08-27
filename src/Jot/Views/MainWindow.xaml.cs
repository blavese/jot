using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using Jot.Services;
using Jot.ViewModels;

namespace Jot.Views;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        Width = vm.RestoredWindowWidth;
        Height = vm.RestoredWindowHeight;

        Loaded += (_, _) =>
        {
            Editor.CaretIndex = Math.Min(vm.RestoredCaretIndex, Editor.Text.Length);
            Editor.Focus();
        };

        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Vm.FindBarOpen = true;
            FindBox.Focus();
            FindBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && Vm.FindBarOpen)
        {
            Vm.FindBarOpen = false;
            Editor.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Vm.NewCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Vm.OpenCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            Vm.SaveAsCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FindNext(Keyboard.Modifiers == ModifierKeys.Shift ? -1 : 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Vm.FindBarOpen = false;
            Editor.Focus();
            e.Handled = true;
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext(1);
    private void FindPrev_Click(object sender, RoutedEventArgs e) => FindNext(-1);

    private void FindNext(int direction)
    {
        var query = FindBox.Text;
        if (string.IsNullOrEmpty(query)) return;

        var text = Editor.Text;
        var startFrom = direction > 0
            ? Editor.SelectionStart + Editor.SelectionLength
            : Editor.SelectionStart - 1;

        int index;
        if (direction > 0)
        {
            index = text.IndexOf(query, Math.Max(0, startFrom), StringComparison.OrdinalIgnoreCase);
            if (index < 0) index = text.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            var searchIn = startFrom >= 0 ? text[..Math.Min(startFrom + query.Length, text.Length)] : "";
            index = searchIn.LastIndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index < 0) index = text.LastIndexOf(query, StringComparison.OrdinalIgnoreCase);
        }

        if (index < 0) return;

        Editor.Select(index, query.Length);
        Editor.Focus();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        // keeps the custom chrome from covering the taskbar / hanging off
        // screen edges when maximized, a known WindowChrome quirk
        var hwndSource = (System.Windows.Interop.HwndSource)System.Windows.Interop.HwndSource.FromVisual(this)!;
        hwndSource.AddHook(WindowProc);
    }

    private static nint WindowProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;
        if (msg == WM_GETMINMAXINFO)
        {
            MaximizeHelper.ApplyWorkAreaBounds(hwnd, lParam);
            handled = true;
        }
        return 0;
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Vm.PersistDraft(Editor.CaretIndex, ActualWidth, ActualHeight);
    }
}
