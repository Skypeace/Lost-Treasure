using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


namespace SgLib
{
	public class IntroUIManager : MonoBehaviour 
	{
		public Button freeGifts;
		public Text freeGiftsText;
		public Button Story;
		public Button Challenge;
		public GameObject rewardingUI;
		public MinMaxReward Minmaxvalue;

		void Start () 
		{
			rewardingUI.SetActive(false);
		}

		public void OnStoryPress () 
		{
			SceneManager.LoadScene("Storyselection");
		}

		public void OnChallengePress ()
		{
			SceneManager.LoadScene("Challenge");
		}
			
		void Update()
		{
			if (freeGifts.gameObject.activeSelf)
			{
				TimeSpan timeToReward = DailyRewardController.Instance.TimeUntilReward;

				if (timeToReward <= TimeSpan.Zero)
				{
					freeGiftsText.text = "GRAB YOUR REWARD!";
					freeGifts.animator.SetTrigger("AnimateText");
					freeGifts.interactable = true;
				}
				else
				{
					freeGiftsText.text = string.Format("REWARD IN {0:00}.{1:00}.{2:00}", timeToReward.Hours, timeToReward.Minutes, timeToReward.Seconds);
				}
			}
		}
		public void GrabDailyReward()
		{
			if (DailyRewardController.Instance.TimeUntilReward <= TimeSpan.Zero)
			{
				//                freeGifts.animator.Stop();

				float reward = UnityEngine.Random.Range(Minmaxvalue.minRewardValue, Minmaxvalue.maxRewardValue);

				// Round the number and make it mutiplies of 5 only.
				int roundedReward = (Mathf.RoundToInt(reward) / 5) * 5;

				// Show the reward UI
				ShowRewardUI(roundedReward);

				// Update next time for the reward
				DailyRewardController.Instance.SetNextRewardTime(Minmaxvalue.rewardIntervalHours, Minmaxvalue.rewardIntervalMinutes, Minmaxvalue.rewardIntervalSeconds);
			}
		}
		public void ShowRewardUI(int reward)
		{
			rewardingUI.SetActive(true);
			rewardingUI.GetComponent<GiftRewardController>().Reward(reward);
		}
		public void HandleSelectCharacterButton()
		{
			SceneManager.LoadScene("CharacterSelection");
		}
	}
}