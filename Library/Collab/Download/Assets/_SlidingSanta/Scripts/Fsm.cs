using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fsm : MonoBehaviour 
{
	GameObject player;
	GameObject Bullet;
	float firePerSec=5;
	float time= 5;       // time = firePersec make instance shoot 1 shot

	public enum State
	{
		Attack,Idle
	}

	public State state = State.Idle;

	void OnEnable()
	{
		Bullet = transform.FindChild ("Bullet").gameObject;

	}


	void Update () 
	{
		time += Time.deltaTime;

		if (state == State.Idle)
		{
		}

		else if(state == State.Attack)
		{
			if (player != null) 
			{
				if(time >= firePerSec)
				{
					Bullet.SetActive (true);
					transform.LookAt (player.transform);

					Debug.Log("Fire");
					time = 0;
				}
			}


		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player") 
		{
			player = other.gameObject;
			Debug.Log ("Enter");
			state = State.Attack;
		}

	}

	void OnTriggerExit(Collider other)
	{
		if (other.tag == "Player") 
		{
			Debug.Log ("Exit");
			state = State.Idle;
		}

	}
		
}
