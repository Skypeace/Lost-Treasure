using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SgLib
{
	public class MinMaxReward : MonoBehaviour 
	{
		[Header("Daily Reward Config")]
		[Tooltip("Number of hours between 2 rewards")]
		public int rewardIntervalHours = 6;
		[Tooltip("Number of minues between 2 rewards")]
		public int rewardIntervalMinutes = 0;
		[Tooltip("Number of seconds between 2 rewards")]
		public int rewardIntervalSeconds = 0;
		public float minRewardValue = 20;
		public float maxRewardValue = 50;

		void Start () 
		{
			
		}
		

		void Update () 
		{
			
		}
	}
}