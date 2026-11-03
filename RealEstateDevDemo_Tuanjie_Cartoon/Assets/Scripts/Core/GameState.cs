using System;
using System.Collections.Generic;

namespace RealEstateDemo.Core
{
    [Serializable] public class LotState
    {
        public int Id; public string Name; public int X; public int Y;
        public GeologyType Geology; public int BasePrice; public int Area;
        public string Tags; public bool IsOwned;
    }

    [Serializable] public class DesignState
    {
        public int Layout; public int Floors = 12; public int Orientation;
        public int Landscaping = 1; public int Property = 1; public int Parking = 1;
        public int Score()
        {
            return Math.Min(100, 55 + Layout * 4 + (Floors >= 12 ? 4 : 0) +
                (Orientation == 1 ? 8 : Orientation == 0 ? 3 : 0) +
                Landscaping * 5 + Property * 4 + Parking * 3);
        }
        public int ExtraCost()
        {
            return Layout * 18000 + (Floors / 6 - 1) * 35000 + Landscaping * 16000 +
                Property * 12000 + Parking * 20000;
        }
    }

    [Serializable] public class ProjectState
    {
        public int LotId = -1; public bool Surveyed; public bool RiskRevealed;
        public DesignState Design = new DesignState(); public int TeamId = -1;
        public int SupplierId = -1; public int TimberSupplierId = -1;
        public bool TeamNegotiated; public int TeamQuote; public bool MaterialsPurchased;
        public int MaterialCost; public float Progress; public int DelayDays;
        public float Quality = 80; public int UnitsTotal = 24; public int UnitsSold;
        public int UnitPrice; public bool SalesOpen; public SalesMode SalesMode;
        public int LandCost; public int ConstructionSpent; public int SalesRevenue;
        public int CostReviewed; public ConstructionStage Stage;
    }

    [Serializable] public class AIState
    {
        public string Name; public AIPersonality Personality; public int Budget;
        public bool Active = true; public int BidCount;
    }

    [Serializable] public class TenderState
    {
        public bool Active; public int StartingPrice; public int CurrentBid;
        public int HighBidder = -1; public int ProofAmount; public bool BudgetLocked;
        public int LockedBudget; public bool QuickPayment; public List<int> UsedCards = new List<int>();
        public List<AIState> AI = new List<AIState>();
    }

    [Serializable] public class GameState
    {
        public int Version = 1; public int Seed; public uint RngState;
        public int Cash; public int Debt; public int Reputation = 50;
        public int Year = 2008; public int Month = 1; public int Day = 1;
        public GamePhase Phase; public int RerollsLeft = 3;
        public float MaterialPriceMultiplier = 1f; public float MarketHeat = 1f;
        public int MonthlyOverhead = 8000; public bool TutorialEnabled = true;
        public int TutorialStep; public bool IsBankrupt; public string EndReason = "";
        public List<LotState> Lots = new List<LotState>();
        public ProjectState Project = new ProjectState(); public TenderState Tender = new TenderState();
        public List<string> FiredEvents = new List<string>(); public List<string> Logs = new List<string>();

        public void Log(string message)
        {
            Logs.Add($"[{Year}-{Month:00}-{Day:00}] {message}");
            if (Logs.Count > 80) Logs.RemoveAt(0);
        }
        public int NetAssets()
        {
            if (Project.LotId < 0) return Cash - Debt;
            float remaining = Project.UnitsTotal <= 0 ? 1f : (Project.UnitsTotal - Project.UnitsSold) / (float)Project.UnitsTotal;
            int projectAsset = (int)((Project.LandCost + Project.ConstructionSpent) * (Project.SalesOpen ? remaining : 1f));
            return Cash - Debt + projectAsset;
        }
        public int MaxLoan() { return Math.Max(0, NetAssets() * (100 + Reputation) / 100 - Debt); }
        public int AvailableBid() { return Cash + MaxLoan() + Tender.ProofAmount; }
    }
}
