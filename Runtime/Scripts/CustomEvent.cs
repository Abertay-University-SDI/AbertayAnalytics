using System.Collections.Generic;

namespace Abertay.Analytics
{
    [System.Serializable]
    public class CustomEvent
    {
        public string eventTimestamp;
        public string userID;
        public string eventName;
        public string eventUUID;
        public float GA_Value;
        public Dictionary<string, object> eventParams;
    }
}