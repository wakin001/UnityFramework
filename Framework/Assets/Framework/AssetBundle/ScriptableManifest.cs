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
        public class Asset
        {
            public string name;
            public string hash;
            public string[] dependencies;
            public int size;
            public int group;
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

        public Hash128 GetHash(string assetName)
        {
            for (int i = 0; i < categories.Length; ++i)
            {
                var category = categories[i];
                // assets.
                foreach (var asset in category.assets)
                {
                    if (asset.name == assetName)
                    {
                        return Hash128.Parse(asset.hash);
                    }
                }
                // encrpted assets.
                foreach (var asset in category.encryptedAssets)
                {
                    if (asset.name == assetName)
                    {
                        return Hash128.Parse(asset.hash);
                    }
                }
            }

            // download rawAssets.
            foreach (var asset in rawAssets)
            {
                if (asset.name == assetName)
                {
                    return Hash128.Parse(asset.hash);
                }
            }

            return new Hash128();
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
