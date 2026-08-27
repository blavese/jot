using System.IO;
using System.Text.Json;
using Jot.Models;

namespace Jot.Services;

// keeps a copy of whatever's in the editor at %LocalAppData%\Jot\draft.json,
// independent of whether the user has ever hit save. this is what makes
// closing the window safe even with unsaved text in the box.
public class DraftStore
{
    private readonly string _path;

    public DraftStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jot");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "draft.json");
    }

    public DraftState Load()
    {
        if (!File.Exists(_path)) return new DraftState();
        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<DraftState>(json) ?? new DraftState();
        }
        catch
        {
            return new DraftState();
        }
    }

    public void Save(DraftState state)
    {
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        // write to a temp file first so a crash mid-write can't corrupt the
        // one draft copy this whole feature depends on
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, _path, overwrite: true);
        File.Delete(tmp);
    }
}
