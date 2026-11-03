# 资产、工具与来源说明

## 当前卡通分支的原创 / 本工程生成内容

- 经营内核、Unity 表现层、编辑器工具、测试与教程：为本 Demo 编写的 C# 代码。
- `Tools/Blender/build_cartoon_assets.py`：当前唯一卡通资产重建入口。
- 五阶段建筑、泵车、运输车、挖掘机、塔吊、建材厂、施工队办公室、学校、医院、总部、树木、岩石、路灯、花坛、长椅和别墅：共 20 个模型，均由该脚本通过 `bpy` 参数化生成，不再复用旧版设计库 GLB。建筑、机械、设施和植物均使用真实立体几何。
- `Tools/Blender/Output/CartoonRealEstateAssets.blend`：上述模型的可编辑源场景。
- 地形、实心地基、道路、格网和装饰摆放：Unity C# 运行时生成。
- `Docs/Images/卡通资产总览.png` 与 `精细模型近景.png`：由本工程 Blender 源场景实际渲染。
- `Assets/Resources/Art/CityLogo.png`：为本工程生成的透明背景玩具城市品牌图标，不含第三方游戏标识。
- `ToonLit`、地形 Shader、圆角 UI Sprite 与界面布局：本工程代码生成或编写。

本版是原创程序化低模卡通 Demo，没有复制具体商业游戏的模型、UI 或贴图。

## 字体

- 文件：`Assets/Resources/Fonts/NotoSansSC.otf` 与 `NotoSansSC-Bold.otf`。
- 来源：Noto CJK / Noto Sans CJK Simplified Chinese，上游项目 `googlefonts/noto-cjk`。
- 用途：正文与粗体分离，避免 uGUI 中文方框、标题虚弱和按钮难以阅读。
- 许可：SIL Open Font License 1.1；版权与许可文本见 `Docs/Licenses/NotoSansCJK-copyright.txt`。

## 开发工具

- 团结引擎 / Unity 与 uGUI：遵循用户安装版本对应的许可。
- Blender / bpy：用于建模、FBX 导出和渲染；不作为游戏运行时依赖。
- Three.js `0.180.0`：仅可选 AssetBridge 转换脚本使用，其许可随 npm 依赖提供；交付包不包含 `node_modules`，可用 `npm ci` 恢复。当前卡通模型不经过该转换器。
- .NET 8：仅核心测试使用。

旧 Unity 6 与旧团结分支可能仍保留各自历史资产说明；请以各分支内的 CREDITS 为准，不混用到当前卡通工程。
