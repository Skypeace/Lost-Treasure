using UnityEngine;
using System.Collections;

namespace SgLib
{
    public class GoldController : MonoBehaviour
    {
        public bool stopBounce = false;

        void Start()
        {
            StartCoroutine(Bounce());
        }

        public void GoUp()
        {
            stopBounce = true;
            StartCoroutine(Up());
        }

        IEnumerator Bounce()
        {
            while (!stopBounce)
            {
                Vector3 startPos = transform.position;
                Vector3 endPos = startPos + new Vector3(0, 0.3f, 0);

                float t = 0;
                while (t < 0.5f && !stopBounce)
                {
                    t += Time.deltaTime;
                    float fraction = t / 0.5f;
                    transform.position = Vector3.Lerp(startPos, endPos, fraction);
                    yield return null;
                }

                float r = 0;
                while (r < 0.5f && !stopBounce)
                {
                    r += Time.deltaTime;
                    float fraction = r / 0.5f;
                    transform.position = Vector3.Lerp(endPos, startPos, fraction);
                    yield return null;
                }
            }
        }

        IEnumerator Up()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(0, 4f, 0);
            Quaternion startRot = transform.rotation;
            Quaternion endRot = Quaternion.Euler(0, 180, 0);

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime;
                float fraction = t / 1f;
                transform.position = Vector3.Lerp(startPos, endPos, fraction);
                transform.rotation = Quaternion.Lerp(startRot, endRot, fraction);
                yield return null;
            }

            gameObject.SetActive(false);
            transform.parent = CoinManager.Instance.transform;
        }
    }
}
