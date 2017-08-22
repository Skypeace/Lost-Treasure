using UnityEngine;
using System.Collections;
using EasyMobile;

namespace SgLib
{
    public class ScoreReporter : MonoBehaviour
    {
        [Tooltip("Check to disable automatic score reporting to Game Center (iOS) or GPGS (Android)")]
        public bool disable = false;
        public bool verboseDebugLog = false;

        const string SCORE_LEADERBOARD = "Score";

        void OnEnable()
        {
            GameManager.NewGameEvent += OnNewGameEvent;
        }

        void OnDisable()
        {
            GameManager.NewGameEvent -= OnNewGameEvent;
        }

        void OnNewGameEvent(GameEvent gameEvent)
        {
            if (!disable)
            {
                if (gameEvent == GameEvent.GameOver)
                    ReportScore();
            }
        }

        /// <summary>
        /// Reports score to leaderboard.
        /// </summary>
        public void ReportScore()
        {
            if (!GameServiceManager.Instance.IsInitialized)
                return;
            
            int newScore = ScoreManager.Instance.Score;
            GameServiceManager.Instance.ReportScore(newScore, SCORE_LEADERBOARD);
        }
    }
}