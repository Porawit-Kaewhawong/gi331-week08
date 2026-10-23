using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class GameAdsUISetup : MonoBehaviour
{
    static GameAdsUISetup()
    {
        EditorApplication.delayCall += () =>
        {
            // Auto check if scene has the upgraded UI; if not, build it automatically!
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null && canvas.transform.Find("TopHUD") == null)
            {
                Debug.Log("[GameAdsUISetup] Automatically configuring Game UI for Ads Banner, Interstitial, and Rewarded...");
                BuildGameAdsUI();
            }
        };
    }

    [MenuItem("Tools/Setup Game Ads UI (Banner, Interstitial, Rewarded)")]
    public static void BuildGameAdsUI()
    {
        var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GameAdsUISetup] No Canvas found in scene!");
            return;
        }

        // Configure Canvas Scaler for 1920x1080 landscape
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        // Ensure EventSystem exists
        if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
        }

        // Font Asset
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        // Clean up old loose buttons if they exist
        string[] oldNames = {
            "Button Load Banner", "Button Load Interstitial", "Button Load Reward",
            "Button Show Banner", "Button Show Interstitial", "Button Hide Banner",
            "Image"
        };
        foreach (var oldName in oldNames)
        {
            var oldObj = canvas.transform.Find(oldName);
            if (oldObj != null)
            {
                DestroyImmediate(oldObj.gameObject);
            }
        }

        // Ensure AdsSample GameObject exists with script
        var adsSampleGO = GameObject.Find("AdsSample");
        if (adsSampleGO == null)
        {
            adsSampleGO = new GameObject("AdsSample");
            Undo.RegisterCreatedObjectUndo(adsSampleGO, "Create AdsSample");
        }
        var adsSample = adsSampleGO.GetComponent<AdsSample>();
        if (adsSample == null) adsSample = adsSampleGO.AddComponent<AdsSample>();

        // 1. GAME BACKGROUND
        var bg = GetOrCreateChild(canvas.transform, "GameBackground");
        SetFullStretch(bg);
        var bgImg = GetOrAddComponent<Image>(bg);
        bgImg.color = new Color(0.08f, 0.10f, 0.15f, 1f);
        bgImg.raycastTarget = false;
        bg.SetAsFirstSibling();

        // 2. TOP HUD (Header Bar)
        var topHUD = GetOrCreateChild(canvas.transform, "TopHUD");
        SetRect(topHUD, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(0, 90));
        var topHUDImg = GetOrAddComponent<Image>(topHUD);
        topHUDImg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

        // Divider under Top HUD
        var topDivider = GetOrCreateChild(topHUD, "TopDivider");
        SetRect(topDivider, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 3));
        GetOrAddComponent<Image>(topDivider).color = new Color(0.25f, 0.45f, 0.85f, 0.6f);

        // Top HUD Elements: Level Badge
        var levelBadge = GetOrCreateChild(topHUD, "LevelBadge");
        SetRect(levelBadge, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(200, 50));
        GetOrAddComponent<Image>(levelBadge).color = new Color(0.18f, 0.22f, 0.32f, 1f);
        var levelText = CreateTMP(levelBadge, "LevelText", "STAGE 1", font, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(levelText.rectTransform);

        // Top HUD Elements: Active Boost Status (Live Countdown)
        var boostBadge = GetOrCreateChild(topHUD, "BoostBadge");
        SetRect(boostBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50, 0), new Vector2(580, 50));
        GetOrAddComponent<Image>(boostBadge).color = new Color(0.15f, 0.20f, 0.28f, 0.9f);
        var boostStatusText = CreateTMP(boostBadge, "BoostStatusText", "<color=#AAAAAA>⚡ 50% Double Drops:</color> <color=#FF7777>INACTIVE</color>", font, 22, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(boostStatusText.rectTransform);

        // Top HUD Elements: Coin Counter
        var coinBadge = GetOrCreateChild(topHUD, "CoinBadge");
        SetRect(coinBadge, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-280, 0), new Vector2(200, 50));
        GetOrAddComponent<Image>(coinBadge).color = new Color(0.28f, 0.22f, 0.08f, 1f);
        var coinsText = CreateTMP(coinBadge, "CoinsText", "🪙 1,000", font, 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetFullStretch(coinsText.rectTransform);

        // Top HUD Elements: Banner Status
        var bannerStatusBadge = GetOrCreateChild(topHUD, "BannerStatusBadge");
        SetRect(bannerStatusBadge, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(200, 40));
        var bannerStatusText = CreateTMP(bannerStatusBadge, "BannerStatusText", "● Banner: Active", font, 18, FontStyles.Normal, TextAlignmentOptions.Right, new Color(0.3f, 0.9f, 0.5f, 1f));
        SetFullStretch(bannerStatusText.rectTransform);

        // 3. MAIN CONTENT SAFE AREA (Offset 130px above bottom banner, 90px below top HUD)
        var contentArea = GetOrCreateChild(canvas.transform, "MainContentArea");
        SetRect(contentArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        contentArea.offsetMin = new Vector2(0, 130); // Leave room for bottom banner dock
        contentArea.offsetMax = new Vector2(0, -90); // Leave room for top HUD

        // Title Header
        var titleHeader = GetOrCreateChild(contentArea, "TitleHeader");
        SetRect(titleHeader, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -35), new Vector2(1150, 70));
        CreateTMP(titleHeader, "GameTitleText", "DUNGEON ADVENTURE  •  MOBILE ADS DEMO\n<size=19><color=#88AACC>Fitted for LevelPlay Banner (Bottom Safe Area), Interstitial (Transitions), & Rewarded (Boosts)</color></size>", font, 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        // 4. REWARDED BOOST CARD (Specifically fitted for Rewarded Video Ads)
        var rewardedCard = GetOrCreateChild(contentArea, "RewardedBoostCard");
        SetRect(rewardedCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1200, 240));
        var rewardedCardImg = GetOrAddComponent<Image>(rewardedCard);
        rewardedCardImg.color = new Color(0.12f, 0.16f, 0.24f, 0.98f);

        // Accent border for rewarded card
        var rewardedAccent = GetOrCreateChild(rewardedCard, "RewardedAccent");
        SetRect(rewardedAccent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(6, 0));
        GetOrAddComponent<Image>(rewardedAccent).color = new Color(1f, 0.75f, 0.1f, 1f);

        // Icon Box
        var rewardIconBox = GetOrCreateChild(rewardedCard, "RewardIconBox");
        SetRect(rewardIconBox, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110, 0), new Vector2(150, 150));
        GetOrAddComponent<Image>(rewardIconBox).color = new Color(0.20f, 0.25f, 0.38f, 0.8f);
        var iconTMP = CreateTMP(rewardIconBox, "IconText", "⚡", font, 72, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetFullStretch(iconTMP.rectTransform);

        // Reward Title
        var rewardTitleText = CreateTMP(rewardedCard, "RewardTitleText", "⚡ 50% DOUBLE DROPS BOOST  <color=#FFD700><size=20>[▶ REWARDED AD]</size></color>", font, 30, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.2f, 1f));
        SetRect(rewardTitleText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, 55), new Vector2(680, 45));

        // Reward Description
        var rewardDescText = CreateTMP(rewardedCard, "RewardDescText", "Watch a short rewarded video to boost all coin & loot drops by +50% for 15 minutes!\nGrants +250 Bonus Coins immediately upon completion.", font, 20, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.9f, 0.92f, 0.95f, 1f));
        SetRect(rewardDescText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, -5), new Vector2(680, 50));

        // Reward Status / Countdown
        var rewardStatusText = CreateTMP(rewardedCard, "RewardStatusText", "<color=#FFCC00>● Free Boost Ready:</color> Watch video to activate 15m buff!", font, 19, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.4f, 0.9f, 0.6f, 1f));
        SetRect(rewardStatusText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, -60), new Vector2(680, 35));

        // Watch Reward Button
        var watchRewardButtonGO = GetOrCreateChild(rewardedCard, "Button Watch Rewarded");
        SetRect(watchRewardButtonGO, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(250, 85));
        var watchBtnImg = GetOrAddComponent<Image>(watchRewardButtonGO);
        watchBtnImg.color = new Color(0.12f, 0.65f, 0.35f, 1f);
        var watchBtn = GetOrAddComponent<Button>(watchRewardButtonGO);
        var watchRewardButtonText = CreateTMP(watchRewardButtonGO, "WatchText", "▶ WATCH AD\n<size=17>+15m Buff & 250 🪙</size>", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(watchRewardButtonText.rectTransform);

        // 5. STAGE PROGRESSION CARD (Specifically fitted for Interstitial Ads)
        var stageCard = GetOrCreateChild(contentArea, "StageProgressCard");
        SetRect(stageCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(1200, 210));
        var stageCardImg = GetOrAddComponent<Image>(stageCard);
        stageCardImg.color = new Color(0.12f, 0.16f, 0.24f, 0.98f);

        // Accent border for stage card
        var stageAccent = GetOrCreateChild(stageCard, "StageAccent");
        SetRect(stageAccent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(6, 0));
        GetOrAddComponent<Image>(stageAccent).color = new Color(0.2f, 0.6f, 1f, 1f);

        // Icon Box
        var stageIconBox = GetOrCreateChild(stageCard, "StageIconBox");
        SetRect(stageIconBox, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110, 0), new Vector2(130, 130));
        GetOrAddComponent<Image>(stageIconBox).color = new Color(0.18f, 0.24f, 0.36f, 0.8f);
        var stageIconTMP = CreateTMP(stageIconBox, "StageIconText", "🏰", font, 64, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 1f, 1f));
        SetFullStretch(stageIconTMP.rectTransform);

        // Stage Title
        var stageTitleText = CreateTMP(stageCard, "StageTitleText", "DUNGEON STAGE PROGRESSION  <color=#88CCFF><size=20>[NATURAL TRANSITION]</size></color>", font, 28, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.4f, 0.8f, 1f, 1f));
        SetRect(stageTitleText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, 45), new Vector2(680, 45));

        // Stage Description
        var stageDescText = CreateTMP(stageCard, "StageDescText", "Natural breaks between game levels are the best placement for Interstitial Ads.\nDefeat dungeon monsters and advance to the next level!", font, 20, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.9f, 0.92f, 0.95f, 1f));
        SetRect(stageDescText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, -15), new Vector2(680, 50));

        // Complete Stage Button (Triggers Level Complete Modal)
        var completeStageBtnGO = GetOrCreateChild(stageCard, "Button Complete Stage");
        SetRect(completeStageBtnGO, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(250, 80));
        var completeBtnImg = GetOrAddComponent<Image>(completeStageBtnGO);
        completeBtnImg.color = new Color(0.18f, 0.45f, 0.88f, 1f);
        var completeBtn = GetOrAddComponent<Button>(completeStageBtnGO);
        var completeBtnText = CreateTMP(completeStageBtnGO, "CompleteText", "CLEAR STAGE ▶\n<size=17>Show Interstitial</size>", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(completeBtnText.rectTransform);
        completeBtn.onClick.RemoveAllListeners();
        completeBtn.onClick.AddListener(adsSample.OpenLevelCompleteModal);

        // 6. LEVEL COMPLETE MODAL (Interstitial Ad Showcase)
        var modal = GetOrCreateChild(canvas.transform, "LevelCompleteModal");
        SetFullStretch(modal);
        var modalOverlayImg = GetOrAddComponent<Image>(modal);
        modalOverlayImg.color = new Color(0, 0, 0, 0.82f);
        modal.gameObject.SetActive(false); // Initially hidden

        var modalBox = GetOrCreateChild(modal, "ModalDialogBox");
        SetRect(modalBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850, 520));
        var modalBoxImg = GetOrAddComponent<Image>(modalBox);
        modalBoxImg.color = new Color(0.11f, 0.14f, 0.22f, 1f);

        // Modal Header
        var modalLevelTitleText = CreateTMP(modalBox, "ModalLevelTitleText", "STAGE 1 CLEARED! <color=#FFD700>★★★</color>", font, 40, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetRect(modalLevelTitleText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(800, 60));

        // Modal Score / Stats
        var modalScoreText = CreateTMP(modalBox, "ModalScoreText", "Score: 12,500   •   Stage Reward: +100 Coins\n<size=19><color=#88CCFF>[ Interstitial Ad will play on Next Level ]</color></size>", font, 24, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetRect(modalScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(750, 100));

        // Next Level Button (Triggers Interstitial)
        var nextLevelBtnGO = GetOrCreateChild(modalBox, "Button Next Level");
        SetRect(nextLevelBtnGO, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(160, 90), new Vector2(280, 75));
        GetOrAddComponent<Image>(nextLevelBtnGO).color = new Color(0.15f, 0.68f, 0.35f, 1f);
        var nextLevelBtn = GetOrAddComponent<Button>(nextLevelBtnGO);
        var nextLevelBtnText = CreateTMP(nextLevelBtnGO, "NextLevelText", "NEXT LEVEL ▶\n<size=16>Watch Interstitial</size>", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(nextLevelBtnText.rectTransform);

        // Replay Button
        var replayBtnGO = GetOrCreateChild(modalBox, "Button Replay");
        SetRect(replayBtnGO, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-160, 90), new Vector2(240, 75));
        GetOrAddComponent<Image>(replayBtnGO).color = new Color(0.25f, 0.30f, 0.42f, 1f);
        var replayBtn = GetOrAddComponent<Button>(replayBtnGO);
        var replayBtnText = CreateTMP(replayBtnGO, "ReplayText", "REPLAY ↺", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(replayBtnText.rectTransform);

        // 7. BOTTOM BANNER SAFE AREA DOCK (Specifically fitted for Banner Ads)
        var bannerDock = GetOrCreateChild(canvas.transform, "BottomBannerSafeArea");
        SetRect(bannerDock, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 65), new Vector2(0, 130));
        var bannerDockImg = GetOrAddComponent<Image>(bannerDock);
        bannerDockImg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

        // Top divider for safe area
        var bannerDivider = GetOrCreateChild(bannerDock, "BannerDivider");
        SetRect(bannerDivider, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 3));
        GetOrAddComponent<Image>(bannerDivider).color = new Color(0.20f, 0.35f, 0.65f, 0.6f);

        // Physical Banner Preview Container (Center 320x50 / 728x90)
        var bannerMock = GetOrCreateChild(bannerDock, "BannerMockPreview");
        SetRect(bannerMock, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(728, 90));
        GetOrAddComponent<Image>(bannerMock).color = new Color(0.10f, 0.14f, 0.22f, 1f);
        var bannerMockText = CreateTMP(bannerMock, "BannerMockText", "LEVELPLAY BANNER AD (320x50)  •  Position: BottomCenter\n<size=16><color=#7799BB>Ad Unit: o8efz2kc3xxnb1ou  •  Reserved Safe Area Zone (Zero UI Obstruction)</color></size>", font, 19, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(bannerMockText.rectTransform);

        // Toggle Banner Button
        var toggleBannerGO = GetOrCreateChild(bannerDock, "Button Toggle Banner");
        SetRect(toggleBannerGO, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(190, 50));
        GetOrAddComponent<Image>(toggleBannerGO).color = new Color(0.22f, 0.28f, 0.40f, 1f);
        var toggleBannerBtn = GetOrAddComponent<Button>(toggleBannerGO);
        var toggleBannerText = CreateTMP(toggleBannerGO, "ToggleText", "Toggle Banner", font, 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(toggleBannerText.rectTransform);

        // 8. REWARD TOAST NOTIFICATION
        var toast = GetOrCreateChild(canvas.transform, "RewardToast");
        SetRect(toast, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(750, 60));
        GetOrAddComponent<Image>(toast).color = new Color(0.10f, 0.58f, 0.32f, 0.98f);
        var rewardToastText = CreateTMP(toast, "ToastText", "🎉 REWARD RECEIVED! +250 Coins & 15m Double Drops Activated!", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(rewardToastText.rectTransform);
        toast.gameObject.SetActive(false); // Initially hidden

        // 9. EDITOR AD SIMULATION OVERLAY
        var simOverlay = GetOrCreateChild(canvas.transform, "AdSimulationOverlay");
        SetFullStretch(simOverlay);
        GetOrAddComponent<Image>(simOverlay).color = new Color(0, 0, 0, 0.90f);
        simOverlay.gameObject.SetActive(false); // Initially hidden

        var simBox = GetOrCreateChild(simOverlay, "SimulationBox");
        SetRect(simBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 520));
        GetOrAddComponent<Image>(simBox).color = new Color(0.12f, 0.16f, 0.26f, 1f);

        var simulationTitleText = CreateTMP(simBox, "SimulationTitleText", "REWARDED VIDEO AD (Simulated)", font, 36, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f, 1f));
        SetRect(simulationTitleText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(850, 60));

        var simulationDescText = CreateTMP(simBox, "SimulationDescText", "Simulated Video Ad is playing...\n\nClick below to finish ad and receive your reward.", font, 24, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
        SetRect(simulationDescText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(800, 140));

        var simBtnGO = GetOrCreateChild(simBox, "SimulationCloseButton");
        SetRect(simBtnGO, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 75), new Vector2(400, 75));
        GetOrAddComponent<Image>(simBtnGO).color = new Color(0.15f, 0.68f, 0.35f, 1f);
        var simBtn = GetOrAddComponent<Button>(simBtnGO);
        var simBtnText = CreateTMP(simBtnGO, "SimBtnText", "COMPLETE AD & CONTINUE ▶", font, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetFullStretch(simBtnText.rectTransform);

        // Bind all references to AdsSample
        adsSample.levelText = levelText;
        adsSample.coinsText = coinsText;
        adsSample.boostStatusText = boostStatusText;
        adsSample.bannerStatusText = bannerStatusText;

        adsSample.rewardTitleText = rewardTitleText;
        adsSample.rewardDescText = rewardDescText;
        adsSample.rewardStatusText = rewardStatusText;
        adsSample.watchRewardButton = watchBtn;
        adsSample.watchRewardButtonText = watchRewardButtonText;

        adsSample.levelCompleteModal = modal.gameObject;
        adsSample.modalLevelTitleText = modalLevelTitleText;
        adsSample.modalScoreText = modalScoreText;
        adsSample.nextLevelButton = nextLevelBtn;
        adsSample.replayButton = replayBtn;

        adsSample.bannerSafeAreaDock = bannerDock.gameObject;
        adsSample.bannerMockPreview = bannerMock.gameObject;
        adsSample.toggleBannerButton = toggleBannerBtn;

        adsSample.rewardToast = toast.gameObject;
        adsSample.rewardToastText = rewardToastText;

        adsSample.simulationOverlay = simOverlay.gameObject;
        adsSample.simulationTitleText = simulationTitleText;
        adsSample.simulationDescText = simulationDescText;
        adsSample.simulationCloseButton = simBtn;

        // Wire events
        watchBtn.onClick.RemoveAllListeners();
        watchBtn.onClick.AddListener(adsSample.OnWatchRewardClicked);

        nextLevelBtn.onClick.RemoveAllListeners();
        nextLevelBtn.onClick.AddListener(adsSample.OnNextLevelClicked);

        replayBtn.onClick.RemoveAllListeners();
        replayBtn.onClick.AddListener(adsSample.OnReplayStageClicked);

        toggleBannerBtn.onClick.RemoveAllListeners();
        toggleBannerBtn.onClick.AddListener(adsSample.ToggleBannerAds);

        simBtn.onClick.RemoveAllListeners();
        simBtn.onClick.AddListener(adsSample.OnSimulationCompleteButtonClicked);

        EditorUtility.SetDirty(adsSample);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[GameAdsUISetup] Game UI successfully built and configured for Ads Banner, Interstitial, and Rewarded!");
    }

    private static RectTransform GetOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            child = go.transform;
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }
        return child as RectTransform;
    }

    private static T GetOrAddComponent<T>(Component target) where T : Component
    {
        var comp = target.GetComponent<T>();
        if (comp == null) comp = target.gameObject.AddComponent<T>();
        return comp;
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    private static TextMeshProUGUI CreateTMP(RectTransform parent, string name, string text, TMP_FontAsset font, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        var rt = GetOrCreateChild(parent, name);
        var tmp = GetOrAddComponent<TextMeshProUGUI>(rt);
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }
}
