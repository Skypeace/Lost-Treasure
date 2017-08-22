using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SqLib
{
	public class HpView : MonoBehaviour 
	{

		// Use this for initialization
		void Start () 
		{
			
		}
		
		// Update is called once per frame
		void Update () 
		{
			
		}

		public void UpdateHP(int hp)
		{
			gameObject.GetComponent<Text>().text = hp.ToString();
		}
	}
}