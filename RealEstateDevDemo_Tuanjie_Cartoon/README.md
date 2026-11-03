# 筑梦地产 · 团结 1.10.4 立体轴向修复版

这是房地产开发经营 Demo 的 **Unity 2022.3 LTS / 团结引擎兼容分支**，工程版本标记为团结引擎 1.10.4 原生格式（`2022.3.62t16`，含 `m_TuanjieEditorVersion: 1.10.4`；API 基线为 Unity 2022.3 LTS）。在保留原有经营闭环、存档结构和 C# 核心的基础上，重做了界面、地图、建筑与施工资产。

> 上一运行修复版经用户在真实团结 1.10.4 中复测，按钮标题和红色地表已恢复，但又明确暴露出建筑基座竖起、楼体像躺倒的板。本版已定位并修复 FBX 根旋转被覆盖的问题，同时调整曝光、道路标线和镜头。当前环境没有真实团结引擎编辑器，不能把静态检查冒充为“立体轴向修复版实机验收通过”。首次导入后的 Play、界面实际显示和打包仍需在你的电脑上确认。

## 这版改了什么

- **修复楼体躺倒与基座竖板**：FBX 根节点的 X = −90° 轴向转换曾被场景 Y 旋转覆盖。现在每个模型都有独立外层控制节点，外层负责位置、比例和朝向，内层保留导入旋转；挖机逐帧动画也不会再破坏轴向。
- **默认看到建筑正立面**：镜头改为能同时看到门窗正面和侧面的等距方向，初始视野由 42 收敛到 36，聚焦地块为 26。
- **降低过曝与道路噪声**：降低方向光、环境光和地形过亮色值；道路标线由每隔一格改为每隔三格，保留清晰道路结构。
- **修复全部按钮与静态标题无字**：`Label` 现在会真正写入 `Text.text`，按钮直接传入实际标题；所有 UI 文本按各自内容、字号和字体预加载动态字形。
- **修复高风险渲染路径**：地形改用显式 `COLOR` 语义和标准 Lambert；模型移除自定义光照曲线与色向外推，切换材质时显式保留 `_Color` 与 `_MainTex`，并删除重复地形 Shader。
- **1080P 高清卡通 UI**：以 1920×1080 为原生参考分辨率，启用像素对齐与 Smooth 动态字体；正文和粗体分离，加大字号、按钮和留白，并保留滚动面板锚定与长文本溢出修复。
- **更鲜活的立体地图**：平坦地表搭配实心土壤地基、青蓝水面、分层道路、道路标线、立体地块角柱与中文招牌；加入路灯、花坛、长椅、别墅、树木和岩石。
- **精细玩具感建筑**：20 个 Blender 程序化模型；除地面外均使用真实厚度网格，包含实体坡屋顶、窗框玻璃、阳台栏杆、屋顶设备、施工桁架、液压杆、履带和车辆细节。
- **完整品牌资源**：透明背景玩具城市图标，以及 `NotoSansSC` 正文和 `NotoSansSC-Bold` 粗体字体。

## 用团结 1.10 打开

1. 完整解压，不要只解压 `Assets`。
2. 用团结 Hub / 团结引擎 1.10 打开 `RealEstateDevDemo_Tuanjie_Cartoon` 整个目录。
3. 等待首次脚本编译、字体和 FBX 导入完成。
4. 打开 `Assets/Scenes/Main.unity`。
5. 点击菜单 **房地产 Demo → 检查 Demo 资源**。
6. 点击 **Play**。空场景会自动生成地图、镜头、灯光和 UI。
7. 先将 Game 视图固定为 **1920×1080、100% 显示比例**检查字体和布局，再测试 1600×900 及其他分辨率。

如团结导入出现异常，可用基于 Unity 2022.3 LTS 的编辑器打开同一工程排查。试玩不需要先安装 Blender 或 .NET。

## 基本操作

- 左侧依次完成：公开招标 → 勘测设计 → 组织施工 → 70% 预售 → 交付结算。
- `W/A/S/D` 平移，`Q/E` 旋转，滚轮缩放，鼠标中键拖动。
- 顶部可暂停、切换 `1× / 4× / 12×`、保存、读取和显示格网。
- 存档为单槽 JSON：`Application.persistentDataPath/demo_slot_0.json`。

## 目录

```text
Assets/
  Resources/
    Art/CityLogo.png             # 透明卡通城市图标
    Config/game_config.json      # 可调经营数值
    Fonts/NotoSansSC.otf         # 中文正文
    Fonts/NotoSansSC-Bold.otf    # 中文粗体标题与按钮
    Models/SM_*.fbx              # 20 个精细立体卡通模型
    Shaders/                    # 地形与卡通光照
  Scenes/Main.unity              # 运行时自动组装的空主场景
  Scripts/Core/                 # 不依赖 Unity 的经营内核
  Scripts/Unity/                # UI、地图、镜头、施工动画、存档
  Scripts/Editor/               # 资源检查、导入和 Windows 打包菜单
Docs/                            # 教程、架构、视觉规范和验证边界
Tests/                           # .NET 8 核心自动化测试
Tools/Blender/
  build_cartoon_assets.py        # 当前卡通资产唯一重建入口
  Output/CartoonRealEstateAssets.blend
Tools/AssetBridge/               # 可选 GLB 转换工具，试玩不需要
```

## 重新生成卡通资产

在工程根目录运行：

```bash
blender --background --python Tools/Blender/build_cartoon_assets.py
```

脚本会重新生成 20 个 FBX、保存可编辑 `.blend`，并渲染 `Docs/Images/卡通资产总览.png` 与 `Docs/Images/精细模型近景.png`。不要在未备份的其他 Blender 场景中直接运行，因为脚本会清空当前场景。

## 核心测试

```bash
dotnet run --project Tests/CoreSmokeTests.csproj -c Release
```

本次实际结果：`ALL GREEN: 64 assertions`。全量 Core / Unity / Editor C# 静态编译为 0 警告、0 错误；20 个 FBX 均已回导检查，根节点、非空网格、有限顶点与材质全部通过。

## 文档入口

- `Docs/手把手教程.md`：导入、试玩、改数值、重建模型、测试、打包与排错。
- `Docs/卡通视觉与界面规范.md`：字体、布局、配色、地图、模型和修改入口。
- `Docs/代码架构.md`：分层、模块职责、调用顺序和扩展方式。
- `Docs/代码架构图.mmd`：完整架构图源文件，附 SVG / PNG。
- `Docs/核心经营循环.mmd`：经营阶段与施工状态图。
- `Docs/Blender到Unity资产流水线.mmd`：模型生产与导入图。
- `Docs/团结引擎迁移说明.md`：兼容调整与本机检查清单。
- `Docs/验证与边界.md`：已经实测与尚未实测的明确清单。
- `CREDITS.md`：字体、生成资产和工具来源说明。
