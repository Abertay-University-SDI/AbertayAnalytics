using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abertay.Analytics;

public class HeatmapTester : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Color c = Color.green;
            c.a = 0.5f;
            AnalyticsManager.LogHeatmapEvent("Jump", transform.position, c);
        }
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Color c = Color.red;
            c.a = 0.5f;
            AnalyticsManager.LogHeatmapEvent("Death", transform.position, c);
        }
    }
}
