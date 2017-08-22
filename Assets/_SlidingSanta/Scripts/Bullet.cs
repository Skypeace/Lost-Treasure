using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class TakeDmg : UnityEvent<float>{} 
public class Bullet : MonoBehaviour {
	public static TakeDmg takedmg =new TakeDmg ();

	private GameObject player;
	private Vector3 playerPosition;
	float moveSpeed = 1000f;       // move speed of seek
	Rigidbody rigid;

	void Awake()
	{
		rigid = GetComponent<Rigidbody> ();
		player = GameObject.FindGameObjectWithTag("Player");
	}

	void OnEnable()
	{
		playerPosition = player.transform.position;
		//ForceTo ();
		Invoke("FreeBullet",5);
	}

	void Update ()
	{
		if (player != null) 
		{
			Vector3 displacement = playerPosition - transform.parent.position;
			Vector3 direction = displacement.normalized;
			//seek
			Vector3 velocity = direction * moveSpeed * Time.deltaTime;
			rigid.velocity = velocity;


		}

	}

	void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Player") 
		{
			takedmg.Invoke(1);
			gameObject.SetActive(false);
			transform.position = transform.parent.position;

		}
	}


	void FreeBullet()
	{
		gameObject.SetActive(false);
	}
}
