namespace Jot.Models;

// everything needed to make the app look exactly like you left it
public class DraftState
{
    public string Content { get; set; } = "";
    public string? FilePath { get; set; }
    public bool HasUnsavedChanges { get; set; }
    public int CaretIndex { get; set; }
    public bool WordWrap { get; set; } = true;
    public double FontSize { get; set; } = 16;
    public double WindowWidth { get; set; } = 760;
    public double WindowHeight { get; set; } = 620;
}
