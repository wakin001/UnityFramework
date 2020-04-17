using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// AIScriptデータクラス
    /// </summary>
    public class AIScriptObject : ScriptableObject
    {
        [SerializeField]
        List<AIScriptContainer> _containers = new List<AIScriptContainer>();
        public List<AIScriptContainer> containers { get { return _containers; } }

        static public AIScriptObject CreateAssetInstance()
        {
            return CreateInstance<AIScriptObject>();
        }


#if UNITY_EDITOR
        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="scriptStrs"></param>
        /// <returns></returns>
        public void Parser(string[] scriptStrs)
        {
            // 不要文字列は削除された情報で来ている
            // function~endfunctionでデータを区分け
            for (int lineCnt = 0; lineCnt < scriptStrs.Length; ++lineCnt)
            {
                _containers.Add(new AIScriptContainer(scriptStrs[lineCnt]));
            }
            //script step解析
            ParserStep(_containers);

            // script enum解析
            ParserEnum(_containers);
        }
        static int tabCnt = 0;
        /// <summary>
        /// デバッグ用解析ログ出力関数
        /// </summary>
        /// <param name="rootScript"></param>
        /// <param name="log"></param>
        private void LogStep(List<AIScriptContainer> rootScript, ref List<string> log)
        {
            foreach (var line in rootScript)
            {
                if (line._command == AIScriptDef.Command.EndIf || line._command == AIScriptDef.Command.Else || line._command == AIScriptDef.Command.ElseIF)
                {
                    tabCnt--;
                }

                string tabStr = "";
                for (int i = 0; i < tabCnt; ++i)
                {
                    tabStr += "\t";
                }
                string strLine = string.Format("{0}{1}:step {2}", tabStr, line.ToString(), line._jumpStep);
                log.Add(strLine);

                if (line._command == AIScriptDef.Command.If || line._command == AIScriptDef.Command.Else || line._command == AIScriptDef.Command.ElseIF)
                {
                    tabCnt++;
                }
            }
        }
        private void ParserStep(List<AIScriptContainer> scripts)
        {
            // if分による分岐処理終了用jump命令の追加＆step計算
            for (int i = 0; i < scripts.Count; ++i)
            {
                //elseif, elseの手前にjump命令の追加
                if (scripts[i]._command == AIScriptDef.Command.Else || scripts[i]._command == AIScriptDef.Command.ElseIF)
                {
                    scripts.Insert(i, new AIScriptContainer(AIScriptDef.Command.Jump.ToString()));
                    ++i;
                }
            }
            AIScriptContainer script;
            // if,else,elseif,endifのstep計算
            for (int i = 0; i < scripts.Count; ++i)
            {
                script = scripts[i];
                if (script._command == AIScriptDef.Command.If || script._command == AIScriptDef.Command.Else || script._command == AIScriptDef.Command.ElseIF)
                {
                    //elseif,else,endifのstepカウント
                    int ifCnt = 0;
                    script._jumpStep = 0;
                    for (int ii = i + 1; ii < scripts.Count; ++ii)
                    {
                        if (scripts[ii]._command == AIScriptDef.Command.If)
                        {
                            //階層切り替え
                            ++ifCnt;
                        }
                        else if (ifCnt > 0 && scripts[ii]._command == AIScriptDef.Command.EndIf)
                        {
                            //階層終了
                            --ifCnt;
                        }
                        else if (ifCnt <= 0 && (scripts[ii]._command == AIScriptDef.Command.Else || scripts[ii]._command == AIScriptDef.Command.ElseIF || scripts[ii]._command == AIScriptDef.Command.EndIf))
                        {
                            //内部処理終了
                            // step count
                            ++script._jumpStep;
                            break;
                        }
                        // step count
                        ++script._jumpStep;
                    }
                }
                // jump のstep計算
                else if (script._command == AIScriptDef.Command.Jump)
                {
                    //elseif,else,endifのstepカウント
                    int ifCnt = 0;
                    script._jumpStep = 0;
                    for (int ii = i + 1; ii < scripts.Count; ++ii)
                    {
                        if (scripts[ii]._command == AIScriptDef.Command.If)
                        {
                            //階層切り替え
                            ++ifCnt;
                        }
                        else if (scripts[ii]._command == AIScriptDef.Command.EndIf)
                        {
                            //階層終了
                            --ifCnt;
                        }
                        // step count
                        ++script._jumpStep;

                        if (ifCnt < 0)
                        {
                            //内部処理終了
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// enum系文字列の解析（毎時文字列検索だと処理の無駄なのでID変換をかけておく
        ///　enum系文字列の変換をかけているのでゲーム外の事前変換で呼び出さないこと
        /// </summary>
        /// <param name="scripts"></param>
        private void ParserEnum(List<AIScriptContainer> scripts)
        {
            for (int i = 0; i < scripts.Count; ++i)
            {
                switch (scripts[i]._command)
                {
                    case AIScriptDef.Command.SetTarget:
                        {
                            //ターゲット設定コマンド, enum文字列変換
                            //var dict = Master.ActionTarget.Instance.Dict;
                            //int numParam = scripts[i]._params.Length; // パラメータ数獲得
                            //const int maxNumParam = 2; // 現在２パラメータ迄サポート
                            //numParam = (numParam > maxNumParam) ? maxNumParam : numParam;
                            //for (int paramId = 0; paramId < numParam; ++paramId) // パラメータを2つ迄に拡張
                            //{
                            //    string keyStr = scripts[i]._params[paramId].columns[0].values[0].valString;
                            //    foreach (var dt in dict)
                            //    {
                            //        if (dt.Value.Label.Contains(scripts[i]._params[paramId].columns[0].values[0].valString))
                            //        {
                            //            //同一データ発見、文字列情報からキー情報へ差し替え
                            //            keyStr = dt.Key.ToString();
                            //            break;
                            //        }
                            //    }
                            //    int result;
                            //    Debug.Assert(int.TryParse(keyStr, out result), "-----Error AIScript setTarget Values " + scripts[i]._params[paramId]);
                            //    scripts[i]._params[paramId].columns[0].CreateValue(keyStr);
                            //}
                        }
                        break;
                    case AIScriptDef.Command.MoveAction:
                        {
                            //移動アクションタイプ文字列を数値変換
                            try
                            {
                                int keyId = (int)System.Enum.Parse(typeof(AIScriptDef.eMoveAction), scripts[i]._params[0].ValueString, true);
                                scripts[i]._params[0].columns[0].CreateValue("" + keyId);
                            }
                            catch
                            {
                                Debug.Assert(false, "-----Error AIScript moveAction Values " + scripts[i]._params[0]);
                            }
                        }
                        break;
                    case AIScriptDef.Command.TurnAction:
                        {
                            try
                            {
                                int keyId = (int)System.Enum.Parse(typeof(AIScriptDef.eTurnAction), scripts[i]._params[0].ValueString, true);

                                scripts[i]._params[0].columns[0].CreateValue("" + keyId);
                            }
                            catch
                            {
                                Debug.Assert(false, "-----Error AIScript turnAction Values " + scripts[i]._params[0].ValueString);
                            }
                        }
                        break;
                    case AIScriptDef.Command.AliveNum:
                        {
                            try
                            {
                                int keyId = (int)System.Enum.Parse(typeof(AIScriptDef.eTargetType), scripts[i]._params[0].ValueString, true);
                                scripts[i]._params[0].columns[0].CreateValue("" + keyId);
                            }
                            catch
                            {
                                Debug.Assert(false, "-----Error AIScript AliveNum Values " + scripts[i]._params[0].ValueString);
                            }
                        }
                        break;
                }
            }
        }
#endif
    }
}