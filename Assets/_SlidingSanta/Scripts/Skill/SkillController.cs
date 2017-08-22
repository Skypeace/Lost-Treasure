using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SgLib
{
	public class SkillController : MonoBehaviour
	{
		public Skill skill;

		void Update()
		{
				
		}

		public void UseSkill()
		{
			if(skill)
				skill.action(gameObject);
		}
	}
}
