namespace RealEstateDemo.Core
{
    public class EventSystem
    {
        private readonly GameState state;
        public EventSystem(GameState state) { this.state = state; }
        private bool FireOnce(string id)
        {
            if (state.FiredEvents.Contains(id)) return false;
            state.FiredEvents.Add(id); return true;
        }
        public void OnSurvey()
        {
            var lot = state.Lots[state.Project.LotId];
            if (lot.Geology == GeologyType.RockyHill && FireOnce("碎石地基"))
                state.Log("事件【碎石地基】：建安风险 +20%，预计工期 +15%；建议重新比较施工队报价");
            if (lot.Geology == GeologyType.CoastalSoftSoil && FireOnce("滨海软土"))
                state.Log("事件【滨海软土】：桩基与运输成本上升，材料到场价增加 8%");
        }
        public void OnProgress()
        {
            var lot = state.Lots[state.Project.LotId];
            if (state.Project.Progress >= 25 && lot.Tags.Contains("文物") && FireOnce("挖出文物"))
            {
                state.Project.DelayDays += 10; state.Cash += 80000;
                state.Log("事件【挖出文物】：停工 10 天，获得保护补贴 ¥80,000");
            }
        }
        public void OnMonth()
        {
            var p = state.Project;
            if (p.LotId < 0) return;
            var lot = state.Lots[p.LotId];
            if (state.Month >= 6 && state.Month <= 9 && lot.Geology == GeologyType.CoastalSoftSoil && FireOnce("台风过境"))
            {
                p.DelayDays += 7; state.Log("事件【台风过境】：停工 7 天，材料运输延迟");
            }
            if (state.Month >= 4 && FireOnce("材料涨价"))
            {
                state.MaterialPriceMultiplier = 1.10f;
                state.Log("事件【材料涨价】：木材、混凝土等到场价基数 +10%");
            }
        }
    }
}
