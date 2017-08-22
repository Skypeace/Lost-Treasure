using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PixelCrushers.DialogueSystem;

namespace SgLib
{
	public class StroyUI : MonoBehaviour 
	{
		public Button back;
		public Button Level1;
		public Button Level2;
		public Button Level3;
		public Button Level4;
		public Button Level5;

		void Start () 
		{
			
		}

		public void OnbackPress () 
		{
			SceneManager.LoadScene("Intro");
		}

		public void OnSelectLevelOne ()
		{
			GameObject.Find("Captain Fronda State1").GetComponent<ConversationTrigger>().OnUse();
//			SceneManager.LoadScene("Level1");
		}
		public void OnSelectLevelTwo ()
		{
			GameObject.Find("Captain Fronda State2").GetComponent<ConversationTrigger>().OnUse();
//			SceneManager.LoadScene("Level2");
		}
		public void OnSelectLevelTree ()
		{
			GameObject.Find("Captain Fronda State3").GetComponent<ConversationTrigger>().OnUse();
//			SceneManager.LoadScene("Level3");
		}
		public void OnSelectLevelFour ()
		{
			GameObject.Find("Captain Fronda State4").GetComponent<ConversationTrigger>().OnUse();
//			SceneManager.LoadScene("Level4");
		}
		public void OnSelectLevelFive ()
		{
			GameObject.Find("Captain Fronda State5").GetComponent<ConversationTrigger>().OnUse();
//			SceneManager.LoadScene("Level5");
		}
	}
}