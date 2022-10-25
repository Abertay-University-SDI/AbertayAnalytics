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
        async void IAnalytics.Initialise(System.Action callback)
        {
            try
            {
                Debug.Log("Initialising Unity Analytics");
                InitializationOptions options = new InitializationOptions();

                await UnityServices.InitializeAsync(options);
                List<string> consentIdentifiers = await AnalyticsService.Instance.CheckForRequiredConsents();
            }
            catch (ConsentCheckException e)
            {
                //TODO: actually deal with this...
            }

            Debug.Log("Unity Analytics initialised");
            if (callback != null)
                callback();
        }

        //TODO: Duplication here isn't great
        async void IAnalytics.InitialiseWithCustomID(string userID, Action callback)
        {
            try
            {
                Debug.Log("Initialising Unity Analytics w/ Custom ID");
                InitializationOptions options = new InitializationOptions();

                if (userID.Length > 0)
                    options.SetAnalyticsUserId(userID);

                await UnityServices.InitializeAsync(options);
                List<string> consentIdentifiers = await AnalyticsService.Instance.CheckForRequiredConsents();
            }
            catch (ConsentCheckException e)
            {
                //TODO: actually deal with this...
            }

            Debug.Log("Unity Analytics initialised w/ Custom ID");
            if (callback != null)
                callback();
        }

        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            // The ‘myEvent’ event will get queued up and sent every minute
            AnalyticsService.Instance.CustomData(eventName, parameters);
            AnalyticsService.Instance.Flush();
        }
        void IAnalytics.SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value = 0.0f)
        {
            SendCustomEvent(eventName, parameters);
        }
    }
}