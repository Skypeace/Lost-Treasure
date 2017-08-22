using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine.Events;

namespace SgLib
{
    public class PlayerController : MonoBehaviour
    {
        public static event System.Action StartRun = delegate {};

        [Header("Gameplay Config")]
        public float maxSpeed;
        public float maxSpeedFactor;
        public float initialCollisionForce;
        public float maxCollisionForce;
        public float increaseSpeedFactor;
        public float increaseCollisionForceFactor;
		public float radius;

        [Header("Gameplay References")]
        public GameManager gameManager;
        public CameraController cameraController;
        public MouseOverDetector selectCharacterButtonMOD;
        public GameObject main;
        public GameObject vehicle;
        public GameObject point_1;
        public GameObject point_2;

        [HideInInspector]
        public float turnRightSpeed;
        [HideInInspector]
        public bool startRun;

        private Rigidbody mainRigid;
        private Rigidbody vehicleRigid;
        private MeshCollider mainCollider;
        private MeshCollider vehicleCollider;
        private Vector3 rightvector;
        private Vector3 leftVector;
        private Vector3 upVector;
        private Vector3 totalVector;
        private Vector3 collisionDirection;
		public float currentSpeedFactor;
        private float currentCollisionForce;
        public float turnLeftSpeed;
        private bool stop;
        private bool isPreGameOver;

		private PlayerModel playerModel;
		private PlayerView playerView;

		private bool getButtondown;
		private bool getButton;
		private string product = "Get";

		void Awake () {
			playerModel = gameObject.GetComponent<PlayerModel>();
			PlayerView.getMouseDown.AddListener(GetButtondown);
			PlayerView.getMouse.AddListener(GetButton);
			Bullet.takedmg.AddListener(TakeDamage);
		}
        // Use this for initialization
        void Start()
        {
            //Change the character to the selected one
            GameObject currentVehicle = VehicleManager.Instance.vehicles[VehicleManager.Instance.CurrentVehicleIndex];
            Mesh vehicleMesh = currentVehicle.GetComponent<MeshFilter>().sharedMesh;
            Material vehicleMaterial = currentVehicle.GetComponent<Renderer>().sharedMaterial;
            vehicle.GetComponent<MeshFilter>().mesh = vehicleMesh;
            vehicle.GetComponent<MeshRenderer>().material = vehicleMaterial;
            vehicle.GetComponent<MeshCollider>().sharedMesh = vehicleMesh;

            GameObject currentHuman = HumanManager.Instance.humans[HumanManager.Instance.CurrentHumanIndex];
            Mesh humanMesh = currentHuman.GetComponent<MeshFilter>().sharedMesh;
            Material humanMaterial = currentHuman.GetComponent<Renderer>().sharedMaterial;
            main.GetComponent<MeshFilter>().mesh = humanMesh;
            main.GetComponent<MeshRenderer>().material = humanMaterial;
            main.GetComponent<MeshCollider>().sharedMesh = humanMesh;

			int hp = currentHuman.GetComponent<Character>().hp;
			main.GetComponentInParent<PlayerModel>().HP = hp;

			Skill sk = currentHuman.GetComponent<Skill>();
			main.GetComponentInParent<SkillController>().skill = sk;

            mainRigid = main.GetComponent<Rigidbody>();
            mainCollider = main.GetComponent<MeshCollider>();
            vehicleRigid = vehicle.GetComponent<Rigidbody>();
            vehicleCollider = vehicle.GetComponent<MeshCollider>();
            collisionDirection = (point_2.transform.position - point_1.transform.position).normalized;

            turnLeftSpeed = 0;
            turnRightSpeed = 0;
            currentSpeedFactor = 0;
            currentCollisionForce = initialCollisionForce;
            StartCoroutine(IncreaseSpeed());
            StartCoroutine(IncreaseCollisionForce());
        }
		void TakeDamage (float d)
		{
			playerModel.HP -= (int)d;
		}

		void GetButtondown(bool g)
		{
			getButtondown = g;
			// Don't do anything if selectCharacter button is clicked
			if (selectCharacterButtonMOD.IsMouseOver)
			{
				return;
			}
			else if ( getButtondown == true && !startRun && !UIManager.firstLoad)
			{

				//				Input.GetMouseButtonDown(0)
				startRun = true;

				// Fire event
				StartRun();
			}
		}

		void GetButton (bool g)
		{
			getButton = g;
			if (startRun && !gameManager.gameOver)
			{
				if (getButton == true)
				{

					//					Input.GetMouseButton(0)
					if (turnLeftSpeed < maxSpeed)
					{
						turnLeftSpeed += currentSpeedFactor * Time.deltaTime;
					}
					else
					{
						turnLeftSpeed = maxSpeed;
					}
				}
				else
				{
					if (turnLeftSpeed > 0)
					{
						turnLeftSpeed -= currentSpeedFactor * Time.deltaTime;
					}
					else
					{
						turnLeftSpeed = 0;
					}
				}            
			}
		}
        // Update is called once per frame
		void Update()
        {

			GetButtondown(getButtondown);
			GetButton(getButton);

            if (!stop)
            {
                rightvector = (Vector3.back + Vector3.right) * turnRightSpeed;
                leftVector = (Vector3.back + Vector3.left) * turnLeftSpeed;
                upVector = (Vector3.forward + Vector3.left) * turnLeftSpeed;    // this one is counter of rightVector
                totalVector = rightvector + leftVector + upVector; //Moving direction
                transform.position += totalVector * Time.deltaTime;
                float rotateAngle = Vector3.Angle(rightvector, totalVector);
                if (turnLeftSpeed == maxSpeed)
                {
                    rotateAngle = 90;
                }
                else if (turnLeftSpeed == 0)
                {
                    rotateAngle = 0;
                }
                transform.rotation = Quaternion.Euler(0, 45 + rotateAngle, 0);
            }

            float x = Camera.main.WorldToViewportPoint(transform.position).x;
            if (!gameManager.gameOver && (x > 1.15f || x < -0.15f))
            {
                stop = true;
                cameraController.ShakeCamera();
                StartCoroutine(gameManager.CRGameOver());
                SoundManager.Instance.PlaySound(SoundManager.Instance.crash);
            }
        }

        public bool IsGoingOutOfScreen()
        {
            float viewportX = Camera.main.WorldToViewportPoint(transform.position).x;

            bool isHeadingRightEdge = viewportX >= 0.95f && turnLeftSpeed == 0;
            bool isHeadingLeftEdge = viewportX <= 0.05f && turnLeftSpeed > 0;

            return isHeadingRightEdge || isHeadingLeftEdge;
        }

        bool IsHittingObstacle(Collider other)
        {
            if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
            {
                Debug.Log("About to hit obstacles");
                return true;
            }

            return false;
        }

        IEnumerator IncreaseSpeed()
        {
            while (!gameManager.gameOver)
            {
                if (startRun)
                {
                    if (turnRightSpeed < maxSpeed)
                    {
                        turnRightSpeed += increaseSpeedFactor * Time.deltaTime;
                    }

                    if (currentSpeedFactor < maxSpeedFactor)
                    {
                        currentSpeedFactor += increaseSpeedFactor * Time.deltaTime;
                    }
                }
            
                if (turnRightSpeed > maxSpeed)
                {
                    turnRightSpeed = maxSpeed;
                }
                if (currentSpeedFactor > maxSpeedFactor)
                {
                    currentSpeedFactor = maxSpeedFactor;
                }

//                if (turnRightSpeed == maxSpeed && currentSpeedFactor == maxSpeedFactor)
//                {
//                    yield break;
//                }

                yield return null;
            }
        }


        IEnumerator IncreaseCollisionForce()
        {
            while (!gameManager.gameOver)
            {
                if (startRun)
                {
                    if (currentCollisionForce < maxCollisionForce)
                    {
                        currentCollisionForce += increaseCollisionForceFactor * Time.deltaTime;
                    }
                    else
                    {
                        currentCollisionForce = maxCollisionForce;
                        yield break;
                    }
                }
                yield return null;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!gameManager.gameOver)
            {
                if (other.CompareTag("Wall")) //Hit wall or obstacle
                {
                    stop = true;
                    GetComponent<BoxCollider>().enabled = false;               
                    mainCollider.isTrigger = false;
                    vehicleCollider.isTrigger = false;
                    AttackPhysics();
                    cameraController.ShakeCamera();
                    StartCoroutine(gameManager.CRGameOver());
                    SoundManager.Instance.PlaySound(SoundManager.Instance.crash);
                }
				else if (other.CompareTag("Obstacle"))
				{
					other.enabled = false;  // Disable the item's collider to prevent OnTriggerEnter being called again when it's going up
					SoundManager.Instance.PlaySound(SoundManager.Instance.crash);
					cameraController.ShakeCamera();

					int hp = --GetComponent<PlayerModel>().HP;
					if(hp <= 0)
					{
						GetComponent<BoxCollider>().enabled = false;
						mainCollider.isTrigger = false;
						vehicleCollider.isTrigger = false;
						AttackPhysics();
						StartCoroutine(gameManager.CRGameOver());
					}
				
				}
				else if (other.CompareTag("Bullet"))
				{
					other.enabled = false;  // Disable the item's collider to prevent OnTriggerEnter being called again when it's going up
					SoundManager.Instance.PlaySound(SoundManager.Instance.crash);
					cameraController.ShakeCamera();

					int hp = --GetComponent<PlayerModel>().HP;
					if(hp <= 0)
					{
						GetComponent<BoxCollider>().enabled = false;
						mainCollider.isTrigger = false;
						vehicleCollider.isTrigger = false;
						AttackPhysics();
						StartCoroutine(gameManager.CRGameOver());
					}
				}
                else if (other.CompareTag("Gold"))//Hit gold
                {
					int oldTreasureValue = DialogueLua.GetVariable(product).AsInt;

                    other.enabled = false;  // Disable the item's collider to prevent OnTriggerEnter being called again when it's going up
                    other.GetComponent<GoldController>().GoUp();
                    SoundManager.Instance.PlaySound(SoundManager.Instance.collectItem);
                    ScoreManager.Instance.AddScore(1);  // Count gifts collected this round
                    CoinManager.Instance.AddCoins(1);   // Total gifts having
					int newTreasureValue = Mathf.Clamp(oldTreasureValue +1, 0, 9999);
					DialogueLua.SetVariable(product, newTreasureValue);
					DialogueManager.SendUpdateTracker();
                }
            }      
        }

        void AttackPhysics()
        {
            StartCoroutine(AddCollisionForce(collisionDirection));
        }

        IEnumerator AddCollisionForce(Vector3 dir)
        {
            yield return new WaitForFixedUpdate();
            mainRigid.isKinematic = false;
            vehicleRigid.isKinematic = false;
            yield return null;
            vehicleRigid.AddForce(dir * currentCollisionForce);
            vehicleRigid.AddTorque(dir * currentCollisionForce);
        }
    }
}
