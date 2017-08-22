using UnityEngine;
using System.Collections;

namespace SgLib
{
    public class VehicleManager : MonoBehaviour
    {
        public static VehicleManager Instance;

        public static readonly string CURRENT_VEHICLE_KEY = "SGLIB_CURRENT_VEHICLE";

        public int CurrentVehicleIndex
        {
            get
            {
                return PlayerPrefs.GetInt(CURRENT_VEHICLE_KEY, 0);
            }
            set
            {
                PlayerPrefs.SetInt(CURRENT_VEHICLE_KEY, value);
                PlayerPrefs.Save();
            }
        }

        public GameObject[] vehicles;

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


