using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// Build assetbundles
    /// 注意：
    /// 1. 保存assetbundles的路径要设成相对路径，不要写成绝对路径，比如
    ///     string assetBundleCacheDirectory = "AssetBundles/"; // 这里目录AssetBundles是与Assets同级的
    /// 2. 要打包的文件目录也要设成相对路径
    ///     private static string assetRootDirectory = "Assets/Project/Resources";  // 
    /// </summary>
    public class BuildAssetBundles
    {
        public class AssetBundleFile
        {
            public string assetPath;
            public string assetBundleName;
            public string baseAssetPath;
            public string guid;
            public bool fix;
            public bool auto;
            /// <summary>このアセットに依存してるアセットの数</summary>
            public int dependencyCount;
        }

        // AssetBundleビルド中のエラー出力判定用
        static string error = null;

        [MenuItem("Framework/AssetBundle/Build AssetBundle")]
        static void BuildAllAssetBundles()
        {
            // AssetBundle化対象ファイルリストを生成
            var allAssetFiles = GetAssetBundleFiles();

            // AssetBundle化対象シーンリストを生成
            //GetSceneList(buildFiles, allAssetList);
            var builds = CreateBuildList(allAssetFiles);

            // variantリスト生成
            //var variants = CreateVariantInfoList(allAssetList, isBuildVariants);

            // 通常版ビルド
            BuildTarget target = BuildTarget.StandaloneWindows;
#if UNITY_EDITOR_OSX
            target = BuildTarget.StandaloneOSX;
#endif
            BuildAllWithVariant(AssetBundleDefine.assetBundleCacheDirectory, builds, null, target);

            // 一時退避用ディレクトリ作成
            //Directory.CreateDirectory(tempDirectoryPath);

            AssetDatabase.Refresh();

            using (new DebugTimer("FinalizeAssets"))
            {
                FinalizeAssets(target, true);
            }
        }

        private static Dictionary<string, AssetBundleFile> GetAssetBundleFiles()
        {
            using (var timer = new DebugTimer("GetAssetBundleFiles"))
            {
                Dictionary<string, AssetBundleFile> assetList = new Dictionary<string, AssetBundleFile>();

                string sceneDirectory = AssetBundleDefine.assetRootDirectory + "Scene";

                // 1ファイル＝1AssetBundle
                var files = Directory.GetFiles(AssetBundleDefine.assetRootDirectory, "*.*", SearchOption.AllDirectories);
                List<KeyValuePair<string, string>> allDependencies = new List<KeyValuePair<string, string>>();
                foreach (var f in files)
                {
                    var path = f.Replace("\\", "/");

                    if (!IsValidFile(path))
                    {
                        continue;
                    }

                    //if (path.StartsWith(sceneDirectory))
                    //{
                    //    continue;
                    //}

                    if (path.EndsWith(".unity"))
                    {
                        continue;
                    }

                    AddAssetBundleFile(path, "", assetList, AssetBundleDefine.assetRootDirectory, sceneDirectory);

                    var dependencies = AssetDatabase.GetDependencies(path);
                    foreach (var d in dependencies)
                    {
                        // 自身は含まない
                        if (d == path) continue;
                        // スクリプトはAssetBundleに含まれない
                        var extension = Path.GetExtension(d);
                        if (extension != ".cs" && extension != ".js")// && filterFunction(d))
                        {
                            allDependencies.Add(new KeyValuePair<string, string>(d, path));
                        }
                    }
                }

                foreach (var dependency in allDependencies)
                {
                    AddAssetBundleFile(dependency.Key, dependency.Value, assetList, AssetBundleDefine.assetRootDirectory, sceneDirectory);
                }

                return assetList;
            }
        }

        /// <summary>ビルド対象ファイルか判定</summary>
        private static bool IsValidFile(string path)
        {
            // ドットで始まるファイルは無視する
            if (Path.GetFileName(path).StartsWith("."))
            {
                return false;
            }

            // ドットで始まるフォルダに含まれていたら無視する
            if (IsDotStartDirectory(path))
            {
                return false;
            }

            if (path.EndsWith(".meta"))
            {
                return false;
            }

#if UNITY_EDITOR_WIN
            if (path.Equals("Thumbs.db"))
            {
                return false;
            }
#endif

            return true;
        }
        /// <summary>ドットで始まるフォルダが含まれているか判定</summary>
        private static bool IsDotStartDirectory(string path)
        {
            var directories = path.Split('/');
            foreach (var d in directories)
            {
                if (d.StartsWith("."))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AddAssetBundleFile(string path, string parentPath, Dictionary<string, AssetBundleFile> assetList, string rootDirectory, string sceneDirectory)
        {
            var pathExtension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(pathExtension))
            {
                if (Directory.Exists(path))
                {
                    // ディレクトリに依存関係が持てるのでExceptionにはしない
                    return;
                }
                else
                {
                    throw new Exception("拡張子がないファイルはビルドできません:" + path);
                }
            }
            if (path.Replace(pathExtension, "").EndsWith(" "))
            {
                throw new Exception("ファイル名の末尾に空白が含まれています:" + path);
            }

            // Scene内はGetSceneListで処理済みなのでここでは無視する
            //if (path.Contains(sceneDirectory))
            //{
            //    return;
            //}

            //BuildPipelineで失敗するため無視
            if (pathExtension == ".mask" || pathExtension == ".giparams")
            {
                return;
            }

            AssetBundleFile asset = null;
            if (!assetList.TryGetValue(path, out asset))
            {
                asset = new AssetBundleFile() { assetPath = path, baseAssetPath = path, guid = AssetDatabase.AssetPathToGUID(path) };

                // shadervariantsは強制的にshaderに含める
                if (pathExtension == ".shadervariants")
                {
                    asset.assetBundleName = "shader";
                    asset.auto = false;
                    assetList.Add(path, asset);
                    return;
                }

                // まだ名前設定されていないので、とりあえず親と同じAssetBundle
                if (string.IsNullOrEmpty(parentPath))
                {
                    var assetBundleName = path.Replace(Path.GetExtension(path), "").Replace(rootDirectory, "");
                    asset.assetBundleName = assetBundleName;
                    //if (customFunction == null)
                    //{
                    //    asset.assetBundleName = assetBundleName;
                    //}
                    //else
                    //{
                    //    assetBundleName = customFunction(path, assetBundleName);
                    //    if (string.IsNullOrEmpty(assetBundleName))
                    //    {
                    //        return;
                    //    }
                    //    else
                    //    {
                    //        asset.assetBundleName = assetBundleName;
                    //    }
                    //}
                    // 元々AssetBundle化対象のため、以降のプロセスで名前を変えない
                    asset.fix = true;
                    asset.auto = false;
                }
                else
                {
                    AssetBundleFile parentAssetBundleName;
                    if (!assetList.TryGetValue(parentPath, out parentAssetBundleName))
                    {
                        // 親の名前が登録されてないのでAssetBundle化対象外
                        return;
                    }
                    asset.assetBundleName = parentAssetBundleName.assetBundleName;
                    asset.auto = true;
                    if (parentPath.EndsWith(".spriteatlas"))
                    {
                        asset.fix = true;
                    }
                }

                // パッキングタグのあるスプライトはパッキング名のアセットバンドルに格納される
                // どこからも依存していないパッキングするスプライトがまとめられないのでここで指定する
                //if (IsSetSpritePackingTag(path, ref asset.assetBundleName))
                //{
                //    asset.auto = false;
                //}

                assetList.Add(path, asset);
                return;
            }

            // 非参照カウントアップ
            asset.dependencyCount++;

            if (asset.fix)
            {
                return;
            }

            if (parentPath.EndsWith(".spriteatlas"))
            {
                AssetBundleFile parentAssetBundleName = assetList[parentPath];
                asset.assetBundleName = parentAssetBundleName.assetBundleName;
                asset.auto = true;
                asset.fix = true;
                return;
            }

            // 明示的にAssetBundleに含める
            asset.auto = false;

            if (pathExtension == ".shader" || pathExtension == ".shadervariants")
            {
                // shaderは全て1AssetBundleにまとめる
                asset.assetBundleName = "shader";
                return;
            }

            var parentPathExtension = Path.GetExtension(parentPath);

            // AnimatorControllerが参照しているAnimationClipもAssetBundle化する
            if (pathExtension == ".anim" && (parentPathExtension == ".controller" || parentPathExtension == ".overrideController"))
            {
                // 他のAssetからも参照されてるので、ディレクトリ毎にAssetBundle化
                asset.assetBundleName = Path.GetDirectoryName(path);
                return;
            }

            // 複数Assetで共有されてるマテリアルは別AssetBundleにする
            if (pathExtension == ".mat")
            {
                // 他のAssetからも参照されてるので、マテリアル毎にAssetBundle化
                asset.assetBundleName = path.Replace(pathExtension, "").Replace(rootDirectory, "");

                // テクスチャもマテリアルと同じAssetBundleに入れる
                var materialDependencies = AssetDatabase.GetDependencies(path);
                foreach (var texture in materialDependencies.Where(d => !d.EndsWith(".shader")))
                {
                    AssetBundleFile textureAsset = null;
                    if (!assetList.TryGetValue(texture, out textureAsset))
                    {
                        textureAsset = new AssetBundleFile() { assetPath = texture, baseAssetPath = texture, guid = AssetDatabase.AssetPathToGUID(texture), auto = false };
                        assetList.Add(texture, textureAsset);
                    }
                    // 複数マテリアルから参照されてる場合に問題になるが、マテリアルが1個余計に読まれるだけなので無視して大丈夫そう
                    textureAsset.assetBundleName = asset.assetBundleName;
                }
                return;
            }

            //if (IsSetSpritePackingTag(path, ref asset.assetBundleName))
            //{
            //    return;
            //}

            // その他のAsset
            asset.assetBundleName = path.Replace(Path.GetExtension(path), "").Replace(rootDirectory, "");
        }

        private static List<AssetBundleBuild> CreateBuildList(Dictionary<string, AssetBundleFile> assetList)
        {
            using (new DebugTimer("CreateBuildList"))
            {
                var builds = new List<AssetBundleBuild>();

                var list = assetList.Values.Where(a => a.assetBundleName != null && !a.auto)
                    .OrderByDescending(name => name.dependencyCount)
                    .ToList();

                foreach (var asset in list)
                {
                    var index = builds.FindIndex(b => b.assetBundleName == asset.assetBundleName + AssetBundleDefine.assetBundleExtension);
                    if (index < 0)
                    {
                        builds.Add(new AssetBundleBuild()
                        {
                            assetBundleName = asset.assetBundleName + AssetBundleDefine.assetBundleExtension,
                            assetBundleVariant = "",
                            assetNames = new string[] { asset.baseAssetPath },
                        });
                    }
                    else
                    {
                        var newBuilds = builds[index];
                        var tmpList = new List<string>(newBuilds.assetNames);
                        tmpList.Add(asset.baseAssetPath);
                        newBuilds.assetNames = tmpList.ToArray();
                        builds[index] = newBuilds;
                    }
                }

                return builds;
            }
        }

        /// <summary>variant設定ごとにビルドする</summary>
        private static void BuildAllWithVariant(string buildDirectory, List<AssetBundleBuild> builds, string variant, BuildTarget target)
        {
            string buildPath;
            string sceneBuildPath; // 旧バージョンのフォルダが残るので削除用

            if (variant != null)
            {
                buildPath = buildDirectory + "/build." + variant + "/";
                sceneBuildPath = buildDirectory + "/scene_build." + variant + "/";
            }
            else
            {
                buildPath = buildDirectory + "/build/";
                sceneBuildPath = buildDirectory + "/scene_build/";
            }

            // シェーダーを再読込
            var shader = builds.Where(a => a.assetBundleName == "shader" + AssetBundleDefine.assetBundleExtension).FirstOrDefault();
            if (!string.IsNullOrEmpty(shader.assetBundleName))
            {
                foreach (var build in shader.assetNames)
                {
                    AssetDatabase.ImportAsset(build);
                }
            }

            // cgincだけ更新した際にAssetBundleがビルドされない対策
            DeleteFileIfExists(buildPath + "shader" + AssetBundleDefine.assetBundleExtension + ".manifest");

            // 旧バージョンフォルダを削除
            if (Directory.Exists(sceneBuildPath))
            {
                DeleteDirectory(sceneBuildPath);
            }

            ExecuteBuildAssetBundles(buildPath, builds, target);

            // BuildPipeline.BuildAssetBundles()のコール間隔を100ms以上開ける必要がある。
            System.Threading.Thread.Sleep(100);
        }

        /// <summary>ビルド対象ごとにAssetBundle作成</summary>
        private static void ExecuteBuildAssetBundles(string buildPath, List<AssetBundleBuild> builds, BuildTarget target)
        {
            if (builds.Count == 0)
            {
                return;
            }

            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            // 使わなくなった前回のビルドファイルが邪魔でビルドに失敗することがあるので削除
            //DeleteUnusedFiles(buildPath, builds);

            // 空のディレクトリを削除してビルド成功率をあげる
            //DeleteEmptyDirectory(buildPath);

            BuildPipeline.BuildAssetBundles(buildPath, builds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle, target);

            //if (error != null)
            //{
            //    throw new Exception(error);
            //}
        }

        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void DeleteDirectory(string directory)
        {
            //if (Directory.Exists(directory))
            //{
            //    Directory.Delete(directory);
            //}
            DirectoryInfo di = new DirectoryInfo(directory);

            RemoveReadonlyAttribute(di);

            di.Delete(true);
        }

        private static void RemoveReadonlyAttribute(DirectoryInfo directoryInfo)
        {
            if ((directoryInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                directoryInfo.Attributes = FileAttributes.Normal;
            }
            foreach (FileInfo fi in directoryInfo.GetFiles())
            {
                if ((fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    fi.Attributes = FileAttributes.Normal;
                }
            }
            foreach (DirectoryInfo di in directoryInfo.GetDirectories())
            {
                RemoveReadonlyAttribute(di);
            }
        }


        /// <summary>
        /// カテゴリビルドされたファイルを移動＆統合マニフェスト作成
        /// RawAssetのコピーもここで行う
        /// </summary>
        private static void FinalizeAssets(BuildTarget target, bool isBuildVariants)
        {
            string outputPath = AssetBundleDefine.defaultAssetBundleOutputPath;// ProjectPrefs.GetString(AssetBundleOutputPath, DefaultAssetBundleOutputPath);
            string scriptableObjectManifestPath = AssetDatabase.GenerateUniqueAssetPath("Assets/" + AssetBundleDefine.assetbundleManifestName);
            string outputManifestDirectory = AssetBundleDefine.defaultManifestOutputPath;// ProjectPrefs.GetString(AssetBundleManifestOutputPath, DefaultManifestOutputPath);

            // 変更がないファイルは残したほうが早いかもしれないが、大した差はなさそうなので毎回クリア
            if (Directory.Exists(outputPath))
            {
                DeleteDirectory(outputPath);
            }
            Directory.CreateDirectory(outputPath);
            if (Directory.Exists(outputManifestDirectory))
            {
                DeleteDirectory(outputManifestDirectory);
            }
            Directory.CreateDirectory(outputManifestDirectory);

            // RawAssetのvariantリスト
            var variants = GetRawAssetVariants(isBuildVariants);

            // ビルド済みのvariantディレクトリをリストアップ
            var categoryDirectories = Directory.GetDirectories(AssetBundleDefine.assetBundleCacheDirectory);
            foreach (var categoryDirectory in categoryDirectories)
            {
                var variantDirectories = Directory.GetDirectories(categoryDirectory);
                foreach (var d in variantDirectories)
                {
                    // ファイル名ではないがちょうどいいので
                    var directory = Path.GetFileName(d);
                    if (directory.IndexOf(".") < 0)
                    {
                        variants.Add("");
                    }
                    else if (isBuildVariants)
                    {
                        variants.Add(directory.Split('.')[1]);
                    }
                }
            }

            HashSet<string> basePaths = null;
            foreach (var variant in variants)
            {
                string outputManifestPath;
                string rawAssetRootDirectory;

                if (variant != "")
                {
                    //outputManifestPath = outputManifestDirectory + "/" + AssetDownloader.ManifestName + "." + variant + AssetDownloader.ManifestExtension;
                    outputManifestPath = outputManifestDirectory + "/" + Path.GetFileNameWithoutExtension(AssetBundleDefine.assetbundleManifestName) + "." + variant + Path.GetExtension(AssetBundleDefine.assetbundleManifestName);
                    rawAssetRootDirectory = AssetBundleDefine.rawAssetVariantDirectory + variant + "/";// ProjectPrefs.GetString(RawAssetManager.RawAssetVariantDirectory) + variant + "/";
                }
                else
                {
                    outputManifestPath = outputManifestDirectory + "/" + AssetBundleDefine.assetbundleManifestName;
                    rawAssetRootDirectory = AssetBundleDefine.rawAssetVariantDirectory;// ProjectPrefs.GetString(RawAssetManager.RawAssetRootDirectory);
                }

                foreach (var categoryDirectory in categoryDirectories)
                {
                    var buildPath = categoryDirectory;// + "/build";
                    //var sceneBuildPath = categoryDirectory;// + "/scene_build";
                    if (variant != "")
                    {
                        buildPath += "." + variant;
                        //sceneBuildPath += "." + variant;
                    }
                    var category = Path.GetFileName(categoryDirectory);

                    CreateManifestAndCopyAssetBundle(category, buildPath, outputPath, scriptableObjectManifestPath);
                    //CreateManifestAndCopyAssetBundle(category, sceneBuildPath, outputPath, scriptableObjectManifestPath);
                }

                // RawAssetを出力
                //if (Directory.Exists(rawAssetRootDirectory))
                {
                    //var scriptableObjectManifest = CreateManifestAndRawAssetCopy(rawAssetRootDirectory, outputPath, scriptableObjectManifestPath, basePaths);

                    //if (variant == "")
                    //{
                    //    basePaths = new HashSet<string>();
                    //    // variantなしRawAssetsマニフェスト取得
                    //    foreach (var r in scriptableObjectManifest.rawAssets)
                    //    {
                    //        basePaths.Add(r.name);
                    //    }
                    //}
                }

                CreateManifestAssetBundle(scriptableObjectManifestPath, outputManifestPath, target);

                //// チェック用のマニフェストcsvも出力
                //ManifestToCsv.OutputCsvFromManifest(outputManifestPath, outputManifestPath + ".csv");

                //// マニフェストをjson化
                ManifestToCsv.OutputJsonFromManifest(outputManifestPath, outputManifestPath + ".txt");
            }
        }

        public static void CreateManifestAssetBundle(string scriptableObjectManifestPath, string outputManifestPath, BuildTarget target)
        {
            // マニフェストのAssetBundle化
            AssetBundleBuild[] manifestBuild = new AssetBundleBuild[] { new AssetBundleBuild() { assetBundleName = AssetBundleDefine.assetbundleManifestName, assetNames = new string[] { scriptableObjectManifestPath } } };
            string manifestBuildPath = "manifestbuild";
            if (!Directory.Exists(manifestBuildPath))
            {
                Directory.CreateDirectory(manifestBuildPath);
            }

            BuildPipeline.BuildAssetBundles(manifestBuildPath, manifestBuild, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle, target);
            AssetDatabase.DeleteAsset(scriptableObjectManifestPath);

            if (error != null)
            {
                throw new Exception(error);
            }

            // マニフェストを暗号化
            var bytes = File.ReadAllBytes(manifestBuildPath + "/" + AssetBundleDefine.assetbundleManifestName);
            //var encrypted = GetEncryptionFunction()(bytes);
            // 暗号化したマニフェストを出力
            //AssetBundleCreater.SaveManifestWithHash(encrypted, outputManifestPath);
            AssetBundleCreator.SaveManifestWithHash(bytes, outputManifestPath);
        }

        private static UnityEngine.AssetBundleManifest GetAssetBundleManifest(string manifestPath)
        {
            var assetbundle = UnityEngine.AssetBundle.LoadFromFile(manifestPath);
            var manifest = assetbundle.LoadAllAssets()[0] as UnityEngine.AssetBundleManifest;
            assetbundle.Unload(false);
            return manifest;
        }

        private static HashSet<string> GetRawAssetVariants(bool isBuildVariants)
        {
            using (new DebugTimer("GetRawAssetVariants"))
            {
                string rawAssetVaiantDirectory = AssetBundleDefine.rawAssetVariantDirectory;// ProjectPrefs.GetString(RawAssetManager.RawAssetVariantDirectory);

                HashSet<string> variants = new HashSet<string>();
                // RawAssetRootDirectory用
                variants.Add("");

                if (!isBuildVariants || string.IsNullOrEmpty(rawAssetVaiantDirectory))
                {
                    return variants;
                }

                var variantsDirectories = Directory.GetDirectories(rawAssetVaiantDirectory);
                foreach (var d in variantsDirectories)
                {
                    var variantDirectory = d.Replace("\\", "/");

                    string variantName = variantDirectory.Replace(rawAssetVaiantDirectory, "").Replace("/", "");

                    variants.Add(variantName);
                }

                return variants;
            }
        }

        /// <summary>マニフェスト生成＆AssetBundleをリネームしつつコピー</summary>
        private static void CreateManifestAndCopyAssetBundle(string categoryName, string buildPath, string outputPath, string scriptableObjectManifestPath)
        {
            string manifestPath = buildPath + "/" + Path.GetFileName(buildPath);
            if (!File.Exists(manifestPath))
            {
                return;
            }

            var manifest = GetAssetBundleManifest(manifestPath);
            var scriptableObjectManifest = GetScriptableManifest(scriptableObjectManifestPath);

            //var isEncryptionFunction = GetEncryptionCheckFunction();
            //var encryptionFunction = GetEncryptionFunction();
            //var groupFunction = GetGroupFunction();

            //AssetDatabase.StartAssetEditing();

            var category = scriptableObjectManifest.GetCategory(categoryName);

            List<ScriptableManifest.Asset> assetList = new List<ScriptableManifest.Asset>(category.assets);
            List<ScriptableManifest.Asset> cryptedAssetList = new List<ScriptableManifest.Asset>(category.encryptedAssets);

            var assetBundleNames = manifest.GetAllAssetBundles();

            foreach (var assetBundleName in assetBundleNames)
            {
                ScriptableManifest.Asset asset = new ScriptableManifest.Asset();

                var path = buildPath + "/" + assetBundleName;

                var assetName = assetBundleName.ToLower();

                asset.name = assetName.Substring(0, assetName.Length - AssetBundleDefine.assetBundleExtension.Length);
                var fileInfo = new FileInfo(path);
                asset.size = (int)fileInfo.Length;
                //asset.group = asset.name;// groupFunction(asset.name);

                // 暗号化するかどうか
                bool isEncryption = false;// IsEncryption(assetBundleName, isEncryptionFunction);

                if (isEncryption)
                {
                    //var bytes = File.ReadAllBytes(path);
                    //var encrypted = encryptionFunction(bytes);
                    //asset.hash = Hash.ComputeSha256Hash(encrypted, Hash.EncodeType.Base32NoPadding);
                    //string directory = outputPath + "/" + AssetDownloader.GetSubDirectoryPath(asset.hash) + "/";
                    //Directory.CreateDirectory(directory);
                    //string dst = directory + asset.hash;
                    //if (!File.Exists(dst))
                    //{
                    //    File.WriteAllBytes(dst, encrypted);
                    //}

                    //cryptedAssetList.Add(asset);
                }
                else
                {
                    //asset.hash = Hash.ComputeSha256HashOfFile(path, Hash.EncodeType.Base32NoPadding);
                    // use assetBundle's own hash128.
                    asset.hash = manifest.GetAssetBundleHash(assetBundleName).ToString();
                    //string directory = outputPath + "/" + AssetDownloader.GetSubDirectoryPath(asset.hash) + "/";
                    //Directory.CreateDirectory(directory);
                    //string dst = directory + asset.hash;
                    //if (!File.Exists(dst))
                    //{
                    //    File.Copy(path, dst, true);
                    //}
                    // copy exactly the same directory category.
                    string dst = outputPath + "/" + assetName;
                    string directory = Path.GetDirectoryName(dst);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    if (!File.Exists(dst))
                    {
                        File.Copy(path, dst, true);
                    }
                    assetList.Add(asset);
                }

                if (path.Contains(buildPath + "atlas"))
                {
                    // Atlas化されたSpriteはひとまとめに扱う
                }
                else
                {
                    asset.dependencies = manifest.GetDirectDependencies(assetName)
                        .Select(d => d.Substring(0, d.Length - AssetBundleDefine.assetBundleExtension.Length))
                        .ToArray();
                }
            }

            category.assets = assetList.ToArray();
            category.encryptedAssets = cryptedAssetList.ToArray();

            //AssetDatabase.StopAssetEditing();

            EditorUtility.SetDirty(scriptableObjectManifest);
        }

        /// <summary>
        /// ScriptableObjectManifestを取得する
        /// BuildAssetBundleをすると参照が切れるので、都度取得すること
        /// </summary>
        private static ScriptableManifest GetScriptableManifest(string manifestPath)
        {
            var scriptableObjectManifest = AssetDatabase.LoadAssetAtPath<ScriptableManifest>(manifestPath);
            if (scriptableObjectManifest == null)
            {
                scriptableObjectManifest = ScriptableObject.CreateInstance<ScriptableManifest>();
                scriptableObjectManifest.categories = new ScriptableManifest.Category[0];
                AssetDatabase.CreateAsset(scriptableObjectManifest, manifestPath);
            }
            return scriptableObjectManifest;
        }
    }

}
