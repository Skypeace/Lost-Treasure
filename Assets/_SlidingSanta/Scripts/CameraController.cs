using UnityEngine;
using System.Collections;

namespace SgLib
{
    public class CameraController : MonoBehaviour
    {

        public GameManager gameManager;
        public PlayerController playerController;

        // How long the camera shaking.
        public float shakeDuration = 0.1f;
        // Amplitude of the shake. A larger value shakes the camera harder.
        public float shakeAmount = 0.2f;
        public float decreaseFactor = 0.3f;
   

        private Vector3 originalPos;
        private float currentShakeDuration;
        private float currentDistance;

        // Update is called once per frame
        void LateUpdate()
        {

            if (playerController.startRun && !gameManager.gameOver)
            {
                transform.position += Vector3.back * playerController.turnRightSpeed * Time.deltaTime;
            }
        }

        public void ShakeCamera()
        {
            StartCoroutine(Shake());
        }

        IEnumerator Shake()
        {
            originalPos = transform.position;
            currentShakeDuration = shakeDuration;
            while (currentShakeDuration > 0)
            {
                transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
                currentShakeDuration -= Time.deltaTime * decreaseFactor;
                yield return null;
            }
            transform.position = originalPos;
        }
    }
}
