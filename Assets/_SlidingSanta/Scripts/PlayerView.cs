using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace SgLib
{
	public class GetMouse : UnityEvent<bool>{}
	public class PlayerView : MonoBehaviour 
	{
		public static GetMouse getMouseDown = new GetMouse();
		public static GetMouse getMouse = new GetMouse();
		PlayerModel playerModel;

		void Awake () {
			playerModel = gameObject.GetComponent<PlayerModel>();
		}

		void Update () {
			if(Input.GetMouseButtonDown(0))
			{
				getMouseDown.Invoke(true);
//				playerModel.getButtondown = true;
			}
			else
				getMouseDown.Invoke(false);
			if(Input.GetMouseButton(0))
			{
				getMouse.Invoke(true);
//				playerModel.getButton = true;
			}
			else
			getMouse.Invoke(false);
		}
	}
}