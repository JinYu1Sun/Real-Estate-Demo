using System;
using System.Linq;
using System.Text;
using RealEstateDemo.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RealEstateDemo.Unity
{
    public class GameHUD : MonoBehaviour
    {
        private static readonly Color Ink = Rgb(24, 50, 77);
        private static readonly Color Navy = Rgb(23, 48, 75);
        private static readonly Color NavySoft = Rgb(39, 70, 101);
        private static readonly Color Cream = Rgb(255, 248, 232);
        private static readonly Color Paper = Rgb(255, 253, 246);
        private static readonly Color Teal = Rgb(18, 181, 165);
        private static readonly Color Orange = Rgb(255, 157, 35);
        private static readonly Color Coral = Rgb(255, 103, 91);
        private static readonly Color Sky = Rgb(72, 160, 255);
        private static readonly Color Purple = Rgb(143, 99, 214);
        private static readonly Color Muted = Rgb(105, 126, 145);
        private static readonly Color White = Rgb(255, 255, 255);

        private DemoBootstrap app;
        private Font bodyFont;
        private Font boldFont;
        private Sprite roundedSprite;
        private Text cashText;
        private Text assetText;
        private Text debtText;
        private Text dateText;
        private Text progressText;
        private Text pauseText;
        private Text speedText;
        private Text actionPhaseText;
        private Text infoPhaseText;
        private Text infoText;
        private Text logText;
        private Text toastText;
        private Text tutorialText;
        private Text stepText;
        private RectTransform actionContent;
        private RectTransform infoContent;
        private GamePhase builtPhase;
        private bool actionsDirty = true;
        private float toastUntil;
        private GameObject tutorialPanel;

        private static Color Rgb(int r, int g, int b, int a = 255)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        public void Build(DemoBootstrap bootstrap)
        {
            app = bootstrap;
            bodyFont = Resources.Load<Font>("Fonts/NotoSansSC");
            boldFont = Resources.Load<Font>("Fonts/NotoSansSC-Bold");
            if (bodyFont == null) bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (boldFont == null) boldFont = bodyFont;
            roundedSprite = CreateRoundedSprite();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Tuanjie 1.10 / Unity 2022 LTS 兼容写法。
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var events = new GameObject("EventSystem");
                events.AddComponent<UnityEngine.EventSystems.EventSystem>();
                events.AddComponent<StandaloneInputModule>();
            }

            BuildTop();
            BuildPanels();
            BuildPhaseSteps();
            BuildToast();
            BuildTutorial();
            builtPhase = (GamePhase)(-1);
            RefreshAll();
        }

        private void BuildTop()
        {
            var top = Panel(transform, "Top · 品牌与资金", Navy);
            Anchor(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -112), new Vector2(0, 0));

            var logoObject = new GameObject("Cartoon city logo", typeof(RectTransform), typeof(RawImage));
            logoObject.transform.SetParent(top, false);
            var logo = logoObject.GetComponent<RawImage>();
            logo.texture = Resources.Load<Texture2D>("Art/CityLogo");
            var logoRect = (RectTransform)logoObject.transform;
            logoRect.anchorMin = new Vector2(0, 1);
            logoRect.anchorMax = new Vector2(0, 1);
            logoRect.offsetMin = new Vector2(18, -102);
            logoRect.offsetMax = new Vector2(98, -10);

            var brand = Label(top, "筑梦地产", 30, TextAnchor.MiddleLeft, White, true);
            brand.rectTransform.anchorMin = new Vector2(0, 1);
            brand.rectTransform.anchorMax = new Vector2(0, 1);
            brand.rectTransform.offsetMin = new Vector2(110, -62);
            brand.rectTransform.offsetMax = new Vector2(310, -20);
            var subtitle = Label(top, "TOY CITY TYCOON", 14, TextAnchor.MiddleLeft, Rgb(130, 224, 211), true);
            subtitle.rectTransform.anchorMin = new Vector2(0, 1);
            subtitle.rectTransform.anchorMax = new Vector2(0, 1);
            subtitle.rectTransform.offsetMin = new Vector2(112, -91);
            subtitle.rectTransform.offsetMax = new Vector2(310, -60);

            cashText = Metric(top, "可用资金", "¥0", 330, 204, Teal);
            assetText = Metric(top, "净资产", "¥0", 546, 204, Sky);
            debtText = Metric(top, "开发贷", "¥0", 762, 174, Coral);
            dateText = Metric(top, "游戏日期", "--", 948, 190, Orange);
            progressText = Metric(top, "工程进度", "未拿地", 1150, 184, Purple);

            pauseText = TopControl(top, "暂停", 1352, 86, () => app.TogglePause(), Sky);
            speedText = TopControl(top, "4×", 1448, 70, CycleSpeed, Purple);
            TopControl(top, "保存", 1528, 76, () => app.Save(), Teal);
            TopControl(top, "读取", 1614, 76, () => app.Load(), Teal);
            TopControl(top, "格网", 1700, 82, () => app.MapView.ToggleGrid(), Orange);
        }

        private Text Metric(Transform parent, string title, string value, float x, float width, Color accent)
        {
            var card = Panel(parent, title, NavySoft);
            card.anchorMin = new Vector2(0, 1);
            card.anchorMax = new Vector2(0, 1);
            card.offsetMin = new Vector2(x, -98);
            card.offsetMax = new Vector2(x + width, -14);
            var titleText = Label(card, title, 16, TextAnchor.MiddleLeft, accent, true);
            Anchor(titleText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -34), new Vector2(-10, -5));
            var valueText = Label(card, value, 24, TextAnchor.MiddleLeft, White, true);
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 19;
            valueText.resizeTextMaxSize = 24;
            Anchor(valueText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 9), new Vector2(-10, -35));
            return valueText;
        }

        private Text TopControl(Transform parent, string label, float x, float width, Action action, Color color)
        {
            var button = Button(parent, label, action, color, White, 20);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.offsetMin = new Vector2(x, -91);
            rect.offsetMax = new Vector2(x + width, -22);
            return button.GetComponentInChildren<Text>();
        }

        private void CycleSpeed()
        {
            app.SetSpeed(app.Speed == 1 ? 4 : app.Speed == 4 ? 12 : 1);
        }

        private void BuildPanels()
        {
            var left = Panel(transform, "Actions · 行动面板", Cream);
            Anchor(left, new Vector2(0, 0), new Vector2(0, 1), new Vector2(24, 126), new Vector2(412, -176));
            AddShadow(left.gameObject);
            var actionHeader = Panel(left, "阶段标题", Teal);
            Anchor(actionHeader, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), new Vector2(0, 0));
            actionPhaseText = Label(actionHeader, "开局预览", 25, TextAnchor.MiddleLeft, White, true);
            Anchor(actionPhaseText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22, 0), new Vector2(-16, 0));

            var actionScroll = CreateScroll(left, "行动列表", out actionContent);
            Anchor((RectTransform)actionScroll.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var actionViewport = actionScroll.viewport;
            Anchor(actionViewport, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -84));
            var actionLayout = actionContent.gameObject.AddComponent<VerticalLayoutGroup>();
            actionLayout.spacing = 12;
            actionLayout.childControlHeight = true;
            actionLayout.childControlWidth = true;
            actionLayout.childForceExpandHeight = false;
            actionLayout.childForceExpandWidth = true;
            var actionFitter = actionContent.gameObject.AddComponent<ContentSizeFitter>();
            actionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var right = Panel(transform, "Information · 项目情报", Cream);
            Anchor(right, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-412, 126), new Vector2(-24, -176));
            AddShadow(right.gameObject);
            var infoHeader = Panel(right, "情报标题", Orange);
            Anchor(infoHeader, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), new Vector2(0, 0));
            infoPhaseText = Label(infoHeader, "项目情报", 25, TextAnchor.MiddleLeft, Ink, true);
            Anchor(infoPhaseText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22, 0), new Vector2(-16, 0));

            var infoScroll = CreateScroll(right, "情报内容", out infoContent);
            Anchor((RectTransform)infoScroll.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Anchor(infoScroll.viewport, Vector2.zero, Vector2.one, new Vector2(20, 14), new Vector2(-20, -84));
            infoText = Label(infoContent, "Info", 20, TextAnchor.UpperLeft, Ink, false);
            infoText.lineSpacing = 1.20f;
            infoText.rectTransform.anchorMin = new Vector2(0, 1);
            infoText.rectTransform.anchorMax = new Vector2(1, 1);
            infoText.rectTransform.pivot = new Vector2(0.5f, 1);
            infoText.rectTransform.offsetMin = Vector2.zero;
            infoText.rectTransform.offsetMax = Vector2.zero;
            var infoFitter = infoText.gameObject.AddComponent<ContentSizeFitter>();
            infoFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bottom = Panel(transform, "Log · 城市动态", Rgb(23, 48, 75, 244));
            Anchor(bottom, new Vector2(0, 0), new Vector2(1, 0), new Vector2(432, 18), new Vector2(-432, 108));
            AddShadow(bottom.gameObject);
            var logTitle = Label(bottom, "城市动态", 16, TextAnchor.MiddleLeft, Orange, true);
            Anchor(logTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -32), new Vector2(-20, -4));
            logText = Label(bottom, "欢迎来到筑梦地产。", 19, TextAnchor.UpperLeft, White, false);
            logText.lineSpacing = 1.1f;
            Anchor(logText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 8), new Vector2(-20, -34));
        }

        private ScrollRect CreateScroll(Transform parent, string name, out RectTransform content)
        {
            var scrollObject = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            var viewport = Panel(scrollObject.transform, "Viewport", new Color(0, 0, 0, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Panel(viewport, "Content", new Color(0, 0, 0, 0));
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            return scroll;
        }

        private void BuildPhaseSteps()
        {
            var panel = Panel(transform, "Phase steps", Rgb(255, 253, 246, 242));
            Anchor(panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-390, -164), new Vector2(390, -120));
            stepText = Label(panel, "拿地  →  设计  →  施工  →  预售  →  结算", 20, TextAnchor.MiddleCenter, Ink, true);
            Anchor(stepText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
        }

        private void BuildToast()
        {
            var toast = Panel(transform, "Toast", Coral);
            Anchor(toast, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-400, -252), new Vector2(400, -184));
            AddShadow(toast.gameObject);
            toastText = Label(toast, "Message", 21, TextAnchor.MiddleCenter, White, true);
            Anchor(toastText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 6), new Vector2(-18, -6));
            toast.gameObject.SetActive(false);
        }

        private void BuildTutorial()
        {
            tutorialPanel = Panel(transform, "Tutorial", Rgb(255, 221, 87)).gameObject;
            var rect = (RectTransform)tutorialPanel.transform;
            Anchor(rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-400, 120), new Vector2(400, 172));
            tutorialText = Label(tutorialPanel.transform, "Guide", 19, TextAnchor.MiddleLeft, Ink, true);
            Anchor(tutorialText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 2), new Vector2(-104, -2));
            var close = Button(tutorialPanel.transform, "知道了", () =>
            {
                app.Game.State.TutorialEnabled = false;
                tutorialPanel.SetActive(false);
            }, Orange, Ink, 16);
            var closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.offsetMin = new Vector2(-96, 5);
            closeRect.offsetMax = new Vector2(-6, -5);
        }

        public void RefreshAll()
        {
            if (app == null || app.Game == null) return;
            var s = app.Game.State;
            cashText.text = "¥" + s.Cash.ToString("N0");
            assetText.text = "¥" + s.NetAssets().ToString("N0");
            debtText.text = "¥" + s.Debt.ToString("N0");
            dateText.text = $"{s.Year}-{s.Month:00}-{s.Day:00}";
            progressText.text = s.Project.LotId < 0 ? "未拿地" : $"{s.Project.Progress:0.0}%";
            pauseText.text = app.Paused ? "继续" : "暂停";
            speedText.text = app.Speed + "×";
            actionPhaseText.text = PhaseName(s.Phase) + " · 行动";
            infoPhaseText.text = "项目情报 · " + PhaseName(s.Phase);
            stepText.text = StepText(s.Phase);
            infoText.text = BuildInfo();
            logText.text = string.Join("\n", s.Logs.Skip(Math.Max(0, s.Logs.Count - 2)));
            tutorialPanel.SetActive(s.TutorialEnabled && s.Phase != GamePhase.Settlement);
            tutorialText.text = s.TutorialStep == 0 ? "第 1 步：开始公开招标，用加价牌与 AI 竞价。" :
                s.TutorialStep == 1 ? "第 2 步：完成勘测设计，比较施工队，必要时先贷款。" :
                s.TutorialStep == 2 ? "第 3 步：进度到 70% 后开盘，销售回款。" : "核心流程完成：等待交付、回款和最终评级。";
            if (toastText.transform.parent.gameObject.activeSelf && Time.unscaledTime > toastUntil)
                toastText.transform.parent.gameObject.SetActive(false);
            if (builtPhase != s.Phase || actionsDirty)
            {
                builtPhase = s.Phase;
                RebuildActions();
            }
            PrepareDynamicGlyphs();
        }

        private void PrepareDynamicGlyphs()
        {
            // 每个文本按自己的内容、字号和字体请求字形，覆盖按钮、标题和后续动态文本。
            // 不能只给少数字段统一请求 30 / 24 号字，否则其他字号仍要在首帧临时栅格化。
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                if (text.font == null || string.IsNullOrEmpty(text.text)) continue;
                text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
            }
        }

        private static string StepText(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MapPreview: return "● 拿地   →   设计   →   施工   →   预售   →   结算";
                case GamePhase.Tender: return "● 公开竞价   →   设计   →   施工   →   预售   →   结算";
                case GamePhase.SurveyDesign: return "拿地   →   ● 勘测设计   →   施工   →   预售   →   结算";
                case GamePhase.ConstructionPlanning: return "拿地   →   设计   →   ● 组织施工   →   预售   →   结算";
                case GamePhase.Construction: return "拿地   →   设计   →   ● 动态建造   →   预售   →   结算";
                case GamePhase.Sales: return "拿地   →   设计   →   施工   →   ● 预售回款   →   结算";
                default: return "拿地   →   设计   →   施工   →   预售   →   ● 结算评价";
            }
        }

        public void ShowMessage(string message)
        {
            toastText.text = message;
            toastText.transform.parent.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + 3.5f;
            actionsDirty = true;
        }

        private void RebuildActions()
        {
            actionsDirty = false;
            for (int i = actionContent.childCount - 1; i >= 0; i--) Destroy(actionContent.GetChild(i).gameObject);
            var game = app.Game;
            var s = game.State;
            Header("选择你的下一步");
            switch (s.Phase)
            {
                case GamePhase.MapPreview:
                    ActionButton("开始公开招标 · A 地块", () => game.StartTender());
                    ActionButton($"重摇地图 · 剩 {s.RerollsLeft} 次", () => app.Reroll());
                    ActionButton("显示 / 隐藏格网", () => app.MapView.ToggleGrid());
                    break;
                case GamePhase.Tender:
                    AddCard(CardType.Probe1, "试探加价  +1%");
                    AddCard(CardType.Standard3, "标准加价  +3%");
                    AddCard(CardType.Jump6, "跳价施压  +6%");
                    AddCard(CardType.CostReview, "成本复算 · 本场一次");
                    AddCard(CardType.SurveyReview, "勘测复核 · 本场一次");
                    AddCard(CardType.FundingProof, "资金证明 · +25 万");
                    AddCard(CardType.FastPayment, "快速付款 · 降低尾款压力");
                    AddCard(CardType.BudgetLock, "预算锁定 · 控制风险");
                    ActionButton("退出竞价", () => game.Tender.Pass());
                    break;
                case GamePhase.SurveyDesign:
                    ActionButton("钻孔勘测 · ¥20,000", () => { string m; app.Notify(game.Survey(out m) ? m : m); });
                    var d = s.Project.Design;
                    ActionButton($"户型方案：{new[] { "刚需", "改善", "豪宅" }[d.Layout]}", () => { d.Layout = (d.Layout + 1) % 3; });
                    ActionButton($"建筑层数：{d.Floors} 层", () => { d.Floors = d.Floors == 6 ? 12 : d.Floors == 12 ? 18 : 6; });
                    ActionButton($"朝向：{new[] { "一般", "南北", "西晒" }[d.Orientation == -1 ? 2 : d.Orientation]}", () => { d.Orientation = d.Orientation == 1 ? -1 : d.Orientation + 1; });
                    ActionButton($"园林等级：{d.Landscaping}", () => { d.Landscaping = (d.Landscaping + 1) % 3; });
                    ActionButton($"物业等级：{d.Property}", () => { d.Property = (d.Property + 1) % 3; });
                    ActionButton($"车库 / 储物：{d.Parking}", () => { d.Parking = (d.Parking + 1) % 3; });
                    ActionButton("确认设计并组织施工", () => game.ConfirmDesign());
                    break;
                case GamePhase.ConstructionPlanning:
                    foreach (var team in game.Map.Entities.Where(e => e.Kind == EntityKind.ConstructionTeam))
                        ActionButton($"{(s.Project.TeamId == team.Id ? "✓ " : "")}{team.Name}", () => game.SelectTeam(team.Id));
                    ActionButton("施工队讲价一次 · -5%", () => { string m; app.Notify(game.Negotiate(out m) ? m : m); });
                    ActionButton($"建材商：{(s.Project.SupplierId == 1 ? "城东二厂" : "港城一厂")}", () => s.Project.SupplierId = s.Project.SupplierId == 1 ? 0 : 1);
                    ActionButton("采购四类现货材料", () => { string m; app.Notify(game.PurchaseMaterials(out m) ? m : m); });
                    ActionButton("正式开工", () => { string m; app.Notify(game.StartConstruction(out m) ? m : m); });
                    LoanButtons();
                    break;
                case GamePhase.Construction:
                case GamePhase.Sales:
                    if (!s.Project.SalesOpen)
                    {
                        int recommended = game.Costs.RecommendedUnitPrice(s);
                        ActionButton($"分批开盘 · 推荐 ¥{recommended:N0}", () => OpenSales(SalesMode.Batch, recommended));
                        ActionButton("一次性开盘 · 首批 97 折", () => OpenSales(SalesMode.OneShot, recommended));
                        ActionButton($"低价快销 · ¥{(int)(recommended * 0.9f):N0}", () => OpenSales(SalesMode.Batch, (int)(recommended * 0.9f)));
                        ActionButton($"高价试探 · ¥{(int)(recommended * 1.15f):N0}", () => OpenSales(SalesMode.Batch, (int)(recommended * 1.15f)));
                    }
                    else Header($"已售 {s.Project.UnitsSold}/{s.Project.UnitsTotal} · 单价 ¥{s.Project.UnitPrice:N0}");
                    LoanButtons();
                    break;
                case GamePhase.Settlement:
                    ActionButton("重玩同一地图种子", () => app.Restart(true));
                    ActionButton("开始一张新地图", () => app.Restart(false));
                    break;
            }
            ActionButton("保存游戏", () => app.Save());
            ActionButton("读取游戏", () => app.Load());
            actionContent.anchoredPosition = Vector2.zero;
        }

        private void AddCard(CardType card, string label)
        {
            var button = ActionButton(label, () => { string m; app.Notify(app.Game.Tender.PlayCard(card, out m) ? m : m); });
            if (card > CardType.Jump6 && app.Game.State.Tender.UsedCards.Contains((int)card)) button.interactable = false;
        }

        private void OpenSales(SalesMode mode, int price)
        {
            string message;
            app.Notify(app.Game.Sales.Open(mode, price, out message) ? message : message);
        }

        private void LoanButtons()
        {
            ActionButton("开发贷 · +¥300,000", () => { string m; app.Notify(app.Game.TakeLoan(300000, out m) ? m : m); });
            ActionButton("归还贷款 · ¥100,000", () => { string m; app.Notify(app.Game.RepayLoan(100000, out m) ? m : m); });
        }

        private string BuildInfo()
        {
            var game = app.Game;
            var s = game.State;
            var p = s.Project;
            var text = new StringBuilder();
            text.AppendLine($"<b>城市地图  #{s.Seed}</b>");
            text.AppendLine($"当前阶段：{PhaseName(s.Phase)}");
            text.AppendLine($"净资产：¥{s.NetAssets():N0}");
            text.AppendLine();
            if (s.Phase == GamePhase.MapPreview)
            {
                text.AppendLine("<b><size=24>本期开发目标</size></b>");
                text.AppendLine("竞得 A 地块，完成设计、施工与预售，在保证现金流安全的前提下交付住宅。");
                text.AppendLine();
                text.AppendLine("<b>开局操作</b>");
                text.AppendLine("1. 点击左侧“开始公开招标”。");
                text.AppendLine("2. 用地价卡牌与 AI 竞价，避免超过资金上限。");
                text.AppendLine("3. 拿地后先勘测，再确定户型和施工队。");
                text.AppendLine();
                text.AppendLine("<b>镜头操作</b>");
                text.AppendLine("WASD 平移 · Q/E 旋转 · 滚轮缩放");
                text.AppendLine("中键拖动可快速浏览整张地图。");
                text.AppendLine();
            }
            if (p.LotId >= 0)
            {
                var lot = s.Lots[p.LotId];
                text.AppendLine($"<b>◆ {lot.Name}</b>");
                text.AppendLine($"面积：{lot.Area:N0} ㎡");
                text.AppendLine($"地质：{lot.Geology}");
                text.AppendLine($"特点：{lot.Tags}");
                text.AppendLine();
            }
            if (s.Phase == GamePhase.Tender)
            {
                var t = s.Tender;
                text.AppendLine("<b>◆ 竞价现场</b>");
                text.AppendLine($"当前报价：¥{t.CurrentBid:N0}");
                text.AppendLine($"你的上限：¥{s.AvailableBid():N0}");
                if (t.BudgetLocked) text.AppendLine($"锁定预算：¥{t.LockedBudget:N0}");
                if (p.CostReviewed > 0) text.AppendLine($"保守最高地价：¥{game.Costs.MaxAffordableLand(s):N0}");
                text.AppendLine().AppendLine("<b>AI 公开预算</b>");
                foreach (var ai in t.AI) text.AppendLine($"{ai.Name}：¥{ai.Budget:N0} · {(ai.Active ? "在场" : "退出")}");
            }
            if (s.Phase == GamePhase.SurveyDesign)
            {
                text.AppendLine("<b>◆ 设计方案</b>");
                text.AppendLine($"设计评分：{p.Design.Score()}");
                text.AppendLine($"附加成本：¥{p.Design.ExtraCost():N0}");
                text.AppendLine($"勘测状态：{(p.Surveyed ? "已完成" : "未完成")}");
            }
            if (p.TeamId >= 0)
            {
                var team = game.Map.Entity(p.TeamId);
                text.AppendLine().AppendLine("<b>◆ 施工组织</b>");
                text.AppendLine($"施工队：{team.Name}");
                text.AppendLine($"含运距报价：¥{p.TeamQuote:N0}");
                text.AppendLine($"预计工期：{game.Costs.TeamDays(s, game.Map, team)} 天");
                text.AppendLine($"质量 {team.Quality:0} · 风险 {team.Risk:P0}");
            }
            if (p.LotId >= 0 && s.Phase >= GamePhase.ConstructionPlanning && s.Phase != GamePhase.Settlement)
            {
                var quote = game.Costs.MaterialQuote(s, game.Map, Math.Max(0, p.SupplierId), p.TimberSupplierId < 0 ? 2 : p.TimberSupplierId);
                text.AppendLine().AppendLine("<b>◆ 材料到场价</b>");
                foreach (var line in quote.Lines) text.AppendLine(line);
                text.AppendLine($"<b>合计：¥{quote.Total:N0}</b>");
            }
            if (s.Phase == GamePhase.Construction || s.Phase == GamePhase.Sales)
            {
                text.AppendLine().AppendLine("<b>◆ 工程与销售</b>");
                text.AppendLine($"施工：{ConstructionSystem.StageName(p.Stage)} · {p.Progress:0.0}%");
                text.AppendLine($"停工剩余：{p.DelayDays} 天");
                text.AppendLine($"销售：{p.UnitsSold}/{p.UnitsTotal}");
                text.AppendLine($"累计回款：¥{p.SalesRevenue:N0}");
                text.AppendLine("六月现金预测：" + string.Join(" / ", game.CashForecast().Select(v => (v / 10000) + "万")));
            }
            if (s.Phase == GamePhase.Settlement)
            {
                text.AppendLine().AppendLine($"<size=30><b>最终评级：{game.Grade()}</b></size>").AppendLine(s.EndReason);
                text.AppendLine($"土地成本：¥{p.LandCost:N0}");
                text.AppendLine($"工程支出：¥{p.ConstructionSpent:N0}");
                text.AppendLine($"销售回款：¥{p.SalesRevenue:N0}");
                text.AppendLine($"最终现金：¥{s.Cash:N0}");
                text.AppendLine($"交付质量：{p.Quality:0}");
            }
            return text.ToString();
        }

        private static string PhaseName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MapPreview: return "开局预览";
                case GamePhase.Tender: return "公开竞价";
                case GamePhase.SurveyDesign: return "勘测设计";
                case GamePhase.ConstructionPlanning: return "组织施工";
                case GamePhase.Construction: return "动态建造";
                case GamePhase.Sales: return "预售回款";
                default: return "结算评价";
            }
        }

        private RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            return (RectTransform)go.transform;
        }

        private Text Label(Transform parent, string value, int size, TextAnchor anchor, Color color, bool bold)
        {
            var go = new GameObject(value, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = bold ? boldFont : bodyFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            // name 只是对象标识，显示内容必须真正写入 Text.text；此前漏掉这一行导致按钮只有背景。
            text.text = value;
            text.font.RequestCharactersInTexture(value, size, text.fontStyle);
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private Button Button(Transform parent, string label, Action action, Color background, Color foreground, int fontSize)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = background;
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, White, 0.18f);
            colors.pressedColor = Color.Lerp(background, Ink, 0.16f);
            colors.disabledColor = Rgb(183, 190, 190);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var text = Label(go.transform, label, fontSize, TextAnchor.MiddleCenter, foreground, true);
            Anchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-10, -4));
            button.onClick.AddListener(() =>
            {
                try
                {
                    action();
                    actionsDirty = true;
                    RefreshAll();
                }
                catch (Exception e) { app.Notify(e.Message); }
            });
            return button;
        }

        private Button ActionButton(string label, Action action)
        {
            Color background = Teal;
            if (label.Contains("开始") || label.Contains("确认") || label.Contains("开工") || label.Contains("开盘")) background = Orange;
            if (label.Contains("退出")) background = Coral;
            if (label.Contains("贷") || label.Contains("归还")) background = Purple;
            if (label.Contains("重摇") || label.Contains("格网") || label.Contains("保存") || label.Contains("读取") ||
                label.Contains("户型") || label.Contains("层数") || label.Contains("朝向") || label.Contains("等级") ||
                label.Contains("车库") || label.Contains("建材商") || label.Contains("施工队")) background = Sky;
            var button = Button(actionContent, label, action, background, label.Contains("开始") || label.Contains("确认") || label.Contains("开工") || label.Contains("开盘") ? Ink : White, 20);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 60;
            layout.minHeight = 60;
            return button;
        }

        private void Header(string label)
        {
            var text = Label(actionContent, label, 18, TextAnchor.MiddleLeft, Muted, true);
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 38;
        }

        private static void AddShadow(GameObject go)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.04f, 0.12f, 0.18f, 0.22f);
            shadow.effectDistance = new Vector2(0, -4);
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 128;
            const int radius = 24;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nearestX = Mathf.Clamp(x, radius, size - radius - 1);
                    float nearestY = Mathf.Clamp(y, radius, size - radius - 1);
                    float dx = x - nearestX;
                    float dy = y - nearestY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
