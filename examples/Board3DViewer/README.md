# Board3DViewer

An interactive **3D** viewer for Altium PCB documents, built as an **ASP.NET Blazor Server**
application. Pick one of the bundled boards and the server renders it to a glTF model with
[`OriginalCircuit.Altium.Rendering.Gltf`](../../src/OriginalCircuit.Altium.Rendering.Gltf) and
streams it to a small WebGL scene in the browser.

```
dotnet run --project examples/Board3DViewer
```

Then open the printed URL (e.g. `http://localhost:5187`), choose a board from the dropdown, and
toggle individual features — the substrate, each copper layer, the solder mask, silkscreen, drills,
and the placed component 3D bodies — using the checkboxes. Drag to rotate, scroll to zoom,
right-drag to pan.

## How it works

- **Pure .NET on the server.** `BoardLibrary` finds the repo's `TestData/*.PcbDoc` boards and
  renders each one to a binary glTF (`.glb`) on demand with `GltfRenderer`, caching the bytes. The
  `GET /board.glb?name=…` minimal-API endpoint streams the result.
- **Blazor owns the UI.** `Components/Pages/Home.razor` is an interactive server component: the
  model picker and the per-feature layer-toggle panel are Blazor state, driving the WebGL scene
  through JS interop (`init` / `loadModel` / `setLayerVisible`).
- **three.js is the only client-side piece**, loaded straight from a CDN via the import map in
  `Components/App.razor`. There is **no Node, npm, or Python** in the build or at runtime — the
  whole app is built and run by `dotnet`.

## Rendering notes

The viewer (`wwwroot/board-viewer.js`) is set up to show the board's true colours, which matters
because the glTF stores physically-correct linear PBR materials:

- **ACES filmic tone mapping + sRGB output**, so dark IC bodies stay dark and the greens/golds stay
  saturated instead of washing out under flat ambient light.
- An **image-based environment** (a neutral room, pre-filtered for PBR). This is what makes the
  *metallic* surfaces — the ENIG gold finish, connector shells, component leads — read as bright and
  shiny: a metal with no environment to reflect renders nearly black, which is why a plain
  directional-light setup looks dull.

For a non-interactive, file-based export of the same model (for Blender, Windows 3D Viewer, etc.),
see [ExportBoardGltf](../ExportBoardGltf).
