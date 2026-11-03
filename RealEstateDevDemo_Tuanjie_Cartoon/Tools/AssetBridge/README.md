# AssetBridge：Three.js 资产模块转 GLB

普通试玩和重新运行 `build_cartoon_assets.py` 都不需要本工具；当前 20 个卡通模型也不经过 GLB。只有你要额外把新的 Three.js 设计资产转换为 Blender 可导入 GLB 时才使用。

## 输入约定

资产源目录必须包含 `asset.mjs`，并导出 `createAsset(THREE, options)`。转换器保留几何、对象层级、根变换和材质颜色；为适配当前低模 Demo，会移除纹理引用。

## 使用

在本目录执行：

```bash
npm ci
node export-glb.mjs <资产source目录> <输出.glb>
```

示例：

```bash
node export-glb.mjs /path/to/asset/source ../Blender/Imported/NewAsset.glb
```

如果要把额外 GLB 加入卡通工程，需要在 Blender 中导入后按 `SM_*` 根节点重新组织并导出 FBX，再放入 `Assets/Resources/Models`。先检查世界包围盒和根变换，再决定缩放；不要仅凭来源坐标轴额外旋转 90°。

`node_modules` 不随交付包保留，`npm ci` 会按锁文件恢复 Three.js `0.180.0`。
