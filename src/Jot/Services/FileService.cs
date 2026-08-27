using System.IO;
using System.Text;
using Microsoft.Win32;

namespace Jot.Services;

public class FileService
{
    private const string Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

    public string? PickOpenPath()
    {
        var dialog = new OpenFileDialog { Filter = Filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickSavePath(string? suggestedName)
    {
        var dialog = new SaveFileDialog { Filter = Filter, FileName = suggestedName ?? "Untitled.txt" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string ReadText(string path)
    {
        // detects BOM/encoding the same way Notepad itself does, so files
        // saved by either app round-trip cleanly
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public void WriteText(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
