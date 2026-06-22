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
    renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.0;
    renderer.outputColorSpace = THREE.SRGBColorSpace;

    scene = new THREE.Scene();
    scene.background = new THREE.Color(0x6a6e73); // neutral studio grey, like a mechanical CAD viewer

    camera = new THREE.PerspectiveCamera(35, 1, 0.0005, 100);

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
    return featureNodes().map(n => n.name);
}

export function setLayerVisible(name, visible) {
    const node = featureNodes().find(n => n.name === name);
    if (node) node.visible = visible;
}

// The toggleable feature nodes are the children of the single "Board" root node.
function featureNodes() {
    if (!current) return [];
    const board = current.getObjectByName('Board') || current;
    return board.children.filter(c => c.name);
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
