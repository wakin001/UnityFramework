using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public class AssetBundleDefine
    {
#if UNITY_EDITOR_WIN
        // Where to save the asset bundles
        public const string defaultAssetBundleOutputPath = "C:/nginx-1.16.0/html/AssetBundles";
        public const string defaultManifestOutputPath = "C:/nginx-1.16.0/html/manifests";
#elif UNITY_EDITOR_OSX
        public const string defaultAssetBundleOutputPath = "/Users/fongwang/Documents/Workspace/UnityFramework/LocalServer/AssetBundles";
        public const string defaultManifestOutputPath = "/Users/fongwang/Documents/Workspace/UnityFramework/LocalServer/Manifests";
#else
        public const string defaultAssetBundleOutputPath = "assetbundles";
        public const string defaultManifestOutputPath = "manifests";
#endif
        // ディレクトリ名とAssetBundle名が同じだとエラーになるので拡張子をつける
        public const string assetBundleExtension = "";//".a";

        // The root path of all assets.
        public static string assetRootDirectory = "Assets/Project/Resources/";

        // the cache path of assetbundles.
        public static string assetBundleCacheDirectory = "AssetBundles/";
        // Customized manifest file name.
        public const string assetbundleManifestName = "manifest.asset";

        public const string rawAssetVariantDirectory = "";

        public const string sceneAssetNamePrefix = "s/";
    }
}
