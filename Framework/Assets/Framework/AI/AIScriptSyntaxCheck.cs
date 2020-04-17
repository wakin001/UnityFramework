using System.Collections.Generic;
using System.Linq;
using System;

namespace Framework
{
#if UNITY_EDITOR
    /*
     * 処理追加の方法について。
     * 
     * ・固定変数を増やした場合
     *  AddReservedKeywordsで_useValiableCountに定義を追加してください。
     *  
     * ・固定関数を増やした場合
     *  AddReservedKeywordsで_useFunctionCountに定義を追加してください。 
     *  
     * ・Commandを追加した場合
     *  CheckParameterのSwitch文のCaseへ追加してください。
     *  そのコマンド用で専用のログを出す場合はAnalizeScriptのSwitch文へCase文を追加し直接ログを出すようにしてください。
     * 
     * ・その他のパラメータを追加した場合
     *  追加したパラメータが変数として扱えるなら_useValiableCountへ追加、
     *  そうでないなら_useFunctionCountへ追加してください。
     *  enum等(eMoveActionやeTurnAction等)は変数として扱っています。
     */
    public class AIScriptSyntaxCheck
    {
        /// <summary>
        /// 構文チェックの結果のエラーログ一覧を格納します。
        /// </summary>
        public string[] Errors
        {
            get { return _errors.ToArray(); }
        }
        /// <summary>
        /// 構文チェックの結果の警告ログ一覧を格納します。
        /// </summary>
        public string[] Warnings
        {
            get { return _warnings.ToArray(); }
        }

        /// <summary>
        /// 構文チェックの結果、チェックに通過したかどうかのフラグです。
        /// エラーがあるとfalseになります。
        /// 警告はチェック通過チェックに影響を及ぼしません。
        /// </summary>
        public bool IsSuccessSyntaxCheck
        {
            get { return _isSuccessSyntaxCheck; }
        }

        private List<AIScriptContainer> _containers = new List<AIScriptContainer>();
        private List<string> _errors = new List<string>();
        private List<string> _warnings = new List<string>();
        private string[] scriptOrigin = null;
        private string[] scriptShape = null;
        private bool _isSuccessSyntaxCheck = true;

        private Dictionary<string, int> _useFunctionCount = new Dictionary<string, int>();
        private Dictionary<string, int> _useValiableCount = new Dictionary<string, int>();
        private Dictionary<string, int> _useValiableActionCount = new Dictionary<string, int>();
        private int _ifCount = 0;
        private int _endifCount = 0;
        private int _defCount = 0;
        private int _enddefCount = 0;

        /// <summary>
        /// スクリプトの構文チェック
        /// 問題があればエラーログとしてErrorsにエラー内容を格納します。
        /// 警告はWarningsに内容を格納します。
        /// </summary>
        /// <param name="errors"></param>
        public bool SyntaxCheck(string[] scriptStrs)
        {
            scriptOrigin = scriptStrs;
            // テキストで見たときの行数からチェックしやすくするため不要文字列は削除せずわたす。
            // タブとコメントアウトを削除し、行数を維持したままAIScriptContainerに渡す。
            ScriptSplit(scriptStrs);

            //チェック対象のキーワードの追加
            //基本的にはここに新規のキーワード等追加していく。
            AddReservedKeywords();

            //追加したチェック対象キーワードをもとにチェックをします。
            AnalizeScript();

            //対となるべき演算子(IF,DEF)のチェック
            CheckIfEndifAndDefEnddef();

            //関数呼び出しチェック
            CheckCallFunction();

            //コマンド引数チェック
            CheckCallCommandParameter();

            return _isSuccessSyntaxCheck;
        }

        /// <summary>
        /// チェック対象のキーワードを追加します。
        /// 定義は2種類あり、『関数』と『変数』です。
        /// これらは参照回数から初期化しているかや、定義をしていないのに呼び出していないか等チェックを行います。
        /// 引数を渡すキーワードは関数扱い、それ以外の定義はすべて変数としてチェックを行っています。
        /// 
        /// ここで定義しているものはアプリ側で必ず定義が存在前提で既に定義してあるという意味で1で初期化されています。
        /// </summary>
        private void AddReservedKeywords()
        {
            int definitionCount = 1;

            //AIScript側で動作が定義してある変数
            _useValiableCount.Add(AIScriptDef._HpRate, definitionCount);
            _useValiableCount.Add(AIScriptDef._TrgHpRate, definitionCount);
            _useValiableCount.Add(AIScriptDef._TrgLength, definitionCount);
            _useValiableCount.Add(AIScriptDef._IsFrontTarget, definitionCount);
            _useValiableCount.Add(AIScriptDef._GameTime, definitionCount);
            _useValiableCount.Add(AIScriptDef._AwakeTime, definitionCount);
            _useValiableCount.Add(AIScriptDef._QuestTimeLimit, definitionCount);
            _useValiableCount.Add(AIScriptDef._RecHpRate, definitionCount);
            _useValiableCount.Add(AIScriptDef._CountTime, definitionCount);
            _useValiableCount.Add(AIScriptDef._IsSearchAction, definitionCount);
            _useValiableCount.Add(AIScriptDef._PivotLength, definitionCount);
            _useValiableCount.Add(AIScriptDef._DirectDmgCnt, definitionCount);
            _useValiableCount.Add(AIScriptDef._IndirectDmgCnt, definitionCount);
            _useValiableCount.Add(AIScriptDef._IsBreakRecovery, definitionCount);
            _useValiableCount.Add(AIScriptDef._LastActionId, definitionCount);
            _useValiableCount.Add(AIScriptDef._IsRage, definitionCount);
            _useValiableCount.Add(AIScriptDef._QuesetDifficulty, definitionCount);
            _useValiableCount.Add(AIScriptDef._MarkerCnt, definitionCount);
            _useValiableCount.Add(AIScriptDef._IsActiveParts, definitionCount);
            _useValiableCount.Add(AIScriptDef._ParamId, definitionCount);
            _useValiableCount.Add(AIScriptDef._ActionSetId, definitionCount);
            _useValiableCount.Add(AIScriptDef._IsActiveAbility, definitionCount);
            _useValiableCount.Add(AIScriptDef._IsSpecialMode, definitionCount);

            _useValiableCount.Add(AIScriptDef._GM_CanSetTurn, definitionCount);
            _useValiableCount.Add(AIScriptDef._GM_TurnCnt, definitionCount);
            _useValiableCount.Add(AIScriptDef._GM_CanSetSuddenEvent, definitionCount);
            _useValiableCount.Add(AIScriptDef._GM_CanSetBandit, definitionCount);

            //AIScript側で動作が定義してある関数
            _useFunctionCount.Add(AIScriptDef.Init, definitionCount);
            _useFunctionCount.Add(AIScriptDef.Reset, definitionCount);
            _useFunctionCount.Add(AIScriptDef.UpdateAttack, definitionCount);
            _useFunctionCount.Add(AIScriptDef.UpdateIdle, definitionCount);
            _useFunctionCount.Add(AIScriptDef.UpdateWake, definitionCount);

            //SetTargetに渡している系の定数
            //Enumとして定義してあるようなのですべて変数として定義されているという扱いで追加
            //foreach (var def in Enum.GetNames(typeof(ActionTarget)))
            //{
            //    if (!_useValiableCount.ContainsKey(def))
            //    {
            //        _useValiableCount.Add(def, definitionCount);
            //    }
            //}
            foreach (var def in Enum.GetNames(typeof(AIScriptDef.eMoveAction)))
            {
                if (!_useValiableCount.ContainsKey(def))
                {
                    _useValiableCount.Add(def, definitionCount);
                }
            }
            foreach (var def in Enum.GetNames(typeof(AIScriptDef.eTurnAction)))
            {
                if (!_useValiableCount.ContainsKey(def))
                {
                    _useValiableCount.Add(def, definitionCount);
                }
            }
            foreach (var def in Enum.GetNames(typeof(AIScriptDef.eTargetType)))
            {
                if (!_useValiableCount.ContainsKey(def))
                {
                    _useValiableCount.Add(def, definitionCount);
                }
            }

            //true,false
            _useValiableCount.Add("true", definitionCount);
            _useValiableCount.Add("false", definitionCount);

            //fromActionSet
            //こちらもAIScript側で定義されている値を変数として追加
            _useValiableCount.Add("id", definitionCount);
            _useValiableCount.Add("HomeAction", definitionCount);
            _useValiableCount.Add("counterAction", definitionCount);
            _useValiableCount.Add("barrierAction", definitionCount);
            _useValiableCount.Add("rageAction", definitionCount);
            _useValiableCount.Add("getupAction", definitionCount);
            _useValiableCount.Add("unusualDead", definitionCount);
            _useValiableCount.Add("uniqueDeadAction", definitionCount);
            _useValiableCount.Add("breakAction", definitionCount);
            _useValiableCount.Add("unusualBreak", definitionCount);
            _useValiableCount.Add("alternativeAction", definitionCount);

            for (int i = 0; i < 51; i++)
            {
                _useValiableCount.Add(string.Format("action{0:00}", i + 1), definitionCount);
            }
        }

        private void ScriptSplit(string[] scriptStrs)
        {
            scriptShape = scriptStrs.Select(l => l.Replace("\t", "").Trim()).ToArray();
            for (int lineCnt = 0; lineCnt < scriptShape.Length; ++lineCnt)
            {
                AIScriptContainer container = null;
                if (!scriptShape[lineCnt].StartsWith("#")
                && scriptShape[lineCnt].Length > 0)
                {
                    container = new AIScriptContainer(scriptShape[lineCnt]);
                }
                _containers.Add(container);
            }
        }

        private void AnalizeScript()
        {
            for (int i = 0; i < _containers.Count; ++i)
            {
                var script = _containers[i];
                if (script == null) continue;
                switch (script._command)
                {
                    case AIScriptDef.Command.If:
                        _ifCount++;
                        break;
                    case AIScriptDef.Command.EndIf:
                        _endifCount++;
                        break;
                    case AIScriptDef.Command.EndDef:
                        _enddefCount++;
                        break;
                    case AIScriptDef.Command.Def:
                        _defCount++;
                        if (script._params == null)
                        {
                            _errors.Add(string.Format("関数定義命令がありますが、関数名がありません。\n{0}行: {1}\n", i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (!_useFunctionCount.ContainsKey(script._params[0].ValueString))
                        {
                            _useFunctionCount.Add(script._params[0].ValueString, 0);
                        }
                        break;
                    case AIScriptDef.Command.Set:
                        if (script._params == null)
                        {
                            _errors.Add(string.Format("変数定義命令がありますが、変数名、初期値がありません。\n{0}行: {1}\n", i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (script._params.Length == 1)
                        {
                            _errors.Add(string.Format("変数定義命令がありますが、初期値がありません。\n変数名: {0}\n{1}行: {2}\n", script._params[0], i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (!_useValiableCount.ContainsKey(script._params[0].ValueString))
                        {
                            _useValiableCount.Add(script._params[0].ValueString, 0);
                        }
                        break;
                    case AIScriptDef.Command.FromActionSet:
                        if (script._params == null)
                        {
                            _errors.Add(string.Format("FromActionSet命令がありますが、アクションカラム名、変数名がありません。\n{0}行: {1}\n", i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (script._params.Length < 2)
                        {
                            _errors.Add(string.Format("FromActionSet命令がありますが、変数名がありません。\n{0}行: {1}\n", i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (!_useValiableActionCount.ContainsKey(script._params[1].ValueString))
                        {
                            _useValiableActionCount.Add(script._params[1].ValueString, 0);
                        }
                        break;
                    case AIScriptDef.Command.GM_SetTurnEvent:
                        //ToDo:要件固まったら追記
                        break;
                    case AIScriptDef.Command.Random:
                        if (script._params == null)
                        {
                            _errors.Add(string.Format("Random命令がありますが、格納変数名、指定値(最大値 or 最小値,最大値)がありません。\n{0}行: {1}\n", i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (script._params.Length == 1)
                        {
                            _warnings.Add(string.Format("Random命令がありますが、指定値(最大値 or 最小値,最大値)がありません。\n{0}行: {1}\n", i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (!_useValiableCount.ContainsKey(script._params[0].ValueString))
                        {
                            _useValiableCount.Add(script._params[0].ValueString, 0);
                        }
                        break;
                    default:
                        //ここに来た場合は念のためコマンドのチェックを行う。
                        //主にスペルミスや、未定義のコマンドのチェックのため。
                        //AIScriptContainerでは例外をキャッチして、値の代入を行ってないためここで再度取得する。
                        if (scriptShape[i].StartsWith("#")) break;
                        if (scriptShape[i].Length <= 0) break;

                        string[] column = scriptShape[i].Split(' ');
                        AIScriptDef.Command tmp;
                        if (!Enum.TryParse<AIScriptDef.Command>(column[0], true, out tmp))
                        {   //未定義なコマンド
                            _errors.Add(string.Format("不明な命令 {0} が見つかりました。\n{1}行: {2}\n", column[0], i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                        }
                        break;
                }
            }
        }

        private void CheckIfEndifAndDefEnddef()
        {
            if (_ifCount != _endifCount)
            {
                _errors.Add(string.Format("IFとENDIF演算子の数が一致していません。 if: {0} endif: {1}\n", _ifCount, _endifCount));
                _isSuccessSyntaxCheck = false;
            }
            if (_defCount != _enddefCount)
            {
                _errors.Add(string.Format("DEFとENDDEF演算子の数が一致していません。 if: {0} endif: {1}\n", _defCount, _enddefCount));
                _isSuccessSyntaxCheck = false;
            }
        }

        private void CheckCallFunction()
        {
            for (int i = 0; i < _containers.Count; ++i)
            {
                var script = _containers[i];
                if (script == null) continue;
                switch (script._command)
                {
                    case AIScriptDef.Command.Function:
                        if (script._params == null)
                        {
                            _errors.Add(string.Format("関数実行命令がありますが、関数名がありません。\n{0}行: {1}\n", i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        if (!_useFunctionCount.ContainsKey(script._params[0].ValueString))
                        {
                            _errors.Add(string.Format("未定義の関数呼び出しです。\n関数名: {0}\n{1}行: {2}\n", script._params[0].ValueString, i, scriptOrigin[i]));
                            _isSuccessSyntaxCheck = false;
                            continue;
                        }
                        _useFunctionCount[script._params[0].ValueString]++;
                        break;
                    default:
                        //Function以外の予約語はそれぞれ処理を行う
                        break;
                }
            }
            foreach (var p in _useFunctionCount)
            {
                if (p.Value > 0) continue;
                _warnings.Add(string.Format("一度も使用されていない関数があります。\n関数名: {0}", p.Key));
            }
        }

        private void CheckCallCommandParameter()
        {
            for (int i = 0; i < _containers.Count; ++i)
            {
                var script = _containers[i];
                if (script == null) continue;
                CheckParameter(script._command, script._params, i);
            }
            foreach (var p in _useValiableCount)
            {
                if (p.Value > 0) continue;
                _warnings.Add(string.Format("一度も使用されていない変数があります。\n関数名: {0}", p.Key));
            }
        }

        /// <summary>
        /// コマンドに対して引数を設定するもののチェックを行います。
        /// 特定のコマンドでしか呼び出せない変数などもここでチェックを行います。
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="scriptParameters"></param>
        /// <param name="line"></param>
        private void CheckParameter(AIScriptDef.Command cmd, AIScriptParam[] scriptParameters, int line)
        {
            if (scriptParameters == null) return;
            foreach (var param in scriptParameters)
            {
                if (param.columns == null) continue;
                foreach (var col in param.columns)
                {
                    if (col.values == null) continue;
                    foreach (var value in col.values)
                    {
                        if (value.valType != AIScriptValue.ValueType.String) continue;

                        //IF _isActiveParts 1
                        //等paramsやcolumnsの段階で分割できていないものをここで分解する。
                        string firstValue = value.splitValStrings[0];
                        if (_useValiableCount.ContainsKey(firstValue))
                        {
                            _useValiableCount[firstValue]++;
                            continue;
                        }
                        else
                        {
                            switch (cmd)
                            {
                                case AIScriptDef.Command.Action:
                                    if (_useValiableActionCount.ContainsKey(firstValue))
                                    {
                                        _useValiableActionCount[firstValue]++;
                                        continue;
                                    }
                                    else
                                    {
                                        _errors.Add(string.Format("未定義の変数呼び出しです。\n変数名: {0}\n{1}行: {2}\n", firstValue, line, scriptOrigin[line]));
                                        _isSuccessSyntaxCheck = false;
                                    }
                                    break;
                                case AIScriptDef.Command.If:
                                case AIScriptDef.Command.ElseIF:
                                case AIScriptDef.Command.Add:
                                case AIScriptDef.Command.Sub:
                                case AIScriptDef.Command.Mul:
                                case AIScriptDef.Command.SetTarget:
                                case AIScriptDef.Command.MoveAction:
                                case AIScriptDef.Command.TurnAction:
                                case AIScriptDef.Command.RecTimer:
                                case AIScriptDef.Command.RecHpRate:
                                case AIScriptDef.Command.AliveNum:
                                case AIScriptDef.Command.Jump:
                                case AIScriptDef.Command.Wake:
                                case AIScriptDef.Command.ClearDmgCnt:
                                case AIScriptDef.Command.UnusualPosture:
                                case AIScriptDef.Command.GM_SetTurnEvent:
                                case AIScriptDef.Command.GM_CompleteTurnEvent:
                                case AIScriptDef.Command.GM_SetTurnMax:
                                case AIScriptDef.Command.GM_SetSuddenEvent:
                                case AIScriptDef.Command.GM_SetBanditEvent:
                                    if (_useValiableActionCount.ContainsKey(firstValue))
                                    {
                                        _errors.Add(string.Format("FromActionSetで定義した変数はactionコマンド以外で呼び出すことはできません。\n変数名: {0}\n{1}行: {2}\n", firstValue, line, scriptOrigin[line]));
                                    }
                                    else
                                    {
                                        _errors.Add(string.Format("未定義の変数呼び出しです。\n変数名: {0}\n{1}行: {2}\n", firstValue, line, scriptOrigin[line]));
                                    }
                                    _isSuccessSyntaxCheck = false;
                                    break;
                                default:
                                    //定義系コマンドや引数のないコマンドは処理しない
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
#endif
}
