# ADR-0001: Editor Engine Selection

- Status: Accepted
- Date: 2026-11-01

## Context
MyPad requires a native-feeling Windows editor with reliable plain-text fidelity, undo/redo correctness, selection, native IME input, line number gutter, current-line highlighting, Markdown token highlighting, and responsive performance on Windows 10 and 11 without bloated third-party dependencies.

To make an evidence-backed architectural decision, an in-depth comparative study of real-world Windows text editors was conducted:
1. **Windows 11 Notepad** (Microsoft's modern rewrite using WinUI 3 / XAML hosting + Direct2D plain-text RichEdit).
2. **Notepads App** (Jackie Liu's open-source UWP/WinUI editor with 10k+ GitHub stars).
3. **Notepad++** (Win32 Scintilla C++ engine).
4. **Custom Win2D canvas** (e.g. `TextControlBox-WinUI`, `CodeEditorControl-WinUI`).
5. **Web-based engines** (VS Code / Monaco / WebView2).

---

## Industry Architecture Analysis

### 1. Windows 11 Notepad (`RichEditD2DPT` / Plain-Text RichEdit)
- **Architecture:** WinUI / XAML application shell hosting a Direct2D/DirectWrite plain-text RichEdit window (`RichEditD2DPT`).
- **Key implementation details (documented by Murray Sargent, Microsoft RichEdit architect):**
  - **Strict plain-text mode:** Disables all OLE objects, tables, and rich formatting.
  - **Line ending preservation:** Historically RichEdit converts all line endings to `\r`. Microsoft introduced `EM_SETENDOFLINE` (`EC_ENDOFLINE_DETECTFROMCONTENT`) to detect and retain original line endings (CRLF, LF, CR) on save and copy.
  - **Large ASCII/log file optimization:** RichEdit bypasses expensive AutoURL detection when opening large text files if the sequence `:/` is absent.
  - **Selection model:** Aligned with modern editors (Visual Studio / VS Code), preventing auto-selection of end-of-paragraph (EOP) markers during mouse click and Shift+End.
- **Verdict for MyPad:** Proven, optimal foundation. Low memory footprint (~35-45 MB), instant cold startup, zero external dependencies, native DirectWrite text and emoji rendering, full OS-level IME and UI Automation accessibility.

### 2. Notepads App (UWP / WinUI `RichEditBox`)
- **Architecture:** C# + WinUI XAML wrapping `Windows.UI.Xaml.Controls.RichEditBox`.
- **Lessons learned from production experience:**
  - `RichEditBox` enables responsive editing, line/column tracking, and smooth font zoom via `FontZoomFactorChanged`.
  - **Limitation:** On documents larger than 1–2 MB, heavy XAML event dispatching (`TextChanging`) and string allocations cause UI stutter. Notepads introduced a 1 MB safeguard.
  - **Paste sanitization:** Intercepting paste events is essential to ensure external RTF/HTML from web pages or Office is converted to pure plain text before insertion.
- **Verdict for MyPad:** Confirms `RichEditBox` is the right choice when paired with a safeguard (disabling real-time token highlighting on large files > 1 MB) and plain-text clipboard enforcement.

### 3. Notepad++ (Win32 Scintilla)
- **Architecture:** Native C++ Win32 application powered by `Scintilla.dll` (gap buffer, incremental line lexers).
- **Strengths:** Superb performance with massive files (10–100 MB).
- **Limitations for WinUI 3:**
  - Scintilla is a Win32 HWND control. Hosting HWND inside WinUI 3 introduces **Airspace issues** (Z-order clipping of WinUI popups, rounded corners, theme translucency, and flyouts).
  - Introduces unmanaged native C++ binary dependencies, complicating build pipelines and unpackaged portable distributions.
- **Verdict:** Rejected due to Airspace issues and native binary baggage.

### 4. Custom Text Surface (Win2D Canvas / DirectWrite from scratch)
- **Architecture:** Viewport-virtualized canvas drawing visible text lines (e.g. `TextControlBox`).
- **Strengths:** Full control over custom line backgrounds, glyph gutters, and custom rendering.
- **Critical Limitations:**
  - Reimplementing complex international text layout, bi-directional text (RTL for Arabic/Hebrew), IME composition windows (East Asian input), touch gesture scrolling, font fallback, and screen reader accessibility (UIA `TextPattern`) requires hundreds of thousands of lines of maintenance-heavy code.
  - Word wrapping is difficult to support efficiently.
- **Verdict:** Rejected as an unnecessary and risky reinvention of the wheel.

### 5. Embedded Web Engine (WebView2 / Monaco Editor)
- **Architecture:** Chromium renderer processes executing JavaScript/DOM.
- **Critical Limitations:**
  - 150–250+ MB idle RAM footprint.
  - High cold-start latency (1–2 seconds to initialize Chromium processes).
  - Directly violates MyPad product principles: lightweight, native Windows Notepad-inspired application.
- **Verdict:** Rejected.

---

## Comparison Matrix

| Criterion | WinUI TextBox | WinUI RichEditBox (TOM) | Scintilla (Win32) | Custom Win2D | Monaco / WebView2 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Memory Footprint** | ~30 MB | ~35–45 MB | ~40 MB | ~50 MB | 150–250+ MB |
| **Cold Startup** | Instant | Instant | Fast | Fast | Slow (1–2s) |
| **IME, Font Fallback, UIA** | Native (OS) | Native (OS) | Limited | Requires manual implementation | Built-in (Chromium) |
| **WinUI 3 Integration** | Native XAML | Native XAML | Airspace conflicts | Native Win2D | Isolated frame |
| **Plain-Text Fidelity** | Yes | Yes (with plain-text I/O) | Yes | Yes | Yes |
| **Token Styling (Markdown)** | No | Yes (`ITextRange`) | Yes | Yes | Yes |
| **Undo/Redo Isolation** | No isolation | `BeginUndoGroup` | Yes | Manual implementation | Yes |
| **Line Number Gutter** | No layout metrics | `ITextRange.GetPoint` | Built-in | Built-in | Built-in |

---

## Decision
Adopt **WinUI `RichEditBox`** (utilizing `Microsoft.UI.Text.ITextDocument`) wrapped in a custom MyPad editor control component (`EditorHost` / `EditorControl`).

### Implementation Strategy & Safeguards:
1. **Plain-Text Fidelity:** All document load/save operations will strictly use `ITextDocument.SetText(TextSetOptions.None, ...)` and `ITextDocument.GetText(TextGetOptions.None, out ...)`.
2. **Line Ending Preservation:** Line endings are detected and handled at the I/O layer via `ILineEndingDetector` and `IDocumentFileService` (`LineEndingMode.CRLF`, `LineEndingMode.LF`, `LineEndingMode.CR`), ensuring zero corruption when RichEdit normalizes internally.
3. **Clipboard Sanitization:** Intercept the Paste pipeline to enforce plain-text only (`DataPackageView.GetTextAsync()`).
4. **Presentation-only Token Highlighting:** Markdown styling passes will run inside `ITextDocument.BeginUndoGroup()` and `EndUndoGroup()` so user undo/redo stacks remain completely clean.
5. **Large File Safeguard:** For documents exceeding 1 MB, real-time syntax styling passes will be throttled or disabled, prioritizing fluid typing latency and low memory usage.
6. **Line Number Gutter:** Coordinate calculation using `ITextRange.GetPoint` / `GetRect` synchronized with the scroll viewport.

## Consequences
- Preserves native Windows look, feel, and instant responsiveness.
- Zero bulky external dependencies.
- Windows 10 (19041+) and Windows 11 compatibility validated.
- Officially satisfies Phase 0 technical spike requirements.
