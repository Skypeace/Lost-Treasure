using UnityEngine;
using System.Collections;

namespace SgLib
{
    public class HumanManager : MonoBehaviour
    {

        public static HumanManager Instance;

        public static readonly string CURRENT_HUMAN_KEY = "SGLIB_CURRENT_HUMAN";

        public int CurrentHumanIndex
        {
            get
            {
                return PlayerPrefs.GetInt(CURRENT_HUMAN_KEY, 0);
            }
            set
            {
                PlayerPrefs.SetInt(CURRENT_HUMAN_KEY, value);
                PlayerPrefs.Save();
            }
        }

        public GameObject[] humans;

        void Awake()
        {
            if (Instance)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
