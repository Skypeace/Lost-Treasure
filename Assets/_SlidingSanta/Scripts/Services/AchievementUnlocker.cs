using UnityEngine;
using System.Collections;
using EasyMobile;

namespace SgLib
{
    public class AchievementUnlocker : MonoBehaviour
    {
        [Tooltip("Check to disable automatic achievement unlocking")]
        public bool disable = false;

        void OnEnable()
        {
            ScoreManager.ScoreUpdated += OnScoreUpdated;
        }

        void OnDisable()
        {
            ScoreManager.ScoreUpdated -= OnScoreUpdated;
        }

        void OnScoreUpdated(int score)
        {
            if (disable)
            {
                return;
            }

            string achievement = null;

            if (score == 200)
                achievement = "Score_200";
            else if (score == 190)
                achievement = "Score_190";
            else if (score == 180)
                achievement = "Score_180";
            else if (score == 170)
                achievement = "Score_170";
            else if (score == 160)
                achievement = "Score_160";
            else if (score == 150)
                achievement = "Score_150";
            else if (score == 140)
                achievement = "Score_140";
            else if (score == 130)
                achievement = "Score_130";
            else if (score == 120)
                achievement = "Score_120";
            else if (score == 110)
                achievement = "Score_110";
            else if (score == 100)
                achievement = "Score_100";
            else if (score == 90)
                achievement = "Score_90";
            else if (score == 80)
                achievement = "Score_80";
            else if (score == 70)
                achievement = "Score_70";
            else if (score == 60)
                achievement = "Score_60";
            else if (score == 50)
                achievement = "Score_50";
            else if (score == 40)
                achievement = "Score_40";
            else if (score == 30)
                achievement = "Score_30";
            else if (score == 20)
                achievement = "Score_20";
            else if (score == 10)
                achievement = "Score_10";

            // Unlock achievement
            if (achievement != null)
                GameServiceManager.Instance.UnlockAchievement(achievement);
        }
    }
}
