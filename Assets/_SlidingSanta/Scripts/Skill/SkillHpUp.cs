using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SgLib
{
	public class SkillHpUp : Skill
	{
		
		public override void action(GameObject go)
		{
			go.GetComponent<PlayerModel>().HP += 1;
		}
	}
}