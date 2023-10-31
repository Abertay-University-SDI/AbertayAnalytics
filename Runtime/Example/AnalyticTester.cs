using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abertay.Analytics;

public class AnalyticTester : MonoBehaviour
{
    private void Start()
    {
        //If the Analytic Manager isn't set to initialise on Start you can initialise it here
            //AnalyticsManager.Initialise();
        //Or
            //AnalyticsManager.InitialiseWithCustomID("Custom ID goes here");
    }

    /// <summary>
    /// Simulates a standard Custom Event call with parameters. This is designed for Abertay or Unity Analytics
    /// </summary>
    [ContextMenu("Send event")] public void SendEventUnityAnalyticsStyle()
    {
        // Send custom event
        Dictionary<string, object> parameters = new Dictionary<string, object>()
        {
            { "healthRemaining",  Random.Range(0,1000) },
            { "timeRemaining",  Random.Range(0,1000) },
            { "characterName", "Mario" }
        };
        AnalyticsManager.SendCustomEvent("LevelComplete", parameters);
    }
    [ContextMenu("Send event")]
    public void SendEventUnityAnalyticsStyle2()
    {
        // Send custom event
        Dictionary<string, object> parameters = new Dictionary<string, object>()
        {
            { "someFloat",  Random.Range(0.0f,100.0f) },
            { "timeRemaining",  Random.Range(0,1000) },
            { "otherName", "Luigi" }
        };
        AnalyticsManager.SendCustomEvent("WorldComplete", parameters);
    }

#if GAMEANALYTICS
    /// <summary>
    /// Simulates a standard Custom Event call with parameters. This version is designed for GameAnalytics
    /// Remember: You can't get parameter data through game analytics without paying money
    /// </summary>
    [ContextMenu("Send Game Analytics Event")] void SendEventGameAnalyticsStyle()
    {
        // Send custom event
        Dictionary<string, object> parameters = new Dictionary<string, object>()
        {
            { "healthRemaining",  Random.Range(0,1000) },
            { "timeRemaining",  Random.Range(0,1000) },
            { "powerLevel",  Random.Range(0,1000) },
            { "characterName", "Mario" },
            { "isCool", false }
        };
        if(AnalyticsManager.GetGAInstance != null)
        {
            //Remember! You can't get parameter data through game analytics without paying money
                //AnalyticsManager.GetGAInstance.SendDesignEvent("World01-01:Kills:Goomba", parameters, 123.0f);
            //So this call is likely to be more common
                AnalyticsManager.GetGAInstance.SendDesignEvent("World01-01:Kills:Goomba", 123.0f);

            //You might also want to send a Progression Event
            AnalyticsManager.GetGAInstance.SendProgressionEvent(GameAnalyticsSDK.GAProgressionStatus.Complete, "World01-01");
        }
}
#endif

}
