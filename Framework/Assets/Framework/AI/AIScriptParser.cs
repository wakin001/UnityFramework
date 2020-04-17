using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// AIScript解析クラス
    /// AIScriptObjectを関数単位で解析保持するクラス
    /// </summary>
    public class AIScriptParser
    {
        private Dictionary<string, List<AIScriptContainer>> _scriptFunctions = new Dictionary<string, List<AIScriptContainer>>();

        public AIScriptParser(AIScriptObject script)
        {
            Build(script);
        }
        /// <summary>
        /// スクリプト関数の取得
        /// </summary>
        /// <param name="functionName"></param>
        /// <returns></returns>
        public List<AIScriptContainer> GetScriptFunction(string functionName)
        {
            Debug.Assert(_scriptFunctions.ContainsKey(functionName), "-----Error No Script Function " + functionName);
            return _scriptFunctions[functionName];
        }
        public bool IsScriptFunction(string functionName)
        {
            return _scriptFunctions.ContainsKey(functionName);
        }
        /// <summary>
        /// 関数単位のscript解析
        /// </summary>
        /// <param name="script"></param>
        private void Build(AIScriptObject script)
        {
            for (int lineCnt = 0; lineCnt < script.containers.Count;)
            {
                AIScriptContainer container = script.containers[lineCnt++];
                if (container._command == AIScriptDef.Command.Def)
                {
                    //function発見
                    string functioName = container._params[0].ValueString;
                    List<AIScriptContainer> scriptLines = new List<AIScriptContainer>();
                    //def~enddefのコマンドを保持
                    while (container._command != AIScriptDef.Command.EndDef)
                    {
                        container = script.containers[lineCnt++];
                        if (container._command != AIScriptDef.Command.EndDef)
                        {
                            scriptLines.Add(container);
                        }
                    }
                    //登録
                    _scriptFunctions.Add(functioName, scriptLines);
                }
            }
        }

    }
}