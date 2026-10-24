# LabScape JSON Interchange Specification (SCHEMA.md)
## Formal Schema Contract & Web Developer Interchange Specification
**Document Version:** 1.0.0  
**Target Standard:** JSON Schema Draft 2020-12  
**Encoding:** UTF-8  

---

## 1. Specification Overview

This document specifies the exact JSON data contract produced by the **LabScape** and consumed by downstream web applications. 

### Core Contract Principles:
1. **Recursive Item Hierarchy**: The layout is a tree structure where a `group` item contains a `children` array that can nest any other items (`device`, `furniture`, `shape`, or sub-`group`).
2. **Universal 3D Positioning (`x, y, z`)**: Every item—regardless of type—has explicit `x` (horizontal), `y` (vertical), and `z` (layering / stacking order) coordinates.
3. **Selective Labeling**: Only `device` items render their name on the canvas layout (directly below the icon). All other items (shapes, furniture, groups) do not render names on the canvas, though names are preserved in the schema.
4. **Group Concealment**: Groups possess a configurable `name` in the schema and Properties panel, but group boundaries and names are never rendered as text on the layout.
5. **Exact Shape Taxonomy**: Full vector definitions for `circle`, `rectangle`, `rounded_rectangle`, `triangle`, `rounded_corner_triangle`, `semi_circle`, `line`, `bezier`, and `donut`.

---

## 2. Complete JSON Layout Example

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "version": "1.0.0",
  "canvas": {
    "width": 2400.0,
    "height": 1600.0,
    "unit": "px",
    "gridSize": 20.0,
    "backgroundColor": "#FFFFFF"
  },
  "items": [
    {
      "id": "grp-lab-cluster-01",
      "type": "group",
      "name": "Cluster Alpha (Workstations 1-4)",
      "x": 200.0,
      "y": 150.0,
      "z": 1,
      "width": 450.0,
      "height": 300.0,
      "rotation": 0.0,
      "children": [
        {
          "id": "furn-table-01",
          "type": "furniture",
          "category": "desk_quad",
          "x": 200.0,
          "y": 150.0,
          "z": 2,
          "width": 400.0,
          "height": 240.0,
          "rotation": 0.0,
          "path": "M 0 0 L 400 0 L 400 240 L 0 240 Z",
          "fill": "#F3F4F6",
          "stroke": "#4B5563",
          "strokeWidth": 2.0
        },
        {
          "id": "dev-ws-01",
          "type": "device",
          "name": "ENG-LAB-WS01",
          "x": 230.0,
          "y": 170.0,
          "z": 10,
          "width": 64.0,
          "height": 64.0,
          "rotation": 0.0,
          "icon": "desktop-pc",
          "showNameOnLayout": true,
          "deviceData": {
            "deviceId": 101,
            "displayName": "ENG-LAB-WS01",
            "primaryIp": "192.168.10.41",
            "macAddress": "00:1A:2B:3C:4D:01"
          }
        },
        {
          "id": "dev-ws-02",
          "type": "device",
          "name": "ENG-LAB-WS02",
          "x": 350.0,
          "y": 170.0,
          "z": 11,
          "width": 64.0,
          "height": 64.0,
          "rotation": 0.0,
          "icon": "desktop-pc",
          "showNameOnLayout": true,
          "deviceData": {
            "deviceId": 102,
            "displayName": "ENG-LAB-WS02",
            "primaryIp": "192.168.10.42",
            "macAddress": "00:1A:2B:3C:4D:02"
          }
        }
      ]
    },
    {
      "id": "shape-donut-pillar-01",
      "type": "shape",
      "name": "Structural Column Alpha",
      "shapeType": "donut",
      "x": 800.0,
      "y": 400.0,
      "z": 3,
      "width": 120.0,
      "height": 120.0,
      "rotation": 0.0,
      "innerRadius": 30.0,
      "outerRadius": 60.0,
      "path": "M 860 340 A 60 60 0 1 0 860 460 A 60 60 0 1 0 860 340 M 860 370 A 30 30 0 1 1 860 430 A 30 30 0 1 1 860 370 Z",
      "fill": "#9CA3AF",
      "stroke": "#374151",
      "strokeWidth": 2.0
    },
    {
      "id": "shape-rounded-tri-01",
      "type": "shape",
      "name": "Warning Floor Marking",
      "shapeType": "rounded_corner_triangle",
      "x": 1000.0,
      "y": 500.0,
      "z": 4,
      "width": 100.0,
      "height": 90.0,
      "rotation": 0.0,
      "cornerRadius": 12.0,
      "path": "M 50 12 Q 50 0 60 6 L 94 65 Q 100 75 90 75 L 10 75 Q 0 75 6 65 Z",
      "fill": "#FEF08A",
      "stroke": "#CA8A04",
      "strokeWidth": 2.5
    },
    {
      "id": "shape-bezier-wall-01",
      "type": "shape",
      "name": "Curved Partition Wall",
      "shapeType": "bezier",
      "x": 100.0,
      "y": 800.0,
      "z": 5,
      "width": 400.0,
      "height": 200.0,
      "rotation": 0.0,
      "points": [100.0, 800.0, 220.0, 950.0, 380.0, 700.0, 500.0, 800.0],
      "path": "M 100 800 C 220 950, 380 700, 500 800",
      "fill": "none",
      "stroke": "#1F2937",
      "strokeWidth": 4.0
    }
  ]
}
```

---

## 3. Item Type Specifications

### 3.1 Common Attributes (All Items)
Every item schema extends these base properties:

| Field | Type | Description |
| :--- | :--- | :--- |
| `id` | `string` (UUID) | Unique identifier for the element. |
| `type` | `string` | Discriminator: `"group" \| "device" \| "furniture" \| "shape"`. |
| `x` | `number` | Left position in canvas world coordinates. |
| `y` | `number` | Top position in canvas world coordinates. |
| `z` | `integer` | Stacking order / layer depth ($0 = \text{bottom}$, higher numbers render on top). |
| `width` | `number` | Bounding box width. |
| `height` | `number` | Bounding box height. |
| `rotation` | `number` | Rotation angle in degrees ($0^\circ - 360^\circ$). |

---

### 3.2 `group` Item Type
* **Keyboard Shortcuts**: `Ctrl + G` (Group), `Ctrl + Shift + G` (Ungroup).
* **Canvas Display**: Name is **never** displayed on the canvas. Boundary box is only visible when selected.
* **Schema Contract**:
  ```typescript
  interface GroupItem extends BaseItem {
    type: "group";
    name: string;             // Configurable in Properties panel
    children: LayoutItem[];   // Recursive list of nested items
  }
  ```

---

### 3.3 `device` Item Type
* **Canvas Display**: Displays its `name` (and optional IP) **directly below the icon** in bold text. No other item type displays text on the layout.
* **Properties Panel**: Features a searchable autocomplete dropdown populated from `public.devices`.
* **Validation**:
  - Missing IP: continuous amber warning (`⚠️`).
  - Duplicate IP: high-priority red error state (`⛔`).
* **Schema Contract**:
  ```typescript
  interface DeviceItem extends BaseItem {
    type: "device";
    name: string;               // Rendered below the icon on canvas
    icon: string;               // e.g. "desktop-pc", "laptop", "server"
    showNameOnLayout: true;     // Explicit flag for web renderers
    deviceData: {
      deviceId: number;         // public.devices.device_id
      displayName: string;      // public.devices.display_name
      primaryIp: string;        // public.devices.primary_ip
      macAddress?: string;      // public.devices.mac_address
    };
  }
  ```

---

### 3.4 `shape` Item Type & Taxonomy
Every shape has a descriptive `name` in the schema/properties panel (e.g. *"Meeting Area Circle"* or *"Server Room Pillar"*), but its name is not rendered on the layout.

```typescript
type ShapeType = 
  | "circle"
  | "rectangle"
  | "rounded_rectangle"
  | "triangle"
  | "rounded_corner_triangle"
  | "semi_circle"
  | "line"
  | "bezier"
  | "donut";

interface ShapeItem extends BaseItem {
  type: "shape";
  name: string;                 // Configurable in properties, not on layout
  shapeType: ShapeType;
  path: string;                 // Standard SVG 'd' path string for web renderers
  fill: string;                 // Hex color (#RRGGBB, #AARRGGBB, or 'none')
  stroke: string;               // Stroke color
  strokeWidth: number;          // Stroke thickness in pixels
  cornerRadius?: number;        // For rounded_rectangle & rounded_corner_triangle
  innerRadius?: number;         // For donut
  outerRadius?: number;         // For donut
  points?: number[];            // Coordinates for line [x1,y1,x2,y2] or bezier [x0,y0,cp1x,cp1y,cp2x,cp2y,x1,y1]
}
```

#### Detailed Shape Geometry Definitions:

| Shape Type | Key Properties | Interactive Canvas Handle | SVG Path Generator Formula |
| :--- | :--- | :--- | :--- |
| **`circle`** | $W = H = 2R$ | Normal 8 resize handles | `M cx-r,cy A r,r 0 1,0 cx+r,cy A r,r 0 1,0 cx-r,cy Z` |
| **`rectangle`** | Width, Height | Normal 8 resize handles | `M x,y H x+w V y+h H x Z` |
| **`rounded_rectangle`** | Width, Height, `cornerRadius` | **Corner Radius Handle** (circular yellow handle inside top-left corner) | `M x+r,y H x+w-r A r,r 0 0 1 x+w,y+r ... Z` |
| **`triangle`** | Width, Height | Normal 8 resize handles | `M cx,y L x+w,y+h L x,y+h Z` |
| **`rounded_corner_triangle`**| Width, Height, `cornerRadius` | **Corner Radius Handle** (drags inward to round all 3 vertices) | Chamfered/Arc-blended vertex path |
| **`semi_circle`** | Width, Height ($H = W/2$) | Normal 8 resize handles | `M x,y+h A w/2,w/2 0 0,1 x+w,y+h Z` |
| **`line`** | `points`: $[X_1, Y_1, X_2, Y_2]$ | 2 endpoint thumbs | `M x1,y1 L x2,y2` |
| **`bezier`** | `points`: $[X_0, Y_0, C_{1x}, C_{1y}, C_{2x}, C_{2y}, X_1, Y_1]$ | 2 endpoint thumbs + 2 tangent handles | `M x0,y0 C c1x,c1y, c2x,c2y, x1,y1` |
| **`donut`** | `outerRadius`, `innerRadius` | **Inner Radius Handle** (drags inward/outward to change hole size) | Concentric compound path (`outer_arc M inner_arc`) |

---

### 3.5 Interactive Canvas Handles for Corner Radius & Donuts

To give the designer a genuine CAD/Illustrator feel:
1. **Corner Radius Control**:
   * When a `rounded_rectangle` or `rounded_corner_triangle` is selected, an amber control point appears inside the corner.
   * Dragging the handle inward increases the `cornerRadius`; dragging outward sharpens it to $0$.
   * The numeric `CornerRadius` in the Property Inspector updates simultaneously.
2. **Donut Inner Radius Control**:
   * When a `donut` is selected, an amber control handle rests on the inner circumference.
   * Dragging it radially alters the `innerRadius`.

---

## 4. Downstream Web Developer Integration (Vanilla JS Example)

Any web developer can render this exported layout in an HTML5 `<svg>` element with fewer than 70 lines of code:

```javascript
function renderLayout(containerSvg, layoutJson) {
  containerSvg.setAttribute('viewBox', `0 0 ${layoutJson.canvas.width} ${layoutJson.canvas.height}`);
  
  // Flatten recursive items into an array sorted by Z-Index
  function flattenItems(items) {
    let result = [];
    for (const item of items) {
      if (item.type === 'group' && item.children) {
        result.push(...flattenItems(item.children));
      } else {
        result.push(item);
      }
    }
    return result.sort((a, b) => (a.z || 0) - (b.z || 0));
  }

  const renderQueue = flattenItems(layoutJson.items);

  for (const item of renderQueue) {
    const g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    g.setAttribute('transform', `translate(${item.x}, ${item.y}) rotate(${item.rotation || 0})`);

    // 1. Render Vector Path (Shapes & Furniture)
    if (item.path) {
      const pathEl = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      pathEl.setAttribute('d', item.path);
      pathEl.setAttribute('fill', item.fill || 'none');
      pathEl.setAttribute('stroke', item.stroke || '#000');
      pathEl.setAttribute('stroke-width', item.strokeWidth || 1);
      g.appendChild(pathEl);
    }

    // 2. Render Device Icons and Label (ONLY devices show names)
    if (item.type === 'device') {
      const icon = document.createElementNS('http://www.w3.org/2000/svg', 'use');
      icon.setAttribute('href', `#icon-${item.icon}`);
      g.appendChild(icon);

      // Render name strictly below the device
      const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
      text.textContent = item.name;
      text.setAttribute('x', item.width / 2);
      text.setAttribute('y', item.height + 16);
      text.setAttribute('text-anchor', 'middle');
      text.setAttribute('font-family', 'Segoe UI, sans-serif');
      text.setAttribute('font-size', '12px');
      text.setAttribute('fill', '#1F2937');
      g.appendChild(text);
    }

    containerSvg.appendChild(g);
  }
}
```

---
*End of Schema Contract.*
