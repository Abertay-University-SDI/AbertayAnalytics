using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/*
 * this Heatmap tool was originally developed by: Garen O'Donnell
 * Date Created: 04/10/2019
 * Modified by: Hadi Mehrpouya
 * Modified more by: Gaz Robinson
 * Date last modified: 30/10/2023
 */
namespace Abertay.Analytics
{
    public static class DataRecorder
    {
        public static string m_path = "Assets/Resources/Heatmaps/";

        public static string Path
        {
            get
            {
#if !UNITY_EDITOR
                return Application.dataPath + "/../Analytics/Heatmaps/" + SceneManager.GetActiveScene().name + "/";
#else
                return "Assets/Resources/Heatmaps/" + SceneManager.GetActiveScene().name + "/";
#endif
            }
        }

        public static string FilePath
        {
            get
            {
                return Path + SceneManager.GetActiveScene().name + "Map.txt";
            }
        }

        /*
         * This function will open the file using the path variable
         * and then adds whatever the user is sending to the end of the file
         * If you don't specify a seperator for this function, it will automatically uses ;
         * if the user doesn'tspecity whether to add timestamp. by default this funciton will add it as the last column for each line or before the seperator.
         */
        public static bool RecordEventPosition(string eventName, Vector3 _pos, Color eventColor)
        {
            m_path = Path;

            string filePath = m_path + SceneManager.GetActiveScene().name + "Map";
            bool result = false;
            string lineToAdd = eventName + ":" + "(" + _pos.x + "," + _pos.y + "," + _pos.z + "):(" + eventColor.r + "," + eventColor.g + "," + eventColor.b + "," + eventColor.a + ")";

            if (!Directory.Exists(m_path))
            {
                Directory.CreateDirectory(m_path);
            }
            bool newFile = !File.Exists(filePath + ".txt");
            if (!newFile)
            {
                string currentContent = File.ReadAllText(filePath + ".txt");
                if (currentContent[0] != '#')
                    File.WriteAllText(filePath + ".txt", "#" + SceneManager.GetActiveScene().name + "\n" + currentContent);
            }
            using (StreamWriter sw = File.AppendText(filePath + ".txt")) //This line will try to open the file and if it doesn't exist, it will make it!
            {
                if (newFile)
                {
                    sw.WriteLine("#" + SceneManager.GetActiveScene().name);
                    newFile = false;
                }
                //Write death position vector to our text file as a new line
                sw.WriteLine(lineToAdd);
                sw.Close();
            }

            ////Print the text from the file
            result = true;//If we get to this part of our code, this means things went ok, so we return true. 
            return result;
        }
        /*public static bool RecordEventPosition2D(string eventName, Vector2 _pos, Color eventColor)
        {
            string filePath = m_path + SceneManager.GetActiveScene().name + ".txt";
            bool result = false;
            string lineToAdd = eventName + ":"+"(" + _pos.x + "," + _pos.y + "):(" + eventColor.r + "," + eventColor.g + "," + eventColor.b + "," + eventColor.a + ")";
            using (StreamWriter sw = File.AppendText(m_path + ".txt")) //This line will try to open the file and if it doesn't exist, if will make it!
            {
                //Write death position vector to our text file as a new line
                sw.WriteLine(lineToAdd);
                sw.Close();
            }
            ////Re-import the file to update the reference in the editor
            AssetDatabase.ImportAsset(filePath);
            TextAsset asset = Resources.Load<TextAsset>(filePath + ".txt");
            ////Print the text from the file
            result = true;//If we get to this part of our code, this means things went ok, so we return true. 
            return result;
        }*/

    }
}