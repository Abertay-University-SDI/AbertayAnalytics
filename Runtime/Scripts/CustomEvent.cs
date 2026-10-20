using System.Collections.Generic;
using Newtonsoft.Json;

namespace Abertay.Analytics
{
    [System.Serializable]
    public class CustomEvent
    {
        public string eventTimestamp;
        public string userID;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string sessionID;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string sceneName;
        public string eventName;
        public string eventUUID;
        public Dictionary<string, object> eventParams;
    }
}
