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
        private string m_UserID = "";
        System.Action initCallback = null;

        void IAnalytics.Initialise(Action callback)
        {
            initCallback = callback;
            m_UserID = PlayerPrefs.GetString("UserID", "");
            if (m_UserID.Length < 1)
            {
                m_UserID = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("UserID", m_UserID.ToString());
            }
            CreateDirectory(Application.persistentDataPath + "/Analytics");
        }


        public bool CreateDirectory(string dir)
        {
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.DirectoryInfo info = System.IO.Directory.CreateDirectory(dir);
            }
            return true; // TODO - check return value of System.IO.Directory.CreateDirectory(dir)
        }

        void IAnalytics.InitialiseWithCustomID(string userID, Action callback)
        {
            m_UserID = userID;
            if (m_UserID.Length > 0)
            {
                PlayerPrefs.SetString("UserID", m_UserID.ToString());
            }
            else
            {
                Debug.LogError("Trying to use a custom ID of an empty string.");
            }
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

            //Load all events currently saved to disk
            List<CustomEvent> events = new List<CustomEvent>();
            if (File.Exists(Application.persistentDataPath + "/Analytics/Event.json"))
            {
                events = JsonConvert.DeserializeObject<List<CustomEvent>>(File.ReadAllText(@Application.persistentDataPath + "/Analytics/Event.json"));
            }
            //Add the new event
            events.Add(customEvent);

            string jsonData = JsonConvert.SerializeObject(events, Formatting.Indented);

            byte[] byteData;
            byteData = System.Text.Encoding.ASCII.GetBytes(jsonData);
            string path = Application.persistentDataPath + "/Analytics/Event.json";
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
        void IAnalytics.SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value = 0.0f)
        {
            SendCustomEvent(eventName, parameters);
        }

    }
}