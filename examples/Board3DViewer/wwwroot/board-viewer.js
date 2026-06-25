// Client-side WebGL viewer for the server-rendered glTF boards. Loaded as an ES module by the Blazor
// component via JS interop; three.js itself comes from the CDN import map declared in App.razor.
//
// Rendering is tuned to show the board's true colours (the glTF stores physically-correct linear PBR
// materials):
//   • ACES filmic tone mapping + sRGB output, so dark IC bodies stay dark and greens/golds stay
//     saturated instead of washing out under flat ambient light;
//   • an image-based environment (a neutral room), which is what makes METALLIC surfaces — the ENIG
//     gold finish, connector shells, component leads — read as bright and shiny. A metal with no
//     environment to reflect renders nearly black, which is why a plain-light setup looked dull.
// A single soft key light adds directionality on top of the environment.

import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

let renderer, scene, camera, controls, current, canvasEl, headlight;

export function init(canvas) {
    canvasEl = canvas;
    // A logarithmic depth buffer spreads precision across the whole near..far range instead of crowding it
    // near the camera, so the thin, near-coplanar PCB layers (copper / mask / silkscreen, microns apart on a
    // board that may be hundreds of mm across) don't z-fight — the mask bleeding through the silk.
    renderer = new THREE.WebGLRenderer({ canvas, antialias: true, logarithmicDepthBuffer: true });
    renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.0;
    renderer.outputColorSpace = THREE.SRGBColorSpace;

    scene = new THREE.Scene();
    scene.background = new THREE.Color(0x6a6e73); // neutral studio grey, like a mechanical CAD viewer

    // Near/far in metres. The board is small and never sensibly viewed from closer than a few mm, so a 5 mm
    // near plane (was 0.5 µm) and a 50 m far keep the depth range tight — far better precision than the old
    // 200,000:1 range, on top of the logarithmic depth buffer.
    camera = new THREE.PerspectiveCamera(35, 1, 0.005, 50);

    controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;

    // Image-based lighting: a neutral room, pre-filtered for PBR. Provides soft ambient for the
    // dielectric board/components AND the reflections that make the metals look like metal. (The
    // materials are physically-correct linear PBR, so no extra exposure trickery is needed — dark
    // bodies stay dark because their albedo is genuinely low.)
    const pmrem = new THREE.PMREMGenerator(renderer);
    scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;

    // A headlight that tracks the camera, so whichever side you've rotated toward is lit — including
    // the board's underside. A fixed overhead key leaves the bottom black: fine for a hero render,
    // useless for inspecting a board in a CAD viewer. The environment still supplies the reflections.
    headlight = new THREE.DirectionalLight(0xffffff, 1.4);
    scene.add(headlight);
    scene.add(headlight.target);

    resize();
    addEventListener('resize', resize);
    renderer.setAnimationLoop(() => {
        controls.update();
        // Keep the key light at the camera, aimed at what the camera looks at.
        headlight.position.copy(camera.position);
        headlight.target.position.copy(controls.target);
        headlight.target.updateMatrixWorld();
        renderer.render(scene, camera);
    });

    // Exposed for poking at the scene from the browser console (and for tests).
    window.__viewer = { THREE, scene, camera, controls, renderer };
}

export async function loadModel(url) {
    const gltf = await new GLTFLoader().loadAsync(url);
    if (current) { scene.remove(current); dispose(current); }
    current = gltf.scene;
    scene.add(current);
    frame(current);
    return featureGroups();
}

export function setLayerVisible(group, visible) {
    for (const o of featureObjects())
        if (groupOf(o) === group) o.visible = visible;
}

// Every emitted feature/component node carries a stable `group` in its glTF extras (→ userData), so the
// toggles can find and flip them wherever they sit in the tree — crucially, INSIDE a panel's per-cell
// instance transforms, not just as direct children of "Board". (Node names can't be used: glTF loaders
// uniquify duplicate names across instances, e.g. "Substrate", "Substrate_1", …)
function featureObjects() {
    if (!current) return [];
    const board = current.getObjectByName('Board') || current;
    const out = [];
    board.traverse(o => { if (o.userData && o.userData.group) out.push(o); });
    return out;
}

function groupOf(o) {
    return o.userData.group;
}

function featureGroups() {
    const order = ['Substrate', 'Copper', 'SolderMask', 'Silkscreen', 'Drills', 'Components'];
    const seen = new Set(), groups = [];
    for (const o of featureObjects()) {
        const g = groupOf(o);
        if (!seen.has(g)) { seen.add(g); groups.push(g); }
    }
    const rank = (g) => { const i = order.findIndex(o => g.startsWith(o)); return i < 0 ? 99 : i; };
    return groups.sort((a, b) => rank(a) - rank(b) || a.localeCompare(b));
}

function frame(obj) {
    const box = new THREE.Box3().setFromObject(obj);
    const size = box.getSize(new THREE.Vector3());
    const center = box.getCenter(new THREE.Vector3());
    const radius = Math.max(size.x, size.y, size.z) * 0.5 || 0.05;
    const dist = radius / Math.tan((camera.fov * Math.PI / 180) / 2) * 1.7;
    camera.position.set(center.x + dist * 0.5, center.y + dist * 0.6, center.z + dist * 0.7);
    controls.target.copy(center);
    controls.update();
}

function resize() {
    const w = canvasEl.clientWidth || canvasEl.parentElement.clientWidth || innerWidth;
    const h = canvasEl.clientHeight || canvasEl.parentElement.clientHeight || innerHeight;
    renderer.setSize(w, h, false);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
}

function dispose(obj) {
    obj.traverse(o => {
        if (o.geometry) o.geometry.dispose();
        if (o.material) (Array.isArray(o.material) ? o.material : [o.material]).forEach(m => m.dispose());
    });
}
