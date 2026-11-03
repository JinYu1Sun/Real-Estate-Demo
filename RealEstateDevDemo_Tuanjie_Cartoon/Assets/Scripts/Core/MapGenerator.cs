using System;
using System.Collections.Generic;

namespace RealEstateDemo.Core
{
    public struct GridCell
    {
        public int X; public int Y; public TerrainType Terrain; public float Height; public bool IsRoad;
    }
    public class EntityState
    {
        public int Id; public string Name; public EntityKind Kind; public int X; public int Y;
        public TeamGrade Grade; public int BaseQuote; public int Days; public float Quality; public float Risk;
    }
    public class MapData
    {
        public const int Size = 100;
        public int Seed; public GridCell[] Cells = new GridCell[Size * Size];
        public List<LotState> Lots = new List<LotState>(); public List<EntityState> Entities = new List<EntityState>();
        public GridCell Cell(int x, int y) { return Cells[y * Size + x]; }
        public bool InBounds(int x, int y) { return x >= 0 && y >= 0 && x < Size && y < Size; }
        public EntityState Entity(int id) { return Entities.Find(e => e.Id == id); }
    }

    public class MapGenerator
    {
        public MapData Generate(int seed)
        {
            var random = new Random(seed);
            var map = new MapData { Seed = seed };
            int coastWidth = 5 + random.Next(0, 4);
            int riverBase = 27 + random.Next(-3, 4);
            for (int y = 0; y < MapData.Size; y++)
            for (int x = 0; x < MapData.Size; x++)
            {
                float n = (float)(Math.Sin((x + seed % 31) * 0.17) * Math.Cos((y - seed % 17) * 0.13));
                TerrainType terrain = TerrainType.Plain;
                float height = 0.35f + n * 0.15f;
                if (x < coastWidth) { terrain = TerrainType.Coast; height = 0.05f; }
                int riverX = riverBase + (int)(Math.Sin(y * 0.11) * 3);
                if (Math.Abs(x - riverX) <= 1 && x > coastWidth) { terrain = TerrainType.River; height = -0.2f; }
                if ((x - 66) * (x - 66) + (y - 29) * (y - 29) < 45) { terrain = TerrainType.Lake; height = -0.15f; }
                if (x > 73 && y > 12) { terrain = TerrainType.Hill; height = 1.8f + n * 1.3f + (x - 73) * 0.045f; }
                map.Cells[y * MapData.Size + x] = new GridCell { X = x, Y = y, Terrain = terrain, Height = height, IsRoad = false };
            }
            // 主干路优先于水体，交叉处自然形成桥梁，保证连通性。
            for (int i = 0; i < MapData.Size; i++) { SetRoad(map, 50, i); SetRoad(map, i, 50); }
            for (int x = 8; x <= 88; x++) SetRoad(map, x, 48);
            for (int y = 42; y <= 66; y++) SetRoad(map, 45, y);
            for (int y = 42; y <= 66; y++) SetRoad(map, 56, y);

            int geology = random.Next(0, 3);
            int px = geology == 0 ? 43 : geology == 1 ? 78 : 12;
            int py = geology == 0 ? 46 : 46;
            map.Lots.Add(new LotState { Id = 0, Name = "公开住宅用地 A", X = px, Y = py,
                Geology = (GeologyType)geology, BasePrice = 330000 + geology * 35000,
                Area = 6800, Tags = geology == 1 ? "山丘,碎石,文物" : geology == 2 ? "海岸,软土" : "平地,成熟配套" });
            map.Lots.Add(new LotState { Id = 1, Name = "邀请住宅用地 B（本版展示）", X = 58, Y = 52,
                Geology = GeologyType.Plain, BasePrice = 300000, Area = 5200, Tags = "平地,学校" });

            Add(map, 0, "港城建材一厂", EntityKind.BuildingMaterial, 46, 49);
            Add(map, 1, "城东建材二厂", EntityKind.BuildingMaterial, 60, 51);
            Add(map, 2, "河畔木材商", EntityKind.Timber, 50, 45);
            Add(map, 3, "安达施工队（C）", EntityKind.ConstructionTeam, 38, 49, TeamGrade.C, 125000, 145, 72, 0.14f);
            Add(map, 4, "精工建设（B）", EntityKind.ConstructionTeam, 62, 51, TeamGrade.B, 185000, 112, 91, 0.05f);
            Add(map, 5, "远山工程队（C）", EntityKind.ConstructionTeam, 50, 63, TeamGrade.C, 110000, 155, 68, 0.18f);
            Add(map, 6, "第一小学", EntityKind.School, 43, 55);
            Add(map, 7, "市立医院", EntityKind.Hospital, 57, 43);
            Add(map, 8, "玩家公司", EntityKind.HQ, 49, 53);
            string error;
            if (!Validate(map, out error)) throw new InvalidOperationException($"地图校验失败：{error}");
            return map;
        }

        private static void SetRoad(MapData map, int x, int y)
        {
            int i = y * MapData.Size + x; map.Cells[i].IsRoad = true;
            if (map.Cells[i].Height < 0.2f) map.Cells[i].Height = 0.2f;
        }
        private static void Add(MapData map, int id, string name, EntityKind kind, int x, int y,
            TeamGrade grade = TeamGrade.C, int quote = 0, int days = 0, float quality = 0, float risk = 0)
        {
            map.Entities.Add(new EntityState { Id = id, Name = name, Kind = kind, X = x, Y = y,
                Grade = grade, BaseQuote = quote, Days = days, Quality = quality, Risk = risk });
        }

        public int RoadDistance(MapData map, int ax, int ay, int bx, int by)
        {
            var start = NearestRoad(map, ax, ay); var goal = NearestRoad(map, bx, by);
            var queue = new Queue<int>(); var dist = new int[MapData.Size * MapData.Size];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            int s = start.Item2 * MapData.Size + start.Item1; dist[s] = 0; queue.Enqueue(s);
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                int current = queue.Dequeue(); int x = current % MapData.Size; int y = current / MapData.Size;
                if (x == goal.Item1 && y == goal.Item2) return dist[current] + 2;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + dx[k], ny = y + dy[k];
                    if (!map.InBounds(nx, ny)) continue;
                    int ni = ny * MapData.Size + nx;
                    if (!map.Cells[ni].IsRoad || dist[ni] >= 0) continue;
                    dist[ni] = dist[current] + 1; queue.Enqueue(ni);
                }
            }
            return 9999;
        }

        private static Tuple<int, int> NearestRoad(MapData map, int x, int y)
        {
            int best = int.MaxValue; Tuple<int, int> result = Tuple.Create(50, 50);
            for (int i = 0; i < map.Cells.Length; i++) if (map.Cells[i].IsRoad)
            {
                int d = Math.Abs(map.Cells[i].X - x) + Math.Abs(map.Cells[i].Y - y);
                if (d < best) { best = d; result = Tuple.Create(map.Cells[i].X, map.Cells[i].Y); }
            }
            return result;
        }

        public bool Validate(MapData map, out string error)
        {
            int root = 50 * MapData.Size + 50; var seen = new bool[map.Cells.Length];
            var queue = new Queue<int>(); queue.Enqueue(root); seen[root] = true;
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                int c = queue.Dequeue(); int x = c % 100, y = c / 100;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + dx[k], ny = y + dy[k]; if (!map.InBounds(nx, ny)) continue;
                    int ni = ny * 100 + nx;
                    if (map.Cells[ni].IsRoad && !seen[ni]) { seen[ni] = true; queue.Enqueue(ni); }
                }
            }
            for (int i = 0; i < map.Cells.Length; i++) if (map.Cells[i].IsRoad && !seen[i])
            { error = "存在断流道路"; return false; }
            foreach (var lot in map.Lots) if (RoadDistance(map, lot.X, lot.Y, 50, 50) >= 9999)
            { error = "住宅地块孤岛"; return false; }
            error = ""; return true;
        }
    }
}
