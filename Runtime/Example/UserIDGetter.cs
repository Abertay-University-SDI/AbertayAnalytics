using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UserIDGetter : MonoBehaviour
{
    [SerializeField] private string m_NextSceneName = "";
    [Space]

    [Tooltip("This is an optional parameter. Leave it blank unless you want a custom environment.")]
    [SerializeField] private string m_EnvironmentName = "";

    private InputField m_InputField = null;
    // Start is called before the first frame update
    void Start()
    {
        m_InputField = GetComponentInChildren<InputField>();
    }

    public void SubmitHandler()
    {
        if(m_NextSceneName == "")
        {
            Debug.LogError("No scene has been specified to move to. Please set this in the inspector!\nAnalytics has not yet been initialised!");
        }
        else
        {
            Abertay.Analytics.AnalyticsManager.InitialiseWithCustomID(m_InputField.text, m_EnvironmentName, OnAnalyticsInitialised);
        }
    }

    private void OnAnalyticsInitialised()
    {
        SceneManager.LoadScene(m_NextSceneName);
    }
}
