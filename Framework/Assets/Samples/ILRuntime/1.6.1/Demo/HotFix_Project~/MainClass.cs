using System;
using System.Diagnostics;
using Framework;
using UnityEngine;

namespace HotFix_Project
{
    public class MainClass
    {
        public MainClass()
        {
            
        }

        public static void Launch()
        {
            // go to first view.
            View1 view1 = new View1("Prefabs/Views/View1");
            view1.Start();
        }

        public static void LoadResource()
        {
            //AssetManager.useAssetBundle = false;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            //var prefab = AssetManager.Load("Prefabs/prefab1");
            //if (prefab != null)
            //{
            //    GameObject.Instantiate(prefab);
            //}
            //stopWatch.Stop();
            //UnityEngine.Debug.Log("sync load resource, time: " + stopWatch.ElapsedMilliseconds);

            AssetManager.LoadAsync<UnityEngine.Object>("Prefabs/prefab1", (pref) =>
            {
                GameObject.Instantiate((GameObject)pref);
                stopWatch.Stop();
                UnityEngine.Debug.Log("sync load resource, time: " + stopWatch.ElapsedMilliseconds);
            });
        }
    }
}
