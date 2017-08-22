using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SgLib
{
public class Bot : MonoBehaviour 
	{
		[Space]
		public PlayerController player;

		public float moveSpeed = 20;
		public float rotateSpeed = 5f;
		public float radius = 1f;

		public Vector3 Position
		{
			get
			{
				return transform.position;
			}
			set
			{
				transform.position = value;
			}
		}

		void Update ()
		{

			player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

			if(player.tag == "Player")
			{
				Vector3 velocity = Seek(player.transform.position);

				float remainingDistance = Vector3.Distance(player.transform.position, Position);
				if (remainingDistance >= player.radius + radius)
				{
					Position = Position + velocity;
				}
			}



			Rotate(player.transform.position);
		}

		public Vector3 Seek(Vector3 target)
		{
			Vector3 displacement = (target - Position);
			Vector3 direction = displacement.normalized;

			return direction * moveSpeed * Time.deltaTime;
		}

		public void Rotate(Vector3 direction)
		{
			Quaternion lookAt = Quaternion.LookRotation(Vector3.forward, direction);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, lookAt, rotateSpeed);
		}

	}
}