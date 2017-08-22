using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SgLib
{

    public class CharacterScroller : MonoBehaviour
    {
        [Header("Object References")]



        public GameObject vehicleObject;
        public GameObject humanObject;

        [Header("Scroller Config")]
        public float minScale = 1f;
        public float maxScale = 1.5f;
        public float characterSpace = 3f;
        public float moveForwardAmount = 2f;
        public float swipeThresholdX = 5f;
        public float swipeThresholdY = 30f;
        public float rotateSpeed = 30f;
        public float snapTime = 0.3f;
        public float resetRotateSpeed = 180f;
        [Range(0.1f, 1f)]
        public float scrollSpeedFactor = 0.25f;
        // adjust this tweak scrolling speed
        public float yRange = 2f;
        // half the vertical length of the scrolling area for each line.
        public Vector3 vehicleCenterPoint;
        public Vector3 humanCenterPoint;
        public Vector3 originalScale = Vector3.one;
        public Vector3 originalRotation = Vector3.zero;

        [Header("UI stuff")]
        public Text totalGold;
        public GameObject confirmButton;
        public GameObject disabedConfirmButton;
        public Color lockColor = Color.black;

        [Header("Human stuff")]
        public GameObject humanSelector;
        public GameObject humanOwnedText;
        public Text humanPriceText;
        public GameObject humanUnlockButton;
        public GameObject humanLockButton;

        [Header("Vehicle stuff")]
        public GameObject vehicleSelector;
        public GameObject vehicleOwnedText;
        public Text vehiclePriceText;
        public GameObject vehicleUnlockButton;
        public GameObject vehicleLockButton;

        List<GameObject> listVehicle = new List<GameObject>();
        List<GameObject> listHuman = new List<GameObject>();
        GameObject currentVehicle;
        GameObject currentHuman;
        GameObject lastCurrentVehicle;
        GameObject lastCurrentHuman;

        public Renderer vehicleRen;
        public MeshFilter vehicleMesh;

        public Renderer humanRen;
        public MeshFilter humanMesh;

        IEnumerator rotateVehicleCoroutine;
        IEnumerator rotateHumanCoroutine;
        Vector3 startPos;
        Vector3 endPos;
        float startTime;
        float endTime;
        float yMousePos;
        bool isCurrentVehicleRotating = false;
        bool isCurrentHumanRotating = false;
        bool hasMovedVehicle = false;
        bool hasMovedHuman = false;

        // Use this for initialization
        void Start()
        {
        
            lockColor.a = 0;    // need this for later setting material colors to work

            vehicleRen = vehicleObject.GetComponent<Renderer>();
            vehicleMesh.mesh = vehicleObject.GetComponent<MeshFilter>().sharedMesh;

            humanRen = humanObject.GetComponent<Renderer>();
            humanMesh.mesh = humanObject.GetComponent<MeshFilter>().sharedMesh;

            #region Vehicles
            int currentVehicleIndex = VehicleManager.Instance.CurrentVehicleIndex;

            currentVehicleIndex = Mathf.Clamp(currentVehicleIndex, 0, VehicleManager.Instance.vehicles.Length - 1);
       
            for (int i = 0; i < VehicleManager.Instance.vehicles.Length; i++)
            {
                int deltaIndex = i - currentVehicleIndex;

                GameObject vehicle = (GameObject)Instantiate(VehicleManager.Instance.vehicles[i], vehicleCenterPoint, Quaternion.Euler(originalRotation.x, originalRotation.y, originalRotation.z));
                Character vehicleData = vehicle.GetComponent<Character>();
                vehicleData.characterSequenceNumber = i;
                listVehicle.Add(vehicle);
                vehicle.transform.localScale = originalScale;
                vehicle.transform.position = vehicleCenterPoint + new Vector3(deltaIndex * characterSpace, 0, 0);

                // Set color based on locking status
                Renderer vehicleRdr = vehicle.GetComponent<Renderer>();

                if (vehicleData.IsUnlocked)
                    vehicleRdr.material.SetColor("_Color", Color.white);
                else
                    vehicleRdr.material.SetColor("_Color", lockColor);

                UpdateVehicleSelector(vehicleData.IsUnlocked, vehicleData.price);

                // set as child of this object
                vehicle.transform.parent = transform;                 
            }
            #endregion

            #region Humans
            int currentHumanIndex = HumanManager.Instance.CurrentHumanIndex;
            currentHumanIndex = Mathf.Clamp(currentHumanIndex, 0, HumanManager.Instance.humans.Length - 1);

            for (int i = 0; i < HumanManager.Instance.humans.Length; i++)
            {
                int deltaIndex = i - currentHumanIndex;
                GameObject human = (GameObject)Instantiate(HumanManager.Instance.humans[i], humanCenterPoint, Quaternion.Euler(originalRotation.x, originalRotation.y, originalRotation.z));
                Character humanData = human.GetComponent<Character>();
                humanData.characterSequenceNumber = i;
                listHuman.Add(human);
                human.transform.localScale = originalScale;
                human.transform.position = humanCenterPoint + new Vector3(deltaIndex * characterSpace, 0, 0);

                Renderer humanRdr = human.GetComponent<Renderer>();

                if (humanData.IsUnlocked)
                    humanRdr.sharedMaterial.SetColor("_Color", Color.white);
                else
                    humanRdr.sharedMaterial.SetColor("_Color", lockColor);

                UpdateHumanSelector(humanData.IsUnlocked, humanData.price);

                human.transform.parent = transform;
            }
            #endregion

            // Highlight current vehicle
            currentVehicle = listVehicle[currentVehicleIndex];
            currentVehicle.transform.localScale = maxScale * originalScale;
            currentVehicle.transform.position += moveForwardAmount * Vector3.forward;
            lastCurrentVehicle = null;
            StartRotateCurrentVehicle();

            //Hilight current human
            currentHuman = listHuman[currentHumanIndex];
            currentHuman.transform.localScale = maxScale * originalScale;
            currentHuman.transform.position += moveForwardAmount * Vector3.forward;
            lastCurrentHuman = null;
            StartRotateCurrentHuman();

            DisplayCharacter();
            StartCoroutine(CRRotateCharacter(vehicleObject.transform.parent.transform));
        }

        // Update is called once per frame
        void Update()
        {
            #region Scrolling
            // Do the scrolling stuff
            if (Input.GetMouseButtonDown(0))    // first touch
            {
                startPos = Input.mousePosition;
                startTime = Time.time;
                hasMovedVehicle = false;
                hasMovedHuman = false;
                yMousePos = Camera.main.ScreenToWorldPoint(startPos).y;
            }
            else if (Input.GetMouseButton(0))   // touch stays
            {
                endPos = Input.mousePosition;
                endTime = Time.time;            

                float deltaX = Mathf.Abs(startPos.x - endPos.x);
                float deltaY = Mathf.Abs(startPos.y - endPos.y);

                if (deltaX >= swipeThresholdX && deltaY <= swipeThresholdY)
                {
                    float speed = deltaX / (endTime - startTime);
                    Vector3 dir = (startPos.x - endPos.x < 0) ? Vector3.right : Vector3.left;
                    Vector3 moveVector = dir * (speed / 10) * scrollSpeedFactor * Time.deltaTime;

                    if (yMousePos < vehicleCenterPoint.y + yRange && yMousePos > vehicleCenterPoint.y - yRange)
                    {
                        // Swipe vehicle line
                        hasMovedVehicle = true;
                        if (isCurrentVehicleRotating)
                            StopRotateCurrentVehicle(true);
                    
                        // Move and scale the children
                        for (int i = 0; i < listVehicle.Count; i++)
                        {
                            MoveAndScale(listVehicle[i].transform, moveVector);
                        }

                        // Update for next step
                        startPos = endPos;
                        startTime = endTime;
                    }
                    else if (yMousePos < humanCenterPoint.y + yRange && yMousePos > humanCenterPoint.y - yRange)
                    {
                        // Swipe human line
                        hasMovedHuman = true;
                        if (isCurrentHumanRotating)
                            StopRotateCurrentHuman(true);
                        // Move and scale the children
                        for (int i = 0; i < listHuman.Count; i++)
                        {
                            MoveAndScale(listHuman[i].transform, moveVector);
                        }

                        // Update for next step
                        startPos = endPos;
                        startTime = endTime;
                    }

               
                }
           
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (hasMovedVehicle)
                {
                    // Store the last currentCharacter
                    lastCurrentVehicle = currentVehicle;

                    // Update current character to the one nearest to center point
                    currentVehicle = FindVehicleNearestToCenter();

                    // Snap
                    float snapDistance = vehicleCenterPoint.x - currentVehicle.transform.position.x;
                    StartCoroutine(SnapVehicleAndRotate(snapDistance));
                }

                if (hasMovedHuman)
                {
                    lastCurrentHuman = currentHuman;

                    currentHuman = FindHumanNearestToCenter();

                    float snapDistance = humanCenterPoint.x - currentHuman.transform.position.x;
                    StartCoroutine(SnapHumanAndRotate(snapDistance));
                }

                DisplayCharacter();
            }

            #endregion

            #region Update UI
            totalGold.text = CoinManager.Instance.Coins.ToString();

            Character vehicleData = currentVehicle.GetComponent<Character>();
            Character humanData = currentHuman.GetComponent<Character>();

            vehiclePriceText.text = vehicleData.price.ToString();
            humanPriceText.text = humanData.price.ToString();

            if (currentVehicle != lastCurrentVehicle)
            {
                UpdateVehicleSelector(vehicleData.IsUnlocked, vehicleData.price);
            }

            if (currentHuman != lastCurrentHuman)
            {
                UpdateHumanSelector(humanData.IsUnlocked, humanData.price);
            }     

            UpdateConfirmButton();
            #endregion
        }

        void UpdateHumanSelector(bool isUnlocked, int price)
        {
            if (isUnlocked)
            {
                humanOwnedText.SetActive(true);
                humanPriceText.transform.parent.gameObject.SetActive(false);
                humanUnlockButton.SetActive(false);
                humanLockButton.SetActive(false);
            }
            else
            {
                humanOwnedText.SetActive(false);
                humanPriceText.transform.parent.gameObject.SetActive(true);

                if (CoinManager.Instance.Coins >= price)
                {
                    humanUnlockButton.SetActive(true);
                    humanLockButton.SetActive(false);
                }
                else
                {
                    humanUnlockButton.SetActive(false);
                    humanLockButton.SetActive(true);
                }
            }
        }

        void UpdateVehicleSelector(bool isUnlocked, int price)
        {
            if (isUnlocked)
            { 
                vehicleOwnedText.SetActive(true);
                vehiclePriceText.transform.parent.gameObject.SetActive(false);
                vehicleUnlockButton.SetActive(false);
                vehicleLockButton.SetActive(false);
            }
            else
            {  
                vehicleOwnedText.SetActive(false);
                vehiclePriceText.transform.parent.gameObject.SetActive(true);
                if (CoinManager.Instance.Coins >= price)
                {
                    vehicleUnlockButton.SetActive(true);
                    vehicleLockButton.SetActive(false);
                }
                else
                {
                    vehicleUnlockButton.SetActive(false);
                    vehicleLockButton.SetActive(true);
                }    
            }
        }

        void MoveAndScale(Transform tf, Vector3 moveVector)
        {
            // Move
            tf.position += moveVector;

            // Scale and move forward according to distance from current position to center position
            float d = Mathf.Abs(tf.position.x - vehicleCenterPoint.x);
            if (d < (characterSpace / 2))
            {
                float factor = 1 - d / (characterSpace / 2);
                float scaleFactor = Mathf.Lerp(minScale, maxScale, factor);
                tf.localScale = scaleFactor * originalScale;

                float fd = Mathf.Lerp(0, moveForwardAmount, factor);
                Vector3 pos = tf.position;
                pos.z = vehicleCenterPoint.z + fd;
                tf.position = pos;
            }
            else
            {
                tf.localScale = minScale * originalScale;
                Vector3 pos = tf.position;
                pos.z = vehicleCenterPoint.z;
                tf.position = pos;
            }
        }

        GameObject FindVehicleNearestToCenter()
        {
            float min = -1;
            GameObject nearestObj = null;

            for (int i = 0; i < listVehicle.Count; i++)
            {
                float d = Mathf.Abs(listVehicle[i].transform.position.x - vehicleCenterPoint.x);
                if (d < min || min < 0)
                {
                    min = d;
                    nearestObj = listVehicle[i];
                }
            }

            return nearestObj;
        }

        GameObject FindHumanNearestToCenter()
        {
            float min = -1;
            GameObject nearestObj = null;

            for (int i = 0; i < listHuman.Count; i++)
            {
                float d = Mathf.Abs(listHuman[i].transform.position.x - humanCenterPoint.x);
                if (d < min || min < 0)
                {
                    min = d;
                    nearestObj = listHuman[i];
                }
            }

            return nearestObj;
        }

        IEnumerator SnapVehicleAndRotate(float snapDistance)
        {       
            float snapDistanceAbs = Mathf.Abs(snapDistance);
            float snapSpeed = snapDistanceAbs / snapTime;
            float sign = snapDistance / snapDistanceAbs; 
            float movedDistance = 0;

            SoundManager.Instance.PlaySound(SoundManager.Instance.tick);

            while (Mathf.Abs(movedDistance) < snapDistanceAbs)
            {
                float d = sign * snapSpeed * Time.deltaTime;
                float remainedDistance = Mathf.Abs(snapDistanceAbs - Mathf.Abs(movedDistance));
                d = Mathf.Clamp(d, -remainedDistance, remainedDistance);

                Vector3 moveVector = new Vector3(d, 0, 0);
                for (int i = 0; i < listVehicle.Count; i++)
                {
                    MoveAndScale(listVehicle[i].transform, moveVector);
                }

                movedDistance += d;
                yield return null;
            } 
            
            if (currentVehicle != lastCurrentVehicle || !isCurrentVehicleRotating)
            {
                // Stop rotating the last current character
                StopRotateCurrentVehicle();

                // Now rotate the new current character
                StartRotateCurrentVehicle();
            }
        }

        IEnumerator SnapHumanAndRotate(float snapDistance)
        {
            float snapDistanceAbs = Mathf.Abs(snapDistance);
            float snapSpeed = snapDistanceAbs / snapTime;
            float sign = snapDistance / snapDistanceAbs;
            float movedDistance = 0;

            SoundManager.Instance.PlaySound(SoundManager.Instance.tick);

            while (Mathf.Abs(movedDistance) < snapDistanceAbs)
            {
                float d = sign * snapSpeed * Time.deltaTime;
                float remainedDistance = Mathf.Abs(snapDistanceAbs - Mathf.Abs(movedDistance));
                d = Mathf.Clamp(d, -remainedDistance, remainedDistance);

                Vector3 moveVector = new Vector3(d, 0, 0);
                for (int i = 0; i < listHuman.Count; i++)
                {
                    MoveAndScale(listHuman[i].transform, moveVector);
                }

                movedDistance += d;
                yield return null;
            }

            if (currentHuman != lastCurrentHuman || !isCurrentHumanRotating)
            {
                // Stop rotating the last current character
                StopRotateCurrentHuman();

                // Now rotate the new current character
                StartRotateCurrentHuman();
            }
        }

        void StartRotateCurrentVehicle()
        {
            StopRotateCurrentVehicle(false);   // stop previous rotation if any
            rotateVehicleCoroutine = CRRotateCharacter(currentVehicle.transform);
            StartCoroutine(rotateVehicleCoroutine);
            isCurrentVehicleRotating = true;
        }

        void StopRotateCurrentVehicle(bool resetRotation = false)
        {
            if (rotateVehicleCoroutine != null)
            {
                StopCoroutine(rotateVehicleCoroutine);
            }

            isCurrentVehicleRotating = false;

            if (resetRotation)
                StartCoroutine(CRResetCharacterRotation(currentVehicle.transform));        
        }


        void StartRotateCurrentHuman()
        {
            StopRotateCurrentHuman(false);
            rotateHumanCoroutine = CRRotateCharacter(currentHuman.transform);
            StartCoroutine(rotateHumanCoroutine);
            isCurrentHumanRotating = true;
        }


        void StopRotateCurrentHuman(bool resetRotation = false)
        {
            if (rotateHumanCoroutine != null)
            {
                StopCoroutine(rotateHumanCoroutine);
            }

            isCurrentHumanRotating = false;

            if (resetRotation)
                StartCoroutine(CRResetCharacterRotation(currentHuman.transform));
      
        }

        IEnumerator CRRotateCharacter(Transform charTf)
        {
            while (true)
            {
                charTf.Rotate(new Vector3(0, rotateSpeed * Time.deltaTime, 0));
                yield return null;
            }
        }

        IEnumerator CRResetCharacterRotation(Transform charTf)
        {
            Vector3 startRotation = charTf.rotation.eulerAngles;
            Vector3 endRotation = originalRotation;
            float timePast = 0;
            float rotateAngle = Mathf.Abs(endRotation.y - startRotation.y);
            float rotateTime = rotateAngle / resetRotateSpeed;

            while (timePast < rotateTime)
            {
                timePast += Time.deltaTime;
                Vector3 rotation = Vector3.Lerp(startRotation, endRotation, timePast / rotateTime);
                charTf.rotation = Quaternion.Euler(rotation);
                yield return null;
            }
        }

        public void UnlockVehicleButton()
        {
            bool unlockSucceeded = currentVehicle.GetComponent<Character>().Unlock();
            if (unlockSucceeded)
            {
                currentVehicle.GetComponent<Renderer>().material.SetColor("_Color", Color.white);
                vehicleUnlockButton.gameObject.SetActive(false);
            }
        }

        public void UnlockHumanButton()
        {
            bool unlockSucceeded = currentHuman.GetComponent<Character>().Unlock();
            if (unlockSucceeded)
            {
                currentHuman.GetComponent<Renderer>().material.SetColor("_Color", Color.white);
                humanUnlockButton.gameObject.SetActive(false);
            }
        }

        void DisplayCharacter()
        {
            vehicleMesh.mesh = currentVehicle.GetComponent<MeshFilter>().sharedMesh;
            vehicleRen.material = currentVehicle.GetComponent<Renderer>().sharedMaterial;

            humanMesh.mesh = currentHuman.GetComponent<MeshFilter>().sharedMesh;
            humanRen.material = currentHuman.GetComponent<Renderer>().sharedMaterial;
        }

        void UpdateConfirmButton()
        {
            if (currentHuman.GetComponent<Character>().IsUnlocked && currentVehicle.GetComponent<Character>().IsUnlocked)
            {
                confirmButton.SetActive(true);
                disabedConfirmButton.SetActive(false);
            }
            else
            {
                confirmButton.SetActive(false);
                disabedConfirmButton.SetActive(true);
            }
        }

        public void HandleConfirmButton()
        {
            VehicleManager.Instance.CurrentVehicleIndex = currentVehicle.GetComponent<Character>().characterSequenceNumber;
            HumanManager.Instance.CurrentHumanIndex = currentHuman.GetComponent<Character>().characterSequenceNumber;
            BackToMainScene();
        }

        public void BackToMainScene()
        {
            UIManager.firstLoad = false;
            SceneManager.LoadScene("Intro");
        }
    }
}
