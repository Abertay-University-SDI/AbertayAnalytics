using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UserIDGetter : MonoBehaviour
{
    [SerializeField] private bool m_ChangeSceneOnSubmission = false;
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
        if(m_ChangeSceneOnSubmission)
            Abertay.Analytics.AnalyticsManager.InitialiseWithCustomID(m_InputField.text, m_EnvironmentName, OnAnalyticsInitialised);
        else
            Abertay.Analytics.AnalyticsManager.InitialiseWithCustomID(m_InputField.text, m_EnvironmentName, null);
    }

    private void OnAnalyticsInitialised()
    {
        SceneManager.LoadScene(m_NextSceneName);
    }
}
