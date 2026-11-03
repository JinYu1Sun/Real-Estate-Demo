using System;

namespace RealEstateDemo.Core
{
    public class TenderSystem
    {
        private readonly GameState state; private readonly GameConfig config;
        public TenderSystem(GameState state, GameConfig config) { this.state = state; this.config = config; }

        public void Start(int lotId)
        {
            var lot = state.Lots[lotId]; state.Project.LotId = lotId;
            state.Tender = new TenderState { Active = true, StartingPrice = lot.BasePrice, CurrentBid = lot.BasePrice };
            state.Tender.AI.Add(new AIState { Name = "稳健地产", Personality = AIPersonality.Steady, Budget = (int)(lot.BasePrice * 1.18f) });
            state.Tender.AI.Add(new AIState { Name = "激进置业", Personality = AIPersonality.Aggressive, Budget = (int)(lot.BasePrice * 1.52f) });
            state.Tender.AI.Add(new AIState { Name = "陷阱资本", Personality = AIPersonality.Trap, Budget = (int)(lot.BasePrice * 1.34f) });
            state.Phase = GamePhase.Tender;
            state.Log($"{lot.Name} 开拍，起拍 ¥{lot.BasePrice:N0}；你的当前可报价额度 ¥{state.AvailableBid():N0}");
        }

        public bool PlayCard(CardType card, out string message)
        {
            var t = state.Tender;
            if (!t.Active) { message = "当前没有进行中的招标"; return false; }
            if (card > CardType.Jump6 && t.UsedCards.Contains((int)card)) { message = "这张牌本场已经用过"; return false; }
            int oldBid = t.CurrentBid;
            message = "";
            switch (card)
            {
                case CardType.CostReview:
                    state.Project.CostReviewed++; message = "成本区间已收窄，最高可承受地价已显示"; break;
                case CardType.SurveyReview:
                    state.Project.RiskRevealed = true; message = $"地质复核：{state.Lots[state.Project.LotId].Tags}"; break;
                case CardType.FundingProof:
                    t.ProofAmount += 250000; message = "资金证明增加 ¥250,000 报价额度（占用授信）"; break;
                case CardType.FastPayment:
                    t.QuickPayment = true; message = "快速付款：AI 必须越过额外 2% 的有效报价；中标后支付 2% 手续费"; break;
                case CardType.BudgetLock:
                    t.BudgetLocked = true; t.LockedBudget = Math.Max(t.CurrentBid + 20000, (int)(t.CurrentBid * 1.15f));
                    message = $"预算已锁定为 ¥{t.LockedBudget:N0}"; break;
                default:
                    float rate = card == CardType.Probe1 ? 1.01f : card == CardType.Standard3 ? 1.03f : 1.06f;
                    int bid = (int)Math.Ceiling(t.CurrentBid * rate / 1000f) * 1000;
                    if (bid > state.AvailableBid()) { message = "报价超过现金、授信与资金证明总额"; return false; }
                    if (t.BudgetLocked && bid > t.LockedBudget) { message = "预算锁定生效，不能继续突破限价"; return false; }
                    t.CurrentBid = bid; t.HighBidder = 0;
                    state.Log($"你报价 ¥{bid:N0}"); break;
            }
            if (card > CardType.Jump6) t.UsedCards.Add((int)card);
            if (card <= CardType.Jump6) RunAI(oldBid);
            if (card <= CardType.Jump6 && t.Active)
                message = t.HighBidder == 0 ? "你暂时领先" : $"当前领先：{t.AI[t.HighBidder - 1].Name}";
            if (!state.Tender.Active && state.Phase == GamePhase.SurveyDesign) message = "中标！进入勘测与设计";
            return true;
        }

        private void RunAI(int oldBid)
        {
            var t = state.Tender; int bestBid = t.CurrentBid; int bestAI = 0;
            for (int i = 0; i < t.AI.Count; i++)
            {
                var ai = t.AI[i]; if (!ai.Active) continue;
                float pressure = t.QuickPayment ? 1.02f : 1f;
                float rate = ai.Personality == AIPersonality.Steady ? 1.01f : ai.Personality == AIPersonality.Aggressive ? 1.04f : 1.025f;
                if (ai.Personality == AIPersonality.Trap && state.AvailableBid() < t.CurrentBid * 1.2f) rate = 1.045f;
                rate += GameRandom.Float(state) * 0.012f;
                int bid = (int)Math.Ceiling(oldBid * rate * pressure / 1000f) * 1000;
                if (bid <= t.CurrentBid) bid = t.CurrentBid + 1000;
                if (bid <= ai.Budget && bid > bestBid) { bestBid = bid; bestAI = i + 1; }
                else if (bid > ai.Budget) ai.Active = false;
            }
            if (bestAI == 0) { Win(); return; }
            t.CurrentBid = bestBid; t.HighBidder = bestAI; t.AI[bestAI - 1].BidCount++;
            state.Log($"{t.AI[bestAI - 1].Name} 跟价至 ¥{bestBid:N0}；AI 恶意抬价压力上升");
        }

        private void Win()
        {
            var t = state.Tender; t.Active = false;
            int fee = t.QuickPayment ? (int)(t.CurrentBid * 0.02f) : 0;
            state.Project.LandCost = t.CurrentBid + fee; state.Cash -= state.Project.LandCost;
            state.Lots[state.Project.LotId].IsOwned = true;
            state.Phase = GamePhase.SurveyDesign;
            state.Log($"中标！土地价款 ¥{t.CurrentBid:N0}，付款手续费 ¥{fee:N0}，剩余现金 ¥{state.Cash:N0}");
            if (state.TutorialEnabled) state.TutorialStep = Math.Max(state.TutorialStep, 1);
        }

        public void Pass()
        {
            if (!state.Tender.Active) return;
            state.Tender.Active = false; state.Phase = GamePhase.Settlement;
            state.EndReason = state.Tender.HighBidder > 0 ? $"你退出竞价，{state.Tender.AI[state.Tender.HighBidder - 1].Name} 中标" : "你放弃了本地块";
            state.Log(state.EndReason);
        }
    }
}
