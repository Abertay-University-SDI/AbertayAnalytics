//#define GAMEANALYTICS //Uncomment this if you want to use Game Analytics
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Abertay.Analytics
{
    public enum AnalyticSystem
    {
        UnityAnalytics,
        #if GAMEANALYTICS
        GameAnalytics,
        #endif
        AbertayAnalytics
    }
    public class AnalyticsManager : MonoBehaviour
    {
        //Static
        private static AnalyticsManager Instance;
        private static bool Initialised = false;

        //Serialized
        [Tooltip("If you don't initialise this on Start, you need to do this yourself or it won't work!")]
        [SerializeField] private bool m_InitialiseOnStart = true;
        [SerializeField] private AnalyticSystem m_SystemType = AnalyticSystem.UnityAnalytics;

#if GAMEANALYTICS
        //Getter
        public static GameAnalytics GetGAInstance
        {
            get
            {
                if (Instance.m_SystemType == AnalyticSystem.GameAnalytics)
                    return (GameAnalytics)Instance.m_AnalyticSystem;
                else
                    return null;
            }
        }
#endif
        //private member
        private IAnalytics m_AnalyticSystem;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                switch (m_SystemType)
                {
                    case AnalyticSystem.UnityAnalytics:
                    default:
                        m_AnalyticSystem = new UnityAnalytics();
                        break;
#if GAMEANALYTICS
                    case AnalyticSystem.GameAnalytics:
                        m_AnalyticSystem = new GameAnalytics();
                        break;
#endif
                    case AnalyticSystem.AbertayAnalytics:
                        m_AnalyticSystem = new AbertayAnalytics();
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Trying to create a second analytics manager.\nDestroying this one!");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (!Initialised && m_InitialiseOnStart)
            {
                m_AnalyticSystem.Initialise(() => { Initialised = true; });
            }
        }
        public static void Initialise()
        {
            if (!Initialised)
            {
                Instance.m_AnalyticSystem.Initialise(() => { Initialised = true; });
            }
            else
            {
                Debug.LogError("Attempting to initialise an already initialised system.");
            }
        }
        public static void InitialiseWithCustomID(string customID)
        {
            if (!Initialised)
            {
                Instance.m_AnalyticSystem.InitialiseWithCustomID(customID, () => { Initialised = true; });
            }
            else
            {
                Debug.LogError("Attempting to initialise an already initialised system.");
            }
        }
        public static void SendCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            Instance.m_AnalyticSystem.SendCustomEvent(eventName, parameters);
        }
    }
}