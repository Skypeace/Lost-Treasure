using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SgLib
{
	public abstract class Skill : MonoBehaviour 
	{
		public virtual void action(GameObject go){}
	}
}