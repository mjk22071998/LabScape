# Hallmark - Anti-AI-Slop & Editorial UI Design Rules

Derived from [nutlope/hallmark](https://github.com/nutlope/hallmark). Makes user interfaces look crafted, deliberate, and made by a senior designer rather than template-generated.

---

## 1. The Design Floor (Mandatory Invariants)

1. **Tokens Over Literals**:
   - Every color, brush, font family, font size, margin, and corner radius must route through declared theme resources/tokens (`DynamicResource` in WPF/XAML, CSS variables in Web).
   - No ad-hoc hex codes or hardcoded literal styles past the resource dictionary / token block.
2. **Restrained Transitions & Physics**:
   - Never use blanket transitions (`transition: all`). Never animate layout dimensions (`width`, `height`, `top`, `left`) directly.
   - Use crisp, subtle opacity or transform transitions (100–150ms).
3. **Typography & Hierarchy**:
   - Never mix italic and roman styles within a single headline.
   - Distinct, purposeful weights (`SemiBold` header, `Medium` subhead, `Normal` body).
   - Tabular numerals (`Consolas`, `Cascadia Code`, `Segoe UI`) for coordinates, dimensions, angles, zoom percentages, and stats.
   - No tracked uppercase or small-caps on labels/captions: sentence-case or clean lower-case with natural letter spacing.
4. **No Cliché AI-Slop Tells**:
   - Never use purple-to-blue generic SaaS gradients.
   - Never nest cards inside cards (`border` within `border` with identical radius).
   - Never use emoji as UI icons. Always use crisp vector geometries or SVG stencils.
   - Never display artificial status dots or middle-dot meta strings.
5. **Optical Alignment & Concentric Geometry**:
   - Concentric radii for nested borders: `outer_radius = inner_radius + padding`.
   - Optical alignment takes precedence over raw mathematical centering.
6. **Focus & Tactile Feedback**:
   - Every interactive control (button, tab, palette item, swatch, input) must have distinct `Hover`, `Pressed`, and `Focused` states with immediate visual response.
7. **Accessibility & Contrast**:
   - WCAG 2.2 AA contrast minimum across all theme brushes and text states.

---

## 2. Redesign Contract (Preserve Logic & Wiring)

When redesigning an existing application or screen:
- **Keep the Content**: Preserve all domain models, facts, labels, and data fields.
- **Keep the Wiring**: Preserve all command bindings, event handlers, test hooks, IDs, and MVVM bindings. Never break functionality during visual redesign.
- **Redesign the System & Chrome**: Elevate visual balance, typography, surfaces, spacing rhythms, docking panes, and controls into a unified, high-polish theme.
