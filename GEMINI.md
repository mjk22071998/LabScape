# Everything Claude Code (ECC) - Workspace Rules & Architectural Guidelines

This project implements the **Everything Claude Code (ECC)** engineering standard across UI/UX, software architecture, security, and performance.

---

## 1. UI & Design-Engineering Standards (`make-interfaces-feel-better`)

1. **Concentric Radii**:
   - For nested elements: `outer_radius = inner_radius + padding`.
   - Maintain optical coherence across cards, palettes, inspector controls, and tab borders.
2. **Optical Alignment & Hierarchy**:
   - Visual balance takes priority over pure mechanical centering.
   - Adjust icons, badges, indicators, and buttons with precise offsets when necessary.
3. **Tabular Numerics & Typography**:
   - Always use tabular/monospace fonts for coordinates, dimensions, angles, zoom percentages, and dynamic counters (`Consolas`, `Cascadia Code`, `Segoe UI`).
   - Clean typographic scale with distinct weights: Header (`SemiBold`), Section Subtitle (`Medium`), Property Label (`Normal`), Values (`Medium` / `SemiBold`).
4. **Interactive States & Tactile Feedback**:
   - Every clickable element (tools, tabs, buttons, swatches) must provide distinct `Hover`, `Pressed`, and `Focused` visual feedback.
   - Smooth, non-jarring transitions (150ms).
5. **Direct3D GPU Acceleration**:
   - Every vector canvas, stencil, icon, and dynamic image must be frozen (`DrawingImage.Freeze()`) to enable Tier 2 hardware caching directly in GPU VRAM.

---

## 2. Software & C# Architecture Standards (`csharp-patterns`)

1. **MVVM & Separation of Concerns**:
   - Models (`LabScape.Core`) contain pure domain logic and data structures without UI dependencies.
   - ViewModels (`LabScape.ViewModels`) handle state, observable notifications (`CommunityToolkit.Mvvm`), validation, and undoable commands.
   - Views (`LabScape.App`) handle XAML presentation, converters, and Direct3D rendering bindings.
2. **Commands & Undo/Redo**:
   - All state modifications (item movement, resizing, color styling, property updates, network assignments) must be encapsulated in `IUndoableCommand` implementations.
   - Clear and descriptive command labels for Undo/Redo menus and status bars.
3. **Null Safety & Resilience**:
   - Strict nullable reference types (`<Nullable>enable</Nullable>`).
   - Never swallow exceptions silently; use structured logging, user-friendly error banners, or contextual notifications.
4. **Repository & Storage Integrity**:
   - Backward-compatible JSON schema with strong typing.
   - Atomic file saves to prevent corrupted state on unexpected system shutdown.

---

## 3. Active Rule Modules in `.agents/rules/`

- `common-coding-style.md`
- `common-performance.md`
- `common-security.md`
- `common-testing.md`
- `common-git-workflow.md`
- `csharp-coding-style.md`
- `csharp-patterns.md`
- `csharp-testing.md`
- `csharp-security.md`
- `design-quality.md`
- `ponytail.md` (Lazy senior dev mode: lean, minimal, edge-case correct changes)

---

## 4. Verification & Delivery Gate

- **Automated Verification**: Run `dotnet test LabScape.slnx` before committing any change.
- **Git Hygiene**: Conventional commits (`feat`, `fix`, `refactor`, `perf`, `test`, `chore`).
- **Release Automation**: Release pipelines triggered on `master` pushes with multi-format packaging (Setup Installer + Portable Archive).
