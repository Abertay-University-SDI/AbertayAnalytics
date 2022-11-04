using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Globalization;

namespace Abertay.Analytics
{
    public class AbertayAnalytics : IAnalytics
    {
        private string m_UserID = "NULL";
        private string m_Environment = "";
        System.Action initCallback = null;

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
                m_UserID = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("UserID", m_UserID.ToString());
            }
            CreateDirectory(Application.persistentDataPath + "/Analytics");
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
            m_UserID = userID;
            if (environmentName.Length > 0)
                m_Environment = environmentName;
            if (!(m_UserID.Length > 0)) {            
                Debug.LogError("Trying to use a custom ID of an empty string. Setting to NULL");
                m_UserID = "NULL";
            }
            CreateDirectory(Application.persistentDataPath + "/Analytics");
            callback();
        }

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

            string fileName = "/Analytics/Event" + (m_Environment.Length > 0 ? ("_" + m_Environment):("")) + ".json";
            string path = Application.persistentDataPath + fileName;

            //Load all events currently saved to disk
            List<CustomEvent> events = new List<CustomEvent>();
            if (File.Exists(path))
            {
                events = JsonConvert.DeserializeObject<List<CustomEvent>>(File.ReadAllText(@path));
            }
            //Add the new event
            events.Add(customEvent);

            string jsonData = JsonConvert.SerializeObject(events, Formatting.Indented);

            byte[] byteData;
            byteData = System.Text.Encoding.ASCII.GetBytes(jsonData);
            // attempt to save event
            try
            {
                // save data here
                File.WriteAllBytes(path, byteData);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save JSON data to: " + path);
                Debug.LogError("Error " + e.Message);
            }
        }
#if GAMEANALYTICS
        void IAnalytics.SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value = 0.0f)
        {
            SendCustomEvent(eventName, parameters);
        }
#endif

    }
}