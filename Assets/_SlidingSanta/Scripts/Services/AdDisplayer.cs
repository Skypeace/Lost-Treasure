using UnityEngine;
using System.Collections;
using EasyMobile;

namespace SgLib
{
    public class AdDisplayer : MonoBehaviour
    {
        public UIManager uiManager;

        [Header("BANNER AD DISPLAY CONFIG")]
        [Tooltip("Whether or not to show banner ad")]
        public bool showBannerAd = true;
        public BannerAdPosition bannerAdPosition = BannerAdPosition.Bottom;

        [Header("INTERSTITIAL AD DISPLAY CONFIG")]
        [Tooltip("Whether or not to show interstitial ad")]
        public bool showInterstitialAd = true;
        [Tooltip("Show interstitial ad every [how many] games")]
        public int gamesPerInterstitial = 3;
        [Tooltip("How many seconds after game over that interstitial ad is shown")]
        public float showInterstitialDelay = 2f;

        [Header("REWARDED AD DISPLAY CONFIG")]
        [Tooltip("Whether or not to show rewarded ad")]
        public bool showRewardedAd = true;
        [Tooltip("How many coins the user earns after watching rewarded ad")]
        public int reward = 50;

        private static int gameCount = 0;

        void OnEnable()
        {
            GameManager.NewGameEvent += PlayerController_NewGameEvent;
        }

        void OnDisable()
        {
            GameManager.NewGameEvent -= PlayerController_NewGameEvent;
        }

        void Start()
        {
            // Show banner ad
            if (showBannerAd && !AdManager.Instance.IsAdRemoved() && !AdManager.Instance.IsShowingBannerAd())
            {
                AdManager.Instance.ShowBannerAd(bannerAdPosition);
            }
        }

        void PlayerController_NewGameEvent(GameEvent e)
        {       
            if (e == GameEvent.Start)
            {
                // Do something when game starts
            }
            else if (e == GameEvent.GameOver)
            {
                // Show interstitial ad
                if (showInterstitialAd && !AdManager.Instance.IsAdRemoved())
                {
                    gameCount++;

                    if (gameCount >= gamesPerInterstitial)
                    {
                        if (AdManager.Instance.IsInterstitialAdReady())
                        {
                            // Show default ad after some optional delay
                            StartCoroutine(ShowInterstitial(showInterstitialDelay));

                            // Reset game count
                            gameCount = 0;
                        }
                    }
                }

                // Show rewarded ad
                if (showRewardedAd)
                {
                    StartCoroutine(CRShowWatchAdForCoins(2f)); 
                }
            }
        }

        IEnumerator ShowInterstitial(float delay = 0f)
        {        
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            AdManager.Instance.ShowInterstitialAd();
        }

        IEnumerator CRShowWatchAdForCoins(float delay = 0f)
        {
            yield return new WaitForSeconds(delay);

            bool isAdReady = AdManager.Instance != null && AdManager.Instance.IsRewardedAdReady();

            #if UNITY_EDITOR
            isAdReady = true;   // for testing
            #endif

            if (isAdReady)
            {
                uiManager.ShowWatchForCoinsBtn();
            }
        }

        public void ShowRewardedVideo()
        {
            uiManager.HideWatchForCoinsBtn();

            #if UNITY_EDITOR
            RewardCoins();  // for testing in the editor
            #else
        // Subscribe to rewarded ad events and show it
        AdManager.RewardedAdCompleted += OnRewardedAdCompleted;
        AdManager.Instance.ShowRewardedAd();
            #endif
        }

        void OnRewardedAdCompleted(RewardedAdNetwork adNetwork, AdLocation location)
        {
            // Unsubscribe
            AdManager.RewardedAdCompleted -= OnRewardedAdCompleted;

            // Reward the user with coins
            RewardCoins();
        }

        void RewardCoins()
        {
            uiManager.ShowRewardUI(reward);
        }
    }
}
