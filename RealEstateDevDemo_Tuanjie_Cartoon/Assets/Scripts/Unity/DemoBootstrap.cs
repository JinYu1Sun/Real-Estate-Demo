using System;
using RealEstateDemo.Core;
using UnityEngine;

namespace RealEstateDemo.Unity
{
    public class DemoBootstrap : MonoBehaviour
    {
        public GameCore Game { get; private set; }
        public MapView MapView { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public bool Paused { get; private set; }
        public int Speed { get; private set; } = 4;
        private GameHUD hud; private ConstructionVisual constructionVisual;
        private float accumulator; private float uiClock;
        private const float SecondsPerDay = 0.55f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            // Tuanjie / Unity 2022 LTS 兼容写法：避免依赖 Unity 6 的新对象查找 API。
            if (FindObjectOfType<DemoBootstrap>() == null)
                new GameObject("RealEstateDemo").AddComponent<DemoBootstrap>();
        }

        private void Awake()
        {
            Game = new GameCore(SaveSystem.LoadConfig());
            CreateScene(); RebuildWorld();
        }

        private void CreateScene()
        {
            var cameraObject = new GameObject("Isometric Camera"); cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>(); CameraRig = cameraObject.AddComponent<CameraRig>();
            cameraObject.AddComponent<AudioListener>();
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 0.82f; light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48, -32, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.44f, 0.48f);
            RenderSettings.ambientIntensity = 0.65f;
            RenderSettings.fog = false;
            MapView = new GameObject("Map").AddComponent<MapView>();
            MapView.LotClicked += OnLotClicked;
            constructionVisual = new GameObject("Construction").AddComponent<ConstructionVisual>();
            hud = new GameObject("HUD").AddComponent<GameHUD>(); hud.Build(this);
        }

        private void RebuildWorld()
        {
            MapView.Rebuild(Game); constructionVisual.Bind(Game, MapView);
            if (hud != null) hud.RefreshAll();
        }

        private void OnLotClicked(int lotId)
        {
            if (lotId != 0) { Notify("邀请招标属于后续 P1，本版以 A 地块完成核心循环"); return; }
            if (Game.State.Phase == GamePhase.MapPreview) { Game.StartTender(); hud.RefreshAll(); }
            else CameraRig.Focus(MapView.CellToWorld(Game.Map, Game.State.Lots[lotId].X, Game.State.Lots[lotId].Y));
        }

        private void Update()
        {
            if (!Paused && (Game.State.Phase == GamePhase.Construction || Game.State.Phase == GamePhase.Sales))
            {
                accumulator += Time.deltaTime * Speed;
                while (accumulator >= SecondsPerDay)
                { accumulator -= SecondsPerDay; Game.AdvanceDay(); }
            }
            uiClock += Time.deltaTime;
            if (uiClock >= 0.25f) { uiClock = 0; hud.RefreshAll(); }
        }

        public void Notify(string message)
        {
            Debug.Log(message); hud?.ShowMessage(message); hud?.RefreshAll();
        }
        public void Reroll()
        {
            if (Game.Reroll()) RebuildWorld(); else Notify("当前不能重摇，或三次机会已用完");
        }
        public void Restart(bool sameSeed)
        {
            int seed = sameSeed ? Game.State.Seed : Game.State.Seed + 7919;
            Game.NewGame(seed); Paused = false; accumulator = 0; RebuildWorld();
        }
        public void Save()
        {
            try { SaveSystem.Save(Game.State); Notify("已保存到单槽：" + Application.persistentDataPath); }
            catch (Exception e) { Notify("保存失败：" + e.Message); }
        }
        public void Load()
        {
            try { Game.Restore(SaveSystem.Load(), SaveSystem.LoadConfig()); Paused = false; accumulator = 0; RebuildWorld(); Notify("读档成功"); }
            catch (Exception e) { Notify("读档失败：" + e.Message); }
        }
        public void TogglePause() { Paused = !Paused; hud.RefreshAll(); }
        public void SetSpeed(int speed) { Speed = speed; Paused = false; hud.RefreshAll(); }
    }
}
