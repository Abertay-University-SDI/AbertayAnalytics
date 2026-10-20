using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abertay.Analytics;

public class AnalyticTester : MonoBehaviour
{
    public Transform mover;
    private void Start()
    {
        //If the Analytic Manager isn't set to initialise on Start you can initialise it here
            //AnalyticsManager.Initialise();
        //Or
            //AnalyticsManager.InitialiseWithCustomID("Custom ID goes here");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Send custom event
            Dictionary<string, object> parameters = new Dictionary<string, object>()
            {
                { "someFloat",  Random.Range(0.0f,100.0f) },
                { "timeRemaining",  Random.Range(0,1000) },
                { "otherName", "Luigi" },
                {"position", mover.position }
            };
            AnalyticsManager.SendCustomEvent("WorldComplete", parameters);
        }
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
            { "characterName", "Mario" },
            {"position", mover.position }
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
            { "otherName", "Luigi" },
            {"position", mover.position }
        };
        AnalyticsManager.SendCustomEvent("WorldComplete", parameters);

        AnalyticsManager.LogHeatmapEvent("CustEvent", mover.position, Color.green);
    }

}
