using System;

namespace RealEstateDemo.Core
{
    public class SalesSystem
    {
        private readonly GameState state; private readonly CostEngine costs;
        public SalesSystem(GameState state, CostEngine costs) { this.state = state; this.costs = costs; }

        public bool Open(SalesMode mode, int unitPrice, out string message)
        {
            var p = state.Project;
            if (p.Progress < 70) { message = "形象进度达到 70% 才能申请预售"; return false; }
            if (p.SalesOpen) { message = "项目已经开盘"; return false; }
            if (unitPrice <= 0) { message = "售价必须大于零"; return false; }
            p.SalesOpen = true; p.SalesMode = mode; p.UnitPrice = unitPrice;
            state.Phase = GamePhase.Sales;
            state.Log($"{(mode == SalesMode.Batch ? "分批" : "一次性")}开盘，单价 ¥{unitPrice:N0}，共 {p.UnitsTotal} 套");
            if (mode == SalesMode.OneShot)
            {
                int count = Math.Min(14, p.UnitsTotal - p.UnitsSold);
                Sell(count, (int)(unitPrice * 0.97f));
                state.Log($"集中推售成交 {count} 套，一次性开盘折让 3%");
            }
            if (state.TutorialEnabled) state.TutorialStep = Math.Max(state.TutorialStep, 3);
            message = "预售许可通过，已开盘"; return true;
        }

        public void AdvanceDay()
        {
            var p = state.Project; if (!p.SalesOpen || p.UnitsSold >= p.UnitsTotal) return;
            int recommended = costs.RecommendedUnitPrice(state);
            float ratio = p.UnitPrice / (float)recommended;
            float chance = ratio <= 0.9f ? 0.78f : ratio <= 1.05f ? 0.48f : ratio <= 1.2f ? 0.20f : 0.06f;
            if (GameRandom.Float(state) < chance)
            {
                int count = p.SalesMode == SalesMode.OneShot && GameRandom.Float(state) < 0.3f ? 2 : 1;
                Sell(Math.Min(count, p.UnitsTotal - p.UnitsSold), p.UnitPrice);
            }
        }

        private void Sell(int count, int price)
        {
            var p = state.Project; int income = count * price;
            p.UnitsSold += count; p.SalesRevenue += income; state.Cash += income;
            state.Log($"销售回款 {count} 套，到账 ¥{income:N0}，累计去化 {p.UnitsSold}/{p.UnitsTotal}");
        }
    }
}
