using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System;
#endif
namespace Framework
{
    /// <summary>
    /// AIScript管理クラス
    /// </summary>
    public class AIScriptLoader
#if UNITY_EDITOR
        : AssetPostprocessor
#endif
    {
        //const string scriptPath = AssetPath.aiScript + "/";
        const string scriptDirectoryPath = "Assets/Project/AIScriptWorks/";
        const string outputDirectoryPath = "Assets/Project/Resources/AIScripts/";

#if UNITY_EDITOR
        //EnemyAIScriptシンタックスチェック後ログ出力を行うかどうか
        private static int isOutputSyntaxLogFolder = -1;
#endif
        const string outputSyntaxLogName = "Gluon/Action/Enabled Output SyntaxLog";

        // 関数単位のscript構成
        Dictionary<string, AIScriptParser> _dict = new Dictionary<string, AIScriptParser>();

        /// <summary>
        /// AIスクリプトデータの取得
        /// 
        /// </summary>
        /// <param name="scriptFile">スクリプトファイル名</param>
        /// <returns></returns>
        public AIScriptParser Load(string scriptFile)
        {
            if (!_dict.ContainsKey(scriptFile))
            {
                AIScriptObject aiScript = null;
                //#if UNITY_EDITOR
                //                if (AssetLoader.useAssetBundle)
                //                {
                //#endif
                //                    //新規スクリプト
                //                    aiScript = AssetLoader.LoadAssetWithName<AIScriptObject>(AssetPath.aiScript, scriptPath + scriptFile);
                //#if UNITY_EDITOR
                //                }
                //                else
                //                {
                //                    aiScript = ExportScriptObject(scriptDirectoryPath + scriptFile + ".txt");
                //                }
                //#endif
#if UNITY_EDITOR
                AssetManager.useAssetBundle = false;
#endif
                aiScript = AssetManager.Load<AIScriptObject>(scriptFile);

                if (aiScript == null)
                {
                    Debug.Log("-----Error Non AIScript Load " + scriptFile);
                    return null;
                }

                //登録
                _dict.Add(scriptFile, new AIScriptParser(aiScript));
            }
            //読み込みずみファイルあり
            return _dict[scriptFile];
        }

        /// <summary>
        /// スクリプト解放
        /// </summary>
        void Clear()
        {
            _dict.Clear();
        }

#if UNITY_EDITOR
        /// <summary>
        /// スクリプトビルド
        /// 不要情報の削除などのコード整理
        /// </summary>
        [MenuItem("Framework/AI/ExportAIScript")]
        public static void ExportAIScript()
        {
            //MasterLoader.Load();

            var files = Directory.GetFiles(scriptDirectoryPath, "*.txt", SearchOption.AllDirectories);
            foreach (var path in files)
            {
                // AIScript Asset構築
                AIScriptObject instance = ExportScriptObject(path);

                //保存
                string resourcePath = outputDirectoryPath + path.Replace(scriptDirectoryPath, "").Replace(".txt", ".asset");
                AIScriptObject resource = AssetDatabase.LoadAssetAtPath<AIScriptObject>(resourcePath);
                if (resource != null)
                {
                    instance.name = "AIScriptObject";
                    EditorUtility.CopySerialized(instance, resource);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(resourcePath));
                    AssetDatabase.CreateAsset(instance, resourcePath);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>特定パスのテキストをAIScriptObjectに変換</summary>
        static AIScriptObject ExportScriptObject(string path)
        {
            var lines = File.ReadAllLines(path);
            lines = ExcludeTabAndComment(lines);

            // AIScript Asset構築
            AIScriptObject instance = AIScriptObject.CreateAssetInstance();
            instance.Parser(lines);

            return instance;
        }

        static string[] ExcludeTabAndComment(string[] lines)
        {
            return lines.Select(l => l.Replace("\t", "").Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToArray();
        }

        static void SyntaxCheck(string path)
        {
            //AIScriptWork以下のフォルダのmetaファイルまで削除すると、
            //ここに渡されてエラーになるため、渡された場合はスキップする。
            if (!path.EndsWith(".txt")) return;
            string[] lines = File.ReadAllLines(path);
            if (lines.Length <= 0) return;

            AIScriptSyntaxCheck syntaxChecker = new AIScriptSyntaxCheck();
            string consoleOutputLog = "";   //タグあり
            string fileOutputLog = "";      //タグなし
            bool isCheckResult = syntaxChecker.SyntaxCheck(lines);
            if (isCheckResult)
            {
                consoleOutputLog += "AIScript SyntaxCheck: <color=green>OK</color>\n" + path;
                fileOutputLog += "AIScript SyntaxCheck: OK\n" + path;
            }
            else
            {
                consoleOutputLog += "AIScript SyntaxCheck: <color=red>NG</color>\n" + path;
                fileOutputLog += "AIScript SyntaxCheck: NG\n" + path;
            }
            consoleOutputLog += "\n\n";
            fileOutputLog += "\n\n";

            foreach (var err in syntaxChecker.Errors)
            {
                consoleOutputLog += "<color=red><b>Error</b></color>: " + err;
                fileOutputLog += "Error: " + err;
            }
            foreach (var warning in syntaxChecker.Warnings)
            {
                consoleOutputLog += "<color=yellow><b>Warning</b></color>: " + warning;
                fileOutputLog += "Warning: " + warning;
            }
            Debug.Log(consoleOutputLog);

            if (EnabledOutputSyntaxLog)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string outputDirPath = desktopPath + "\\SyntaxCheckLog\\";
                if (!Directory.Exists(outputDirPath))
                {
                    Directory.CreateDirectory(outputDirPath);
                }

                FileInfo info = new FileInfo(path);
                string fileName = "【" + (isCheckResult ? "OK" : "NG") + "】" + info.Name;
                File.WriteAllText(outputDirPath + fileName, fileOutputLog);
            }
        }

        /// <summary>.txtと.assetを1対1対応させる</summary>
        //[MenuItem("Gluon/AI/Update AIScript")]
        static void UpdateScriptAsset()
        {
            var texts = Directory.GetFiles(scriptDirectoryPath, "*.txt", SearchOption.AllDirectories);
            var assets = Directory.GetFiles(outputDirectoryPath, "*.asset", SearchOption.AllDirectories);

            texts = texts.Select(t => t.Replace(scriptDirectoryPath, "").Replace(".txt", "")).ToArray();
            assets = assets.Select(t => t.Replace(outputDirectoryPath, "").Replace(".asset", "")).ToArray();

            var addList = texts.Except(assets).ToArray();
            foreach (var path in addList)
            {
                string resourcePath = outputDirectoryPath + path + ".asset";

                AIScriptObject instance = AIScriptObject.CreateAssetInstance();
                Directory.CreateDirectory(Path.GetDirectoryName(resourcePath));
                AssetDatabase.CreateAsset(instance, resourcePath);
            }

            if (addList.Length > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var removeList = assets.Except(texts);
            foreach (var path in removeList)
            {
                string resourcePath = outputDirectoryPath + path + ".asset";

                AssetDatabase.DeleteAsset(resourcePath);
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (ContainsScriptPath(importedAssets))
            {
                UpdateScriptAsset();

                //ファイルの更新扱いの時に構文チェックを行う。
                var targetScripts = importedAssets.Where(v => v.StartsWith(scriptDirectoryPath));
                foreach (var script in targetScripts)
                {
                    SyntaxCheck(script);
                }
                return;
            }
            if (ContainsScriptPath(deletedAssets))
            {
                UpdateScriptAsset();
                return;
            }
            if (ContainsScriptPath(movedAssets))
            {
                UpdateScriptAsset();
                return;
            }
            if (ContainsScriptPath(movedFromAssetPaths))
            {
                UpdateScriptAsset();
                return;
            }
        }

        static bool ContainsScriptPath(string[] paths)
        {
            foreach (var path in paths)
            {
                if (path.StartsWith(scriptDirectoryPath) || path.StartsWith(outputDirectoryPath))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool EnabledOutputSyntaxLog
        {
            get
            {
                if (isOutputSyntaxLogFolder == -1)
                {
                    isOutputSyntaxLogFolder = EditorPrefs.GetBool(outputSyntaxLogName, false) ? 1 : 0;
                }
                return isOutputSyntaxLogFolder != 0;
            }
            set
            {
                int NewValue = value ? 1 : 0;
                if (isOutputSyntaxLogFolder != NewValue)
                {
                    isOutputSyntaxLogFolder = NewValue;
                    EditorPrefs.SetBool(outputSyntaxLogName, value);
                }
                isOutputSyntaxLogFolder = value ? 1 : 0;
            }
        }
        [MenuItem(outputSyntaxLogName)]
        public static void ToggleOutputSyntaxLog()
        {
            EnabledOutputSyntaxLog = !EnabledOutputSyntaxLog;
        }
        [MenuItem(outputSyntaxLogName, true)]
        public static bool ToggleSDKEnabledValidate()
        {
            Menu.SetChecked(outputSyntaxLogName, EnabledOutputSyntaxLog);
            return true;
        }
#endif
    }

}
