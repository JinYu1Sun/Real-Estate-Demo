using UnityEngine;

namespace RealEstateDemo.Unity
{
    public static class ModelLibrary
    {
        public static GameObject Spawn(string resourceName, Transform parent, Vector3 position, float scale = 1f)
        {
            var prefab = Resources.Load<GameObject>("Models/" + resourceName);

            // 外层节点只承担游戏世界的平移、缩放和 Y 轴朝向。
            // FBX 根节点通常带 X = -90° 的 Blender 轴向转换；直接旋转实例根会覆盖它，使整栋楼躺倒。
            var controller = new GameObject(resourceName + "_Controller");
            controller.transform.SetParent(parent, false);
            controller.transform.localPosition = position;
            controller.transform.localRotation = Quaternion.identity;
            controller.transform.localScale = Vector3.one * scale;

            GameObject model;
            if (prefab != null) model = Object.Instantiate(prefab);
            else
            {
                // 资产丢失时仍能测试经营逻辑，并在 Console 给出明确原因。
                Debug.LogWarning("缺少模型，使用占位体：" + resourceName);
                model = GameObject.CreatePrimitive(PrimitiveType.Cube);
                model.name = resourceName + "_Fallback";
                model.transform.localScale = new Vector3(6, 2, 6);
            }
            // worldPositionStays = false：保留导入器生成的局部旋转与缩放，而不是重置 FBX 根变换。
            model.transform.SetParent(controller.transform, false);
            ApplyToonShader(model);
            return controller;
        }

        private static void ApplyToonShader(GameObject instance)
        {
            var shader = Resources.Load<Shader>("Shaders/ToonLit");
            if (shader == null || !shader.isSupported)
            {
                // 导入器生成的 Built-in Standard 材质本身就是安全回退，不能强行换成不支持的 Shader。
                Debug.LogWarning("卡通漫反射 Shader 不可用，保留模型导入材质：" + instance.name);
                return;
            }
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    // 切换 Shader 前显式保存颜色与贴图，再写回同名属性，避免导入器差异造成丢色。
                    Color color = Color.white;
                    if (material.HasProperty("_Color")) color = material.GetColor("_Color");
                    else if (material.HasProperty("_BaseColor")) color = material.GetColor("_BaseColor");
                    Texture texture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                    material.shader = shader;
                    material.SetColor("_Color", color);
                    material.SetTexture("_MainTex", texture);
                }
                renderer.materials = materials;
            }
        }
    }
}
