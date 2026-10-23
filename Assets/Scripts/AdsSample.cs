using System;
using System.Collections;
using TMPro;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

public class AdsSample : MonoBehaviour
{
    public static AdsSample Instance { get; private set; }

    [Header("LevelPlay Ads")]
    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedVideoAd;
    private bool isAdsEnabled = false;
    private bool isBannerVisible = true;

    [Header("Player & Game State")]
    public int playerCoins = 1000;
    public int currentLevel = 1;
    public float boostRemainingSeconds = 0f;
    public bool IsBoostActive => boostRemainingSeconds > 0;

    [Header("UI References - Top HUD")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI boostStatusText;
    public TextMeshProUGUI bannerStatusText;

    [Header("UI References - Rewarded Boost Card")]
    public TextMeshProUGUI rewardTitleText;
    public TextMeshProUGUI rewardDescText;
    public TextMeshProUGUI rewardStatusText;
    public Button watchRewardButton;
    public TextMeshProUGUI watchRewardButtonText;

    [Header("UI References - Stage Clear / Interstitial Flow")]
    public GameObject levelCompleteModal;
    public TextMeshProUGUI modalLevelTitleText;
    public TextMeshProUGUI modalScoreText;
    public Button nextLevelButton;
    public Button replayButton;

    [Header("UI References - Bottom Banner Dock")]
    public GameObject bannerSafeAreaDock;
    public GameObject bannerMockPreview;
    public Button toggleBannerButton;

    [Header("UI References - Toast & Editor Simulator")]
    public GameObject rewardToast;
    public TextMeshProUGUI rewardToastText;
    public GameObject simulationOverlay;
    public TextMeshProUGUI simulationTitleText;
    public TextMeshProUGUI simulationDescText;
    public Button simulationCloseButton;

    private Coroutine toastCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        EnsureUIExists();
        AutoBindUI();
    }

    [ContextMenu("Rebuild Game Ads UI")]
    public void EnsureUIExists()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        if (canvas.transform.Find("TopHUD") != null) return;

        // Configure Canvas Scaler
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Clean up old placeholder buttons if any
        string[] oldNames = {
            "Button Load Banner", "Button Load Interstitial", "Button Load Reward",
            "Button Show Banner", "Button Show Interstitial", "Button Hide Banner",
            "Image"
        };
        foreach (var oldName in oldNames)
        {
            var oldObj = canvas.transform.Find(oldName);
            if (oldObj != null) Destroy(oldObj.gameObject);
        }

        // 1. GAME BACKGROUND
        var bg = CreateUIObject("GameBackground", canvas.transform);
        SetFullStretch(bg);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.10f, 0.15f, 1f);
        bgImg.raycastTarget = false;
        bg.SetAsFirstSibling();

        // 2. TOP HUD
        var topHUD = CreateUIObject("TopHUD", canvas.transform);
        SetRect(topHUD, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(0, 90));
        topHUD.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

        var topDivider = CreateUIObject("TopDivider", topHUD);
        SetRect(topDivider, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 3));
        topDivider.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.85f, 0.6f);

        var levelBadge = CreateUIObject("LevelBadge", topHUD);
        SetRect(levelBadge, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(200, 50));
        levelBadge.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f, 1f);
        var lText = CreateTMP(levelBadge, "LevelText", "STAGE 1", font, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(lText.rectTransform);

        var boostBadge = CreateUIObject("BoostBadge", topHUD);
        SetRect(boostBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50, 0), new Vector2(580, 50));
        boostBadge.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.20f, 0.28f, 0.9f);
        var bText = CreateTMP(boostBadge, "BoostStatusText", "<color=#AAAAAA>50% Double Drops Boost:</color> <color=#FF7777>INACTIVE</color>", font, 22, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(bText.rectTransform);

        var coinBadge = CreateUIObject("CoinBadge", topHUD);
        SetRect(coinBadge, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-280, 0), new Vector2(200, 50));
        coinBadge.gameObject.AddComponent<Image>().color = new Color(0.28f, 0.22f, 0.08f, 1f);
        var cText = CreateTMP(coinBadge, "CoinsText", "Coins 1,000", font, 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetFullStretch(cText.rectTransform);

        var bannerStatusBadge = CreateUIObject("BannerStatusBadge", topHUD);
        SetRect(bannerStatusBadge, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(200, 40));
        var banText = CreateTMP(bannerStatusBadge, "BannerStatusText", "● Banner: Active", font, 18, FontStyles.Normal, TextAlignmentOptions.Right, new Color(0.3f, 0.9f, 0.5f, 1f));
        SetFullStretch(banText.rectTransform);

        // 3. MAIN CONTENT SAFE AREA
        var contentArea = CreateUIObject("MainContentArea", canvas.transform);
        SetRect(contentArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        contentArea.offsetMin = new Vector2(0, 130);
        contentArea.offsetMax = new Vector2(0, -90);

        var titleHeader = CreateUIObject("TitleHeader", contentArea);
        SetRect(titleHeader, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -35), new Vector2(1150, 70));
        CreateTMP(titleHeader, "GameTitleText", "DUNGEON ADVENTURE  •  MOBILE ADS DEMO\n<size=19><color=#88AACC>Fitted for LevelPlay Banner (Bottom Safe Area), Interstitial (Transitions), & Rewarded (Boosts)</color></size>", font, 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // 4. REWARDED BOOST CARD
        var rewardedCard = CreateUIObject("RewardedBoostCard", contentArea);
        SetRect(rewardedCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1200, 240));
        rewardedCard.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.98f);

        var rewardedAccent = CreateUIObject("RewardedAccent", rewardedCard);
        SetRect(rewardedAccent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(6, 0));
        rewardedAccent.gameObject.AddComponent<Image>().color = new Color(1f, 0.75f, 0.1f, 1f);

        var rewardIconBox = CreateUIObject("RewardIconBox", rewardedCard);
        SetRect(rewardIconBox, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110, 0), new Vector2(150, 150));
        rewardIconBox.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.25f, 0.38f, 0.8f);
        var iconTMP = CreateTMP(rewardIconBox, "IconText", "⚡", font, 72, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetFullStretch(iconTMP.rectTransform);

        var rTitle = CreateTMP(rewardedCard, "RewardTitleText", "50% Double Drops Boost  <color=#FFD700><size=20>[▶ REWARDED AD]</size></color>", font, 30, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.2f, 1f));
        SetRect(rTitle.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, 55), new Vector2(680, 45));

        var rDesc = CreateTMP(rewardedCard, "RewardDescText", "Watch a short rewarded video to boost all coin & loot drops by +50% for 15 minutes!\nGrants +250 Bonus Coins immediately upon completion.", font, 20, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.9f, 0.92f, 0.95f, 1f));
        SetRect(rDesc.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, -5), new Vector2(680, 50));

        var rStatus = CreateTMP(rewardedCard, "RewardStatusText", "<color=#FFCC00>● Free Boost Ready:</color> Watch video to activate 15m buff!", font, 19, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.4f, 0.9f, 0.6f, 1f));
        SetRect(rStatus.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, -60), new Vector2(680, 35));

        var watchBtnGO = CreateUIObject("Button Watch Rewarded", rewardedCard);
        SetRect(watchBtnGO, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(250, 85));
        watchBtnGO.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.65f, 0.35f, 1f);
        watchBtnGO.gameObject.AddComponent<Button>();
        var watchTxt = CreateTMP(watchBtnGO, "WatchText", "WATCH AD\n<size=17>+15m Buff & 250 Coins</size>", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(watchTxt.rectTransform);

        // 5. STAGE PROGRESSION CARD
        var stageCard = CreateUIObject("StageProgressCard", contentArea);
        SetRect(stageCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(1200, 210));
        stageCard.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.98f);

        var stageAccent = CreateUIObject("StageAccent", stageCard);
        SetRect(stageAccent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(6, 0));
        stageAccent.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 1f);

        var stageIconBox = CreateUIObject("StageIconBox", stageCard);
        SetRect(stageIconBox, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110, 0), new Vector2(130, 130));
        stageIconBox.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.36f, 0.8f);
        var sIconTMP = CreateTMP(stageIconBox, "StageIconText", "🏰", font, 64, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 1f, 1f));
        SetFullStretch(sIconTMP.rectTransform);

        var sTitle = CreateTMP(stageCard, "StageTitleText", "DUNGEON STAGE PROGRESSION  <color=#88CCFF><size=20>[NATURAL TRANSITION]</size></color>", font, 28, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.4f, 0.8f, 1f, 1f));
        SetRect(sTitle.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, 45), new Vector2(680, 45));

        var sDesc = CreateTMP(stageCard, "StageDescText", "Natural breaks between game levels are the best placement for Interstitial Ads.\nDefeat dungeon monsters and advance to the next level!", font, 20, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.9f, 0.92f, 0.95f, 1f));
        SetRect(sDesc.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, -15), new Vector2(680, 50));

        var completeStageBtnGO = CreateUIObject("Button Complete Stage", stageCard);
        SetRect(completeStageBtnGO, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(250, 80));
        completeStageBtnGO.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.45f, 0.88f, 1f);
        var compBtn = completeStageBtnGO.gameObject.AddComponent<Button>();
        var compTxt = CreateTMP(completeStageBtnGO, "CompleteText", "CLEAR STAGE ▶\n<size=17>Show Interstitial</size>", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(compTxt.rectTransform);
        compBtn.onClick.AddListener(OpenLevelCompleteModal);

        // 6. LEVEL COMPLETE MODAL
        var modal = CreateUIObject("LevelCompleteModal", canvas.transform);
        SetFullStretch(modal);
        modal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.82f);
        modal.gameObject.SetActive(false);

        var modalBox = CreateUIObject("ModalDialogBox", modal);
        SetRect(modalBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850, 520));
        modalBox.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.14f, 0.22f, 1f);

        var mTitle = CreateTMP(modalBox, "ModalLevelTitleText", "STAGE 1 CLEARED! <color=#FFD700>★★★</color>", font, 40, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetRect(mTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(800, 60));

        var mScore = CreateTMP(modalBox, "ModalScoreText", "Score: 12,500   •   Stage Reward: +100 Coins\n<size=19><color=#88CCFF>[ Interstitial Ad will play on Next Level ]</color></size>", font, 24, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetRect(mScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(750, 100));

        var nextLevelBtnGO = CreateUIObject("Button Next Level", modalBox);
        SetRect(nextLevelBtnGO, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(160, 90), new Vector2(280, 75));
        nextLevelBtnGO.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.68f, 0.35f, 1f);
        nextLevelBtnGO.gameObject.AddComponent<Button>();
        var nextTxt = CreateTMP(nextLevelBtnGO, "NextLevelText", "NEXT LEVEL ▶\n<size=16>Watch Interstitial</size>", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(nextTxt.rectTransform);

        var replayBtnGO = CreateUIObject("Button Replay", modalBox);
        SetRect(replayBtnGO, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-160, 90), new Vector2(240, 75));
        replayBtnGO.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.30f, 0.42f, 1f);
        replayBtnGO.gameObject.AddComponent<Button>();
        var repTxt = CreateTMP(replayBtnGO, "ReplayText", "REPLAY ↺", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(repTxt.rectTransform);

        // 7. BOTTOM BANNER SAFE AREA DOCK
        var bannerDock = CreateUIObject("BottomBannerSafeArea", canvas.transform);
        SetRect(bannerDock, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 65), new Vector2(0, 130));
        bannerDock.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

        var bannerDivider = CreateUIObject("BannerDivider", bannerDock);
        SetRect(bannerDivider, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 3));
        bannerDivider.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.35f, 0.65f, 0.6f);

        var bannerMock = CreateUIObject("BannerMockPreview", bannerDock);
        SetRect(bannerMock, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(728, 90));
        bannerMock.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.14f, 0.22f, 1f);
        var mockTxt = CreateTMP(bannerMock, "BannerMockText", "LEVELPLAY BANNER AD (320x50)  •  Position: BottomCenter\n<size=16><color=#7799BB>Ad Unit: o8efz2kc3xxnb1ou  •  Reserved Safe Area Zone (Zero UI Obstruction)</color></size>", font, 19, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(mockTxt.rectTransform);

        var toggleBannerGO = CreateUIObject("Button Toggle Banner", bannerDock);
        SetRect(toggleBannerGO, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(190, 50));
        toggleBannerGO.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.28f, 0.40f, 1f);
        toggleBannerGO.gameObject.AddComponent<Button>();
        var togTxt = CreateTMP(toggleBannerGO, "ToggleText", "Toggle Banner", font, 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(togTxt.rectTransform);

        // 8. REWARD TOAST
        var toast = CreateUIObject("RewardToast", canvas.transform);
        SetRect(toast, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(750, 60));
        toast.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.58f, 0.32f, 0.98f);
        var toastTxt = CreateTMP(toast, "ToastText", "🎉 REWARD RECEIVED! +250 Coins & 15m Double Drops Activated!", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(toastTxt.rectTransform);
        toast.gameObject.SetActive(false);

        // 9. AD SIMULATION OVERLAY
        var simOverlay = CreateUIObject("AdSimulationOverlay", canvas.transform);
        SetFullStretch(simOverlay);
        simOverlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.90f);
        simOverlay.gameObject.SetActive(false);

        var simBox = CreateUIObject("SimulationBox", simOverlay);
        SetRect(simBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 520));
        simBox.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.26f, 1f);

        var sTitleTMP = CreateTMP(simBox, "SimulationTitleText", "REWARDED VIDEO AD (Simulated)", font, 36, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetRect(sTitleTMP.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(850, 60));

        var sDescTMP = CreateTMP(simBox, "SimulationDescText", "Simulated Video Ad is playing...\n\nClick below to finish ad and receive your reward.", font, 24, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetRect(sDescTMP.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(800, 140));

        var simBtnGO = CreateUIObject("SimulationCloseButton", simBox);
        SetRect(simBtnGO, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 75), new Vector2(400, 75));
        simBtnGO.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.68f, 0.35f, 1f);
        simBtnGO.gameObject.AddComponent<Button>();
        var sBtnTxt = CreateTMP(simBtnGO, "SimBtnText", "COMPLETE AD & CONTINUE ▶", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(sBtnTxt.rectTransform);

        AutoBindUI();
    }

    private RectTransform CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    private TextMeshProUGUI CreateTMP(RectTransform parent, string name, string text, TMP_FontAsset font, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        var rt = CreateUIObject(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    public void AutoBindUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Auto find HUD
        if (levelText == null) levelText = FindComponentByName<TextMeshProUGUI>("LevelText", canvas.transform);
        if (coinsText == null) coinsText = FindComponentByName<TextMeshProUGUI>("CoinsText", canvas.transform);
        if (boostStatusText == null) boostStatusText = FindComponentByName<TextMeshProUGUI>("BoostStatusText", canvas.transform);
        if (bannerStatusText == null) bannerStatusText = FindComponentByName<TextMeshProUGUI>("BannerStatusText", canvas.transform);

        // Auto find Rewarded Card
        if (rewardTitleText == null) rewardTitleText = FindComponentByName<TextMeshProUGUI>("RewardTitleText", canvas.transform);
        if (rewardDescText == null) rewardDescText = FindComponentByName<TextMeshProUGUI>("RewardDescText", canvas.transform);
        if (rewardStatusText == null) rewardStatusText = FindComponentByName<TextMeshProUGUI>("RewardStatusText", canvas.transform);
        if (watchRewardButton == null) watchRewardButton = FindComponentByName<Button>("Button Watch Rewarded", canvas.transform);
        if (watchRewardButtonText == null && watchRewardButton != null) watchRewardButtonText = watchRewardButton.GetComponentInChildren<TextMeshProUGUI>();

        // Auto find Interstitial / Level Complete Modal
        if (levelCompleteModal == null)
        {
            var modal = FindTransformByName("LevelCompleteModal", canvas.transform);
            if (modal != null) levelCompleteModal = modal.gameObject;
        }
        if (modalLevelTitleText == null) modalLevelTitleText = FindComponentByName<TextMeshProUGUI>("ModalLevelTitleText", canvas.transform);
        if (modalScoreText == null) modalScoreText = FindComponentByName<TextMeshProUGUI>("ModalScoreText", canvas.transform);
        if (nextLevelButton == null) nextLevelButton = FindComponentByName<Button>("Button Next Level", canvas.transform);
        if (replayButton == null) replayButton = FindComponentByName<Button>("Button Replay", canvas.transform);

        // Auto find Banner Safe Area Dock
        if (bannerSafeAreaDock == null)
        {
            var dock = FindTransformByName("BottomBannerSafeArea", canvas.transform);
            if (dock != null) bannerSafeAreaDock = dock.gameObject;
        }
        if (bannerMockPreview == null)
        {
            var preview = FindTransformByName("BannerMockPreview", canvas.transform);
            if (preview != null) bannerMockPreview = preview.gameObject;
        }
        if (toggleBannerButton == null) toggleBannerButton = FindComponentByName<Button>("Button Toggle Banner", canvas.transform);

        // Auto find Toast
        if (rewardToast == null)
        {
            var toast = FindTransformByName("RewardToast", canvas.transform);
            if (toast != null) rewardToast = toast.gameObject;
        }
        if (rewardToastText == null && rewardToast != null) rewardToastText = rewardToast.GetComponentInChildren<TextMeshProUGUI>();

        // Auto find Simulation Overlay
        if (simulationOverlay == null)
        {
            var sim = FindTransformByName("AdSimulationOverlay", canvas.transform);
            if (sim != null) simulationOverlay = sim.gameObject;
        }
        if (simulationTitleText == null) simulationTitleText = FindComponentByName<TextMeshProUGUI>("SimulationTitleText", canvas.transform);
        if (simulationDescText == null) simulationDescText = FindComponentByName<TextMeshProUGUI>("SimulationDescText", canvas.transform);
        if (simulationCloseButton == null) simulationCloseButton = FindComponentByName<Button>("SimulationCloseButton", canvas.transform);

        // Wire runtime button listeners
        if (watchRewardButton != null)
        {
            watchRewardButton.onClick.RemoveListener(OnWatchRewardClicked);
            watchRewardButton.onClick.AddListener(OnWatchRewardClicked);
        }
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }
        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(OnReplayStageClicked);
            replayButton.onClick.AddListener(OnReplayStageClicked);
        }
        if (toggleBannerButton != null)
        {
            toggleBannerButton.onClick.RemoveListener(ToggleBannerAds);
            toggleBannerButton.onClick.AddListener(ToggleBannerAds);
        }
        if (simulationCloseButton != null)
        {
            simulationCloseButton.onClick.RemoveListener(OnSimulationCompleteButtonClicked);
            simulationCloseButton.onClick.AddListener(OnSimulationCompleteButtonClicked);
        }
    }

    private T FindComponentByName<T>(string name, Transform root) where T : Component
    {
        var target = FindTransformByName(name, root);
        return target != null ? target.GetComponent<T>() : null;
    }

    private Transform FindTransformByName(string name, Transform root)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindTransformByName(name, root.GetChild(i));
            if (result != null) return result;
        }
        return null;
    }

    public void Start()
    {
        UpdateUI();

        Debug.Log("[AdsSample] Validating Integration and Initializing LevelPlay...");
        LevelPlay.ValidateIntegration();

        LevelPlay.OnInitSuccess += SdkInitCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitFailedEvent;

        LevelPlay.Init(AdConfig.AppKey);
    }

    private void Update()
    {
        // Live countdown for active 15-minute boost
        if (boostRemainingSeconds > 0)
        {
            boostRemainingSeconds -= Time.deltaTime;
            if (boostRemainingSeconds <= 0)
            {
                boostRemainingSeconds = 0;
                Debug.Log("[AdsSample] 50% Double Drops Boost has expired.");
            }
            UpdateBoostUI();
        }
    }

    private void SdkInitFailedEvent(LevelPlayInitError error)
    {
        Debug.LogWarning($"[AdsSample] LevelPlay Init Failed: {error}");
        isAdsEnabled = false;
        UpdateBannerStatusUI("Init Failed");
    }

    private void SdkInitCompletedEvent(LevelPlayConfiguration configuration)
    {
        Debug.Log("[AdsSample] LevelPlay SDK Initialized Successfully.");
        isAdsEnabled = true;
        EnableAds();
    }

    private void EnableAds()
    {
        // 1. Configure Banner Ad at BottomCenter specifically for reserved safe area
        var configBuilder = new LevelPlayBannerAd.Config.Builder();
        configBuilder.SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter);
        var bannerConfig = configBuilder.Build();

        bannerAd = new LevelPlayBannerAd(AdConfig.BannerAdUnitId, bannerConfig);
        bannerAd.OnAdLoaded += OnBannerLoaded;
        bannerAd.OnAdLoadFailed += OnBannerLoadFailed;
        bannerAd.OnAdDisplayed += OnBannerDisplayed;

        // Auto load and show banner to fit into bottom safe area
        LoadBannerAds();

        // 2. Interstitial Ad for stage transitions
        interstitialAd = new LevelPlayInterstitialAd(AdConfig.InterstitialAdUnitId);
        interstitialAd.OnAdLoaded += OnInterstitialLoaded;
        interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
        interstitialAd.OnAdClosed += OnInterstitialClosed;
        interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
        LoadInterstitialAds();

        // 3. Rewarded Video Ad for opt-in 50% Double Drops buff
        rewardedVideoAd = new LevelPlayRewardedAd(AdConfig.RewardedVideoAdUnitId);
        rewardedVideoAd.OnAdLoaded += OnRewardedLoaded;
        rewardedVideoAd.OnAdLoadFailed += OnRewardedLoadFailed;
        rewardedVideoAd.OnAdRewarded += OnRewardedAdRewarded;
        rewardedVideoAd.OnAdClosed += OnRewardedClosed;
        rewardedVideoAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
        LoadRewardedVideoAds();

        UpdateUI();
    }

    // ==========================================
    // BANNER AD HANDLERS (Fitted to Bottom Safe Area)
    // ==========================================

    public void LoadBannerAds()
    {
        if (bannerAd != null)
        {
            Debug.Log("[AdsSample] Loading Banner Ad...");
            bannerAd.LoadAd();
        }
    }

    public void ShowBannerAds()
    {
        isBannerVisible = true;
        if (bannerAd != null)
        {
            bannerAd.ShowAd();
        }
        if (bannerMockPreview != null) bannerMockPreview.SetActive(true);
        UpdateBannerStatusUI("Banner: Visible");
    }

    public void HideBannerAds()
    {
        isBannerVisible = false;
        if (bannerAd != null)
        {
            bannerAd.HideAd();
        }
        if (bannerMockPreview != null) bannerMockPreview.SetActive(false);
        UpdateBannerStatusUI("Banner: Hidden");
    }

    public void ToggleBannerAds()
    {
        if (isBannerVisible)
        {
            HideBannerAds();
        }
        else
        {
            ShowBannerAds();
        }
    }

    private void OnBannerLoaded(LevelPlayAdInfo info)
    {
        Debug.Log("[AdsSample] Banner Ad Loaded.");
        if (isBannerVisible)
        {
            ShowBannerAds();
        }
    }

    private void OnBannerLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdsSample] Banner Ad Failed to Load: {error}");
        UpdateBannerStatusUI("Banner: Standby");
    }

    private void OnBannerDisplayed(LevelPlayAdInfo info)
    {
        Debug.Log("[AdsSample] Banner Ad Displayed at BottomCenter.");
        UpdateBannerStatusUI("Banner: Active");
    }

    // ==========================================
    // INTERSTITIAL AD HANDLERS (Natural Stage Transitions)
    // ==========================================

    public void LoadInterstitialAds()
    {
        if (interstitialAd != null)
        {
            interstitialAd.LoadAd();
        }
    }

    // Triggered when player finishes a gameplay stage
    public void OpenLevelCompleteModal()
    {
        if (levelCompleteModal != null)
        {
            levelCompleteModal.SetActive(true);
        }
        if (modalLevelTitleText != null)
        {
            modalLevelTitleText.text = $"STAGE {currentLevel} CLEARED! <color=#FFD700>★★★</color>";
        }
        if (modalScoreText != null)
        {
            modalScoreText.text = $"Score: {(currentLevel * 12500):N0}   •   Stage Reward: +100 Coins\n<size=18><color=#88CCFF>[ Interstitial Ad will play on Next Level ]</color></size>";
        }
    }

    // Triggered by "Next Level" button in the Level Complete Screen
    public void OnNextLevelClicked()
    {
        Debug.Log("[AdsSample] 'Next Level' clicked - Initiating natural interstitial ad transition.");

        if (levelCompleteModal != null)
        {
            levelCompleteModal.SetActive(false);
        }

        // Show interstitial ad if ready
        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
        else
        {
#if UNITY_EDITOR
            // In Unity Editor, display sleek simulation overlay
            ShowEditorAdSimulation("INTERSTITIAL AD (Simulated)", "Natural stage transition ad is playing...\n\nClick below to finish ad and enter Next Stage.", () =>
            {
                AdvanceToNextLevel();
            });
#else
            Debug.Log("[AdsSample] Interstitial Ad not ready, proceeding smoothly to next stage.");
            AdvanceToNextLevel();
#endif
        }
    }

    public void ShowInterstitialAds()
    {
        OnNextLevelClicked();
    }

    private void OnInterstitialLoaded(LevelPlayAdInfo info)
    {
        Debug.Log("[AdsSample] Interstitial Ad Loaded and ready for next transition.");
    }

    private void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdsSample] Interstitial Ad Failed to Load: {error}");
    }

    private void OnInterstitialClosed(LevelPlayAdInfo info)
    {
        Debug.Log("[AdsSample] Interstitial Ad closed by user. Advancing to next stage.");
        AdvanceToNextLevel();
        LoadInterstitialAds(); // Preload next interstitial
    }

    private void OnInterstitialDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdsSample] Interstitial Display Failed: {error}. Advancing anyway.");
        AdvanceToNextLevel();
        LoadInterstitialAds();
    }

    private void AdvanceToNextLevel()
    {
        currentLevel++;
        playerCoins += 100; // Stage clear coin reward
        ShowToast($"STAGE {currentLevel - 1} COMPLETE! +100 Coins. Entering STAGE {currentLevel}!");
        UpdateUI();
    }

    public void OnReplayStageClicked()
    {
        if (levelCompleteModal != null)
        {
            levelCompleteModal.SetActive(false);
        }
        ShowToast($"Restarting Stage {currentLevel}...");
    }

    // ==========================================
    // REWARDED VIDEO AD HANDLERS (Opt-in Buff & Coins)
    // ==========================================

    public void LoadRewardedVideoAds()
    {
        if (rewardedVideoAd != null)
        {
            rewardedVideoAd.LoadAd();
        }
    }

    // Triggered by "Watch Ad" button on Rewarded Boost Card
    public void OnWatchRewardClicked()
    {
        Debug.Log("[AdsSample] Rewarded Ad opt-in requested by player.");

        if (rewardedVideoAd != null && rewardedVideoAd.IsAdReady())
        {
            rewardedVideoAd.ShowAd();
        }
        else
        {
#if UNITY_EDITOR
            // In Unity Editor, display simulation overlay
            ShowEditorAdSimulation("REWARDED VIDEO AD (Simulated)", "Sponsor video is playing (15s)...\n\nClick below to complete ad and claim 50% Double Drops + 250 Coins!", () =>
            {
                GrantRewardedBuff();
            });
#else
            ShowToast("Ad is currently loading... Please try again in a moment.");
            LoadRewardedVideoAds();
#endif
        }
    }

    public void ShowRewardedVideoAds()
    {
        OnWatchRewardClicked();
    }

    private void OnRewardedLoaded(LevelPlayAdInfo info)
    {
        Debug.Log("[AdsSample] Rewarded Video Ad Loaded and ready.");
        UpdateBoostUI();
    }

    private void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdsSample] Rewarded Video Failed to Load: {error}");
        UpdateBoostUI();
    }

    private void OnRewardedAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        Debug.Log($"[AdsSample] User earned reward: {reward?.Name} ({reward?.Amount})");
        GrantRewardedBuff();
    }

    private void GrantRewardedBuff()
    {
        // 1. Add 15 minutes (= 900 seconds) to boost
        boostRemainingSeconds += 900f;

        // 2. Grant +250 bonus coins
        playerCoins += 250;

        ShowToast("🎉 REWARD RECEIVED! +250 Coins & 15m Double Drops Activated!");
        UpdateUI();
        LoadRewardedVideoAds(); // Preload next rewarded ad
    }

    private void OnRewardedClosed(LevelPlayAdInfo info)
    {
        Debug.Log("[AdsSample] Rewarded Video Ad Closed.");
        LoadRewardedVideoAds();
    }

    private void OnRewardedDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdsSample] Rewarded Video Display Failed: {error}");
        LoadRewardedVideoAds();
    }

    // ==========================================
    // UI UPDATES & FORMATTING
    // ==========================================

    public void UpdateUI()
    {
        if (levelText != null)
        {
            levelText.text = $"STAGE {currentLevel}";
        }
        if (coinsText != null)
        {
            coinsText.text = $"Coins {playerCoins:N0}";
        }
        UpdateBoostUI();
        UpdateBannerStatusUI(isBannerVisible ? "Banner: Active" : "Banner: Hidden");
    }

    public void UpdateBoostUI()
    {
        if (IsBoostActive)
        {
            TimeSpan time = TimeSpan.FromSeconds(boostRemainingSeconds);
            string formattedTime = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);

            if (boostStatusText != null)
            {
                boostStatusText.text = $"<color=#00FFAA>50% DOUBLE DROPS:</color> <color=#FFD700>{formattedTime}</color>";
            }
            if (rewardStatusText != null)
            {
                rewardStatusText.text = $"<color=#00FFAA>● ACTIVE BUFF:</color> {formattedTime} remaining (+50% loot & coins active!)";
            }
            if (watchRewardButtonText != null)
            {
                watchRewardButtonText.text = $"EXTEND (+15m)\n<size=16>Watch Ad (+250 Coins)</size>";
            }
        }
        else
        {
            if (boostStatusText != null)
            {
                boostStatusText.text = "<color=#AAAAAA>50% Double Drops:</color> <color=#FF7777>INACTIVE</color>";
            }
            if (rewardStatusText != null)
            {
                rewardStatusText.text = "<color=#FFCC00>● Free Boost Ready:</color> Watch a short video to activate 15m buff!";
            }
            if (watchRewardButtonText != null)
            {
                watchRewardButtonText.text = $"WATCH AD\n<size=16>+15m Buff & 250 Coins</size>";
            }
        }
    }

    private void UpdateBannerStatusUI(string status)
    {
        if (bannerStatusText != null)
        {
            bannerStatusText.text = status;
        }
    }

    public void ShowToast(string message)
    {
        if (rewardToast == null || rewardToastText == null) return;

        rewardToastText.text = message;
        rewardToast.SetActive(true);

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(HideToastAfterDelay(3.5f));
    }

    private IEnumerator HideToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rewardToast != null)
        {
            rewardToast.SetActive(false);
        }
    }

    // ==========================================
    // EDITOR AD SIMULATION OVERLAY
    // ==========================================

    private Action onSimulationCompleteAction;

    public void ShowEditorAdSimulation(string title, string description, Action onComplete)
    {
        if (simulationOverlay == null)
        {
            onComplete?.Invoke();
            return;
        }

        onSimulationCompleteAction = onComplete;
        if (simulationTitleText != null) simulationTitleText.text = title;
        if (simulationDescText != null) simulationDescText.text = description;

        simulationOverlay.SetActive(true);
    }

    public void OnSimulationCompleteButtonClicked()
    {
        if (simulationOverlay != null)
        {
            simulationOverlay.SetActive(false);
        }
        onSimulationCompleteAction?.Invoke();
        onSimulationCompleteAction = null;
    }

    private void OnDisable()
    {
        bannerAd?.DestroyAd();
        interstitialAd?.DestroyAd();
    }
}
