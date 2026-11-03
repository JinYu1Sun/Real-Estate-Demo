using System;
using System.Collections.Generic;

namespace RealEstateDemo.Core
{
    // 对外门面：UI 只调用这里，不直接修改各子系统内部字段。
    public class GameCore
    {
        public GameConfig Config { get; private set; }
        public GameState State { get; private set; }
        public MapData Map { get; private set; }
        public MapGenerator MapGenerator { get; private set; }
        public CostEngine Costs { get; private set; }
        public TenderSystem Tender { get; private set; }
        public ConstructionSystem Construction { get; private set; }
        public SalesSystem Sales { get; private set; }
        public EventSystem Events { get; private set; }

        public GameCore(GameConfig config = null) { Config = config ?? GameConfig.Default(); NewGame(202611); }

        private void Wire()
        {
            MapGenerator = new MapGenerator(); Map = MapGenerator.Generate(State.Seed);
            Costs = new CostEngine(Config, MapGenerator); Events = new EventSystem(State);
            Tender = new TenderSystem(State, Config); Construction = new ConstructionSystem(State, Costs, Events);
            Sales = new SalesSystem(State, Costs);
        }

        public void NewGame(int seed)
        {
            State = new GameState { Seed = seed, RngState = (uint)seed, Cash = Config.InitialCash, Phase = GamePhase.MapPreview };
            MapGenerator = new MapGenerator(); Map = MapGenerator.Generate(seed);
            foreach (var lot in Map.Lots) State.Lots.Add(CloneLot(lot));
            State.Log($"新地图 #{seed}：海岸、河流、道路与经营主体已生成");
            Wire();
        }

        public void Restore(GameState saved, GameConfig config = null)
        {
            if (saved == null || saved.Version != 1) throw new ArgumentException("存档为空或版本不兼容");
            Config = config ?? Config ?? GameConfig.Default(); State = saved; Wire();
        }

        private static LotState CloneLot(LotState lot)
        {
            return new LotState { Id = lot.Id, Name = lot.Name, X = lot.X, Y = lot.Y, Geology = lot.Geology,
                BasePrice = lot.BasePrice, Area = lot.Area, Tags = lot.Tags, IsOwned = lot.IsOwned };
        }

        public bool Reroll()
        {
            if (State.Phase != GamePhase.MapPreview || State.RerollsLeft <= 0) return false;
            int left = State.RerollsLeft - 1; int seed = State.Seed + 7919;
            NewGame(seed); State.RerollsLeft = left; State.Log($"剩余重摇 {left} 次"); return true;
        }

        public void StartTender() { if (State.Phase == GamePhase.MapPreview) Tender.Start(0); }

        public bool Survey(out string message)
        {
            if (State.Phase != GamePhase.SurveyDesign) { message = "当前不能勘测"; return false; }
            if (State.Project.Surveyed) { message = "勘测已完成"; return false; }
            if (State.Cash < 20000) { message = "勘测费不足 ¥20,000"; return false; }
            State.Cash -= 20000; State.Project.Surveyed = true; Events.OnSurvey();
            State.Log("完成钻孔勘测，地质风险与施工难度已确认"); message = "勘测完成"; return true;
        }

        public void ConfirmDesign()
        {
            if (State.Phase == GamePhase.SurveyDesign && State.Project.Surveyed)
            { State.Phase = GamePhase.ConstructionPlanning; State.Log($"设计方案确认：{State.Project.Design.Floors} 层，评分 {State.Project.Design.Score()}"); }
        }

        public void SelectTeam(int id)
        {
            var team = Map.Entity(id); if (team == null || team.Kind != EntityKind.ConstructionTeam) return;
            State.Project.TeamId = id; State.Project.TeamQuote = Costs.TeamQuote(State, Map, team);
            State.Project.TeamNegotiated = false;
            State.Log($"选择 {team.Name}，含进退场报价 ¥{State.Project.TeamQuote:N0}");
        }

        public bool Negotiate(out string message)
        {
            var p = State.Project;
            if (p.TeamId < 0 || p.TeamNegotiated) { message = "请选择尚未讲价的施工队"; return false; }
            p.TeamNegotiated = true; int old = p.TeamQuote; p.TeamQuote = (int)(p.TeamQuote * 0.95f);
            State.Log($"讲价成功：¥{old:N0} → ¥{p.TeamQuote:N0}"); message = "讲价成功，仅可降低 5%"; return true;
        }

        public bool PurchaseMaterials(out string message)
        {
            var p = State.Project;
            if (p.LotId < 0 || p.MaterialsPurchased) { message = "当前无需重复采购"; return false; }
            if (p.SupplierId < 0) p.SupplierId = 0; if (p.TimberSupplierId < 0) p.TimberSupplierId = 2;
            var quote = Costs.MaterialQuote(State, Map, p.SupplierId, p.TimberSupplierId);
            if (State.Cash < quote.Total) { message = $"材料款不足，需要 ¥{quote.Total:N0}，请先贷款"; return false; }
            State.Cash -= quote.Total; p.MaterialCost = quote.Total; p.ConstructionSpent += quote.Total;
            p.MaterialsPurchased = true; State.Log($"四类材料现货采购完成，到场总价 ¥{quote.Total:N0}");
            message = "采购完成"; return true;
        }

        public bool StartConstruction(out string message)
        {
            if (State.Phase != GamePhase.ConstructionPlanning) { message = "当前不在施工组织阶段"; return false; }
            return Construction.Start(Map, out message);
        }

        public void AdvanceDay()
        {
            if (State.Phase != GamePhase.Construction && State.Phase != GamePhase.Sales) return;
            if (State.Project.Progress < 100) Construction.AdvanceDay(Map);
            Sales.AdvanceDay();
            State.Day++;
            if (State.Day > 30)
            {
                State.Day = 1; State.Month++;
                if (State.Month > 12) { State.Month = 1; State.Year++; }
                int interest = (int)(State.Debt * Config.MonthlyLoanRate);
                State.Cash -= State.MonthlyOverhead + interest;
                State.Log($"月度结算：管理支出 ¥{State.MonthlyOverhead:N0}，利息 ¥{interest:N0}");
                Events.OnMonth();
            }
            if (State.Cash < 0 && State.MaxLoan() < -State.Cash)
            {
                State.IsBankrupt = true; State.Phase = GamePhase.Settlement;
                State.EndReason = "现金为负且剩余授信无法覆盖缺口，公司破产"; State.Log(State.EndReason);
            }
            int elapsed = (State.Year - 2008) * 12 + State.Month - 1;
            if (elapsed >= Config.MaxMonths) { State.Phase = GamePhase.Settlement; State.EndReason = "五年剧本结束"; }
            if (State.Project.Progress >= 100 && State.Project.UnitsSold >= State.Project.UnitsTotal)
            { State.Phase = GamePhase.Settlement; State.EndReason = "项目交付并完成全部销售"; State.Log(State.EndReason); }
        }

        public bool TakeLoan(int amount, out string message)
        {
            if (amount <= 0 || amount > State.MaxLoan()) { message = $"贷款金额须为 1 至 ¥{State.MaxLoan():N0}"; return false; }
            State.Debt += amount; State.Cash += amount; State.Log($"开发贷到账 ¥{amount:N0}，月利率 {Config.MonthlyLoanRate:P1}");
            message = "贷款到账"; return true;
        }

        public bool RepayLoan(int amount, out string message)
        {
            if (amount <= 0 || amount > State.Debt || amount > State.Cash) { message = "还款金额超过贷款或现金余额"; return false; }
            State.Debt -= amount; State.Cash -= amount; State.Log($"归还贷款 ¥{amount:N0}"); message = "还款完成"; return true;
        }

        public List<int> CashForecast()
        {
            var result = new List<int>(); int cash = State.Cash;
            for (int i = 0; i < 6; i++)
            { cash -= State.MonthlyOverhead + (int)(State.Debt * Config.MonthlyLoanRate); result.Add(cash); }
            return result;
        }

        public string Grade()
        {
            int assets = State.NetAssets(); string grade = assets >= 1800000 ? "S" : assets >= 1500000 ? "A" : assets >= 1200000 ? "B" : assets >= 1000000 ? "C" : "D";
            if (State.IsBankrupt) grade = "D"; return grade;
        }
    }
}
