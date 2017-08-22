using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SgLib
{
	public class SkillButtonView : MonoBehaviour 
	{
		public float cooldownTime = 1.0f;
		// Use this for initialization
		void Start () 
		{
		
		}

		public void OnButtonPress()
		{
			StartCoroutine("DisableButtonForSeconds", cooldownTime);
		}

		IEnumerator DisableButtonForSeconds(float DisableTime)
		{
			GetComponent<Button>().interactable = false;
			yield return new WaitForSeconds(DisableTime);
			GetComponent<Button>().interactable = true;
		}
	}
}
