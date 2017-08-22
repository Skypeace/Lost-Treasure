using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;

#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

#if UNITY_ANDROID
using GooglePlayGames;
#endif

namespace EasyMobile
{
    [System.Serializable]
    public class Leaderboard
    {
        public string Name { get { return name; } }

        public string IOSId { get { return iOSId; } }

        public string AndroidId { get { return androidId; } }

        public string Id
        {
            get
            {
                #if UNITY_IOS
                return iOSId;
                #elif UNITY_ANDROID
                return androidId;
                #else
                return null;
                #endif
            }
        }

        [SerializeField]
        string name = "LEADERBOARD_NAME";
        [SerializeField]
        string iOSId = "LEADERBOARD_IOS_ID";
        [SerializeField]
        string androidId = "LEADERBOARD_ANDROID_ID";
    }

    [System.Serializable]
    public class Achievement
    {
        public string Name { get { return name; } }

        public string IOSId { get { return iOSId; } }

        public string AndroidId { get { return androidId; } }

        public string Id
        {
            get
            {
                #if UNITY_IOS
                return iOSId;
                #elif UNITY_ANDROID
                return androidId;
                #else
                return null;
                #endif
            }
        }

        [SerializeField]
        private string name = "ACHIEVEMENT_NAME";
        [SerializeField]
        private string iOSId = "ACHIEVEMENT_IOS_ID";
        [SerializeField]
        private string androidId = "ACHIEVEMENT_ANDROID_ID";
    }

    public class GameServiceManager : MonoBehaviour
    {
        public static GameServiceManager Instance { get; private set; }

        public static event System.Action UserAuthenticated = delegate {};

        public delegate void LoadScoreCallback(IScore[] scores);

        public bool verboseDebugLog = false;
        public bool androidDebugLog = false;

        [Header("AUTO INIT CONFIG")]
        [Tooltip("Check if you want the service to be initialized automatically on startup.")]
        public bool autoInitOnStart = true;
        [Tooltip("The delay time of the service's auto initialization after startup.")]
        public float autoInitDelay = 0f;
        [Tooltip("[Android] If the user dismisses the login popup this number of times, we'll stop showing it. Put -1 if you want to keep showing it on later startup.")]
        public int androidMaxLoginRequest = 3;


        [Header("LEADERBOARD SETUP")]
        public Leaderboard[] leaderboards;

        [Header("ACHIEVEMENT SETUP")]
        public Achievement[] achievements;

        public bool IsInitialized
        {
            get
            {
                return Social.localUser.authenticated;
            }
        }
                        
        #if UNITY_ANDROID
        const string ANDROID_LOGIN_REQUEST_NUMBER_PPKEY = "SGLIB_ANDROID_LOGIN_REQUEST_NUMBER";
        #endif

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

        void Start()
        {
            if (autoInitOnStart)
                StartCoroutine(CRAutoInit(autoInitDelay));
        }

        // On iOS, the OS automatically shows the user login popup when the app gets focus for the first 3 times.
        // Subsequent authentication calls will be ignored.
        // On Android, if the user dismisses the login popup for a number of times determined
        // by androidMaxLoginRequest, we'll stop show it.
        public IEnumerator CRAutoInit(float delay = 0f)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            #if UNITY_IOS
            if (!IsInitialized)
                Init();
            #elif UNITY_ANDROID
            if (!IsInitialized)
            {
                int loginRequestNumber = PlayerPrefs.GetInt(ANDROID_LOGIN_REQUEST_NUMBER_PPKEY, 0);

                if (loginRequestNumber < androidMaxLoginRequest || androidMaxLoginRequest <= 0)
                {
                    Init();
                    loginRequestNumber++;
                    PlayerPrefs.SetInt(ANDROID_LOGIN_REQUEST_NUMBER_PPKEY, loginRequestNumber);
                    PlayerPrefs.Save();
                }
                else
                {
                    if (verboseDebugLog)
                        Debug.Log("androidMaxLoginRequest exceeded. Requests attempted: " + loginRequestNumber);
                }
            }
            #endif
        }

        /// <summary>
        /// Initializes the service. This is required before any other actions can be done e.g reporting scores.
        /// </summary>
        public void Init()
        {
            // Authenticate and register a ProcessAuthentication callback
            // This call needs to be made before we can proceed to other calls in the Social API
            #if UNITY_IOS && !UNITY_EDITOR
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
            Social.localUser.Authenticate(ProcessAuthentication);

            #elif UNITY_ANDROID && !UNITY_EDITOR
            if (Social.Active != PlayGamesPlatform.Instance)
            {
                PlayGamesPlatform.Activate();
            }
            PlayGamesPlatform.DebugLogEnabled = androidDebugLog;

            Social.localUser.Authenticate(ProcessAuthentication);
            #else
            Debug.Log("GameServiceManager not initializing: platform not supported.");
            #endif
        }

        // This function gets called when Authenticate completes
        // Note that if the operation is successful, Social.localUser will contain data from the server.
        void ProcessAuthentication(bool success)
        {
            if (success)
            {
                Debug.Log("User authenticated to GameServiceManager, checking achievements...");

                // Fire event
                UserAuthenticated();

                #if UNITY_ANDROID
                // Reset login request number
                PlayerPrefs.SetInt(ANDROID_LOGIN_REQUEST_NUMBER_PPKEY, 0);
                PlayerPrefs.Save();
                #endif


                // Request loaded achievements, and register a callback for processing them
                Social.LoadAchievements(ProcessLoadedAchievements);

            }
            else
                Debug.Log("Failed to authenticate user to GameServiceManager.");
        }

        // This function gets called when the LoadAchievement call completes
        void ProcessLoadedAchievements(IAchievement[] achievements)
        {
            if (achievements.Length == 0)
                Debug.Log("No achievements found.");
            else
                Debug.Log("Got " + achievements.Length + " achievements.");
        }

        /// <summary>
        /// Returns a leaderboard it one with a leaderboardName was declared before within leaderboards array.
        /// </summary>
        /// <returns>The leaderboard by name.</returns>
        /// <param name="leaderboardName">Leaderboard name.</param>
        public Leaderboard GetLeaderboardByName(string leaderboardName)
        {
            foreach (Leaderboard ldb in leaderboards)
            {
                if (ldb.Name.Equals(leaderboardName))
                    return ldb;
            }

            return null;
        }

        /// <summary>
        /// Returns an achievement it one with an achievementName was declared before within achievements array.
        /// </summary>
        /// <returns>The achievement by name.</returns>
        /// <param name="achievementName">Achievement name.</param>
        public Achievement GetAchievementByName(string achievementName)
        {
            foreach (Achievement acm in achievements)
            {
                if (acm.Name.Equals(achievementName))
                    return acm;
            }

            return null;
        }

        /// <summary>
        /// Shows the leaderboard UI.
        /// </summary>
        public void ShowLeaderboardUI()
        {
            #if UNITY_IOS || UNITY_ANDROID
            if (IsInitialized)
                Social.ShowLeaderboardUI();
            else
            {
                Debug.Log("ShowLeaderboardUI FAILED: user is not logged in.");
            }
            #else
                Debug.Log("ShowLeaderboardUI FAILED: platform not supported.");
            #endif
        }

        /// <summary>
        /// Shows the achievements UI.
        /// </summary>
        public void ShowAchievementsUI()
        {
            #if UNITY_IOS || UNITY_ANDROID
            if (IsInitialized)
                Social.ShowAchievementsUI();
            else
            {
                Debug.Log("ShowAchievementsUI FAILED: user is not logged in.");                    
            }
            #else
                Debug.Log("ShowAchievementsUI FAILED: platform not supported.");
            #endif
        }

        /// <summary>
        /// Reports the given score to the leaderboard with the specified name.
        /// </summary>
        /// <param name="score">Score.</param>
        /// <param name="leaderboardName">Leaderboard name.</param>
        public void ReportScore(long score, string leaderboardName)
        {
            #if UNITY_IOS || UNITY_ANDROID
            Leaderboard ldb = GetLeaderboardByName(leaderboardName);

            if (ldb != null)
            {
                DoReportScore(score, ldb.Id);
            }
            else
            {
                Debug.Log("ReportScore FAILED: unknown leaderboard name.");
            }
            #else
                Debug.Log("ReportScore FAILED: platform not supported.");
            #endif
        }

        void DoReportScore(long score, string leaderboardId)
        {
            if (!IsInitialized)
            {
                Debug.Log("DoReportScore FAILED: user is not logged in.");
                return;
            }

            if (verboseDebugLog)
                Debug.Log("Reporting score: " + score);

            Social.ReportScore(
                score,
                leaderboardId,
                (bool success) =>
                {
                    if (verboseDebugLog)
                        Debug.Log(success ? "Score reported successfully." : "Failed to report score.");
                }
            );
        }

        /// <summary>
        /// Unlocks the achievement with the specified name.
        /// </summary>
        /// <param name="achievementName">Achievement name.</param>
        public void UnlockAchievement(string achievementName)
        {
            #if UNITY_IOS || UNITY_ANDROID
            Achievement acm = GetAchievementByName(achievementName);

            if (acm != null)
            {
                DoReportAchievementProgress(acm.Id, 100.0f);
            }
            else
            {
                Debug.Log("UnlockAchievement FAILED: unknown achievement name.");
            }
            #else
                Debug.Log("UnlockAchievement FAILED: platform not supported.");
            #endif
        }

        // Progress of 0.0% means reveal the achievement.
        // Progress of 100.0% means unlock the achievement.
        void DoReportAchievementProgress(string achievementId, double progress)
        {
            if (!IsInitialized)
            {
                Debug.Log("DoReportAchievementProgress FAILED: user is not logged in.");
                return;
            }

            if (verboseDebugLog)
                Debug.Log("Reporting progress of " + progress + "% for achievement: " + achievementId);

            Social.ReportProgress(
                achievementId, 
                progress, 
                (bool success) =>
                { 
                    if (verboseDebugLog)
                        Debug.Log(success ? "Successfully reported progress of " + progress + "% for achievement: " + achievementId : "Failed to report progress for achievement: " + achievementId);
                }
            );
        }
    }
}
