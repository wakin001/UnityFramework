using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Stage
{
    /// <summary>
    /// 
    /// </summary>
    public class ExportAIScriptToScriptObject
    {
        const string scriptDirectoryPath = "Assets/Project/AIScriptWorks/";
        const string outputDirectoryPath = "Assets/Project/Resources/AIScripts/";

        [MenuItem("Assets/ExportAIScriptToScriptObject")]
        public static void Export()
        {
            foreach (Object obj in Selection.objects)
            {
                ExportAIScriptToScriptObjectWithObj(obj);
            }
        }

        public static void ExportAIScriptToScriptObjectWithObj(Object obj)
        {
            string activeObjectPath = AssetDatabase.GetAssetPath(obj);
            FileAttributes attr = File.GetAttributes(activeObjectPath);

            //detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {   // is directory.
                ExportAIScriptToScriptObjectWithFolder(activeObjectPath);
            }
            else
            {
                ExportAIScriptToScriptObjectWithFile(activeObjectPath, Path.GetDirectoryName(activeObjectPath));
            }
        }

        public static void ExportAIScriptToScriptObjectWithFolder(string folderPath)
        {
            string[] directories = Directory.GetDirectories(folderPath);
            foreach (string direcotryPath in directories)
            {
                string[] filePaths = Directory.GetFiles(direcotryPath);
                foreach (string filePath in filePaths)
                {
                    ExportAIScriptToScriptObjectWithFile(filePath, direcotryPath);
                }

                string[] subDirectories = Directory.GetDirectories(direcotryPath);
                foreach (string subFolder in subDirectories)
                {
                    ExportAIScriptToScriptObjectWithFolder(subFolder);
                }
            }

            string[] paths = Directory.GetFiles(folderPath);
            foreach (string filePath in paths)
            {
                ExportAIScriptToScriptObjectWithFile(filePath, folderPath);
            }
        }

        public static void ExportAIScriptToScriptObjectWithFile(string filePath, string direcotryPath)
        {
        }
    }
}


