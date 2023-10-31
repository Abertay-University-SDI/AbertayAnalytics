using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Globalization;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Abertay.Analytics
{
    public class AbertayAnalytics : IAnalytics
    {
        private string m_UserID = "NULL";
        private string m_Environment = "";

        /// <summary>
        /// The environment name can be used to specify a different file name
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="environmentName"></param>
        void IAnalytics.Initialise(Action callback, string environmentName)
        {
            if(environmentName.Length > 0)
                m_Environment = environmentName;
            if (m_UserID.Length < 1)
            {
                SetUserID(System.Guid.NewGuid().ToString());
            }
#if UNITY_EDITOR
            string path = Application.dataPath;
#else
        string path = Application.dataPath + "/..";
#endif
            CreateDirectory(path + "/Analytics/Events");
            callback();
        }


        public bool CreateDirectory(string dir)
        {
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.DirectoryInfo info = System.IO.Directory.CreateDirectory(dir);
            }
            return true; // TODO - check return value of System.IO.Directory.CreateDirectory(dir)
        }

        /// <summary>
        /// The environment name can be used to specify a different file name
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="callback"></param>
        /// <param name="environmentName"></param>
        void IAnalytics.InitialiseWithCustomID(string userID, Action callback, string environmentName)
        {
            if (environmentName.Length > 0)
                m_Environment = environmentName;
            if (!(userID.Length > 0)) {            
                Debug.LogError("Trying to use a custom ID of an empty string. Setting to NULL");
                SetUserID("NULL");
            }
            else
            {
                SetUserID(userID);
            }
#if UNITY_EDITOR
            string path = Application.dataPath + "/Analytics/Events";
#else
            string path = Application.dataPath + "/../Analytics/Events";
#endif
            CreateDirectory(path);
            callback();
        }
        //TODO: This could be way more efficient
        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            //Build custom event structure
            CustomEvent customEvent = new CustomEvent();
            var now = DateTime.UtcNow;
            string datePatt = @"d/MM/yyyy HH:mm:ss";
            string timeStamp = now.ToString(datePatt);
            customEvent.eventTimestamp = timeStamp;
            customEvent.userID = m_UserID;
            customEvent.eventName = eventName;
            customEvent.eventUUID = Hash128.Compute(eventName + customEvent.eventTimestamp + customEvent.userID).ToString(); //TODO: something better than this?
            customEvent.eventParams = parameters;

            string JSONfileName = "/Analytics/Events/Events" + (m_Environment.Length > 0 ? ("_" + m_Environment):("")) + "_JSON.json";
            string CSVfileName = "/Analytics/Events/Events" + (m_Environment.Length > 0 ? ("_" + m_Environment) : ("")) + "_CSV.csv";

#if UNITY_EDITOR
            string path = Application.dataPath;
#else
        string path = Application.dataPath + "/..";
#endif
            string CSVpath  = path + CSVfileName;
            string JSONpath = path + JSONfileName;

            //TODO: CSV export
            {
                string header = "Timestamp,UserID,Event Name,UUID";
                foreach(KeyValuePair<string,object> kvp in customEvent.eventParams)
                {
                    header += ",Param/" + kvp.Key;
                }
            }

            //Load all events currently saved to disk
            List<CustomEvent> events = new List<CustomEvent>();
            if (File.Exists(JSONpath))
            {
                events = JsonConvert.DeserializeObject<List<CustomEvent>>(File.ReadAllText(@JSONpath));
            }
            //Add the new event
            events.Add(customEvent);

            //TODO: CSV export
            File.WriteAllText(CSVpath, "");
            using (StreamWriter sw = File.AppendText(CSVpath))
            {
                HashSet<string> keys = new HashSet<string>();
                foreach (CustomEvent ce in events)
                {
                    foreach (KeyValuePair<string, object> kvp in ce.eventParams)
                    {
                        keys.Add(kvp.Key);
                    }
                }
                string header = "Timestamp,UserID,Event Name,UUID";
                foreach (string s in keys)
                {
                    header += ",Param/" + s;
                }
                sw.WriteLine(header);

                string line = "";

                foreach (CustomEvent ce in events)
                {
                    //log core info
                    line = ce.eventTimestamp + "," + ce.userID + "," + ce.eventName + "," + ce.eventUUID;
                    foreach(string s in keys)
                    {
                        if (ce.eventParams.ContainsKey(s))
                        {
                            line += "," + ce.eventParams[s];
                        }
                        else
                        {
                            line += ",";
                        }
                    }
                    sw.WriteLine(line);
                }
                sw.Close();
            }

            string jsonData = JsonConvert.SerializeObject(events, Formatting.Indented);

            byte[] byteData;
            byteData = System.Text.Encoding.ASCII.GetBytes(jsonData);
            // attempt to save event
            try
            {
                // save data here
                File.WriteAllBytes(JSONpath, byteData);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save JSON data to: " + JSONpath);
                Debug.LogError("Error " + e.Message);
            }
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public void SetUserID(string userId)
        {
            m_UserID = userId;
            PlayerPrefs.SetString("UserID", m_UserID);
        }
#if GAMEANALYTICS
        void IAnalytics.SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value = 0.0f)
        {
            SendCustomEvent(eventName, parameters);
        }
#endif

    }
}