using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SgLib
{
	public class SkillSpeedUp : Skill
	{

		// Use this for initialization
		public override void action(GameObject go) 
		{
			PlayerController p = go.GetComponent<PlayerController>();
			if(p.maxSpeed <= p.turnRightSpeed)
			{
				p.increaseSpeedFactor = 2.0f;
				p.turnRightSpeed = 0.0f;
				p.turnLeftSpeed = 0.0f;
				p.currentSpeedFactor = 1;
			}
		}
	}
}
