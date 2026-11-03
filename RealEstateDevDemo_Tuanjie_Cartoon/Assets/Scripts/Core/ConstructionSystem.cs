using System;

namespace RealEstateDemo.Core
{
    public class ConstructionSystem
    {
        private readonly GameState state; private readonly CostEngine costs; private readonly EventSystem events;
        public ConstructionSystem(GameState state, CostEngine costs, EventSystem events)
        { this.state = state; this.costs = costs; this.events = events; }

        public bool Start(MapData map, out string message)
        {
            var p = state.Project;
            if (!p.Surveyed) { message = "请先完成地质勘测"; return false; }
            if (p.TeamId < 0) { message = "请选择施工队"; return false; }
            if (!p.MaterialsPurchased) { message = "请先采购现货材料"; return false; }
            if (p.TeamQuote > 0)
            {
                if (state.Cash < p.TeamQuote) { message = "现金不足以支付施工队首款，请先贷款"; return false; }
                state.Cash -= p.TeamQuote; p.ConstructionSpent += p.TeamQuote;
            }
            p.Stage = ConstructionStage.SiteGrading; state.Phase = GamePhase.Construction;
            var team = map.Entity(p.TeamId); p.Quality = team.Quality;
            state.Log($"{team.Name} 进场，预计 {costs.TeamDays(state, map, team)} 天，质量基准 {p.Quality:0}");
            if (state.TutorialEnabled) state.TutorialStep = Math.Max(state.TutorialStep, 2);
            message = "施工开始"; return true;
        }

        public void AdvanceDay(MapData map)
        {
            var p = state.Project; if (p.Progress >= 100) return;
            if (p.DelayDays > 0) { p.DelayDays--; return; }
            var team = map.Entity(p.TeamId);
            int days = Math.Max(1, costs.TeamDays(state, map, team));
            p.Progress = Math.Min(100, p.Progress + 100f / days);
            ConstructionStage old = p.Stage;
            p.Stage = p.Progress >= 100 ? ConstructionStage.Delivered : p.Progress >= 85 ? ConstructionStage.ToppedOut :
                p.Progress >= 25 ? ConstructionStage.Structure : p.Progress >= 10 ? ConstructionStage.PileFoundation : ConstructionStage.SiteGrading;
            if (old != p.Stage) state.Log($"形象进度进入【{StageName(p.Stage)}】：{p.Progress:0.0}%");
            events.OnProgress();
            if (p.Progress >= 100) state.Log("项目交付！完成剩余销售后可结算");
        }

        public static string StageName(ConstructionStage stage)
        {
            switch (stage)
            {
                case ConstructionStage.SiteGrading: return "场地平整";
                case ConstructionStage.PileFoundation: return "桩基";
                case ConstructionStage.Structure: return "主体";
                case ConstructionStage.ToppedOut: return "封顶";
                default: return "交付";
            }
        }
    }
}
