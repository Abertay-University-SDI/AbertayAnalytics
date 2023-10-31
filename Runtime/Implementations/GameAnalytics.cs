//#define GAMEANALYTICS 
#if GAMEANALYTICS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameAnalyticsSDK;

namespace Abertay.Analytics
{
    public class GameAnalytics : IAnalytics
    {
        void IAnalytics.Initialise(System.Action callback, string environmentName)
        {
            Debug.Log("Initialising GameAnalytics");
            GameAnalyticsSDK.GameAnalytics.Initialize();


            Debug.Log("GameAnalytics initialised");
            if (callback != null)
                callback();
        }

        void IAnalytics.InitialiseWithCustomID(string userID, Action callback, string environmentName)
        {
            if (userID.Length > 0)
            {
                GameAnalyticsSDK.GameAnalytics.SetCustomId(userID);
            }
            else
            {
                Debug.LogWarning("Supplied User ID was empty. Using default GameAnalytics ID for this device instead.");
            }

            GameAnalyticsSDK.GameAnalytics.Initialize();
            if (callback != null)
                callback();
        }
        public void SendDesignEvent(string eventName, float GA_Value)
        {
            GameAnalyticsSDK.GameAnalytics.NewDesignEvent(eventName, GA_Value);
        }
        public void SendDesignEvent(string eventName, Dictionary<string, object> parameters, float GA_Value)
        {
            GameAnalyticsSDK.GameAnalytics.NewDesignEvent(eventName, GA_Value, parameters);
        }
        void IAnalytics.SendCustomEvent(string eventName, Dictionary<string, object> parameters, float GA_Value)
        {
            //Debug.LogWarning("This call doesn't pass a value to GameAnalytics.\nYou should use the other SendCustomEvent function instead!");
            SendDesignEvent(eventName, parameters, GA_Value);
        }

        public void SendProgressionEvent(GameAnalyticsSDK.GAProgressionStatus progressionStatus, string progression01)
        {
            GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(progressionStatus, progression01);
        }
        public void SendProgressionEvent(GameAnalyticsSDK.GAProgressionStatus progressionStatus, string progression01, int score)
        {
            GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(progressionStatus, progression01, score);
        }
        public void SendProgressionEvent(GameAnalyticsSDK.GAProgressionStatus progressionStatus, string progression01, string progression02 )
        {
            GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(progressionStatus, progression01, progression02);
        }
        public void SendProgressionEvent(GameAnalyticsSDK.GAProgressionStatus progressionStatus, string progression01, string progression02, int score)
        {
            GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(progressionStatus, progression01, progression02, score);
        }
        public void SendProgressionEvent(GameAnalyticsSDK.GAProgressionStatus progressionStatus, string progression01, string progression02, string progression03)
        {
            GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(progressionStatus, progression01, progression02, progression03);
        }
        public void SendProgressionEvent(GameAnalyticsSDK.GAProgressionStatus progressionStatus, string progression01, string progression02, string progression03, int score)
        {
            GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(progressionStatus, progression01, progression02, progression03, score);
        }
    }
}
#endif