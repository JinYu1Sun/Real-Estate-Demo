using System;
using System.Collections.Generic;
using RealEstateDemo.Core;
using UnityEngine;

namespace RealEstateDemo.Unity
{
    public class MapView : MonoBehaviour
    {
        private const float CellSize = 2f;
        private GameObject gridRoot;
        public event Action<int> LotClicked;

        public Vector3 CellToWorld(MapData map, int x, int y, float offset = 0)
        {
            // 经营数据保留地质高度；表现层按需求使用平整地面，水体略下沉。
            float h = map.InBounds(x, y) && IsWater(map.Cell(x, y).Terrain) ? -0.16f : 0f;
            return new Vector3((x - 49.5f) * CellSize, h + offset, (y - 49.5f) * CellSize);
        }

        private static bool IsWater(TerrainType terrain)
        {
            return terrain == TerrainType.Lake || terrain == TerrainType.River;
        }

        public void Rebuild(GameCore game)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            BuildTerrain(game.Map);
            BuildRoads(game.Map);
            BuildGrid(game.Map);
            foreach (var lot in game.State.Lots) BuildLot(game, lot);
            foreach (var entity in game.Map.Entities) BuildEntity(game.Map, entity);
            BuildDecorations(game);
            BuildStreetFurniture(game);
        }

        private void BuildTerrain(MapData map)
        {
            var vertices = new List<Vector3>(40000);
            var triangles = new List<int>(60000);
            var colors = new List<Color>(40000);
            foreach (var cell in map.Cells)
            {
                Color color = TerrainColor(cell.Terrain);
                float variation = 0.97f + ((cell.X * 17 + cell.Y * 31 + map.Seed) % 5) * 0.012f;
                float height = IsWater(cell.Terrain) ? -0.16f : 0f;
                AddQuad(vertices, triangles, colors,
                    (cell.X - 49.5f) * CellSize, height, (cell.Y - 49.5f) * CellSize,
                    CellSize, CellSize, color * variation);
            }
            CreateColoredMesh("Flat toy terrain", vertices, triangles, colors, transform);
            Box("Solid earth foundation", transform, new Vector3(0, -0.82f, 0), new Vector3(202, 1.25f, 202),
                new Color(0.26f, 0.14f, 0.065f));
        }

        private static Color TerrainColor(TerrainType type)
        {
            switch (type)
            {
                case TerrainType.Coast: return new Color(0.82f, 0.62f, 0.29f);
                case TerrainType.Hill: return new Color(0.17f, 0.41f, 0.11f);
                case TerrainType.Lake: return new Color(0.025f, 0.34f, 0.58f);
                case TerrainType.River: return new Color(0.02f, 0.40f, 0.64f);
                default: return new Color(0.23f, 0.50f, 0.13f);
            }
        }

        private void BuildRoads(MapData map)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            var shoulder = new Color(0.76f, 0.68f, 0.50f);
            var asphalt = new Color(0.15f, 0.21f, 0.28f);
            var stripe = new Color(1.00f, 0.86f, 0.30f);
            foreach (var cell in map.Cells)
            {
                if (!cell.IsRoad) continue;
                float x = (cell.X - 49.5f) * CellSize;
                float z = (cell.Y - 49.5f) * CellSize;
                float height = IsWater(cell.Terrain) ? 0.04f : 0.055f;
                AddQuad(vertices, triangles, colors, x, height, z, 2.10f, 2.10f, shoulder);
                AddQuad(vertices, triangles, colors, x, height + 0.018f, z, 1.72f, 1.72f, asphalt);
                if ((cell.X + cell.Y) % 4 != 0) continue;
                bool horizontal = map.InBounds(cell.X - 1, cell.Y) && map.Cell(cell.X - 1, cell.Y).IsRoad ||
                    map.InBounds(cell.X + 1, cell.Y) && map.Cell(cell.X + 1, cell.Y).IsRoad;
                bool vertical = map.InBounds(cell.X, cell.Y - 1) && map.Cell(cell.X, cell.Y - 1).IsRoad ||
                    map.InBounds(cell.X, cell.Y + 1) && map.Cell(cell.X, cell.Y + 1).IsRoad;
                if (horizontal)
                    AddQuad(vertices, triangles, colors, x, height + 0.035f, z, 0.92f, 0.11f, stripe);
                else if (vertical)
                    AddQuad(vertices, triangles, colors, x, height + 0.035f, z, 0.11f, 0.92f, stripe);
            }
            CreateColoredMesh("Layered roads and markings", vertices, triangles, colors, transform);
        }

        private static void CreateColoredMesh(string name, List<Vector3> vertices, List<int> triangles, List<Color> colors, Transform parent)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var shader = Resources.Load<Shader>("Shaders/GridTerrain");
            if (shader != null && shader.isSupported) renderer.sharedMaterial = new Material(shader);
            else
            {
                Debug.LogWarning("地形顶点色 Shader 不可用，使用 Standard 安全回退。");
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
            }
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, List<Color> colors,
            float x, float y, float z, float width, float depth, Color color)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(x - width / 2, y, z - depth / 2));
            vertices.Add(new Vector3(x + width / 2, y, z - depth / 2));
            vertices.Add(new Vector3(x + width / 2, y, z + depth / 2));
            vertices.Add(new Vector3(x - width / 2, y, z + depth / 2));
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
            for (int i = 0; i < 4; i++) colors.Add(color);
        }

        private void BuildGrid(MapData map)
        {
            gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(transform, false);
            var material = new Material(Shader.Find("Unlit/Color")) { color = new Color(1, 1, 1, 0.35f) };
            for (int row = 0; row <= 100; row += 5) AddGridLine(map, gridRoot, material, true, row);
            for (int col = 0; col <= 100; col += 5) AddGridLine(map, gridRoot, material, false, col);
            gridRoot.SetActive(false);
        }

        private void AddGridLine(MapData map, GameObject parent, Material material, bool horizontal, int index)
        {
            var go = new GameObject(horizontal ? "Row" + index : "Col" + index);
            go.transform.SetParent(parent.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.material = material;
            line.widthMultiplier = 0.10f;
            line.positionCount = 100;
            for (int i = 0; i < 100; i++)
            {
                int x = horizontal ? i : Math.Min(99, index);
                int y = horizontal ? Math.Min(99, index) : i;
                line.SetPosition(i, CellToWorld(map, x, y, 0.12f));
            }
        }

        private void BuildLot(GameCore game, LotState lot)
        {
            var center = CellToWorld(game.Map, lot.X, lot.Y);
            var baseObject = Box(lot.Name + " · 金色地界", transform, center + new Vector3(0, .18f, 0),
                new Vector3(12.7f, .36f, 10.7f), new Color(1f, .60f, .07f));
            AddClick(baseObject, lot.Id);
            var inner = Box(lot.Name + " · 可建设草坪", transform, center + new Vector3(0, .40f, 0),
                new Vector3(11.9f, .18f, 9.9f), lot.IsOwned ? new Color(.08f, .58f, .38f) : new Color(.38f, .72f, .15f));
            AddClick(inner, lot.Id);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box("Lot corner marker", transform, center + new Vector3(x * 5.9f, .78f, z * 4.9f),
                        new Vector3(.28f, .80f, .28f), new Color(1f, .82f, .20f));
            var signPosition = center + new Vector3(0, 2.25f, -5.0f);
            var sign = Box(lot.Name + " · 立体招牌", transform, signPosition, new Vector3(7.4f, 1.35f, .28f),
                new Color(.035f, .10f, .17f));
            sign.transform.localRotation = Quaternion.Euler(0, 35, 0);
            for (float x = -2.8f; x <= 2.8f; x += 5.6f)
                Box("Sign post", transform, center + new Vector3(x, 1.15f, -5.0f), new Vector3(.16f, 2.1f, .16f),
                    new Color(.22f, .12f, .055f));
            CreateWorldLabel(lot.Id == 0 ? "A  公开招标" : "B  展示地块", signPosition + new Vector3(0, .02f, -.28f), 64, .095f, Color.white);
        }

        private void CreateWorldLabel(string value, Vector3 position, int fontSize, float characterSize, Color color)
        {
            var go = new GameObject("Readable label · " + value);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var label = go.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = fontSize;
            label.characterSize = characterSize;
            label.color = color;
            var font = Resources.Load<Font>("Fonts/NotoSansSC-Bold");
            if (font != null)
            {
                font.RequestCharactersInTexture(value, fontSize, FontStyle.Bold);
                label.font = font;
                var textRenderer = go.GetComponent<MeshRenderer>();
                if (textRenderer != null) textRenderer.sharedMaterial = font.material;
            }
            go.AddComponent<BillboardLabel>();
        }

        private void AddClick(GameObject go, int lotId)
        {
            var click = go.AddComponent<ClickableLot>();
            click.LotId = lotId;
            click.Owner = this;
        }

        private void BuildEntity(MapData map, EntityState entity)
        {
            string model = entity.Kind == EntityKind.BuildingMaterial || entity.Kind == EntityKind.Timber ? "SM_Supplier" :
                entity.Kind == EntityKind.ConstructionTeam ? "SM_TeamOffice" : entity.Kind == EntityKind.School ? "SM_School" :
                entity.Kind == EntityKind.Hospital ? "SM_Hospital" : "SM_HQ";
            float scale = entity.Kind == EntityKind.ConstructionTeam ? 1.05f :
                entity.Kind == EntityKind.BuildingMaterial || entity.Kind == EntityKind.Timber ? .88f : .90f;
            Vector3 visualOffset = Vector3.zero;
            if (entity.Id == 1) visualOffset = new Vector3(-4.5f, 0, 4.5f);
            if (entity.Id == 4) visualOffset = new Vector3(5.0f, 0, 3.5f);
            if (entity.Id == 2) visualOffset = new Vector3(-3.0f, 0, -2.0f);
            var go = ModelLibrary.Spawn(model, transform, CellToWorld(map, entity.X, entity.Y, .05f) + visualOffset, scale);
            go.name = entity.Name;
            go.transform.localRotation = Quaternion.Euler(0, entity.Id % 2 == 0 ? -12 : 10, 0);
        }

        private void BuildDecorations(GameCore game)
        {
            var map = game.Map;
            for (int x = 3; x < 97; x += 4)
            {
                for (int y = 3; y < 97; y += 4)
                {
                    var cell = map.Cell(x, y);
                    if (cell.IsRoad || IsWater(cell.Terrain) || cell.Terrain == TerrainType.Coast) continue;
                    if (NearLotOrEntity(game, x, y)) continue;
                    int hash = ((x * 73856093) ^ (y * 19349663) ^ map.Seed) & 0x7fffffff;
                    if (hash % 100 > 38) continue;
                    bool rock = cell.Terrain == TerrainType.Hill && hash % 4 == 0;
                    float scale = .72f + (hash % 38) / 100f;
                    var go = ModelLibrary.Spawn(rock ? "SM_Rock" : "SM_Tree", transform,
                        CellToWorld(map, x, y, .02f), scale);
                    go.name = rock ? "Faceted 3D rock" : "Layered 3D tree";
                    go.transform.localRotation = Quaternion.Euler(0, hash % 360, 0);
                }
            }
        }

        private void BuildStreetFurniture(GameCore game)
        {
            var map = game.Map;
            for (int y = 7; y < 94; y += 9)
            {
                if (NearLotOrEntity(game, 51, y)) continue;
                var left = ModelLibrary.Spawn("SM_Streetlight", transform, CellToWorld(map, 50, y, .08f) + new Vector3(-1.45f, 0, 0), .90f);
                left.transform.localRotation = Quaternion.Euler(0, 90, 0);
                var right = ModelLibrary.Spawn("SM_Streetlight", transform, CellToWorld(map, 50, y + 3, .08f) + new Vector3(1.45f, 0, 0), .90f);
                right.transform.localRotation = Quaternion.Euler(0, -90, 0);
            }
            Vector3[] flowerPositions = { new Vector3(-9, 0, -9), new Vector3(9, 0, -9), new Vector3(-9, 0, 9), new Vector3(9, 0, 9) };
            foreach (var position in flowerPositions)
            {
                ModelLibrary.Spawn("SM_FlowerBed", transform, position + new Vector3(0, .08f, 0), 1.05f);
                ModelLibrary.Spawn("SM_Bench", transform, position + new Vector3(2.6f, .08f, 0), .95f);
            }
            int[,] villas = { { 34, 59 }, { 66, 60 }, { 35, 38 }, { 67, 37 }, { 41, 66 }, { 60, 34 } };
            for (int i = 0; i < villas.GetLength(0); i++)
            {
                int x = villas[i, 0], y = villas[i, 1];
                if (NearLotOrEntity(game, x, y) || IsWater(map.Cell(x, y).Terrain) || map.Cell(x, y).IsRoad) continue;
                var villa = ModelLibrary.Spawn("SM_Villa", transform, CellToWorld(map, x, y, .03f), .85f);
                villa.transform.localRotation = Quaternion.Euler(0, i % 2 == 0 ? -18 : 22, 0);
            }
        }

        private static bool NearLotOrEntity(GameCore game, int x, int y)
        {
            foreach (var lot in game.State.Lots)
                if (Math.Abs(lot.X - x) < 8 && Math.Abs(lot.Y - y) < 8) return true;
            foreach (var entity in game.Map.Entities)
                if (Math.Abs(entity.X - x) < 5 && Math.Abs(entity.Y - y) < 5) return true;
            return false;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = FlatMaterial(color);
            return go;
        }

        private static Material FlatMaterial(Color color)
        {
            var shader = Resources.Load<Shader>("Shaders/ToonLit");
            var material = shader != null && shader.isSupported ? new Material(shader) : new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }

        public void ToggleGrid()
        {
            if (gridRoot != null) gridRoot.SetActive(!gridRoot.activeSelf);
        }

        public void RaiseLotClicked(int lotId)
        {
            LotClicked?.Invoke(lotId);
        }
    }

    public class ClickableLot : MonoBehaviour
    {
        public int LotId;
        public MapView Owner;
        private void OnMouseDown()
        {
            if (Owner != null) Owner.RaiseLotClicked(LotId);
        }
    }

    public class BillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null) transform.rotation = camera.transform.rotation;
        }
    }
}
