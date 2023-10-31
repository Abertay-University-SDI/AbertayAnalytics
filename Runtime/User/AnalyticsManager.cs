//#define GAMEANALYTICS //Game Analytics is no longer supported. Removing this will break the plugin
                        //Kept in case we want to bring it back
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
        [SerializeField] private bool m_InitialiseOnStart   = true;
        [Header("--Analytic Systems--")]
        [SerializeField] private bool m_AbertayAnalytics    = true;
        [SerializeField] private bool m_UnityAnalytics      = false;
#if GAMEANALYTICS
        [SerializeField] private bool m_GameAnalytics = false;
#endif

            [Space(30)]
        [Tooltip("This is an optional parameter. Leave it blank unless you want a custom environment.")]
        [SerializeField] private string m_EnvironmentName = "";

#if GAMEANALYTICS
        //Getter
        /// <summary>
        /// You can use this function to specifically get the GameAnalytics instance.
        /// This is handy for logging specific GameAnalytic events but still have them go through the Abertay Analytics manager
        /// </summary>
        public static GameAnalytics GetGAInstance
        {
            get
            {
                if (Instance.m_GameAnalytics)
                    return (GameAnalytics)Instance.m_AnalyticStack[AnalyticSystem.GameAnalytics];
                else
                    return null;
            }
        }
#endif
        //private member
        private Dictionary<AnalyticSystem, IAnalytics> m_AnalyticStack;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                m_AnalyticStack = new Dictionary<AnalyticSystem, IAnalytics>();
                DontDestroyOnLoad(gameObject);
                if (m_AbertayAnalytics)
                    m_AnalyticStack.Add(AnalyticSystem.AbertayAnalytics, new AbertayAnalytics());
                if (m_UnityAnalytics)
                    m_AnalyticStack.Add(AnalyticSystem.UnityAnalytics, new UnityAnalytics());
#if GAMEANALYTICS
                if (m_GameAnalytics)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("Abertay Analytics warning: GameAnalytics will not send ANYTHING from the editor. You need to do a build to log events with GameAnalytics!");
#endif
                    m_AnalyticStack.Add(AnalyticSystem.GameAnalytics, new GameAnalytics());
                }
#endif
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
                Instance.Init(m_EnvironmentName);
            }
        }
        public static void Initialise(string environmentName = "")
        {
            if (!Initialised)
            {
                Instance.m_EnvironmentName = environmentName;
                Instance.Init(environmentName);
            }
            else
            {
                Debug.LogWarning("Attempting to initialise an already initialised system.");
            }
        }
        private void Init(string environmentName = "")
        {
            //TODO: initialised is set as true when *any* of the Analytics Systems are initialised
            //      should only be true if they are *all* successful
            foreach (KeyValuePair<AnalyticSystem, IAnalytics> pair in Instance.m_AnalyticStack)
            {
                pair.Value.Initialise(() => { Initialised = true; }, environmentName);
            }
        }
        public static void InitialiseWithCustomID(string customID, string environmentName = "", System.Action callback = null)
        {
            if (!Initialised)
            {
                foreach (KeyValuePair<AnalyticSystem, IAnalytics> pair in Instance.m_AnalyticStack)
                {
                    pair.Value.InitialiseWithCustomID(customID, () => { 
                            Initialised = true; 
                            if (callback != null) 
                                callback(); 
                        }, 
                        environmentName);
                }
            }
            else
            {
                Debug.LogWarning("Attempting to initialise an already initialised system.");
            }
        }
       
        public static void SendCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (Initialised)
            {
                foreach (KeyValuePair<AnalyticSystem, IAnalytics> pair in Instance.m_AnalyticStack)
                {
                    pair.Value.SendCustomEvent(eventName, parameters);
                }
            }
            else
            {
                Debug.LogError("Attempting to send a custom event without initialising first.\nDid you forget to call Initialise?");
            }
        }

        public static void LogHeatmapEvent(string eventName, Vector3 _pos, Color eventColor)
        {
            DataRecorder.RecordEventPosition( eventName, _pos, eventColor );
        }
    }
}