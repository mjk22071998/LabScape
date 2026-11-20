# LabScape — Technical Decision Records (TDR)
## Comprehensive Architecture, Engineering & Design Decision Catalog
**Document Version:** 1.0.0  
**Target Platform:** Windows 10 / 11 (64-bit)  
**Runtime:** .NET 10.0 (C# 13, Self-Contained Desktop Deployment)  
**Standard Compliance:** ISO/IEC/IEEE 42010 Architecture Description Standard & Architecture Decision Records (ADR)

---

## Executive Summary

This document serves as the authoritative, permanent record of every major technical, architectural, and design engineering decision made during the lifecycle of the **LabScape** CAD layout and floorplan drafting system. 

Each record captures:
1. **Context & Problem Statement**: The exact engineering or user-experience problem being solved.
2. **Technical Decision**: The chosen architectural mechanism, pattern, or data structure.
3. **Alternatives Evaluated & Rejected**: Viable alternative architectures and why they were not chosen.
4. **Engineering Rationale**: Performance metrics, memory profiles, mathematical correctness, or ergonomics justifying the selection.
5. **Consequences & Systemic Impact**: Trade-offs, ripple effects across assemblies, and testing implications.

---

## Quick Navigation Index

| TDR ID | Domain | Decision Title | Status |
| :--- | :--- | :--- | :--- |
| **[TDR-001](#tdr-001-four-tier-clean-architecture-with-strict-ui-isolation)** | Foundation | Four-Tier Clean Architecture with Strict UI Isolation | Accepted |
| **[TDR-002](#tdr-002-strict-nullable-reference-types-across-all-assemblies)** | Foundation | Strict Nullable Reference Types (`<Nullable>enable</Nullable>`) | Accepted |
| **[TDR-003](#tdr-003-lossless-reversible-state-machine-via-iundoablecommand)** | State | Lossless Reversible State Machine via `IUndoableCommand` | Accepted |
| **[TDR-004](#tdr-004-direct3d-tier-2-gpu-hardware-accelerated-rendering-pipeline)** | Graphics | Direct3D Tier 2 GPU Hardware-Accelerated Rendering Pipeline | Accepted |
| **[TDR-005](#tdr-005-direct-vram-caching-via-freezedrawingimage)** | Graphics | Direct VRAM Caching via `DrawingImage.Freeze()` | Accepted |
| **[TDR-006](#tdr-006-virtual-infinite-canvas-with-visio-style-auto-sizing-grid)** | Canvas | Virtual Infinite Canvas with Visio-Style Auto-Sizing Grid | Accepted |
| **[TDR-007](#tdr-007-relaxing-canvas-boundaries-to-enable-off-canvas-drafting-staging)** | Canvas | Relaxing Canvas Boundaries to Enable Off-Canvas Drafting Staging | Accepted |
| **[TDR-008](#tdr-008-zoom-independent-hit-target-sizing-for-canvas-control-handles)** | Adorners | Zoom-Independent Hit-Target Sizing for Canvas Control Handles | Accepted |
| **[TDR-009](#tdr-009-adaptive-8-to-4-handle-reduction-on-miniature-elements)** | Adorners | Adaptive 8-to-4 Handle Reduction on Miniature Elements (<32px) | Accepted |
| **[TDR-010](#tdr-010-oriented-bounding-box-obb-rotation-prior-to-translation)** | Adorners | Oriented Bounding Box (OBB) Rotation Prior to Translation | Accepted |
| **[TDR-011](#tdr-011-vector-dual-tone-rotation-cursor-with-continuous-tracking)** | Adorners | Vector Dual-Tone Rotation Cursor with Continuous Tracking | Accepted |
| **[TDR-012](#tdr-012-dedicated-control-handles-for-lines-and-freeform-bezier-curves)** | Geometry | Dedicated Control Handles for Lines and Freeform Bézier Curves | Accepted |
| **[TDR-013](#tdr-013-stroke-only-hit-testing-for-open-paths-and-transparent-shapes)** | Geometry | Stroke-Only Hit-Testing for Open Paths and Transparent Shapes | Accepted |
| **[TDR-014](#tdr-014-elliptical-arc-geometry-correction-for-rounded-triangles)** | Geometry | Elliptical Arc Geometry Correction for Rounded Triangles | Accepted |
| **[TDR-015](#tdr-015-magnetic-geometry-and-grid-snapping-with-guideline-rendering)** | Snapping | Magnetic Geometry and Grid Snapping with Guideline Rendering | Accepted |
| **[TDR-016](#tdr-016-alignment-assistance-guidelines-for-hierarchical-group-bounds)** | Snapping | Alignment Assistance Guidelines for Hierarchical Group Bounds | Accepted |
| **[TDR-017](#tdr-017-real-time-equal-size-assistance-guides-matching-visio--drawio)** | Snapping | Real-Time Equal-Size Assistance Guides Matching Visio & draw.io | Accepted |
| **[TDR-018](#tdr-018-multi-selection-acting-as-an-interactive-temporary-group)** | Interaction | Multi-Selection Acting as an Interactive Temporary Group | Accepted |
| **[TDR-019](#tdr-019-hybrid-multi-tone-vector-svg-recoloring-pipeline)** | Stencils | Hybrid Multi-Tone Vector SVG Recoloring Pipeline | Accepted |
| **[TDR-020](#tdr-020-full-hsv-spectrum-color-dialog-for-custom-stencil-shading)** | Stencils | Full HSV Spectrum Color Dialog for Custom Stencil Shading | Accepted |
| **[TDR-021](#tdr-021-procedural-parametric-room-and-workstation-generator)** | Facility CAD | Procedural Parametric Room and Workstation Generator | Accepted |
| **[TDR-022](#tdr-022-dual-wall-opposite-parallel-layouts-for-compact-labs)** | Facility CAD | Dual-Wall Opposite Parallel Layouts for Compact Labs | Accepted |
| **[TDR-023](#tdr-023-dual-mode-enterprise-postgresql-repository-with-offline-fallback)** | Data | Dual-Mode Enterprise PostgreSQL Repository with Offline Fallback | Accepted |
| **[TDR-024](#tdr-024-dpapi-data-protection-api-credential-encryption-vault)** | Security | DPAPI (Data Protection API) Credential Encryption Vault | Accepted |
| **[TDR-025](#tdr-025-batch-static-workstation-ip-assignment-with-collision-guards)** | Networking | Batch Static Workstation IP Assignment with Collision Guards | Accepted |
| **[TDR-026](#tdr-026-atomic-file-persistence-and-json-schema-2020-12-validation)** | Persistence | Atomic File Persistence and JSON Schema 2020-12 Validation | Accepted |
| **[TDR-027](#tdr-027-standalone-zero-install-html5-2d-canvas-web-viewer)** | Distribution | Standalone Zero-Install HTML5 2D Canvas Web Viewer | Accepted |
| **[TDR-028](#tdr-028-avalondock-docking-workspace-with-theme-blended-titlebars)** | UI/UX | AvalonDock Docking Workspace with Theme-Blended Titlebars | Accepted |
| **[TDR-029](#tdr-029-ribbon-ergonomic-reorganization-dedicated-view--page-layout)** | UI/UX | Ribbon Ergonomic Reorganization: Dedicated View & Page Layout | Accepted |
| **[TDR-030](#tdr-030-hierarchical-drag-and-drop-layers-panel-photoshopcanva-style)** | UI/UX | Hierarchical Drag-and-Drop Layers Panel (Photoshop/Canva Style) | Accepted |
| **[TDR-031](#tdr-031-single-file-native-installer-and-portable-zip-packaging)** | DevOps | Single-File Native Installer and Portable Zip Packaging | Accepted |
| **[TDR-032](#tdr-032-desktop-shortcut-lnk-auto-synchronization-pipeline)** | DevOps | Desktop Shortcut (`.lnk`) Auto-Synchronization Pipeline | Accepted |
| **[TDR-033](#tdr-033-adoption-of-ponytail-lazy-senior-dev-mode-for-lean-diffs)** | Process | Adoption of Ponytail Lazy Senior Dev Mode for Lean Diffs | Accepted |
| **[TDR-034](#tdr-034-hallmark-anti-ai-slop-design-engineering-standards)** | Design | Hallmark Anti-AI-Slop Design-Engineering Standards | Accepted |
| **[TDR-035](#tdr-035-automated-100-test-gatekeeper-without-superficial-vanity-badges)** | Quality | Automated 100-Test Gatekeeper Without Superficial Vanity Badges | Accepted |
| **[TDR-036](#tdr-036-real-time-equal-spacing-and-distance-guides-matching-drawio)** | Snapping | Real-Time Equal Spacing and Distance Guides Matching draw.io | Accepted |

---

## Architectural Decision Records

### TDR-001: Four-Tier Clean Architecture with Strict UI Isolation
- **Domain:** Foundation & Solution Topology
- **Status:** Accepted & Enforced
- **Context:** Modeling software like CAD, GIS, or network layout tools often devolves into "God Views" where UI elements directly store coordinates, network addresses, and business rules, preventing testability and portability.
- **Decision:** Split the codebase into four strictly partitioned assemblies:
  1. `LabScape.Core`: Pure domain models, mathematical geometry engines, schema entities, and interface contracts. **Zero UI references** (strictly forbidden to reference `PresentationFramework`, `PresentationCore`, `WindowsBase`, or `<UseWPF>true</UseWPF>`).
  2. `LabScape.Infrastructure`: Database drivers (Npgsql), DPAPI security, JSON serializers, and filesystem repositories.
  3. `LabScape.ViewModels`: Observable view state, validation logic, and undoable commands powered by `CommunityToolkit.Mvvm`.
  4. `LabScape.App`: Direct3D views, XAML controls, adorners, converters, and theme resources.
- **Alternatives Considered:**
  - *Monolithic Project with Folders:* Rejected because compile-time constraints are missing; developers inadvertently import WPF controls into domain calculations.
  - *Standard MVC:* Rejected because WPF's dependency property engine and two-way data-binding natively require MVVM.
- **Rationale:** Strict isolation guarantees that all layout calculations, spatial mathematics, schema migrations, and networking constraints can run headless inside CI runners on Linux/Windows without spinning up desktop display contexts.
- **Consequences:** Changes to data structures must propagate through interfaces; requires converters at the view boundary.

---

### TDR-002: Strict Nullable Reference Types Across All Assemblies
- **Domain:** Foundation & Code Health
- **Status:** Accepted & Enforced
- **Context:** `NullReferenceException` is the leading cause of desktop application crashes, especially when deserializing legacy schema formats or handling optional hardware interfaces.
- **Decision:** Enable `<Nullable>enable</Nullable>` and treat unresolved warnings as errors across every project (`Core`, `Infrastructure`, `ViewModels`, `App`, and `Tests`).
- **Alternatives Considered:**
  - *Opt-in Nullability (Per-File `#nullable enable`):* Rejected due to implementation drift and leaky boundaries.
- **Rationale:** Guarantees compile-time safety across all deserialization boundaries, database mappings, and CAD math routines. Enforces explicit null checks or optional values using modern C# pattern matching.
- **Consequences:** Requires explicit null checks, defensive parameter guards, and null-forgiving operators (`!`) only in strictly proven unit test harnesses.

---

### TDR-003: Lossless Reversible State Machine via `IUndoableCommand`
- **Domain:** State Management & History
- **Status:** Accepted & Enforced
- **Context:** In technical CAD drafting, users make hundreds of granular adjustments (nudge, resize, rotate, style change). Accidental moves or incorrect room generation must be reversable without data loss.
- **Decision:** Implement a dual-stack `UndoRedoController` backed by an `IUndoableCommand` interface with `Execute()`, `Unexecute()`, and user-friendly `Description` properties. Every mutating action (dragging, sizing, grouping, ungrouping, layer reordering, color changes, and template generation) executes through this controller.
- **Alternatives Considered:**
  - *Snapshot/Memento Pattern:* Capturing deep clones of the entire canvas on every drag step. Rejected because memory consumption scales exponentially with canvas item count (e.g., 1,000 items $\times$ 50 undo steps = 50,000 object graphs in RAM).
- **Rationale:** Commands capture only the *delta* (e.g., `OldBounds` vs `NewBounds`, `OldIndex` vs `NewIndex`), maintaining negligible memory overhead while providing infinite, instantaneous undo/redo.
- **Consequences:** All new editing capabilities must be authored as atomic command classes rather than direct property mutations in code-behind.

---

### TDR-004: Direct3D Tier 2 GPU Hardware-Accelerated Rendering Pipeline
- **Domain:** Graphics & Rendering Performance
- **Status:** Accepted & Enforced
- **Context:** Rendering floorplans with hundreds of parametric shapes, SVG server racks, text annotations, and dotted alignment guides can trigger severe CPU rasterization bottlenecks and frame stutter during pan and zoom.
- **Decision:** Mandate Direct3D Tier 2 hardware rendering across all windows and canvases via `RenderOptions.ProcessRenderMode = RenderMode.Default` and `RenderOptions.SetBitmapScalingMode(window, BitmapScalingMode.HighQuality)`.
- **Alternatives Considered:**
  - *Software Fallback / CPU Rasterization:* Rejected due to unacceptable lag (<20 FPS) when zooming dense floorplans.
- **Rationale:** Forces the Windows Desktop Window Manager (DWM) and WPF rendering engine to offload all matrix transformations, geometry tessellation, and alpha blending directly to the GPU VRAM pipeline, maintaining 60 FPS viewport interaction.
- **Consequences:** Requires modern GPU support (standard on all Windows 10/11 x64 systems).

---

### TDR-005: Direct VRAM Caching via `DrawingImage.Freeze()`
- **Domain:** Graphics & Memory
- **Status:** Accepted & Enforced
- **Context:** WPF `Freezable` objects that remain unfrozen continually listen for property change notifications and incur thread-affinity checks on every render pass, degrading framerates.
- **Decision:** Enforce that every static vector asset, stencil geometry, icon image, and reusable brush is explicitly frozen (`.Freeze()`) immediately upon instantiation or loading.
- **Alternatives Considered:**
  - *Mutable Drawing Objects:* Rejected because thread-affinity checks prevent cross-thread asset sharing and double memory overhead.
- **Rationale:** Frozen drawing images become immutable and are cached directly in GPU VRAM as Tier 2 hardware textures, completely bypassing CPU re-evaluation during redraw passes.
- **Consequences:** To update an asset's appearance dynamically, a new instance must be created and frozen, or color mutation must be applied via dedicated shader/color-transformation layers.

---

### TDR-006: Virtual Infinite Canvas with Visio-Style Auto-Sizing Grid
- **Domain:** Canvas & Spatial Coordinate Math
- **Status:** Accepted & Enforced
- **Context:** Fixed-size canvas editors force users to predict floorplan dimensions in advance. If a computer lab or server rack extends beyond the initial boundary, elements are clipped.
- **Decision:** Implement a virtual, unbounded canvas coordinate system with dynamic Visio-style multi-page grid calculation. The editor renders standard physical sheet tiles (A4, A3, Letter) while automatically expanding page boundaries dynamically as items are placed further away, while retaining a minimum printable sheet guarantee.
- **Alternatives Considered:**
  - *Fixed Canvas with Scroll Extents:* Traditional paint-app model. Rejected as inadequate for enterprise floorplan drafting.
  - *Seamless Infinite Grid without Page Tiles:* Rejected because users must eventually print or export architectural drawings to standard paper sizes.
- **Rationale:** Combines the freedom of infinite spatial drafting with the precision of architectural page boundaries and scale indicators.
- **Consequences:** Coordinate transforms must handle both global virtual canvas coordinates and local sheet offset coordinates.

---

### TDR-007: Relaxing Canvas Boundaries to Enable Off-Canvas Drafting Staging
- **Domain:** Canvas & User Experience
- **Status:** Accepted & Enforced
- **Context:** Users drafting complex IT facilities frequently need a "scratchpad" area outside the formal print boundaries to unpack template components, stage spare equipment, or draft modular clusters before final placement.
- **Decision:** Explicitly remove clamping boundaries on element movement and creation. Elements can be positioned at negative coordinates or beyond the current page extents without clipping or validation failure.
- **Alternatives Considered:**
  - *Strict Clamping to [0, 0, Width, Height]:* Rejected because it trapped elements at canvas edges during multi-select moves or paste operations.
- **Rationale:** Matches professional CAD workflows (AutoCAD, MS Visio, Figma) where the area surrounding the formal artboard serves as an active workspace.
- **Consequences:** Export and print engines must distinguish between "All Elements" and "Active Page Bounds" when producing target outputs.

---

### TDR-008: Zoom-Independent Hit-Target Sizing for Canvas Control Handles
- **Domain:** Adorners & Interaction Math
- **Status:** Accepted & Enforced
- **Context:** When users zoom out to view an entire 50-meter building (e.g., 20% zoom), traditional pixel-based handles shrink to microscopic, unclickable dots. When zooming in to inspect cabling (e.g., 400% zoom), handles expand to massive blocks obscuring the shape.
- **Decision:** Calculate all resize, rotate, and endpoint adorner handle geometries dynamically by dividing physical screen pixel dimensions by the current canvas `ZoomScale`.
- **Alternatives Considered:**
  - *Fixed Canvas-Unit Handles:* Rejected due to the scale distortion problem described above.
- **Rationale:** Guarantees that regardless of zoom level (10% to 500%), selection handles always maintain an optimal, constant 8px by 8px tactile touch/click target on the user's physical monitor.
- **Consequences:** Adorners must subscribe to zoom scale property changes and invalidate visual arrangements on viewport transforms.

---

### TDR-009: Adaptive 8-to-4 Handle Reduction on Miniature Elements (<32px)
- **Domain:** Adorners & Ergonomics
- **Status:** Accepted & Enforced
- **Context:** When drafting dense equipment (e.g., network patch panels, power outlets, small chairs) smaller than 32px $\times$ 32px, displaying all 8 resize handles causes handles to overlap completely. Clicking to move the item inadvertently triggers a side resize.
- **Decision:** Implement adaptive handle reduction: if either width or height falls below 32 physical units, the 4 edge midpoint handles (North, South, East, West) are automatically hidden, leaving only the 4 corner handles and the top rotation handle.
- **Alternatives Considered:**
  - *Show All 8 Handles Always:* Rejected because miniature shapes become impossible to move without triggering unwanted scaling.
  - *Hide All Handles on Small Shapes:* Rejected because users could not resize small items.
- **Rationale:** Drastically reduces visual clutter and prevents accidental resize actions while preserving full scaling and rotation capabilities.
- **Consequences:** Adorner layout logic includes an element dimension threshold branch.

---

### TDR-010: Oriented Bounding Box (OBB) Rotation Prior to Translation
- **Domain:** Adorners & Coordinate Math
- **Status:** Accepted & Enforced
- **Context:** Applying rotation after translation in WPF transforms causes rotated items and their selection frames to drift away from the mouse pointer during drag operations.
- **Decision:** Enforce matrix transform ordering: apply element center rotation (`RotateTransform`) *prior* to canvas placement translation (`TranslateTransform`). For rotate handles, calculate polar angle offsets using unrotated local coordinate vectors against the item center point.
- **Alternatives Considered:**
  - *Axis-Aligned Bounding Box (AABB) for Rotated Items:* Rejected because an AABB expands unnaturally when a shape rotates, making selection boxes loose and inaccurate.
- **Rationale:** The selection adorner's bounding box rotates tightly in lockstep with the shape's physical contours, providing accurate Oriented Bounding Box (OBB) visual feedback.
- **Consequences:** All hit-testing and alignment guide algorithms must account for rotated OBB geometries.

---

### TDR-011: Vector Dual-Tone Rotation Cursor with Continuous Tracking
- **Domain:** Adorners & Visual Polish
- **Status:** Accepted & Enforced
- **Context:** Standard OS cursors (`Cursors.Hand`, `Cursors.Cross`) fail to communicate rotation intent, while standard Windows rotation cursors lack contrast against alternating dark/light canvas backgrounds.
- **Decision:** Generate an in-memory dual-tone (white outline, black fill) circular vector rotation cursor. Apply it when hovering over the top rotation handle and update rotation angles continuously during drag with real-time degree angle tooltips.
- **Alternatives Considered:**
  - *Standard OS Hand / Crosshair:* Rejected due to lack of affordance.
  - *Single-Color Cursor:* Rejected because black cursors disappear on dark theme canvas, and white cursors disappear on white print sheets.
- **Rationale:** Dual-tone vector styling guarantees 100% optical contrast across all theme backgrounds, grid densities, and color schemes.
- **Consequences:** Custom cursor creation requires Win32 memory icon allocation and safe handle disposal.

---

### TDR-012: Dedicated Control Handles for Lines and Freeform Bézier Curves
- **Domain:** Geometry & Path Editing
- **Status:** Accepted & Enforced
- **Context:** Treating lines and Bézier curves as rectangular bounding boxes prevents natural CAD routing. Dragging a bounding box handle scales the line rather than moving its endpoints or curve tangents.
- **Decision:** Implement dedicated control handle sets:
  - *Straight Line:* 3 control handles (Start Point, Center Nudge Point, End Point).
  - *Bézier Curve:* 5 control handles (Start Anchor, Control Point 1, Center Apex, Control Point 2, End Anchor) with dotted vector tangent lines connecting anchors to control points.
  - *Unclipped Freeform Geometry:* Prevent WPF layout slot clipping by calculating geometry extents dynamically without rigid bounding box boundaries.
- **Alternatives Considered:**
  - *Standard 8-Handle Box on Paths:* Rejected because rotating or curving a cable created awkward, unintuitive distortions.
- **Rationale:** Delivers exact feature parity with Microsoft Visio and draw.io CAD path routing tools.
- **Consequences:** Geometry paths require custom adorner renderers separate from standard rectangular items.

---

### TDR-013: Stroke-Only Hit-Testing for Open Paths and Transparent Shapes
- **Domain:** Canvas & Hit-Testing Ergonomics
- **Status:** Accepted & Enforced
- **Context:** In floorplans with overlapping boundary walls, network runs, and transparent desk zones, clicking empty space inside a large transparent rectangle or near a curved cable frequently selected the wrong background item.
- **Decision:** Configure hit-testing for open paths (Lines, Curves) and shapes with transparent fills to evaluate clicks strictly against the rendered stroke path (widened by a generous 6px tolerance zone) rather than the bounding envelope.
- **Alternatives Considered:**
  - *Bounding Box Hit-Testing:* Rejected because clicking anywhere in the bounding box of a diagonal line selected the line even when the mouse was inches away.
- **Rationale:** Allows users to place equipment and click through empty spaces within open shapes without interference from surrounding container bounds.
- **Consequences:** Requires geometric pen widening (`Pen.GetFillPath_Object`) during hit-test calculations.

---

### TDR-014: Elliptical Arc Geometry Correction for Rounded Triangles
- **Domain:** Parametric Shapes & Mathematical Precision
- **Status:** Accepted & Enforced
- **Context:** Early procedural implementations of rounded triangles used quadratic Bézier curves that produced an asymmetric, "bell-shaped" distortion at acute vertices.
- **Decision:** Replace Bézier apex approximations with mathematically derived tangent circle arcs using true elliptical arc segments (`ArcSegment`) calculated from the inscribed corner angle and corner radius.
- **Alternatives Considered:**
  - *WPF Polygon with StrokeLineJoin.Round:* Rejected because corner radii could not be dynamically customized or animated in the property inspector.
- **Rationale:** Restores true geometric curvature symmetry, eliminating visual distortion across all triangle aspect ratios.
- **Consequences:** Geometry generation requires trigonometric tangent-offset math.

---

### TDR-015: Magnetic Geometry and Grid Snapping with Guideline Rendering
- **Domain:** CAD Snapping & Alignment
- **Status:** Accepted & Enforced
- **Context:** Eyeballing item alignment results in crooked equipment rows, misaligned wall corners, and uneven workstation spacing.
- **Decision:** Implement a real-time spatial alignment engine with a 6px magnetic capture radius. When moving or resizing an item, the engine calculates distances to grid lines and adjacent geometry edges (Left, Center X, Right, Top, Center Y, Bottom). When within threshold, the position snaps magnetically, and a high-contrast accent guideline renders across the alignment axis.
- **Alternatives Considered:**
  - *Grid-Only Snapping:* Rejected because CAD drafting frequently requires aligning items to non-grid reference edges.
  - *Manual Numerical Alignment Only:* Rejected as tedious and slow.
- **Rationale:** Speeds up drafting by orders of magnitude while ensuring millimeter-precise layout consistency.
- **Consequences:** Spatial search must be optimized to filter candidates within view to prevent frame drops during drag operations.

---

### TDR-016: Alignment Assistance Guidelines for Hierarchical Group Bounds
- **Domain:** CAD Snapping & Grouping
- **Status:** Accepted & Enforced
- **Context:** Users frequently group complex workstation clusters (e.g., 6 desks + 6 PCs + 6 chairs). Moving another group or rack next to this cluster failed to align to the cluster's aggregate boundary because snapping only evaluated individual child items.
- **Decision:** Extend the alignment engine to evaluate both atomic shapes and the aggregate Oriented/Axis-Aligned Bounding Box (AABB/OBB) of `GroupItem` models.
- **Alternatives Considered:**
  - *Individual Child Snapping Only:* Rejected because clusters contain dozens of interior edges, creating chaotic guideline flickering.
- **Rationale:** Enables users to treat multi-item assemblies as unified architectural blocks when aligning them to walls or corridors.
- **Consequences:** Group bounds must be cached and recalculated whenever child elements are mutated.

---

### TDR-017: Real-Time Equal-Size Assistance Guides Matching Visio & draw.io
- **Domain:** CAD Snapping & Size Assistance
- **Status:** Accepted & Enforced
- **Context:** When creating matching tables, server racks, or partition panels, users had to manually inspect dimensions in the properties panel to match sizes.
- **Decision:** Implement automated equal-size assistance: when resizing an element, the engine compares its active width and height against every other shape on the canvas. When dimensions match within a 2px magnetic threshold, dimension snap lines and dimension match indicators render along both matching shapes.
- **Alternatives Considered:**
  - *Manual Copy-Paste / Property Duplication:* Workaround that slows down drafting.
- **Rationale:** Delivers expected modern CAD ergonomics found in Microsoft Visio and draw.io, eliminating tedious property inspector lookups.
- **Consequences:** Must run during resize adorner manipulation with zero performance impact.

---

### TDR-018: Multi-Selection Acting as an Interactive Temporary Group
- **Domain:** Interaction Design & Selection
- **Status:** Accepted & Enforced
- **Context:** Selecting 10 items and dragging or rotating them individually resulted in detached, chaotic behavior.
- **Decision:** When multiple items are selected (via Ctrl+Click or Marquee rubberband drag), the canvas synthesizes an interactive temporary group container with a unified bounding box, unified 8-point handles, and a top rotation handle. Transformations apply to the selection ensemble as a cohesive geometric unit.
- **Alternatives Considered:**
  - *Disjointed Multi-Selection (Individual Adorners):* Visual chaos with overlapping handles.
- **Rationale:** Provides intuitive, predictable manipulation of collections without requiring users to formally create and later dissolve permanent groups.
- **Consequences:** Moving or rotating the temporary group decomposes deltas into atomic updates within a single compound undoable command.

---

### TDR-019: Hybrid Multi-Tone Vector SVG Recoloring Pipeline
- **Domain:** Stencil Engine & Visual Customization
- **Status:** Accepted & Enforced
- **Context:** Standard SVG stencils have hardcoded fills and strokes. When users wanted red server racks, blue PCs, or customized equipment markers, standard monochrome tinting (`ColorOverlay`) turned the entire SVG into a flat silhouette, destroying internal line contrast, keys, ports, and shadows.
- **Decision:** Build a multi-tone vector recoloring pipeline using `SharpVectors`. The engine parses the SVG XML DOM and selectively replaces primary base surfaces with user fill colors, outer contours with stroke colors, while preserving or mathematically recalibrating interior highlight and shadow accents.
- **Alternatives Considered:**
  - *Monochrome Color Tint Shaders:* Rejected because it flattens realistic stencils into opaque silhouette stamps.
  - *Pre-rendered Color Variants:* Rejected because storing hundreds of static color copies bloats the application binary.
- **Rationale:** Enables infinite color combinations across all equipment stencils while preserving full photographic depth, hardware ports, and 3D bevels.
- **Consequences:** Requires custom DOM tree parsing and color transformation caching.

---

### TDR-020: Full HSV Spectrum Color Dialog for Custom Stencil Shading
- **Domain:** Stencil Engine & Color Systems
- **Status:** Accepted & Enforced
- **Context:** Preset color swatches (16-color palette) failed to meet enterprise facility standards (e.g., matching exact company brand guidelines or corporate equipment coding).
- **Decision:** Implement a full HSV spectrum color picker dialog with interactive 2D saturation/value gradients, hue sliders, hex input, RGB channels, and recent color history.
- **Alternatives Considered:**
  - *Standard Windows Forms ColorDialog:* Rejected due to outdated Win32 aesthetic, lack of dark theme support, and missing hex code input.
- **Rationale:** Provides precision color selection matching modern design applications (Figma, Adobe XD) with theme integration.
- **Consequences:** Custom WPF dialog implementation with color space conversion math (RGB $\leftrightarrow$ HSV).

---

### TDR-021: Procedural Parametric Room and Workstation Generator
- **Domain:** Facility CAD & Parametric Generation
- **Status:** Accepted & Enforced
- **Context:** Manually placing 40 desks, 40 chairs, 40 PCs, and perimeter boundary walls for a new computer lab takes over an hour of repetitive drafting.
- **Decision:** Implement a parametric room generation engine (`RoomTemplateGenerator`). Users specify room dimensions (width, height in meters), wall margins, desk dimensions, workstation counts, and orientation. The engine procedurally calculates boundary lines, clusters desks into modular islands, rotates PCs and chairs to face appropriate directions, and emits a structured, grouped CAD model.
- **Alternatives Considered:**
  - *Static Template Files Only:* Rejected because static templates cannot adapt to custom room dimensions or varying student/workstation counts.
- **Rationale:** Accelerates lab layout planning from hours to seconds while generating standard, editable canvas elements.
- **Consequences:** Generator logic must validate geometry constraints (e.g., minimum aisle clearance) to prevent overlapping desks.

---

### TDR-022: Dual-Wall Opposite Parallel Layouts for Compact Labs
- **Domain:** Facility CAD & Layout Patterns
- **Status:** Accepted & Enforced
- **Context:** Narrow laboratories and corridor-style server rooms cannot accommodate center island desk clusters; equipment must align along opposite parallel walls (North-South or East-West) with a central walking aisle.
- **Decision:** Introduce dedicated "Opposite Parallel Walls" generation algorithms in `RoomTemplateGenerator`, automatically calculating workstation spacing along opposing perimeters while guaranteeing code-compliant central walkway clearances.
- **Alternatives Considered:**
  - *Manual Layout after Island Generation:* Tedious for users who frequently model narrow computer labs.
- **Rationale:** Directly addresses real-world architectural constraints of educational institutions and enterprise server rooms.
- **Consequences:** Added layout mode enumerations and parameter controls to the Room Template dialog.

---

### TDR-023: Dual-Mode Enterprise PostgreSQL Repository with Offline Fallback
- **Domain:** Data Architecture & Resilience
- **Status:** Accepted & Enforced
- **Context:** Enterprise users need centralized device inventories (IP addresses, MAC addresses, asset tags) stored in PostgreSQL. However, field engineers frequently draft layouts on laptops in air-gapped server rooms or construction sites without network connectivity.
- **Decision:** Implement a dual-mode repository pattern (`IDeviceRepository`):
  - *Online Mode:* Connects directly to PostgreSQL via `Npgsql` with connection pooling.
  - *Offline / Mock Mode:* Automatically or manually falls back to an in-memory/JSON cached repository when the database is unreachable, with visual diagnostic indicators.
- **Alternatives Considered:**
  - *Direct Database Dependency (Hard Requirement):* Rejected because the app would fail to start or edit layouts when offline.
  - *Local SQLite Replication:* Evaluated, but decided in-memory JSON cache was leaner and zero-dependency for offline use.
- **Rationale:** Guarantees that drafting, editing, and layout planning are never blocked by network latency or database outages.
- **Consequences:** Requires state synchronization and re-validation when transitioning from offline to online.

---

### TDR-024: DPAPI (Data Protection API) Credential Encryption Vault
- **Domain:** Security & Enterprise Compliance
- **Status:** Accepted & Enforced
- **Context:** Storing database connection strings and passwords in cleartext JSON settings files violates enterprise security policies and ISO 27001 compliance.
- **Decision:** Secure all sensitive database credentials using the Windows Data Protection API (`ProtectedData.Protect` / `Unprotect` with `DataProtectionScope.CurrentUser`).
- **Alternatives Considered:**
  - *Hardcoded Encryption Keys (AES/DES):* Rejected because hardcoded keys can be extracted via reverse-engineering (.NET decompilation).
  - *Plaintext Config Files:* Rejected as insecure.
- **Rationale:** DPAPI leverages the user's OS logon security credentials, ensuring encrypted strings can only be decrypted on the specific Windows user profile on that physical machine.
- **Consequences:** Connection configurations cannot be copied between different user profiles or machines without re-entering credentials.

---

### TDR-025: Batch Static Workstation IP Assignment with Collision Guards
- **Domain:** Networking & IT Operations
- **Status:** Accepted & Enforced
- **Context:** Assigning individual IP addresses to 60 computers in a lab manually via the properties inspector takes excessive time and invites human typographical errors (duplicate IPs).
- **Decision:** Build a batch IP assignment engine. Users select a collection of workstation items, specify a base IP (e.g., `192.168.1.10`), an increment step, and an optional naming prefix (e.g., `LAB-PC-`). The engine assigns sequential IPs and hostnames while scanning the entire document and database inventory to block duplicates.
- **Alternatives Considered:**
  - *Manual Entry Only:* High error rate and slow.
  - *External DHCP Server Integration:* Overkill for visual layout planning.
- **Rationale:** Turns a 30-minute error-prone task into a 5-second deterministic operation with built-in collision prevention.
- **Consequences:** Requires IP address parsing and collision detection logic in `LabScape.Core`.

---

### TDR-026: Atomic File Persistence and JSON Schema 2020-12 Validation
- **Domain:** Persistence & Data Integrity
- **Status:** Accepted & Enforced
- **Context:** Power failures, crashes, or disk full conditions during a file save operation can truncate documents, destroying irreplaceable floorplans.
- **Decision:** Implement atomic file writes: documents are serialized to a temporary file (`.tmp`) in the same volume, validated against the official JSON Schema 2020-12 specification, and atomically swapped into place via `File.Replace`.
- **Alternatives Considered:**
  - *Direct Overwrite (`File.WriteAllText`):* High risk of zero-byte file corruption on unexpected shutdown.
  - *Proprietary Binary Format:* Rejected because open JSON ensures long-term archivability and third-party scriptability.
- **Rationale:** Guarantees 100% data integrity; an interrupted save leaves the previous valid document completely intact.
- **Consequences:** Requires temporary write permissions in the target directory.

---

### TDR-027: Standalone Zero-Install HTML5 2D Canvas Web Viewer
- **Domain:** Distribution & Cross-Platform Access
- **Status:** Accepted & Enforced
- **Context:** Facilities managers, contractors, and executives who need to review lab floorplans often use macOS, Linux, iPads, or locked-down enterprise laptops where installing WPF desktop applications is impossible.
- **Decision:** Develop a self-contained, single-file HTML5 2D Canvas Web Viewer (`web-viewer/index.html`). The desktop app exports the layout directly into this interactive viewer, enabling pan, zoom, layer toggles, and device inspector popups in any standard web browser without servers or plugins.
- **Alternatives Considered:**
  - *PDF Export Only:* Static, cannot inspect device metadata (IP, MAC, hardware specs), toggle layers, or zoom dynamically.
  - *Blazor WebAssembly App:* Heavy bundle size (>30MB), requiring web server hosting.
- **Rationale:** Zero-install, instantaneous client-side viewing across all operating systems and mobile devices.
- **Consequences:** Web viewer must maintain visual parity with WPF canvas rendering math.

---

### TDR-028: AvalonDock Docking Workspace with Theme-Blended Titlebars
- **Domain:** UI/UX & Window Management
- **Status:** Accepted & Enforced
- **Context:** Fixed tool panels crowd the canvas on small monitors and cannot be organized across multi-monitor setups.
- **Decision:** Integrate `AvalonDock` for tool window management (Stencils, Properties, Templates, Layers). Customize templates to place dock tab strips at the top, blend tool window titlebars directly into dark and light theme palettes, and configure auto-hide anchors.
- **Alternatives Considered:**
  - *Custom Fixed Grid Panels:* Rigid and inflexible for complex workflows.
  - *Default AvalonDock Styling:* Outdated appearance with high visual clutter.
- **Rationale:** Professional IDE-level docking flexibility (Visual Studio / AutoCAD style) with modern, distraction-free visual aesthetics.
- **Consequences:** Window layout state must be managed carefully across theme switches.

---

### TDR-029: Ribbon Ergonomic Reorganization: Dedicated View & Page Layout
- **Domain:** UI/UX & Information Architecture
- **Status:** Accepted & Enforced
- **Context:** The initial Ribbon 'Home' tab became overloaded with canvas settings, docking toggles, shape tools, and template dialogs, violating cognitive grouping principles.
- **Decision:** Reorganize the Ribbon following Microsoft Office & Visio standards:
  - *Home Tab:* Primary drafting tools (Pointer, Marquee, Shapes, Connectors, Text, Layer Ordering, Alignment).
  - *Page Layout Tab:* Paper size presets (A4, A3, Letter), orientation, margins, grid spacing, snap toggles.
  - *View Tab:* Panels & windows recovery, dock toggles, zoom presets, theme switching.
- **Alternatives Considered:**
  - *Single Mega-Tab:* High visual friction and difficult tool discovery.
- **Rationale:** Reduces visual clutter on the primary drafting surface; tools are organized logically by workflow phase.
- **Consequences:** Users familiar with standard Office/Visio software immediately navigate the interface intuitively.

---

### TDR-030: Hierarchical Drag-and-Drop Layers Panel (Photoshop/Canva Style)
- **Domain:** UI/UX & Document Structure
- **Status:** Accepted & Enforced
- **Context:** In complex floorplans with dozens of overlapping items, relying solely on "Bring Forward" and "Send Backward" buttons is frustrating because users cannot visualize the depth stack.
- **Decision:** Implement a dedicated Layers Panel featuring hierarchical tree/list views, expanders for grouped items, visibility toggles (eye icon), lock toggles, and drag-and-drop layer reordering with animated drop indicator lines.
- **Alternatives Considered:**
  - *Z-Index Numeric Input in Property Inspector:* Abstract and difficult to conceptualize visually.
- **Rationale:** Mirrors industry-standard graphics workflows (Adobe Photoshop, Canva, Figma), giving users immediate, tactile control over document hierarchy.
- **Consequences:** Drag-and-drop operations in the layers panel must translate directly into undoable `ReorderLayerCommand` instances.

---

### TDR-031: Single-File Native Installer and Portable Zip Packaging
- **Domain:** Deployment & DevOps
- **Status:** Accepted & Enforced
- **Context:** Enterprise deployment environments vary: some corporate IT departments allow self-extracting single-file setups, while others require zero-install portable zip packages for non-administrator workstations.
- **Decision:** Implement automated multi-format packaging in `build-release.ps1`:
  1. *Native Single-File Setup Installer:* A self-contained Windows installer executable (`LabScape-Setup-v1.0.0.exe`) with administrator shortcut creation and uninstaller.
  2. *Portable Zip Archive:* A self-contained x64 zero-install bundle (`LabScape-v1.0.0-Portable-win-x64.zip`) ready for USB drive deployment.
- **Alternatives Considered:**
  - *MSI Installer via WiX Toolset:* Complex external build tool dependencies.
- **Rationale:** Maximizes distribution flexibility across all corporate IT governance tiers without requiring external dependencies.
- **Consequences:** Packaging script must maintain both delivery pipelines simultaneously.

---

### TDR-032: Desktop Shortcut (`.lnk`) Auto-Synchronization Pipeline
- **Domain:** Development Workflow & User Convenience
- **Status:** Accepted & Enforced
- **Context:** Developers and reviewers testing release builds in the root repository frequently launch outdated binaries if desktop shortcuts point to obsolete build directories.
- **Decision:** Include automated `.lnk` creation and refresh within `build-release.ps1` and dedicated launcher scripts to ensure the root `LabScape.lnk` always targets the latest compiled release executable.
- **Alternatives Considered:**
  - *Manual Shortcut Creation:* Error-prone; leads to false bug reports on stale binaries.
- **Rationale:** Eliminates friction when reviewing local release packages.
- **Consequences:** Shortcut script requires WScript.Shell COM object invocation.

---

### TDR-033: Adoption of Ponytail (Lazy Senior Dev Mode) for Lean Diffs
- **Domain:** Engineering Culture & Code Quality
- **Status:** Accepted & Enforced
- **Context:** Large software projects frequently suffer from speculative architecture, premature generalization, and sprawling multi-thousand-line PRs that introduce bugs and technical debt.
- **Decision:** Enforce the "Ponytail" engineering standard across all development:
  1. The best code is the code never written.
  2. Solve the root cause in shared code rather than adding wrappers.
  3. Shortest working diff wins once scope is known.
  4. Zero speculative abstraction or "for later" boilerplate.
- **Alternatives Considered:**
  - *Traditional Enterprise Layering (Heavy Facades/Adapters):* Bloats codebases with boilerplate and slows execution.
- **Rationale:** Keeps the codebase clean, lean, easily auditable, and laser-focused on actual user requirements.
- **Consequences:** Strict discipline against adding unrequested features or over-engineering solutions.

---

### TDR-034: Hallmark Anti-AI-Slop Design-Engineering Standards
- **Domain:** UI/UX & Design Craftsmanship
- **Status:** Accepted & Enforced
- **Context:** Automated UI generation often produces generic, bland, "template-like" interfaces with uniform padding, jarring colors, and mechanical alignment.
- **Decision:** Implement the Hallmark design principles:
  - *Concentric Radii:* Outer corner radius = Inner corner radius + Padding.
  - *Optical Alignment:* Visual balance takes priority over purely mechanical centering.
  - *Tabular Typography:* Monospace/tabular fonts for dimensions, angles, and coordinates (`Consolas`, `Cascadia Code`).
  - *Restrained Tactile Feedback:* Consistent 150ms hover/pressed/focused transitions.
- **Alternatives Considered:**
  - *Default WPF Controls / Generic Styling:* Results in clunky, outdated enterprise aesthetics.
- **Rationale:** Elevates the software from a functional prototype to an intentional, polished, production-grade CAD application.
- **Consequences:** Requires custom XAML styles and control templates across all UI elements.

---

### TDR-035: Automated 100-Test Gatekeeper Without Superficial Vanity Badges
- **Domain:** Quality Assurance & Continuous Integration
- **Status:** Accepted & Enforced
- **Context:** Automated quality badges (e.g., self-generated shields or third-party ratings) can become superficial vanity metrics if they do not reflect actual behavioral contracts, while creating external dependencies.
- **Decision:** Rely on an uncompromising automated unit test suite (100 exhaustive tests in `LabScape.Tests`) covering architecture rules, mathematical coordinate transforms, converter edge cases, and JSON schema compatibility, while keeping the repository clean of superficial rating badges.
- **Alternatives Considered:**
  - *Self-Generated Quality Badges:* Flawed because internal metrics lack external credibility.
  - *Third-Party SaaS Badges (CodeFactor/SonarCloud):* Adds external dashboard coupling and configuration overhead.
- **Rationale:** True code quality is demonstrated by automated, reproducible verification in CI that halts regressions, rather than decorative badges.
- **Consequences:** Every feature and bugfix must maintain 100% test pass rate in both Debug and Release configurations.

---

### TDR-036: Real-Time Equal Spacing and Distance Guides Matching draw.io
- **Domain:** CAD Snapping & Spacing Assistance
- **Status:** Accepted & Enforced
- **Context:** Placing equipment rows, desks, or server racks at uniform distances required manual grid counting or numerical property entries. In modern diagramming tools like draw.io (`DistanceGuides.js`) and Figma, dragging items automatically reveals equal spacing indicators when gaps between elements match.
- **Decision:** Implement real-time equal spacing detection and snapping across X and Y axes in `CanvasView`:
  1. *Between Pattern ($A < M < B$):* When dragging item/group/multi-selection $M$ between two stationary obstacles, snaps when $\text{gap}(A, M) = \text{gap}(M, B)$.
  2. *Adjacent Pattern ($A < B < M$):* When dragging next to two stationary obstacles with an existing gap, snaps when $\text{gap}(B, M) = \text{gap}(A, B)$.
  3. *Visual Indicators:* Displays high-contrast sky blue (`#0284C7`) dimension bracket lines, tick marks, and floating badges (`↔ {gap} px`, `↕ {gap} px`) across both matching gaps.
  4. *Universal Scope:* Fully active when dragging individual shapes, groups (`GroupItemViewModel`), and multiple selections.
- **Alternatives Considered:**
  - *Menu-Only "Distribute Horizontally/Vertically":* Useful for post-hoc formatting, but lacks real-time interactive tactile feedback while drafting.
- **Rationale:** Provides exact feature parity with draw.io's `enableDistanceGuides` and smart spacing in professional CAD software, eliminating manual measurement.
- **Consequences:** Spacing search runs dynamically during drag-move operations with proximity filtering to preserve 60 FPS performance.

---

### TDR-037: SVG Individual Color Mapping & Granular Vector Palette Customization
- **Domain:** Graphics Rendering, Vector Styling & MVVM Architecture
- **Status:** Accepted & Enforced
- **Context:** Previously, stencil recoloring in `SvgStencilService` only supported global multi-tone tinting derived from a single fill color based on luminance thresholds. Users could not customize individual components of vector stencils (such as changing just the monitor glass, keyboard surface, PC chassis, or status LED).
- **Decision:** Implement granular per-color remapping (`SvgColorMap`) across the graphics pipeline:
  1. *Color Discovery:* `SvgStencilService.GetStencilColors` dynamically traverses the vector `DrawingGroup` (and imported SVG XML) to discover all distinct solid fill and stroke hex colors.
  2. *Domain & Persistence:* Add `SvgColorMap` (`Dictionary<string, string>?`) to `LayoutItemBase` and `IStylableItemViewModel`, persisted in document JSON as `"svg_color_map"`.
  3. *Properties Inspector UI:* Render an "SVG Individual Colors" palette section under Appearance & Style with concentric radii and tabular typography (`Consolas`), displaying each original color swatch next to its active color swatch, human-readable labels, and an interactive 16.7M color spectrum picker.
  4. *Direct3D Tier-2 Acceleration:* Custom recolored `DrawingGroup` instances are frozen (`DrawingImage.Freeze()`) and cached by composite key, preserving Direct3D GPU caching in VRAM.
  5. *Undo/Redo Support:* Wrap color map edits into `ModifySvgColorMapCommand`, enabling instantaneous live canvas updates on Ctrl+Z / Ctrl+Y.
- **Alternatives Considered:**
  - *Global-Only Tinting:* Preserved as fallback when no granular color map is defined, but insufficient for multi-colored equipment components.
  - *Full Inkscape/Illustrator Vector Node Editor:* High complexity with excessive UI overhead for layout design workflows.
- **Rationale:** Allows complete aesthetic customization of equipment stencils and imported SVGs down to each individual component color without sacrificing performance.
- **Consequences:** MultiBinding converter binds `SvgColorMap` to trigger re-renders; color mappings serialize backward-compatibly in JSON layouts.

---

## Conclusion & Governance

All future additions and architectural modifications to LabScape must adhere to these established records. Any proposal to overturn or modify an existing decision requires authoring a new Technical Decision Record detailing the context, evaluation criteria, and empirical rationale.
