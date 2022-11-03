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
        [SerializeField] private AnalyticSystem m_SystemType = AnalyticSystem.AbertayAnalytics;

        [Tooltip("This is an optional parameter. Leave it blank unless you want a custom environment.")]
        [SerializeField] private string m_EnvironmentName = "";

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
        private IAnalytics          m_AnalyticSystem;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
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
                Debug.LogWarning("Trying to create a second analytics manager.\nDestroying this new one!");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (!Initialised && m_InitialiseOnStart)
            {
                m_AnalyticSystem.Initialise(() => { Initialised = true; }, m_EnvironmentName);                
            }
        }
        public static void Initialise(string environmentName = "")
        {
            if (!Initialised)
            {
                Instance.m_AnalyticSystem.Initialise(() => { Initialised = true; }, environmentName);
            }
            else
            {
                Debug.LogError("Attempting to initialise an already initialised system.");
            }
        }
        public static void InitialiseWithCustomID(string customID, string environmentName = "", System.Action callback = null)
        {
            if (!Initialised)
            {
                Instance.m_AnalyticSystem.InitialiseWithCustomID(customID, () => { Initialised = true; if (callback != null) callback(); }, environmentName);
            }
            else
            {
                Debug.LogError("Attempting to initialise an already initialised system.");
            }
        }
       
        public static void SendCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (Initialised)
                Instance.m_AnalyticSystem.SendCustomEvent(eventName, parameters);
            else
                Debug.LogError("Attempting to send a custom event without initialising first.\nDid you forget to call Initialise?");
        }
    }
}