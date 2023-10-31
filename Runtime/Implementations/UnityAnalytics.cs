using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Analytics;
using Unity.Services.Core.Environments;
using Unity.Services.Analytics;
using System;

namespace Abertay.Analytics
{
    public class UnityAnalytics : IAnalytics
    {
        async void IAnalytics.Initialise(Action callback, string environmentName)
        {
            Debug.Log("Initialising Unity Analytics");
            InitializationOptions options = new InitializationOptions();

            if(environmentName.Length > 0)
                options.SetEnvironmentName(environmentName);

            await UnityServices.InitializeAsync(options);

            AnalyticsService.Instance.StartDataCollection();
            

            Debug.Log("Unity Analytics initialised");
            if (callback != null)
                callback();
        }

        //TODO: Duplication here isn't great
        async void IAnalytics.InitialiseWithCustomID(string userID, Action callback, string environmentName)
        {
            Debug.Log("Initialising Unity Analytics w/ Custom ID");
            InitializationOptions options = new InitializationOptions();

            if (environmentName.Length > 0)
                options.SetEnvironmentName(environmentName);
            if (userID.Length > 0)
            {
                SetUserID(userID);
                //options.SetAnalyticsUserId(userID);
            }
            else
            {
                Debug.LogWarning("Supplied User ID was empty. Using default Unity ID for this device instead.");
            }

            await UnityServices.InitializeAsync(options);

            AnalyticsService.Instance.StartDataCollection();

            Debug.Log("Unity Analytics initialised w/ Custom ID");
            if (callback != null)
                callback();
        }

        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            // The ‘myEvent’ event will get queued up and sent every minute
            AnalyticsService.Instance.CustomData(eventName, parameters);
            AnalyticsService.Instance.Flush();  //Technically don't need to do this...
        }

        public void SetUserID(string userID)
        {
            UnityServices.ExternalUserId = userID;
        }
#if GAMEANALYTICS
        void IAnalytics.SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value = 0.0f)
        {
            SendCustomEvent(eventName, parameters);
        }
#endif
    }
}