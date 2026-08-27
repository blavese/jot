# jot

A minimal notepad for Windows. Opens to a blank page, gets out of the way.

## the point of it

Whatever's in the editor when you close the window is still there when you open it again. Not saved to a file, just remembered, the way a real notepad on your desk doesn't erase itself when you close the notebook. Actually saving to a `.txt` file is still there when you want it (Ctrl+S), this is a safety net underneath that.

## features

- New / Open / Save / Save As, same as any text editor
- Find (Ctrl+F), next/previous, wraps around
- Word wrap toggle, adjustable font size (A- / A+)
- Word and character count in the status bar
- Custom minimal dark UI, no toolbars or ribbons

## why it's safe to just close the window

Every edit is written to a small local draft file (`%LocalAppData%\Jot\draft.json`) a few hundred milliseconds after you stop typing, and again the instant the window closes. That's separate from whatever `.txt` file you may or may not have open. Close jot mid-sentence, reopen it, the sentence is still there.

## building

```bash
git clone https://github.com/blavese/jot.git
cd jot/src/Jot
dotnet build
```

Run `Jot.exe` from `bin/Debug/net8.0-windows/`, or `dotnet run`.

## requirements

Windows 10/11, .NET 8 runtime (or the SDK to build from source).

## license

MIT, see [LICENSE](LICENSE).
