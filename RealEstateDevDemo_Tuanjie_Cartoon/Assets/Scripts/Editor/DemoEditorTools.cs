using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealEstateDemo.Editor
{
    public class DemoEditorTools : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Resources/Models")) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f; importer.importAnimation = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshVertices = true;
        }

        private void OnPreprocessAsset()
        {
            var font = assetImporter as TrueTypeFontImporter;
            if (font == null || !assetPath.Contains("Resources/Fonts")) return;
            font.fontTextureCase = FontTextureCase.Dynamic;
            font.fontRenderingMode = FontRenderingMode.Smooth;
            font.includeFontData = true;
            font.fontSize = 32;
        }

        [MenuItem("房地产 Demo/一键生成主场景")]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            AssetDatabase.Refresh();
            Debug.Log("主场景已生成。直接点击 Play，C# 会自动创建地图、镜头、UI 与经营内核。");
        }

        [MenuItem("房地产 Demo/检查 Demo 资源")]
        public static void ValidateResources()
        {
            string[] required = { "SM_Building_Stage0", "SM_Building_Stage1", "SM_Building_Stage2", "SM_Building_Stage3",
                "SM_Building_Stage4", "SM_TowerCrane", "SM_Excavator", "SM_PumpTruck", "SM_TransportTruck", "SM_Supplier",
                "SM_TeamOffice", "SM_School", "SM_Hospital", "SM_HQ", "SM_Tree", "SM_Rock",
                "SM_Streetlight", "SM_FlowerBed", "SM_Bench", "SM_Villa" };
            foreach (var name in required)
            {
                var model = Resources.Load<GameObject>("Models/" + name);
                if (model == null) throw new FileNotFoundException("Unity 尚未导入模型：" + name);
            }
            if (Resources.Load<TextAsset>("Config/game_config") == null) throw new FileNotFoundException("缺少数值配置");
            if (Resources.Load<Font>("Fonts/NotoSansSC") == null) throw new FileNotFoundException("缺少中文正文字体");
            if (Resources.Load<Font>("Fonts/NotoSansSC-Bold") == null) throw new FileNotFoundException("缺少中文粗体字体");
            if (Resources.Load<Texture2D>("Art/CityLogo") == null) throw new FileNotFoundException("缺少卡通城市图标");
            var terrainShader = Resources.Load<Shader>("Shaders/GridTerrain");
            var toonShader = Resources.Load<Shader>("Shaders/ToonLit");
            if (terrainShader == null) throw new FileNotFoundException("缺少地形 Shader");
            if (toonShader == null) throw new FileNotFoundException("缺少卡通漫反射 Shader");
            if (!terrainShader.isSupported || !toonShader.isSupported)
                throw new System.Exception("当前图形后端不支持地形或卡通漫反射 Shader，请查看 Console 的 Shader 编译信息。");
            Debug.Log("资源检查通过：20 个精细卡通模型、中文双字体、城市图标、数值配置与两个 Lambert Shader 均可加载并受当前图形后端支持。");
        }

        [MenuItem("房地产 Demo/打包 Windows x64")]
        public static void BuildWindows()
        {
            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            Directory.CreateDirectory("Builds/Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = "Builds/Windows/RealEstateDemo.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows 打包失败，请查看 Console");
            Debug.Log("Windows Demo 打包完成：" + options.locationPathName);
        }
    }
}
