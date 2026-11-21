# Bezier Multi-Control Points & Rotation Handle Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove rotation handles from Line and Bezier shapes so they are manipulated naturally via endpoints/control points, and implement dynamic multi-control-point Bezier curve editing matching draw.io ergonomics (interactive midpoint `+` handles, right-click add/remove, and Properties Inspector controls).

**Architecture:** Extend `ShapeItem` in `LabScape.Core` with backward-compatible `control_points` array while keeping legacy `control_point1` & `control_point2` in sync. Render smooth multi-point Bezier geometry using De Casteljau's algorithm in `ShapeGeometryHelper`. Update `SelectionAdorner` to eliminate rotation stalks on Line and Bezier, draw interactive handles for all $N$ control points, render draw.io-style midpoint handles that dynamically spawn new control points on click/drag, and support right-click removal.

**Tech Stack:** .NET 10.0, WPF, Direct3D/PathGeometry, CommunityToolkit.Mvvm, xUnit.

**Spec:** User request: "remove the rotation handles from line and bezeir curve and add an option to add more control points in bezeir curve just like draw.io".

## Global Constraints
- Target Framework: `.NET 10.0` across all projects (`LabScape.Core`, `LabScape.ViewModels`, `LabScape.App`, `LabScape.Tests`).
- Architecture: Zero UI dependencies in `LabScape.Core` and `LabScape.ViewModels`.
- Backward Compatibility: Existing JSON layout documents containing `control_point1_x/y` and `control_point2_x/y` must load identically without data loss.
- Undo/Redo: Every addition, deletion, or movement of control points must be fully undoable via `ModifyBezierControlPointsCommand`.
- Testing: All existing 103 unit tests must remain green or be updated where specifications changed, and new tests added for multi-point Bezier support.

## Review Focus
1. Backward compatibility when deserializing older floorplans that only specify `control_point1_x/y` and `control_point2_x/y`.
2. Bounding box and dimension calculations (`UpdateBezierDimensions`) when control points are moved or added outside initial bounds.
3. Edge cases when removing control points (curve must enforce a minimum of 1 control point).
4. Snapping and hit-testing precision so midpoint `+` handles do not conflict with primary control points.
5. Canvas rendering performance under high zoom or during rapid interactive dragging of control points.

---

### Task 1: Core Domain Model & JSON Schema Support

**Files:**
- Modify: `src/LabScape.Core/Models/ShapeItem.cs:25-60`
- Modify: `docs/SCHEMA.md:85-115`
- Test: `tests/LabScape.Tests/CoreTests.cs`

**Interfaces:**
- Produces: `ControlPoint` class (`double X`, `double Y`) in `LabScape.Core.Models`
- Produces: `ShapeItem.ControlPoints` (`List<ControlPoint>`) synchronized with `ControlPoint1X/Y` and `ControlPoint2X/Y`

- [ ] **Step 1: Write failing tests in `tests/LabScape.Tests/CoreTests.cs`**

```csharp
[Fact]
public void ShapeItem_MultiControlPoints_SerializesAndMaintainsBackwardCompatibility()
{
    var shape = new ShapeItem
    {
        ShapeType = ShapeType.Bezier,
        StartX = 10,
        StartY = 80,
        EndX = 200,
        EndY = 80
    };
    shape.ControlPoints.Add(new ControlPoint(40, 20));
    shape.ControlPoints.Add(new ControlPoint(100, 140));
    shape.ControlPoints.Add(new ControlPoint(160, 20));

    Assert.Equal(3, shape.ControlPoints.Count);
    Assert.Equal(40, shape.ControlPoint1X);
    Assert.Equal(100, shape.ControlPoint2X);

    string json = JsonSerializer.Serialize(shape);
    Assert.Contains("\"control_points\":", json);

    var deserialized = JsonSerializer.Deserialize<ShapeItem>(json);
    Assert.NotNull(deserialized);
    Assert.Equal(3, deserialized.ControlPoints.Count);
    Assert.Equal(160, deserialized.ControlPoints[2].X);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~ShapeItem_MultiControlPoints`
Expected: FAIL (compilation failure: `ControlPoint` / `ControlPoints` does not exist).

- [ ] **Step 3: Implement `ControlPoint` and update `ShapeItem.cs`**

In `src/LabScape.Core/Models/ShapeItem.cs`:
- Define `public class ControlPoint { public double X { get; set; } public double Y { get; set; } }`
- Add `[JsonPropertyName("control_points")] public List<ControlPoint> ControlPoints { get; set; } = new();`
- Sync with legacy properties `ControlPoint1X`, `ControlPoint1Y`, `ControlPoint2X`, `ControlPoint2Y` when accessed or modified.
- Update `docs/SCHEMA.md` with the new `control_points` schema definition.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~ShapeItem_MultiControlPoints`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LabScape.Core/Models/ShapeItem.cs docs/SCHEMA.md tests/LabScape.Tests/CoreTests.cs
git commit -m "feat(core): add multi-control-points support to ShapeItem with backward compatibility"
```

---

### Task 2: ViewModel State, Observable Operations & Undo/Redo Command

**Files:**
- Modify: `src/LabScape.ViewModels/Items/ShapeItemViewModel.cs`
- Modify: `src/LabScape.ViewModels/Commands/UndoableCommands.cs:292-370`
- Modify: `src/LabScape.ViewModels/PropertiesViewModel.cs`
- Test: `tests/LabScape.Tests/CoreTests.cs`

**Interfaces:**
- Produces: `ShapeItemViewModel.GetControlPoints() -> IReadOnlyList<Point>`
- Produces: `ShapeItemViewModel.AddControlPoint(double x, double y, int? index = null)`
- Produces: `ShapeItemViewModel.RemoveControlPoint(int index) -> bool`
- Produces: `ShapeItemViewModel.SetControlPoint(int index, double x, double y)`
- Produces: `ModifyBezierControlPointsCommand(ShapeItemViewModel item, Point oldStart, Point oldEnd, IReadOnlyList<Point> oldCPs, Point newStart, Point newEnd, IReadOnlyList<Point> newCPs)`
- Produces: `PropertiesViewModel.AddBezierControlPointCommand` & `RemoveBezierControlPointCommand`

- [ ] **Step 1: Write failing tests in `tests/LabScape.Tests/CoreTests.cs`**

```csharp
[Fact]
public void ShapeItemViewModel_AddAndRemoveControlPoints_WithUndoRedo()
{
    var model = new ShapeItem { ShapeType = ShapeType.Bezier };
    var vm = new ShapeItemViewModel(model);
    var undoService = new UndoRedoService();

    Assert.Equal(2, vm.GetControlPoints().Count);

    // Add 3rd control point
    var oldPoints = vm.GetControlPoints().ToList();
    vm.AddControlPoint(70, 50, 1);
    var newPoints = vm.GetControlPoints().ToList();

    var cmd = new ModifyBezierControlPointsCommand(
        vm,
        new Point(vm.StartX, vm.StartY), new Point(vm.EndX, vm.EndY), oldPoints,
        new Point(vm.StartX, vm.StartY), new Point(vm.EndX, vm.EndY), newPoints);
    undoService.ExecuteCommand(cmd);

    Assert.Equal(3, vm.GetControlPoints().Count);
    Assert.Equal(70, vm.GetControlPoints()[1].X);

    undoService.Undo();
    Assert.Equal(2, vm.GetControlPoints().Count);

    undoService.Redo();
    Assert.Equal(3, vm.GetControlPoints().Count);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~ShapeItemViewModel_AddAndRemoveControlPoints`
Expected: FAIL (`GetControlPoints` or `AddControlPoint` not defined).

- [ ] **Step 3: Implement methods on `ShapeItemViewModel`, `ModifyBezierControlPointsCommand`, and `PropertiesViewModel`**

- In `ShapeItemViewModel.cs`:
  - Manage control points list synced with model.
  - Implement `GetControlPoints()`, `AddControlPoint()`, `RemoveControlPoint()`, and `SetControlPoint()`.
  - Dispatch property change events and notify dimension updates.
- In `UndoableCommands.cs`:
  - Update `ModifyBezierControlPointsCommand` to store point collections, executing and unexecuting point lists while preserving legacy constructor.
- In `PropertiesViewModel.cs`:
  - Add relay commands `AddBezierControlPointCommand` and `RemoveBezierControlPointCommand`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~ShapeItemViewModel_AddAndRemoveControlPoints`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LabScape.ViewModels/ tests/LabScape.Tests/CoreTests.cs
git commit -m "feat(vm): implement multi-control-point management and undo/redo support"
```

---

### Task 3: Multi-Point Bezier Geometry Generation (De Casteljau Algorithm) & Web Viewer

**Files:**
- Modify: `src/LabScape.App/Helpers/ShapeGeometryHelper.cs:131-147`
- Modify: `web-viewer/index.html:965-975`
- Test: `tests/LabScape.Tests/CoreTests.cs`

**Interfaces:**
- Consumes: `ShapeItemViewModel.GetControlPoints()`
- Produces: Continuous smooth `Geometry` via `ShapeGeometryHelper.CreateGeometry()` supporting 1, 2, and $N \ge 3$ control points.

- [ ] **Step 1: Write failing tests in `tests/LabScape.Tests/CoreTests.cs`**

```csharp
[Fact]
public void ShapeGeometryHelper_CreateGeometry_MultiPointBezier_ProducesSmoothPath()
{
    var model = new ShapeItem { ShapeType = ShapeType.Bezier, StartX = 0, StartY = 50, EndX = 200, EndY = 50 };
    var vm = new ShapeItemViewModel(model);
    vm.AddControlPoint(50, 10);
    vm.AddControlPoint(100, 90);
    vm.AddControlPoint(150, 20);

    var geom = ShapeGeometryHelper.CreateGeometry(vm);
    Assert.NotNull(geom);
    Assert.IsType<PathGeometry>(geom);
    var pg = (PathGeometry)geom;
    Assert.Single(pg.Figures);
    Assert.NotEmpty(pg.Figures[0].Segments);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~ShapeGeometryHelper_CreateGeometry_MultiPointBezier`
Expected: FAIL.

- [ ] **Step 3: Implement De Casteljau evaluation in `ShapeGeometryHelper.cs` and `web-viewer/index.html`**

- In `ShapeGeometryHelper.cs`:
  - If 1 control point: emit `QuadraticBezierSegment`.
  - If 2 control points: emit `BezierSegment` (exact existing behavior).
  - If 3+ control points: evaluate degree-$(N+1)$ polynomial using De Casteljau's algorithm across 64 smooth subdivisions into `PolyLineSegment`.
- In `web-viewer/index.html`:
  - When `item.control_points` has > 2 points, evaluate De Casteljau in HTML5 Canvas 2D context.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~ShapeGeometryHelper_CreateGeometry_MultiPointBezier`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LabScape.App/Helpers/ShapeGeometryHelper.cs web-viewer/index.html tests/LabScape.Tests/CoreTests.cs
git commit -m "feat(geometry): render multi-point Bezier splines via De Casteljau algorithm"
```

---

### Task 4: Interactive Adorner — Remove Rotation Handles & Add Draw.io-Style Handles

**Files:**
- Modify: `src/LabScape.App/Adorners/SelectionAdorner.cs`
- Modify: `tests/LabScape.Tests/CoreTests.cs:4060-4200`
- Test: `tests/LabScape.Tests/CoreTests.cs`

**Interfaces:**
- Produces: `SelectionAdorner` rendering 0 rotation stalks for Line and Bezier.
- Produces: Interactive handles for all $N$ control points with dotted tangent links.
- Produces: Virtual midpoint `+` handles between consecutive points that spawn a new control point on click/drag.
- Produces: Right-click removal of control points (down to minimum 1).

- [ ] **Step 1: Update code analysis test in `tests/LabScape.Tests/CoreTests.cs`**

Update `SelectionAdorner_CodeAnalysis_VerifiesLine3Handles_AndBezier5HandlesWithDottedTangents`:
- Verify Line has `pStart` and `pEnd` and NO `pRot`.
- Verify Bezier has `pStart`, `pEnd`, `GetControlPoints`, `DottedTangentPen`, and NO `pRot`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~SelectionAdorner_CodeAnalysis`
Expected: FAIL (due to updated handle contract assertions).

- [ ] **Step 3: Update `SelectionAdorner.cs`**

- **Remove rotation handle from Line**:
  - In `OnRender`: eliminate `pRot` and rotation stalk line. Only draw `pStart` and `pEnd`.
  - In `OnMouseDown`, `UpdateCursor`, and `HitTestCore`: remove `pRot` hit tests for Line.
- **Remove rotation handle from Bezier**:
  - In `OnRender`: eliminate `pRot` and rotation stalk line.
  - In `OnMouseDown`, `UpdateCursor`, and `HitTestCore`: remove `pRot` hit tests for Bezier.
- **Render All Control Points**:
  - Iterate over `shapeBezier.GetControlPoints()`, drawing `ControlPointBrush` circular handles.
  - Draw `DottedTangentPen` lines connecting $P_{start} \to C_0 \to C_1 \dots \to P_{end}$.
- **Draw.io-Style Midpoint `+` Handles**:
  - Compute midpoints between each consecutive pair of points along the control polygon.
  - Draw subtle circular midpoint handles with `+` symbol (cyan/light blue).
  - In `OnMouseDown`: hitting a midpoint handle inserts a new control point at that index and immediately engages dragging!
- **Control Point Dragging & Removal**:
  - Dynamically track active control point index `_activeControlPointIndex`.
  - On mouse up, execute `ModifyBezierControlPointsCommand`.
  - On right mouse button on a control point, remove it (if count > 1) and record undoable command.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test LabScape.slnx`
Expected: PASS (All 103+ unit tests green).

- [ ] **Step 5: Commit**

```bash
git add src/LabScape.App/Adorners/SelectionAdorner.cs tests/LabScape.Tests/CoreTests.cs
git commit -m "feat(adorner): remove rotation handles from Line and Bezier, add draw.io-style control points"
```

---

### Task 5: Properties Inspector & Context Menu Integration

**Files:**
- Modify: `src/LabScape.App/Views/PropertiesInspectorView.xaml`
- Modify: `src/LabScape.App/Views/CanvasView.xaml`
- Test: `tests/LabScape.Tests/CoreTests.cs`

**Interfaces:**
- Produces: In `PropertiesInspectorView.xaml`, Bezier curve panel with `Add Control Point` button, `Remove Control Point` button, and point coordinates list.
- Produces: Context menu item on Canvas for Bezier items to add/remove control points.

- [ ] **Step 1: Write integration test verifying Property Inspector commands execute**

```csharp
[Fact]
public void PropertiesViewModel_BezierControlPointsCommands_ExecuteSuccessfully()
{
    var model = new ShapeItem { ShapeType = ShapeType.Bezier };
    var item = new ShapeItemViewModel(model);
    var cvm = new CanvasViewModel();
    cvm.Items.Add(item);
    cvm.SelectItem(item);

    var pvm = new PropertiesViewModel(cvm, new UndoRedoService());
    pvm.UpdateSelection();

    Assert.True(pvm.CanAddBezierControlPoint);
    pvm.AddBezierControlPointCommand.Execute(null);
    Assert.Equal(3, item.GetControlPoints().Count);

    Assert.True(pvm.CanRemoveBezierControlPoint);
    pvm.RemoveBezierControlPointCommand.Execute(null);
    Assert.Equal(2, item.GetControlPoints().Count);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test LabScape.slnx --filter FullyQualifiedName~PropertiesViewModel_BezierControlPointsCommands`
Expected: FAIL.

- [ ] **Step 3: Implement PropertiesInspectorView XAML & CanvasView Context Menu**

- In `PropertiesInspectorView.xaml`:
  - In the Bezier section, add a sleek panel displaying the number of control points, `+ Add Control Point`, and `- Remove Control Point` buttons.
- In `CanvasView.xaml`:
  - Add contextual commands to the shape context menu when a Bezier is selected.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test LabScape.slnx`
Expected: PASS (All tests green).

- [ ] **Step 5: Commit**

```bash
git add src/LabScape.App/Views/PropertiesInspectorView.xaml src/LabScape.App/Views/CanvasView.xaml src/LabScape.ViewModels/PropertiesViewModel.cs tests/LabScape.Tests/CoreTests.cs
git commit -m "feat(ui): add Bezier control point controls to Properties Inspector and context menu"
```

---

### Task 6: Final Verification & Local Binary Build

**Files:**
- All modified projects
- Dist binaries

- [ ] **Step 1: Run full automated test suite**

Run: `dotnet test LabScape.slnx`
Expected: PASS (100% green, 0 failures).

- [ ] **Step 2: Build solution and release package**

Run: `dotnet build LabScape.slnx`
Run: `dotnet publish src/LabScape.App/LabScape.App.csproj -c Release -o dist/publish --nologo`
Expected: Build Succeeded (0 errors, 0 warnings).

- [ ] **Step 3: Verify git status**

Run: `git status`
Expected: Clean working tree ready for review.
