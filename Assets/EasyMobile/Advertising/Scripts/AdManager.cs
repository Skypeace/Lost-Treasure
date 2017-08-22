using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using ChartboostSDK;

#if UNITY_ADS
using UnityEngine.Advertisements;

// only compile UnityAds code on supported platforms
#endif

namespace EasyMobile
{
    // List of all supported ad networks
    public enum AdNetwork
    {
        None,
        AdMob,
        Chartboost,
        UnityAds
    }

    public enum AdType
    {
        Banner,
        Interstitial,
        Rewarded
    }

    public enum BannerAdNetwork
    {
        None = AdNetwork.None,
        AdMob = AdNetwork.AdMob
    }

    public enum InterstitialAdNetwork
    {
        None = AdNetwork.None,
        AdMob = AdNetwork.AdMob,
        Chartboost = AdNetwork.Chartboost,
        UnityAds = AdNetwork.UnityAds
    }

    public enum RewardedAdNetwork
    {
        None = AdNetwork.None,
        Chartboost = AdNetwork.Chartboost,
        UnityAds = AdNetwork.UnityAds
    }

    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        public static event Action<InterstitialAdNetwork, AdLocation> InterstitialAdCompleted = delegate {};
        public static event Action<RewardedAdNetwork, AdLocation> RewardedAdCompleted = delegate {};
        public static event Action AdsRemoved = delegate {};

        public bool verboseDebugLog = false;

        [Header("ADS AUTO-LOAD CONFIG")]
        [Tooltip("Check if you want default ads to be loaded automatically")]
        public bool autoLoadDefaultAds = true;
        [Tooltip("How many seconds between two ads readiness checks")]
        public float ADS_CHECK_INTERVAL = 10f;
        [Tooltip("How many seconds (at least) between two ad loading requests, to prevent sending too many requests to ad networks")]
        public float ADS_LOAD_INTERVAL = 30f;

        [Header("[iOS] DEFAULT AD NETWORKS")]
        public BannerAdNetwork iosDefaultBannerAdNetwork = BannerAdNetwork.AdMob;
        public InterstitialAdNetwork iosDefaultInterstitialAdNetwork = InterstitialAdNetwork.Chartboost;
        public RewardedAdNetwork iosDefaultRewardedAdNetwork = RewardedAdNetwork.UnityAds;

		[Header("[ANDROID] DEFAULT AD NETWORKS")]
		public BannerAdNetwork androidDefaultBannerAdNetwork = BannerAdNetwork.AdMob;
		public InterstitialAdNetwork androidDefaultInterstitialAdNetwork = InterstitialAdNetwork.UnityAds;
		public RewardedAdNetwork androidDefaultRewardedAdNetwork = RewardedAdNetwork.Chartboost;

        [Header("[iOS] [ADMOB] AD UNIT IDS")]
        public string iosAdMobBannerAdId = "ADMOB_BANNER_ID_IOS";
        public string iosAdMobInterstitialAdId = "ADMOB_INTERSTITIAL_ID_IOS";
        [HideInInspector]
        public string iosAdMobRewardedAdId = "ADMOB_REWARDED_ID_IOS";

        [Header("[ANDROID] [ADMOB] AD UNIT IDS")]
        public string androidAdMobBannerAdId = "ADMOB_BANNER_ID_ANDROID";
        public string androidAdMobInterstitialAdId = "ADMOB_INTERSTITIAL_ID_ANDROID";
        [HideInInspector]
        public string androidAdMobRewardedAdId = "ADMOB_REWARDED_ID_ANDROID";

        const string UNITYADS_REWARDED_ZONE_ID = "rewardedVideo";
        float lastInterstitialAdLoadTimestamp = -1000f;
        float lastRewardedAdLoadTimestamp = -1000f;
        List<BannerAdNetwork> activeBannerAdNetworks = new List<BannerAdNetwork>();

        // For storing removeAds status
        const string AD_REMOVE_STATUS_KEY = "EMS_REMOVE_ADS";
        const int AD_ENABLED = 1;
        const int AD_DISABLED = -1;

        // AdMob specific ad objects
        BannerView admobBannerView;
        InterstitialAd admobInterstitial;
        RewardBasedVideoAd admobRewardedAd;

        void Awake()
        {
            if (Instance)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void OnEnable()
        {
            Chartboost.didCacheInterstitial += CBDidCacheInterstitial;
            Chartboost.didDisplayInterstitial += CBDidDisplayInterstitial;
            Chartboost.didClickInterstitial += CBDidClickInterstitial;
            Chartboost.didCloseInterstitial += CBDidCloseInterstitial;
            Chartboost.didDismissInterstitial += CBDidDismissInterstitial;
            Chartboost.didFailToLoadInterstitial += CBDidFailToLoadInterstitial;

            Chartboost.didCacheRewardedVideo += CBDidCacheRewardedVideo;
            Chartboost.didClickRewardedVideo += CBDidClickRewardedVideo;
            Chartboost.didCloseRewardedVideo += CBDidCloseRewardedVideo;
            Chartboost.didFailToLoadRewardedVideo += CBDidFailToLoadRewardedVideo;
            Chartboost.didCompleteRewardedVideo += CBDidCompleteRewardedVideo;
        }

        void OnDisable()
        {
            Chartboost.didCacheInterstitial -= CBDidCacheInterstitial;
            Chartboost.didDisplayInterstitial -= CBDidDisplayInterstitial;
            Chartboost.didClickInterstitial -= CBDidClickInterstitial;
            Chartboost.didCloseInterstitial -= CBDidCloseInterstitial;
            Chartboost.didDismissInterstitial -= CBDidDismissInterstitial;
            Chartboost.didFailToLoadInterstitial -= CBDidFailToLoadInterstitial;

            Chartboost.didCacheRewardedVideo -= CBDidCacheRewardedVideo;
            Chartboost.didClickRewardedVideo -= CBDidClickRewardedVideo;
            Chartboost.didCloseRewardedVideo -= CBDidCloseRewardedVideo;
            Chartboost.didFailToLoadRewardedVideo -= CBDidFailToLoadRewardedVideo;
            Chartboost.didCompleteRewardedVideo -= CBDidCompleteRewardedVideo;
        }

        void Start()
        {
            // Start the coroutine that checks for ads readiness and performs loading if they're not.
            StartCoroutine(CRAutoLoadAds());
        }

        // This coroutine regularly checks if intersititial and rewarded ads are loaded, if they aren't
        // it will automatically perform loading; the process will stop when ads are removed.
		// It will also do nothing if autoLoadDefaultAds is disable at runtime.
        IEnumerator CRAutoLoadAds()
        {
            // We'll load Chartboost ads manually, so turning off the autocache feature
            Chartboost.setAutoCacheAds(false);

            while (true)
            {
                if (autoLoadDefaultAds)
                {
                    if (verboseDebugLog)
                        Debug.Log("Performing ads readiness check...");

                    foreach (AdType type in Enum.GetValues(typeof(AdType)))
                    {
                        switch (type)
                        {
                            case AdType.Interstitial:
                                if (!IsInterstitialAdReady() && !IsAdRemoved())
                                {
                                    if (Time.realtimeSinceStartup - lastInterstitialAdLoadTimestamp >= ADS_LOAD_INTERVAL)
                                    {
                                        LoadInterstitialAd();
                                        lastInterstitialAdLoadTimestamp = Time.realtimeSinceStartup;
                                    }
                                }
                                break;
                            case AdType.Rewarded:
                                if (!IsRewardedAdReady())
                                {
                                    if (Time.realtimeSinceStartup - lastRewardedAdLoadTimestamp >= ADS_LOAD_INTERVAL)
                                    {
                                        LoadRewardedAd();
                                        lastRewardedAdLoadTimestamp = Time.realtimeSinceStartup;
                                    }
                                }
                                break;
                            default:
							// Only interstitial and rewarded ad need to be loaded beforehand
                                break;
                        }         
                    }
                }

                yield return new WaitForSeconds(ADS_CHECK_INTERVAL);
            }
        }

		/// <summary>
		/// Shows banner ad using the default banner ad network and SmartBanner size.
		/// </summary>
		/// <param name="pos">Position.</param>
		public void ShowBannerAd(BannerAdPosition pos)
		{
			BannerAdNetwork defaultBannerAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultBannerAdNetwork : iosDefaultBannerAdNetwork;
			ShowBannerAd(defaultBannerAdNetwork, pos, BannerAdSize.SmartBanner);
		}

        /// <summary>
        /// Shows banner ad using the specified ad network, if that netword doesn't support banner ads, this method is a no-opt.
        /// </summary>
        /// <param name="adNetwork">Ad network.</param>
        /// <param name="pos">Position.</param>
        /// <param name="size">Size.</param>
        public void ShowBannerAd(BannerAdNetwork adNetwork, BannerAdPosition pos, BannerAdSize size)
        {
            if (IsAdRemoved())
            {
                if (verboseDebugLog)
                    Debug.Log("ShowBannerAd: FAIL. Ads were removed.");

                return;
            }                
            
            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    if (admobBannerView == null)
                    {
                        if (Application.platform == RuntimePlatform.Android)
                            admobBannerView = new BannerView(androidAdMobBannerAdId, size.ToAdMobAdSize(), pos.ToAdMobAdPosition());
                        else if (Application.platform == RuntimePlatform.IPhonePlayer)
                            admobBannerView = new BannerView(iosAdMobBannerAdId, size.ToAdMobAdSize(), pos.ToAdMobAdPosition());

                        if (admobBannerView != null)
                        {
                            // Register for ad events.
                            admobBannerView.OnAdLoaded += HandleAdMobBannerAdLoaded;
                            admobBannerView.OnAdFailedToLoad += HandleAdMobBannerAdFailedToLoad;
                            admobBannerView.OnAdOpening += HandleAdMobBannerAdOpened;
                            admobBannerView.OnAdClosed += HandleAdMobBannerAdClosed;
                            admobBannerView.OnAdLeavingApplication += HandleAdMobBannerAdLeftApplication;

                            // Load ad
                            admobBannerView.LoadAd(CreateAdMobAdRequest());
                        }
                    }

                    if (admobBannerView != null)
                    {
                        admobBannerView.Show();

                        if (!activeBannerAdNetworks.Contains(adNetwork))
                            activeBannerAdNetworks.Add(adNetwork);
                    }
                    break;
                default:
                    if (verboseDebugLog)
                        Debug.Log("ShowBannerAd: FAIL. Banner ad is not supported by network: " + adNetwork.ToString());
                    break;
            }
        }

        /// <summary>
        /// Hides banner ad of the default banner ad network.
        /// </summary>
        public void HideBannerAd()
        {
			BannerAdNetwork defaultBannerAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultBannerAdNetwork : iosDefaultBannerAdNetwork;
            HideBannerAd(defaultBannerAdNetwork);
        }

        /// <summary>
        /// Hides banner ad of the specified banner ad network if one is shown, otherwise this method is a no-opt.
        /// </summary>
        /// <param name="adNetwork">Ad network.</param>
        public void HideBannerAd(BannerAdNetwork adNetwork)
        {
            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    if (admobBannerView != null)
                    {
                        admobBannerView.Hide();

                        if (activeBannerAdNetworks.Contains(adNetwork))
                            activeBannerAdNetworks.Remove(adNetwork);
                    }   
                    break;
                default:
                    if (verboseDebugLog)
                        Debug.Log("HideBannerAd: FAIL. Banner ad is not supported by network: " + adNetwork.ToString());
                    break;
            }
        }

        /// <summary>
        /// Determines whether there's a banner ad being shown.
        /// </summary>
        /// <returns><c>true</c> if this instance is showing banner ad; otherwise, <c>false</c>.</returns>
        public bool IsShowingBannerAd()
        {
            return activeBannerAdNetworks.Count > 0;
        }

        /// <summary>
        /// Returns the array of banner ad networks having a banner ad being shown.
        /// </summary>
        /// <returns>The active banner ad networks.</returns>
        public BannerAdNetwork[] GetActiveBannerAdNetworks()
        {
            return activeBannerAdNetworks.ToArray();
        }

        /// <summary>
        /// Loads the default interstitial ad.
        /// </summary>
        public void LoadInterstitialAd()
        {
			InterstitialAdNetwork defaultInterstitialAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultInterstitialAdNetwork : iosDefaultInterstitialAdNetwork;
            LoadInterstitialAd(defaultInterstitialAdNetwork, AdLocation.Default);
        }

        /// <summary>
        /// Loads interstitial ad using the specified interstitial ad network at the specified location.
        ///     - For AdMob, the location will be ignored. You can pass AdLocation.Default.
        ///     - For Chartboost, select one of available locations or create a new location using AdLocation.LocationFromName(name).
        ///     - For UnityAds, create a new location for the desired zoneId using AdLocation.LocationFromName(zoneId).
        /// If the specified network doesn't support interstitial ads, this method is a no-opt.
        /// </summary>
        /// <param name="adNetwork">Ad network.</param>
        /// <param name="location">Location.</param>
        public void LoadInterstitialAd(InterstitialAdNetwork adNetwork, AdLocation location)
        {
            if (IsAdRemoved())
                return;

            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    // Destroy old interstitial object if any
                    if (admobInterstitial != null)
                    {
                        admobInterstitial.Destroy();
                        admobInterstitial = null;
                    }
                        
                    // Create new interstitial object
                    if (Application.platform == RuntimePlatform.Android)
                        admobInterstitial = new InterstitialAd(androidAdMobInterstitialAdId);
                    else if (Application.platform == RuntimePlatform.IPhonePlayer)
                        admobInterstitial = new InterstitialAd(iosAdMobInterstitialAdId);

                    if (admobInterstitial != null)
                    {
                        // Register for ad events.
                        admobInterstitial.OnAdLoaded += HandleAdMobInterstitialLoaded;
                        admobInterstitial.OnAdFailedToLoad += HandleAdMobInterstitialFailedToLoad;
                        admobInterstitial.OnAdOpening += HandleAdMobInterstitialOpened;
                        admobInterstitial.OnAdClosed += HandleAdMobInterstitialClosed;
                        admobInterstitial.OnAdLeavingApplication += HandleAdMobInterstitialLeftApplication;

                        // Load the ad
                        admobInterstitial.LoadAd(CreateAdMobAdRequest());
                    }
    
                    break;
                case AdNetwork.Chartboost:
                    Chartboost.cacheInterstitial(location.ToChartboostLocation());
                    break;
                case AdNetwork.UnityAds:
                    // UnityAds are loaded automatically
                    break;
            }
        }

        /// <summary>
        /// Determines whether the default interstitial ad is ready to show.
        /// </summary>
        /// <returns><c>true</c> if the default interstitial ad is ready; otherwise, <c>false</c>.</returns>
        public bool IsInterstitialAdReady()
        {
			InterstitialAdNetwork defaultInterstitialAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultInterstitialAdNetwork : iosDefaultInterstitialAdNetwork;
            return IsInterstitialAdReady(defaultInterstitialAdNetwork, AdLocation.Default);
        }

        /// <summary>
        /// Determines whether interstitial ad of the specified ad network is ready to show at the given location.
        ///     - For AdMob, the location will be ignored.
        ///     - For Chartboost, select one of available locations or create a new location using AdLocation.LocationFromName(name).
        ///     - For UnityAds, create a new location for the desired zoneId using AdLocation.LocationFromName(zoneId).
        /// If the specified network doesn't support interstitial ads, this method always returns false.
        /// </summary>
        /// <returns><c>true</c> if interstitial ad is ready for the specified location; otherwise, <c>false</c>.</returns>
        /// <param name="adNetwork">Ad network.</param>
        /// <param name="location">Location.</param>
        public bool IsInterstitialAdReady(InterstitialAdNetwork adNetwork, AdLocation location)
        {
            if (IsAdRemoved())
                return false;
            
            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    return (admobInterstitial != null && admobInterstitial.IsLoaded());
                case AdNetwork.Chartboost:
                    return Chartboost.hasInterstitial(location.ToChartboostLocation());
                case AdNetwork.UnityAds:
                    #if UNITY_ADS
                    if (location == AdLocation.Default)
                        return Advertisement.IsReady();
                    else
                        return Advertisement.IsReady(location.ToUnityAdsZoneId());
                    #else
                    return false;
                    #endif
                default:
                    if (verboseDebugLog)
                        Debug.Log("IsInterstitialAdReady: FAIL. Interstitial ad is not supported by network: " + adNetwork.ToString());
                    return false;
            }
        }

        /// <summary>
        /// Shows the default interstitial ad.
        /// </summary>
        public void ShowInterstitialAd()
        {
			InterstitialAdNetwork defaultInterstitialAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultInterstitialAdNetwork : iosDefaultInterstitialAdNetwork;
            ShowInterstitialAd(defaultInterstitialAdNetwork, AdLocation.Default);
        }

        /// <summary>
        /// Show an interstitial ad using the specified ad network, at the specified location.
        ///     - For AdMob, the location will be ignored.
        ///     - For Chartboost, select one of available locations or create a new location using AdLocation.LocationFromName(name).
        ///     - For UnityAds, create a new location for the desired zoneId using AdLocation.LocationFromName(zoneId).
        /// If the specified network doesn't support interstitial ads, this method is a no-opt.
        /// </summary>
        /// <param name="adNetwork">Ad network.</param>
        /// <param name="location">Location.</param>
        public void ShowInterstitialAd(InterstitialAdNetwork adNetwork, AdLocation location)
        {
            if (IsAdRemoved())
            {
                if (verboseDebugLog)
                    Debug.Log("ShowInterstitialAd: FAIL. Ads were removed.");

                return;
            }
                            
            if (!IsInterstitialAdReady(adNetwork, location))
            {
                if (verboseDebugLog)
                    Debug.Log("ShowInterstitialAd: FAIL. Interstitial ad is not loaded.");

                return;
            }
                
            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    admobInterstitial.Show();
                    break;
                case AdNetwork.Chartboost:
                    Chartboost.showInterstitial(location.ToChartboostLocation());
                    break;
                case AdNetwork.UnityAds:
                    #if UNITY_ADS
                    if (location == AdLocation.Default)
                    {
                        Advertisement.Show();
                    }
                    else
                    {
                        var showOptions = new ShowOptions { resultCallback = UnityAdsInterstitialCallback };
                        Advertisement.Show(location.ToUnityAdsZoneId(), showOptions);
                    } 
                    #endif
                    break;
                default:
                    if (verboseDebugLog)
                        Debug.Log("ShowInterstitialAd: FAIL. Interstitial ad is not supported by network: " + adNetwork.ToString());
                    break;
            }
        }

        /// <summary>
        /// Loads the default rewarded ad.
        /// </summary>
        public void LoadRewardedAd()
        {
			RewardedAdNetwork defaultRewardedAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultRewardedAdNetwork : iosDefaultRewardedAdNetwork;
            LoadRewardedAd(defaultRewardedAdNetwork, AdLocation.Default);
        }

        /// <summary>
        /// Loads a rewarded ad using the specified ad network, at the specified location.
        ///     - For AdMob, the location will be ignored.
        ///     - For Chartboost, select one of available locations or create a new location using AdLocation.LocationFromName(name).
        ///     - For UnityAds, create a new location for the desired zoneId using AdLocation.LocationFromName(zoneId).
        /// If the specified network doesn't support rewarded ads, this method is a no-opt.
        /// </summary>
        /// <param name="adNetwork">Ad network.</param>
        /// <param name="location">Location.</param>
        public void LoadRewardedAd(RewardedAdNetwork adNetwork, AdLocation location)
        {
            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    if (admobRewardedAd == null)
                    {
                        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                        {
                            admobRewardedAd = RewardBasedVideoAd.Instance;

                            // RewardBasedVideoAd is a singleton, so handlers should only be registered once.
                            admobRewardedAd.OnAdLoaded += HandleAdMobRewardBasedVideoLoaded;
                            admobRewardedAd.OnAdFailedToLoad += HandleAdMobRewardBasedVideoFailedToLoad;
                            admobRewardedAd.OnAdOpening += HandleAdMobRewardBasedVideoOpened;
                            admobRewardedAd.OnAdStarted += HandleAdMobRewardBasedVideoStarted;
                            admobRewardedAd.OnAdRewarded += HandleAdMobRewardBasedVideoRewarded;
                            admobRewardedAd.OnAdClosed += HandleAdMobRewardBasedVideoClosed;
                            admobRewardedAd.OnAdLeavingApplication += HandleAdMobRewardBasedVideoLeftApplication;
                        }
                    }
                     
                    if (Application.platform == RuntimePlatform.Android)
                        admobRewardedAd.LoadAd(CreateAdMobAdRequest(), androidAdMobRewardedAdId);
                    else if (Application.platform == RuntimePlatform.IPhonePlayer)
                        admobRewardedAd.LoadAd(CreateAdMobAdRequest(), iosAdMobRewardedAdId);

                    break;
                case AdNetwork.Chartboost:
                    Chartboost.cacheRewardedVideo(location.ToChartboostLocation());
                    break;
                case AdNetwork.UnityAds:
                    // UnityAds rewarded videos are loaded automatically.
                    break;
            }
        }

        /// <summary>
        /// Determines whether the default rewarded ad is ready to show.
        /// </summary>
        /// <returns><c>true</c> if rewarded ad ready is ready; otherwise, <c>false</c>.</returns>
        public bool IsRewardedAdReady()
        {
			RewardedAdNetwork defaultRewardedAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultRewardedAdNetwork : iosDefaultRewardedAdNetwork;
            return IsRewardedAdReady(defaultRewardedAdNetwork, AdLocation.Default);
        }

        /// <summary>
        /// Determines whether a rewarded ad is ready to show, using the specified ad network, at the specified location.
        ///     - For AdMob, the location will be ignored.
        ///     - For Chartboost, select one of available locations or create a new location using AdLocation.LocationFromName(name).
        ///     - For UnityAds, create a new location for the desired zoneId using AdLocation.LocationFromName(zoneId).
        /// If the specified network doesn't support rewarded ads, this method always returns false.
        /// </summary>
        /// <returns><c>true</c> if rewarded ad is ready; otherwise, <c>false</c>.</returns>
        /// <param name="adNetwork">Ad network.</param>
        /// <param name="location">Location.</param>
        public bool IsRewardedAdReady(RewardedAdNetwork adNetwork, AdLocation location)
        {
            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    return (admobRewardedAd != null && admobRewardedAd.IsLoaded());
                case AdNetwork.Chartboost:
                    return Chartboost.hasRewardedVideo(location.ToChartboostLocation());
                case AdNetwork.UnityAds:
                    #if UNITY_ADS
                    return Advertisement.IsReady(UNITYADS_REWARDED_ZONE_ID);
                    #else
                    return false;
                    #endif
                default:
                    if (verboseDebugLog)
                        Debug.Log("IsRewardedAdReady: FAIL. Rewarded ad is not supported by network: " + adNetwork.ToString());
                    return false;
            }
        }

        /// <summary>
        /// Shows the default rewarded ad.
        /// </summary>
        public void ShowRewardedAd()
        {
			RewardedAdNetwork defaultRewardedAdNetwork = Application.platform == RuntimePlatform.Android ? androidDefaultRewardedAdNetwork : iosDefaultRewardedAdNetwork;
            ShowRewardedAd(defaultRewardedAdNetwork, AdLocation.Default);
        }

        /// <summary>
        /// Shows a rewarded ad using the specified ad network, at the specified location.
        ///     - For AdMob, the location will be ignored.
        ///     - For Chartboost, select one of available locations or create a new location using AdLocation.LocationFromName(name).
        ///     - For UnityAds, create a new location for the desired zoneId using AdLocation.LocationFromName(zoneId).
        /// If the specified network doesn't support rewarded ads, this method is a no-opt.
        /// </summary>
        /// <param name="adNetwork">Ad network.</param>
        /// <param name="location">Location.</param>
        public void ShowRewardedAd(RewardedAdNetwork adNetwork, AdLocation location)
        {           
            if (!IsRewardedAdReady(adNetwork, location))
            {
                if (verboseDebugLog)
                    Debug.Log("ShowRewardedAd: FAIL. Rewarded ad is not loaded.");

                return;
            }                

            switch ((AdNetwork)adNetwork)
            {
                case AdNetwork.AdMob:
                    admobRewardedAd.Show();
                    break;
                case AdNetwork.Chartboost:
                    Chartboost.showRewardedVideo(location.ToChartboostLocation());
                    break;
                case AdNetwork.UnityAds:
                    #if UNITY_ADS
                    var showOptions = new ShowOptions { resultCallback = UnityAdsRewardedAdCallback };
                    Advertisement.Show(UNITYADS_REWARDED_ZONE_ID, showOptions);
                    #endif
                    break;
                default:
                    if (verboseDebugLog)
                        Debug.Log("ShowRewardedAd: FAIL. Rewarded ad is not supported by network: " + adNetwork.ToString());
                    break;
            }
        }

        /// <summary>
        /// Determines whether ads were removed.
        /// </summary>
        /// <returns><c>true</c> if ads were removed; otherwise, <c>false</c>.</returns>
        public bool IsAdRemoved()
        {
            return (PlayerPrefs.GetInt(AD_REMOVE_STATUS_KEY, AD_ENABLED) == AD_DISABLED);
        }

        /// <summary>
        /// Removes ads permanently. Use this for the RemoveAds button.
        /// This will hide the default banner ad if it is being shown;
        /// prohibit future loading and showing of banner ad and interstitial ad
        /// while still allow loading and showing rewarded ad.
        /// </summary>
        public void RemoveAds()
        {
            Debug.Log("Removing ads...");

			// Hide the default banner ad if any is being shown
			HideBannerAd();

            // Update ad availability
            PlayerPrefs.SetInt(AD_REMOVE_STATUS_KEY, AD_DISABLED);
            PlayerPrefs.Save();

            // Fire event
            AdsRemoved();
        }

        AdRequest CreateAdMobAdRequest()
        {
            return new AdRequest.Builder().Build();
        }

        #region UnityAds Interstitial & RewardedAd callback handlers

        #if UNITY_ADS
        
        void UnityAdsInterstitialCallback(ShowResult result)
        {
            switch (result)
            {
                case ShowResult.Finished:
                    if (verboseDebugLog)
                        Debug.Log("UnityAds interstitial ad was completed.");
                    
                    // Fire event
                    InterstitialAdCompleted(InterstitialAdNetwork.UnityAds, AdLocation.Default);

                    break;
                case ShowResult.Skipped:
                    if (verboseDebugLog)
                        Debug.Log("UnityAds interstitial ad was skipped before reaching the end.");

                    break;
                case ShowResult.Failed:
                    if (verboseDebugLog)
                        Debug.LogError("UnityAds interstitial ad failed to be shown.");

                    break;
            }
        }

        void UnityAdsRewardedAdCallback(ShowResult result)
        {
            switch (result)
            {
                case ShowResult.Finished:
                    if (verboseDebugLog)
                        Debug.Log("UnityAds rewarded video ad was completed.");

                    // Fire event
                    RewardedAdCompleted(RewardedAdNetwork.UnityAds, AdLocation.Default);

                    break;
                case ShowResult.Skipped:
                    if (verboseDebugLog)
                        Debug.Log("UnityAds rewarded video ad was skipped before reaching the end.");

                    break;
                case ShowResult.Failed:
                    if (verboseDebugLog)
                        Debug.LogError("UnityAds rewarded video ad failed to be shown.");

                    break;
            }
        }

        #endif

        #endregion

        #region Chartboost Interstitial callback handlers

        void CBDidCacheInterstitial(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost interstitial ad was loaded successfully.");
        }

        void CBDidDisplayInterstitial(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost interstitial ad has been displayed.");
        }

        void CBDidClickInterstitial(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost interstitial ad was clicked.");
        }

        void CBDidCloseInterstitial(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost interstitial ad was closed.");
        }

        void CBDidDismissInterstitial(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost interstitial ad was dismissed.");

            // Fire event
            InterstitialAdCompleted(InterstitialAdNetwork.Chartboost, AdLocation.LocationFromName(location.ToString()));
        }

        void CBDidFailToLoadInterstitial(CBLocation location, CBImpressionError error)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost interstitial ad failed to load.");
        }

        #endregion

        #region Chartboost RewardedAd callback handlers

        void CBDidCacheRewardedVideo(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost rewarded video ad was loaded successfully.");
        }

        void CBDidFailToLoadRewardedVideo(CBLocation location, CBImpressionError error)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost rewarded video ad failed to load.");
        }

        void CBDidCompleteRewardedVideo(CBLocation location, int reward)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost rewarded video ad was completed.");

            // Fire event
            RewardedAdCompleted(RewardedAdNetwork.Chartboost, AdLocation.LocationFromName(location.ToString()));
        }

        void CBDidClickRewardedVideo(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost rewarded video ad was clicked.");
        }

        void CBDidCloseRewardedVideo(CBLocation location)
        {
            if (verboseDebugLog)
                Debug.Log("Chartboost rewarded video ad was closed.");
        }

        #endregion

        #region AdMob Banner callback handlers

        void HandleAdMobBannerAdLoaded(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob banner ad was loaded successfully.");
        }

        void HandleAdMobBannerAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob banner ad failed to load.");
        }

        void HandleAdMobBannerAdOpened(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob banner ad was clicked.");
        }

        void HandleAdMobBannerAdClosed(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob banner ad was closed.");
        }

        void HandleAdMobBannerAdLeftApplication(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("HandleAdMobBannerAdLeftApplication event received");
        }

        #endregion

        #region AdMob Interstitial callback handlers

        void HandleAdMobInterstitialLoaded(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob interstitial ad was loaded successfully.");
        }

        void HandleAdMobInterstitialFailedToLoad(object sender, AdFailedToLoadEventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob interstitial ad failed to load.");
        }

        void HandleAdMobInterstitialOpened(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob interstitial ad was clicked.");
        }

        void HandleAdMobInterstitialClosed(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob interstitial ad was closed.");

            // Fire event
            InterstitialAdCompleted(InterstitialAdNetwork.AdMob, AdLocation.Default);
        }

        void HandleAdMobInterstitialLeftApplication(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("HandleAdMobInterstitialLeftApplication event received");
        }

        #endregion

        #region AdMob RewardBasedVideo callback handlers

        public void HandleAdMobRewardBasedVideoLoaded(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob rewarded video ad was loaded successfully.");
        }

        public void HandleAdMobRewardBasedVideoFailedToLoad(object sender, AdFailedToLoadEventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob rewarded video ad failed to load.");
        }

        public void HandleAdMobRewardBasedVideoOpened(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob rewarded video ad was clicked.");
        }

        public void HandleAdMobRewardBasedVideoStarted(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob rewarded video ad has started.");
        }

        public void HandleAdMobRewardBasedVideoClosed(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob rewarded video ad was closed.");
        }

        public void HandleAdMobRewardBasedVideoRewarded(object sender, Reward args)
        {
            if (verboseDebugLog)
                Debug.Log("AdMob rewarded video ad was completed.");
        }

        public void HandleAdMobRewardBasedVideoLeftApplication(object sender, EventArgs args)
        {
            if (verboseDebugLog)
                Debug.Log("HandleRewardBasedVideoLeftApplication event received");
        }

        #endregion
    }
}













