//#define GAMEANALYTICS //Game Analytics is no longer supported. Removing this will break the plugin
//Kept in case we want to bring it back
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Abertay.Analytics
{
    public enum AnalyticSystem
    {
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

        [Space(30)]
        [Tooltip("This is an optional parameter. Leave it blank unless you want a custom environment.")]
        [SerializeField] private string m_EnvironmentName = "";

        [Header("Heatmap")]
        [SerializeField] private TextAsset m_CurrentHeatmap = null;
        private TextAsset m_LastHeatmap = null;
        [SerializeField] private bool m_ShowHeatmap = false;
        [SerializeField][Range(0.0f, 1.0f)] private float m_GizmoOpacity = 0.5f;
        [SerializeField][Range(0.0f, 1.0f)] private float m_GizmoSize = 0.5f;
        private static List<string> m_EventNames = new List<string>();
        private static List<Vector3> m_EventPositions = new List<Vector3>();
        private static List<Color> m_EventColors = new List<Color>();
        private float alpha = 1.0f;

        //private member
        private Dictionary<AnalyticSystem, IAnalytics> m_AnalyticStack;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                m_AnalyticStack = new Dictionary<AnalyticSystem, IAnalytics>();
                m_AnalyticStack.Add(AnalyticSystem.AbertayAnalytics, new AbertayAnalytics());
                
                DontDestroyOnLoad(gameObject);
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

        public static void SetUserID(string newUserID)
        {
            foreach (KeyValuePair<AnalyticSystem, IAnalytics> pair in Instance.m_AnalyticStack)
            {
                pair.Value.SetUserID(newUserID);
            }
        }
        /// <summary>Flush queued JSON Lines events without ending the session.</summary>
        public static bool FlushLocalDataForExport()
        {
            if (Instance == null || !Initialised || Instance.m_AnalyticStack == null)
                return true;

            if (Instance.m_AnalyticStack.TryGetValue(AnalyticSystem.AbertayAnalytics, out IAnalytics backend)
                && backend is AbertayAnalytics local)
                return local.FlushPendingWrites();

            return true;
        }

        /// <summary>Get the active local JSONL log, if a session has been initialised.</summary>
        public static string GetActiveLocalJsonPath()
        {
            if (Instance != null && Initialised && Instance.m_AnalyticStack != null
                && Instance.m_AnalyticStack.TryGetValue(AnalyticSystem.AbertayAnalytics, out IAnalytics backend)
                && backend is AbertayAnalytics local)
                return local.JsonFilePath;

            return null;
        }

        public static string GetCurrentSessionID()
        {
            if (Instance != null && Initialised && Instance.m_AnalyticStack != null
                && Instance.m_AnalyticStack.TryGetValue(AnalyticSystem.AbertayAnalytics, out IAnalytics backend)
                && backend is AbertayAnalytics local)
                return local.SessionID;

            return null;
        }

        public static void LogHeatmapEvent(string eventName, Vector3 _pos, Color eventColor)
        {
            DataRecorder.RecordEventPosition( eventName, _pos, eventColor );
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            if (Initialised && m_AnalyticStack != null)
            {
                foreach (IAnalytics backend in m_AnalyticStack.Values)
                    backend.OnQuit();
            }
            Initialised = false;
            Instance = null;
        }

        public void OnApplicationQuit()
        {
            if (Initialised)
            {
                foreach (KeyValuePair<AnalyticSystem, IAnalytics> pair in Instance.m_AnalyticStack)
                {
                    pair.Value.OnQuit();
                }


#if UNITY_EDITOR
                ////Re-import the file to update the reference in the editor
                UnityEditor.AssetDatabase.ImportAsset(DataRecorder.FilePath);
                TextAsset asset = Resources.Load<TextAsset>(DataRecorder.FilePath);
#endif
            }
        }

        private void OnValidate()
        {
            bool failure = false;
            if (m_CurrentHeatmap != null && m_CurrentHeatmap != m_LastHeatmap)
            {
                m_EventNames.Clear();
                m_EventPositions.Clear();
                m_EventColors.Clear();

                string file = m_CurrentHeatmap.text;
                string[] lines = file.Split('\n');
                if (lines.Length > 0)
                {
                    foreach (string line in lines)
                    {
                        if (line.Length > 0)
                        {
                            if (line[0] == '#')
                            {
                                string scene = line.Substring(1);
                                scene = scene.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
                                string currentScene = SceneManager.GetActiveScene().name;
                                if (scene != currentScene)
                                {
                                    Debug.LogWarning("Scene name in heatmap file ("+ scene +") does not match active scene (" + currentScene + ")\n" +
                                        $"It looks like this might be a heatmap for another scene, or the scene has been renamed.");
                                }
                                continue;
                            }
                            //going through the text file line by line and adding it to a list of vectors.
                            string[] splitString = line.Split(':');
                            if (splitString.Length != 3)
                            {
                                failure = true;
                                break;
                            }
                            m_EventNames.Add(splitString[0]);
                            m_EventPositions.Add(stringToVec(splitString[1]));
                            m_EventColors.Add(stringToCol(splitString[2]));
                        }
                    }
                    if (m_EventNames.Count == 0)
                    {
                        failure = true;
                    }
                }
                if(failure)
                {
                    Debug.LogError("That's not a valid Heatmap file!");
                    m_CurrentHeatmap = null;
                }
            }
            if(failure || m_CurrentHeatmap == null)
            {
                m_EventNames.Clear();
                m_EventPositions.Clear();
                m_EventColors.Clear();
            }
            m_LastHeatmap = m_CurrentHeatmap;
        }

        private void OnDrawGizmos()
        {
            if (m_ShowHeatmap && m_GizmoOpacity > 0.0f)
            {
                Vector3 p;
                Vector3 s = Vector3.one * 0.5f;
                Color c = Color.green;
                c.a = m_GizmoOpacity;
                for (int i=0; i < m_EventNames.Count; i++)
                {
                    c = m_EventColors[i];
                    c.a *= m_GizmoOpacity;
                    Gizmos.color = c;
                    //c.a = m_GizmoOpacity;
                    p = m_EventPositions[i];
                    Gizmos.DrawSphere(p, m_GizmoSize);
                }
            }
        }
        public static Vector3 stringToVec(string _st)
        {
            Vector3 result = new Vector3();
            _st = _st.Replace("(", string.Empty);
            _st = _st.Replace(")", string.Empty);
            string[] vals = _st.Split(',');
            if (vals.Length == 3)
            {
                result.Set(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
            }
            return result;
        }
        public static Color stringToCol(string _st)
        {
            Color result = Color.magenta;
            _st = _st.Replace("(", string.Empty);
            _st = _st.Replace(")", string.Empty);
            string[] vals = _st.Split(',');
            if (vals.Length == 4)
            {
                result = new Color(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[3]));
            }
            return result;
        }
    }

}