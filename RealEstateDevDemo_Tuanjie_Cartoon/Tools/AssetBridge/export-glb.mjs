// 将设计资产的 Three.js 模块无损转换为 Blender 可导入的 GLB。
// 用法：node export-glb.mjs <资产source目录> <输出.glb>
import * as THREE from 'three';
import { GLTFExporter } from 'three/addons/exporters/GLTFExporter.js';
import fs from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

// Node 没有浏览器的 FileReader；GLTFExporter 导出二进制时需要它。
globalThis.FileReader = class {
  readAsArrayBuffer(blob) {
    blob.arrayBuffer().then((buffer) => {
      this.result = buffer;
      this.onloadend?.();
    });
  }
  readAsDataURL(blob) {
    blob.arrayBuffer().then((buffer) => {
      this.result = `data:${blob.type};base64,${Buffer.from(buffer).toString('base64')}`;
      this.onloadend?.();
    });
  }
};

const assetDir = process.argv[2];
const output = process.argv[3];
if (!assetDir || !output) throw new Error('用法：node export-glb.mjs <资产source目录> <输出.glb>');

// 资产加载器面向浏览器；在 Node 中为 file:// 增加只读文件支持。
const originalFetch = globalThis.fetch;
globalThis.fetch = async (input, init) => {
  const url = new URL(input instanceof Request ? input.url : String(input));
  if (url.protocol === 'file:') return new Response(await fs.readFile(url));
  return originalFetch(input, init);
};

const { createAsset } = await import(pathToFileURL(path.resolve(assetDir, 'asset.mjs')).href);
const emptyTexture = async () => new THREE.Texture();
const root = await createAsset(THREE, { states: 'lazy', sharedResources: false, loadTexture: emptyTexture });
// Unity Demo 使用纯色低模材质；移除 Node 无法解码的纹理，保留几何、层级与材质色。
root.traverse((object) => {
  if (!object.material) return;
  const materials = Array.isArray(object.material) ? object.material : [object.material];
  for (const material of materials) {
    for (const key of Object.keys(material)) if (material[key]?.isTexture) material[key] = null;
  }
});
root.updateMatrixWorld(true);
const exporter = new GLTFExporter();
const result = await exporter.parseAsync(root, { binary: true, onlyVisible: true });
await fs.mkdir(path.dirname(output), { recursive: true });
await fs.writeFile(output, Buffer.from(result));
console.log(`GLB exported: ${output} (${result.byteLength} bytes)`);
await root.dispose();
