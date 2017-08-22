using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SgLib
{

	[System.Serializable]

	public class HPChangeEvent : UnityEvent <int> {}

	public class PlayerModel : MonoBehaviour

	{

		[SerializeField]

		private int hp = 5;

		public int HP

		{
			get { return hp; }
			set
			{
				hp = value;

				if (hp > 5 ) hp = 5;

				else if (hp < 0 ) hp = 0;

				onHPChangedEvents.Invoke(hp);
			}
		}

		[SerializeField]

		public HPChangeEvent onHPChangedEvents;
	}
}