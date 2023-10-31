using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Abertay.Analytics
{
    public interface IAnalytics
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if successful</returns>
        public void Initialise(System.Action callback, string environmentName);
        public void InitialiseWithCustomID(string userID, System.Action callback, string environmentName);
        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters);

        public void SetUserID(string userID);
#if GAMEANALYTICS
        /// <summary>
        /// This version of the function call is only necessary when using Game Analytics.
        /// You could use it anyway for complete compatability, but you might be sending unecessary data
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="parameters">These values are only availble through raw data export (a premium GA feature)</param>
        /// <param name="value"></param>
        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value = 0.0f);
#endif
    }
}