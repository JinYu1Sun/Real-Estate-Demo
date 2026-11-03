using System;

namespace RealEstateDemo.Core
{
    [Serializable] public class MaterialConfig
    {
        public string Name; public int Quantity; public int BasePrice; public int UnitFreight; public int Loading;
    }

    [Serializable] public class GameConfig
    {
        public int InitialCash = 1500000; public int MaxMonths = 60;
        public int ConservativeRevenue = 1700000; public int BaseBuildCost = 220000;
        public int LandPreparation = 60000; public int Infrastructure = 40000;
        public int Taxes = 80000; public int Finance = 30000; public int Marketing = 25000;
        public int RiskReserve = 50000; public int TargetProfit = 150000;
        public int BaseUnitPrice = 52000; public float MonthlyLoanRate = 0.006f;
        public MaterialConfig[] Materials = Array.Empty<MaterialConfig>();

        public static GameConfig Default()
        {
            return new GameConfig
            {
                Materials = new[]
                {
                    new MaterialConfig { Name = "碎石", Quantity = 80, BasePrice = 160, UnitFreight = 4, Loading = 800 },
                    new MaterialConfig { Name = "钢筋", Quantity = 40, BasePrice = 4200, UnitFreight = 12, Loading = 1600 },
                    new MaterialConfig { Name = "混凝土", Quantity = 300, BasePrice = 520, UnitFreight = 5, Loading = 2000 },
                    new MaterialConfig { Name = "木材", Quantity = 25, BasePrice = 1800, UnitFreight = 10, Loading = 1000 }
                }
            };
        }
    }
}
