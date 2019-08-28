using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Framework
{
    public class ScriptableManifest : ScriptableObject
    {
        [System.Serializable]
        public class Asset : RawAsset
        {
            public string[] dependencies;
        }

        [System.Serializable]
        public class RawAsset
        {
            public string name;
            public string hash;
            public int size;
            public int group;
        }

        [System.Serializable]
        public class Category
        {
            public string name;
            public Asset[] assets;
            public Asset[] encryptedAssets;
        }

        public Category[] categories;
        public RawAsset[] rawAssets;

        private Dictionary<string, RawAsset> _assetsDic = new Dictionary<string, RawAsset>();
        public Dictionary<string, RawAsset> assetsDic
        {
            get
            {
                if (_assetsDic.Count == 0)
                {
                    // Initialize
                    for (int i = 0; i < categories.Length; ++i)
                    {
                        var category = categories[i];
                        // assets.
                        foreach (var asset in category.assets)
                        {
                            _assetsDic.Add(asset.name, asset);
                        }
                        // encrpted assets.
                        foreach (var asset in category.encryptedAssets)
                        {
                            _assetsDic.Add(asset.name, asset);
                        }
                    }

                    // download rawAssets.
                    foreach (var asset in rawAssets)
                    {
                        _assetsDic.Add(asset.name, asset);
                    }
                }
                return _assetsDic;
            }
        }


        public void CopyFrom(ScriptableManifest src)
        {
            categories = src.categories;
            rawAssets = src.rawAssets;
        }

        public static ScriptableManifest LoadManifest(string manifestSavePath)
        {
            try
            {
                // manifestは暗号化されている
                var assetBundle = AssetBundleCreator.LoadManifest(manifestSavePath);

                ScriptableManifest manifest = assetBundle.LoadAllAssets()[0] as ScriptableManifest;

                assetBundle.Unload(false); // 必要？

                return manifest;
            }
            catch
            {
                // ファイルが壊れてる
                File.Delete(manifestSavePath);
                return null;
            }
        }

        public static ScriptableManifest LoadManifest(byte[] bytes)
        {
            try
            {
                // manifestは暗号化されている
                var assetBundle = AssetBundleCreator.LoadManifest(bytes);

                ScriptableManifest manifest = assetBundle.LoadAllAssets()[0] as ScriptableManifest;

                assetBundle.Unload(false); // 必要？

                return manifest;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the hash of asset.
        /// </summary>
        public string GetHash(string assetName)
        {
            RawAsset asset = null;
            if (assetsDic.TryGetValue(assetName, out asset))
            {
                return asset.hash;
            }
            return "";
        }

        /// <summary>
        /// Find all dependencies of the asset.
        /// </summary>
        public HashSet<string> GetAllDependencies(string assetName, HashSet<string> dependencies = null)
        {
            if (dependencies == null)
            {
                dependencies = new HashSet<string>();
            }
            RawAsset rawAsset = null;
            if (assetsDic.TryGetValue(assetName, out rawAsset))
            {
                Asset asset = rawAsset as Asset;
                if (asset != null)
                {
                    foreach (string dependence in asset.dependencies)
                    {
                        dependencies.Add(dependence);
                        // Find iteratively.
                        GetAllDependencies(dependence, dependencies);
                    }
                }
            }
            return dependencies;
        }

#if UNITY_EDITOR
        public Category GetCategory(string categoryName)
        {
            var category = Array.Find(categories, c => c.name == categoryName);
            if (category != null)
            {
                return category;
            }

            var tmpList = new List<Category>(categories);
            var newCategory = new Category() { name = categoryName, assets = new Asset[0], encryptedAssets = new Asset[0] };
            tmpList.Add(newCategory);
            categories = tmpList.ToArray();
            return newCategory;
        }
#endif
    }

}
