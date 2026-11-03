using System;
using System.Collections.Generic;

namespace RealEstateDemo.Core
{
    public class CostBreakdown
    {
        public int Total; public List<string> Lines = new List<string>();
        public void Add(string name, int value) { Total += value; Lines.Add($"{name}：¥{value:N0}"); }
    }

    public class CostEngine
    {
        private readonly GameConfig config; private readonly MapGenerator mapGenerator;
        public CostEngine(GameConfig config, MapGenerator mapGenerator) { this.config = config; this.mapGenerator = mapGenerator; }

        public int MaxAffordableLand(GameState state)
        {
            int build = config.BaseBuildCost + state.Project.Design.ExtraCost();
            return Math.Max(0, config.ConservativeRevenue - build - config.LandPreparation -
                config.Infrastructure - config.Taxes - config.Finance - config.Marketing -
                config.RiskReserve - config.TargetProfit);
        }

        public int TeamQuote(GameState state, MapData map, EntityState team)
        {
            var lot = state.Lots[state.Project.LotId];
            int distance = mapGenerator.RoadDistance(map, lot.X, lot.Y, team.X, team.Y);
            int geology = lot.Geology == GeologyType.Plain ? 0 : 18000;
            return team.BaseQuote + distance * 850 + geology;
        }

        public int TeamDays(GameState state, MapData map, EntityState team)
        {
            var lot = state.Lots[state.Project.LotId];
            float geology = lot.Geology == GeologyType.Plain ? 1f : 1.15f;
            float floors = 0.85f + state.Project.Design.Floors / 40f;
            return (int)Math.Ceiling(team.Days * geology * floors);
        }

        public CostBreakdown MaterialQuote(GameState state, MapData map, int supplierId, int timberSupplierId)
        {
            var result = new CostBreakdown(); var lot = state.Lots[state.Project.LotId];
            var normal = map.Entity(supplierId); var timber = map.Entity(timberSupplierId);
            foreach (var material in config.Materials)
            {
                var supplier = material.Name == "木材" ? timber : normal;
                int distance = mapGenerator.RoadDistance(map, lot.X, lot.Y, supplier.X, supplier.Y);
                float terrain = lot.Geology == GeologyType.CoastalSoftSoil ? 1.08f : lot.Geology == GeologyType.RockyHill ? 1.05f : 1f;
                int unit = (int)Math.Ceiling((material.BasePrice * state.MaterialPriceMultiplier +
                    distance * material.UnitFreight + material.Loading) * terrain);
                result.Add($"{material.Name} {material.Quantity} 单位（{supplier.Name}，道路 {distance} 格）", unit * material.Quantity);
            }
            return result;
        }

        public int RecommendedUnitPrice(GameState state)
        {
            var p = state.Project; var lot = state.Lots[p.LotId];
            float view = lot.Geology == GeologyType.CoastalSoftSoil ? 1.08f : lot.Geology == GeologyType.RockyHill ? 1.03f : 1f;
            float quality = 0.85f + p.Quality / 500f;
            return (int)Math.Round(config.BaseUnitPrice * (0.7f + p.Design.Score() / 100f * 0.6f) * view * quality * state.MarketHeat / 100f) * 100;
        }

        public CostBreakdown FullCost(GameState state)
        {
            var c = new CostBreakdown(); var p = state.Project;
            c.Add("土地价款", p.LandCost);
            c.Add("基础建安", config.BaseBuildCost + p.Design.ExtraCost());
            c.Add("施工队及进退场", p.TeamQuote);
            c.Add("材料到场", p.MaterialCost);
            c.Add("土地整治", config.LandPreparation);
            c.Add("配套费", config.Infrastructure);
            c.Add("税费", config.Taxes); c.Add("财务费用", config.Finance);
            c.Add("营销费", config.Marketing); c.Add("风险准备金", config.RiskReserve);
            return c;
        }
    }
}
