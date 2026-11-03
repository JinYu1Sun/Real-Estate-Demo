using System;
using System.IO;
using RealEstateDemo.Core;
using UnityEngine;

namespace RealEstateDemo.Unity
{
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "demo_slot_0.json");
        public static bool HasSave => File.Exists(SavePath);
        public static void Save(GameState state)
        {
            string json = JsonUtility.ToJson(state, true);
            string temp = SavePath + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(temp, SavePath);
        }
        public static GameState Load()
        {
            if (!HasSave) throw new FileNotFoundException("没有找到单槽存档", SavePath);
            var state = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
            if (state == null || state.Version != 1) throw new InvalidDataException("存档版本不兼容");
            return state;
        }
        public static GameConfig LoadConfig()
        {
            var asset = Resources.Load<TextAsset>("Config/game_config");
            return asset == null ? GameConfig.Default() : JsonUtility.FromJson<GameConfig>(asset.text);
        }
    }
}
