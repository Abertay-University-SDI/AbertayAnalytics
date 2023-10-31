using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;


public class HeatmapRenderer : MonoBehaviour
{
    public static Material          m_HeatmapMaterial   = null;
    private static List<string>     m_EventNames        = new List<string>();
    private static List<Vector3>    m_EventPositions    = new List<Vector3>();
    private static List<Color>      m_EventColors       = new List<Color>();
    private static string           m_path              = "Assets/Resources/Text/";
    private static Transform        m_Parent            = null;
    private static Dictionary<string, Material> m_Materials = new Dictionary<string, Material>();
    [MenuItem("Tools/Heatmap/Generate Heatmap Render", false, 10)]
    public static void ReadEventData()
    {
        m_path = "Assets/Resources/Heatmaps/" + SceneManager.GetActiveScene().name + "/";

        m_HeatmapMaterial = Resources.Load<Material>("HeatmapMaterial");
        m_EventNames.Clear();
        m_EventPositions.Clear();
        m_EventColors.Clear();

        ClearHeatmapObjects();
        string[] files = Directory.GetFiles(m_path, "*.txt");
        foreach(string file in files)
        {
            
            string fullPath = file; //Creates and uses a file per scence. This application uses your scene name to generate death textfile. 
                                             //Read the text from directly from the txt file
           // string fullPath = filePath + ".txt";
            string eventCoords = "";
            StreamReader reader = new StreamReader(fullPath);
            while ((eventCoords = reader.ReadLine()) != null)
            {
                //going through the text file line by line and adding it to a list of vectors.
                string[] splitString = eventCoords.Split(':');
                m_EventNames.Add(splitString[0]);
                m_EventPositions.Add(stringToVec(splitString[1]));
                m_EventColors.Add(stringToCol(splitString[2]));
                eventCoords = "";
            }
            reader.Close();
        }
        
        

        if(m_Parent == null)
        {
            m_Parent = (new GameObject("Heatmap")).GetComponent<Transform>();
        }

        //Reset Parent transform
        m_Parent.transform.position = Vector3.zero;
        m_Parent.transform.rotation = Quaternion.identity;
        m_Parent.transform.localScale = Vector3.one;

        RenderEventData();
    }

    [MenuItem("Tools/Heatmap Data/Combine Heatmaps for current Scene")]
    public static void CombineHeatmaps()
    {
        string totalString = "";
        m_path = "Assets/Resources/Heatmaps/" + SceneManager.GetActiveScene().name + "/";

        string[] files = Directory.GetFiles(m_path, "*.txt");
        string[] metaFiles = Directory.GetFiles(m_path, "*.meta");
        foreach (string file in files)
        {
            string fullPath = file; //Creates and uses a file per scence. This application uses your scene name to generate death textfile. 
                                    //Read the text from directly from the txt file
                                    // string fullPath = filePath + ".txt";
            string eventCoords = "";
            StreamReader reader = new StreamReader(fullPath);
            while ((eventCoords = reader.ReadLine()) != null)
            {
                totalString += eventCoords + "\n";
            }
            reader.Close();
            File.Delete(file);
        }
        foreach (string file in metaFiles)
        {
            File.Delete(file);
        }
        File.WriteAllText(m_path + SceneManager.GetActiveScene().name + "Map_Combined.txt", totalString);
        AssetDatabase.ImportAsset(m_path + SceneManager.GetActiveScene().name + "Map_Combined.txt");
        AssetDatabase.Refresh();
    }


    public static Vector3 stringToVec(string _st)
    {
        Vector3 result = new Vector3();
        _st = _st.Replace("(", string.Empty);
        _st = _st.Replace(")", string.Empty);
        string[] vals = _st.Split(',');
        if (vals.Length == 3)
        {
            result.Set(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
        }
        return result;
    }
    public static Color stringToCol(string _st)
    {
        Color result = Color.magenta;
        _st = _st.Replace("(", string.Empty);
        _st = _st.Replace(")", string.Empty);
        string[] vals = _st.Split(',');
        if (vals.Length == 4)
        {
            result = new Color(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[3]));
        }
        return result;
    }
    private static void ClearMaterials()
    {
        foreach(KeyValuePair<string,Material> kvp in m_Materials)
        {
            DestroyImmediate(kvp.Value);
        }
        m_Materials.Clear();
    }
    public static void RenderEventData()
    {
        ClearMaterials();
        for (int i=0; i < m_EventPositions.Count; i++)
        {
            Color c = m_EventColors[i];
            string hex = ColorUtility.ToHtmlStringRGBA(c);

            if (!m_Materials.ContainsKey(hex))
            {
                Material m = new Material(m_HeatmapMaterial);
                m.SetColor("_Color", c);
                m_Materials.Add(hex, m);
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer r = go.GetComponent<Renderer>();
            r.material = m_Materials[hex];
            go.name = m_EventNames[i];
            go.transform.parent = m_Parent;
            go.transform.localPosition = m_EventPositions[i];
            go.isStatic = true;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            DestroyImmediate(go.GetComponent<Collider>());
        }
    }

    [MenuItem("Tools/Heatmap/Clear Heatmap Render", false, 20)]
    public static void ClearHeatmapObjects()
    {
        if (m_Parent == null)
            m_Parent = GameObject.Find("Heatmap")?.transform;

        if (m_Parent != null)
        {
            int c = m_Parent.childCount;
            for (int i = 0; i < c; i++)
            {
                GameObject.DestroyImmediate(m_Parent.GetChild(0).gameObject);
            }
        }
    }
    public static void SetHeatmapDisplay(bool showHeatmap)
    {
        if (m_Parent == null)
            m_Parent = GameObject.Find("Heatmap")?.transform;
        if (m_Parent != null)
        {
            m_Parent.gameObject.SetActive(showHeatmap);
        }
        else
        {
            Debug.LogWarning("Couldn't find an object named Heatmap.");
        }
    }

    [MenuItem("Tools/Heatmap/Show \u2215 Hide Heatmap", false, 30)]
    public static void ToggleHeatmapDisplay()
    {
        if (m_Parent == null)
            m_Parent = GameObject.Find("Heatmap")?.transform;
        if (m_Parent != null)
        {
            m_Parent.gameObject.SetActive(!m_Parent.gameObject.activeSelf);
        }
        else
        {
            Debug.LogWarning("Couldn't find an object named Heatmap.");
        }
    }
}
