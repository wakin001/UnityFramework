using UnityEngine;
using System.Collections;
using UnityEditor;
//using CsvHelper;
using System.IO;
using System;
using System.Reflection;

namespace Framework
{
    /// <summary>manifestをcsvに変換する</summary>
    public class ManifestToCsv
    {
        //[MenuItem("Assets/Cute/AssetBundle/Output csv from manifest")]
        //public static void OutputCsvFromSelectedManifest()
        //{
        //    var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        //    OutputCsvFromManifest(path, "manifest.csv");
        //}

        //public static void OutputCsvFromManifest(string path, string outputPath)
        //{
        //    var manifest = ScriptableManifest.LoadManifest(path);
        //    if (manifest == null)
        //    {
        //        return;
        //    }

        //    using (var stream = new StreamWriter(outputPath, false, new System.Text.UTF8Encoding(false)))
        //    using (var writer = new CsvWriter(stream))
        //    {
        //        foreach (var category in manifest.categories)
        //        {
        //            foreach (var asset in category.assets)
        //            {
        //                OutputAsset(writer, asset);
        //            }
        //            foreach (var asset in category.encryptedAssets)
        //            {
        //                OutputAsset(writer, asset);
        //            }
        //        }

        //        foreach (var r in manifest.rawAssets)
        //        {
        //            writer.WriteField(r.name);
        //            writer.WriteField(r.hash);
        //            writer.WriteField(r.size);
        //            writer.WriteField(r.group);
        //            writer.NextRecord();
        //        }
        //    }
        //}

        //private static void OutputAsset(CsvWriter writer, ScriptableManifest.Asset asset)
        //{
        //    writer.WriteField(asset.name);
        //    writer.WriteField(asset.hash);
        //    writer.WriteField(asset.size);
        //    writer.WriteField(asset.group);
        //    foreach (var d in asset.dependencies)
        //    {
        //        writer.WriteField(d);
        //    }
        //    writer.NextRecord();
        //}

        [MenuItem("Assets/Cute/AssetBundle/Output json from manifest")]
        public static void OutputJsonFromSelectedManifest()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            OutputJsonFromManifest(path, "manifest.txt");
        }

        public static void OutputJsonFromManifest(string path, string outputPath)
        {
            var manifest = ScriptableManifest.LoadManifest(path);
            if (manifest == null)
            {
                return;
            }

            var json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(outputPath, json);
        }

        [MenuItem("Assets/Cute/AssetBundle/Create manifest from json")]
        public static void CreateManifestFromJson()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            CreateManifestFromJson(path, "assetbundle.manifest");
        }

        public static void CreateManifestFromJson(string path, string outputManifestPath)
        {
            var json = File.ReadAllText(path);
            var manifest = ScriptableObject.CreateInstance<ScriptableManifest>();
            JsonUtility.FromJsonOverwrite(json, manifest);

            string scriptableObjectManifestPath = "Assets/scriptableObjectManifest.asset";
            AssetDatabase.CreateAsset(manifest, scriptableObjectManifestPath);

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildAssetBundles.CreateManifestAssetBundle(scriptableObjectManifestPath, outputManifestPath, target);

            AssetDatabase.DeleteAsset(scriptableObjectManifestPath);
        }
    }
}