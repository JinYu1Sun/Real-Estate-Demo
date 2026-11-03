namespace RealEstateDemo.Core
{
    public enum GamePhase { MapPreview, Tender, SurveyDesign, ConstructionPlanning, Construction, Sales, Settlement }
    public enum TerrainType { Plain, Coast, Hill, Lake, River }
    public enum GeologyType { Plain, RockyHill, CoastalSoftSoil }
    public enum ConstructionStage { SiteGrading, PileFoundation, Structure, ToppedOut, Delivered }
    public enum AIPersonality { Steady, Aggressive, Trap }
    public enum TeamGrade { C, B }
    public enum EntityKind { BuildingMaterial, Timber, ConstructionTeam, School, Hospital, HQ }
    public enum CardType { Probe1 = 0, Standard3 = 1, Jump6 = 2, CostReview = 3, SurveyReview = 4, FundingProof = 5, FastPayment = 6, BudgetLock = 7 }
    public enum SalesMode { Batch, OneShot }
}
