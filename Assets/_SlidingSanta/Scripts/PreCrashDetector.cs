using UnityEngine;
using System.Collections;

namespace SgLib
{
    public class PreCrashDetector : MonoBehaviour
    {

        public static event System.Action PreCrashing;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
            {
                PreCrashing();
            }
        }
    }
}

