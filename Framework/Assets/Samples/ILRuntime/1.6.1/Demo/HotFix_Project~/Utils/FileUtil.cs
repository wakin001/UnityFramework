using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HotFix_Project
{
    public class FileUtil
    {
        public static void Save(string toPath, byte[] data)
        {
            //Create the Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(toPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(toPath));
            }

            try
            {
                File.WriteAllBytes(toPath, data);
                Debug.Log("Saved Data to: " + toPath.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Save Data to: " + toPath.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);
            }
        }
    }
}

