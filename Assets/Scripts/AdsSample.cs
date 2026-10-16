using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class AdsSample : MonoBehaviour
{
    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedVideoAd;
    bool isAdsEnabled = false;

    public void Start()
    {
        LevelPlay.ValidateIntegration();
        // Register OnInitFailed and OnInitSuccess listeners
        LevelPlay.OnInitSuccess += SdkInitCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitFailedEvent;
        // SDK init
        LevelPlay.Init(AdConfig.AppKey);
    }

    private void SdkInitFailedEvent(LevelPlayInitError error)
    {
        Debug.Log($"Ads not enable : {error}");
    }

    private void SdkInitCompletedEvent(LevelPlayConfiguration configuration)
    {
        EnableAds();
        isAdsEnabled = true;
    }

    void EnableAds()
    {
        var configBuilder = new LevelPlayBannerAd.Config.Builder();
        configBuilder.SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter);
        var bannerConfig = configBuilder.Build();

        bannerAd = new LevelPlayBannerAd(AdConfig.BannerAdUnitId, bannerConfig);
        interstitialAd = new LevelPlayInterstitialAd(AdConfig.InterstitialAdUnitId);
        rewardedVideoAd = new LevelPlayRewardedAd(AdConfig.RewardedVideoAdUnitId);
    }

    public void LoadBannerAds()
    {
        bannerAd.LoadAd();
    }
    public void ShowBannerAds()
    {
        bannerAd.ShowAd();
    }
    public void HideBannerAds()
    {
        bannerAd.HideAd();
    }

    public void LoadInterstitialAds()
    {
        interstitialAd.LoadAd();
    }
    public void ShowInterstitialAds()
    {
        if (interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("[LevelPlaySample] LevelPlay Interstital Ad is not ready");
        }
    }

    public void LoadRewardedVideoAds()
    {
        rewardedVideoAd.LoadAd();
    }

    public void ShowRewardedVideoAds()
    {
        if (rewardedVideoAd.IsAdReady())
        {
            rewardedVideoAd.ShowAd();
        }
        else
        {
            Debug.Log("[LevelPlaySample] LevelPlay Rewarded Video Ad is not ready");
        }
    }

    private void OnDisable()
    {
        bannerAd?.DestroyAd();
        interstitialAd?.DestroyAd();
    }
}
