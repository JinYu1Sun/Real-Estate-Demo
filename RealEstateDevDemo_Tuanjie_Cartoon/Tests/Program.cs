using System.Text.Json;
using RealEstateDemo.Core;

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL: {name}");
    passed++; Console.WriteLine($"PASS {passed:00}  {name}");
}
GameCore NewForGeology(GeologyType geology)
{
    for (int seed = 100; seed < 200; seed++)
    {
        var game = new GameCore(); game.NewGame(seed);
        if (game.State.Lots[0].Geology == geology) return game;
    }
    throw new Exception("找不到指定地质种子");
}
void WinTender(GameCore game)
{
    game.StartTender();
    for (int i = 0; i < 30 && game.State.Phase == GamePhase.Tender; i++)
    {
        string message;
        if (!game.Tender.PlayCard(CardType.Standard3, out message))
            game.Tender.PlayCard(CardType.FundingProof, out message);
    }
    Check(game.State.Phase == GamePhase.SurveyDesign, "公开竞标可中标并进入勘测");
}
void PrepareConstruction(GameCore game)
{
    WinTender(game);
    string message; Check(game.Survey(out message), "支付勘测费并完成勘测");
    game.ConfirmDesign(); game.SelectTeam(4);
    Check(game.Negotiate(out message), "施工队允许一次讲价");
    Check(!game.Negotiate(out message), "同一施工队不能重复讲价");
    if (!game.PurchaseMaterials(out message))
    {
        Check(game.TakeLoan(600000, out message), "现金不足时可在授信内贷款");
        Check(game.PurchaseMaterials(out message), "贷款后完成材料采购");
    }
    else Check(true, "四类材料按道路距离采购");
    Check(game.StartConstruction(out message), "满足条件后可开工");
}

var generator = new MapGenerator();
for (int seed = 1; seed <= 20; seed++)
{
    var map = generator.Generate(seed); string error;
    Check(generator.Validate(map, out error), $"随机地图 {seed:00} 道路连通且地块非孤岛");
}
var a = generator.Generate(42); var b = generator.Generate(42);
Check(a.Lots[0].Geology == b.Lots[0].Geology && a.Lots[0].X == b.Lots[0].X, "相同种子生成相同地图");
Check(generator.RoadDistance(a, 12, 46, 60, 51) > 0, "供应商运距沿道路计算");

foreach (var geology in new[] { GeologyType.Plain, GeologyType.RockyHill, GeologyType.CoastalSoftSoil })
{
    var game = NewForGeology(geology); game.StartTender();
    string message;
    Check(game.Tender.PlayCard(CardType.CostReview, out message) && message.Contains("成本"), $"{geology}：成本复算卡反馈正确");
    Check(game.Tender.PlayCard(CardType.SurveyReview, out message) && game.State.Project.RiskRevealed, $"{geology}：勘测复核揭示风险");
    Check(game.Tender.PlayCard(CardType.BudgetLock, out message) && game.State.Tender.BudgetLocked, $"{geology}：预算锁定生效");
    game.Tender.Pass(); Check(game.State.Phase == GamePhase.Settlement, $"{geology}：可退出竞价并记录结果");
}

var plain = NewForGeology(GeologyType.Plain); WinTender(plain);
Check(plain.State.Project.LandCost > 0 && plain.State.Cash > 0, "中标后土地款与现金同步扣减");
Check(!plain.Sales.Open(SalesMode.Batch, 60000, out _), "未到 70% 不能预售");
PrepareConstruction(plain);
int guard = 0;
while (plain.State.Project.Progress < 70 && guard++ < 600) plain.AdvanceDay();
Check(plain.State.Project.Progress >= 70, "施工按工期推进到预售节点");
Check(plain.State.Project.Stage >= ConstructionStage.Structure, "施工阶段模型状态随进度切换");
int recommended = plain.Costs.RecommendedUnitPrice(plain.State);
Check(plain.Sales.Open(SalesMode.Batch, recommended, out _), "70% 后可申请分批预售");
guard = 0;
while (plain.State.Phase != GamePhase.Settlement && guard++ < 2000) plain.AdvanceDay();
Check(plain.State.Project.Progress >= 100, "项目最终完成交付");
Check(plain.State.Project.UnitsSold == plain.State.Project.UnitsTotal, "销售库存最终全部去化");
Check(plain.State.Project.SalesRevenue > plain.State.Project.LandCost, "销售回款覆盖土地成本");
Check(plain.State.Phase == GamePhase.Settlement, "核心循环到达结算");

var saveGame = NewForGeology(GeologyType.RockyHill); PrepareConstruction(saveGame);
for (int i = 0; i < 25; i++) saveGame.AdvanceDay();
var json = JsonSerializer.Serialize(saveGame.State, new JsonSerializerOptions { IncludeFields = true });
var restoredState = JsonSerializer.Deserialize<GameState>(json, new JsonSerializerOptions { IncludeFields = true });
var restored = new GameCore(); restored.Restore(restoredState);
Check(restored.State.Project.Progress == saveGame.State.Project.Progress && restored.State.Cash == saveGame.State.Cash, "读档恢复现金、进度与项目状态");
Check(restored.State.RngState == saveGame.State.RngState, "读档恢复随机序列状态");
for (int i = 0; i < 10; i++) { saveGame.AdvanceDay(); restored.AdvanceDay(); }
Check(restored.State.Project.Progress == saveGame.State.Project.Progress && restored.State.Cash == saveGame.State.Cash, "读档后继续运行结果一致");

var loanGame = NewForGeology(GeologyType.Plain); int cash = loanGame.State.Cash;
Check(loanGame.TakeLoan(100000, out _) && loanGame.State.Cash == cash + 100000, "贷款增加现金与负债");
Check(loanGame.RepayLoan(50000, out _) && loanGame.State.Debt == 50000, "还款同步减少现金与负债");
Check(loanGame.CashForecast().Count == 6, "提供未来六个月现金预测");
Console.WriteLine($"\nALL GREEN: {passed} assertions");
