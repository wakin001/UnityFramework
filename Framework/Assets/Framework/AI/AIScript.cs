using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Framework
{
    /// <summary>
    /// EnemyAIScript実行制御クラス
    /// memo: AIScirpt変数データは文字列による管理をしている、一部命令によっては数値を元にしているため数値Parseしているが
    /// 　　　これが原因でGCが発生している
    /// </summary>
    public class AIScript : MonoBehaviour
    {
        //private CommonObjectStatus _target = null;      // ターゲット

        //AI制御用（このオブジェクトの状態がゲストに同期されます）
        AIScriptContext _context = new AIScriptContext();

        AIScriptLoader aiScriptLoader = new AIScriptLoader();

        //private EnemyCtrl _enemyCtrl;
        private AIScriptParser _script;
        //コマンド命令関数
        private delegate int ExecScriptCommand(ref AIScriptContainer script);
        Dictionary<int, ExecScriptCommand> _delegateScriptCommand = new Dictionary<int, ExecScriptCommand>();
        //private static Gluon.Event.CharacterSpecialState _tmpChangeBossState = new Gluon.Event.CharacterSpecialState();

        private bool _isEndScript;
        private List<AIScriptContainer> _updateAwakeContainer = null;
        private List<AIScriptContainer> _updateAttackContainer = null;
        private List<AIScriptContainer> _updateIdleContainer = null;

        // Initialize後に処理するAction
        private System.Action _postInitializeAction = null;

#if UNITY_EDITOR && GLUON_DEBUG
        // スクリプトログ
        public class DebugLog
        {
            public Dictionary<string, string> values = new Dictionary<string, string>();
            public List<string> command = new List<string>();
            public List<string> console = new List<string>();
            public void Clear()
            {
                values.Clear();
                command.Clear();
                console.Clear();
            }
        }

        [System.NonSerialized]
        protected DebugLog[] _log = new DebugLog[2];
        public DebugLog[] Logs { get { return _log; } }

        [System.NonSerialized]
        private int _tabCnt = 0;
#endif
        public bool IsValid()
        {
            return (_script != null);
        }

        // Use this for initialization
        void Awake()
        {
            Initialize();

            if (_postInitializeAction != null)
            {
                _postInitializeAction();
                _postInitializeAction = null;
            }
        }
        public void Initialize()
        {
            //スクリプトコマンド登録
            _delegateScriptCommand[(int)AIScriptDef.Command.If] = ScriptCommandIf;
            _delegateScriptCommand[(int)AIScriptDef.Command.ElseIF] = ScriptCommandElseIf;
            _delegateScriptCommand[(int)AIScriptDef.Command.Else] = ScriptCommandElse;
            _delegateScriptCommand[(int)AIScriptDef.Command.EndIf] = ScriptCommandEndIf;
            _delegateScriptCommand[(int)AIScriptDef.Command.Set] = ScriptCommandSet;
            _delegateScriptCommand[(int)AIScriptDef.Command.Add] = ScriptCommandAdd;
            _delegateScriptCommand[(int)AIScriptDef.Command.Sub] = ScriptCommandSub;
            _delegateScriptCommand[(int)AIScriptDef.Command.Mul] = ScriptCommandMul;
            _delegateScriptCommand[(int)AIScriptDef.Command.SetTarget] = ScriptCommandSetTarget;
            _delegateScriptCommand[(int)AIScriptDef.Command.EndScript] = ScriptCommandEndScript;
            _delegateScriptCommand[(int)AIScriptDef.Command.Action] = ScriptCommandAction;
            _delegateScriptCommand[(int)AIScriptDef.Command.Function] = ScriptCommandFunction;
            _delegateScriptCommand[(int)AIScriptDef.Command.MoveAction] = ScriptCommnadMoveAction;
            _delegateScriptCommand[(int)AIScriptDef.Command.TurnAction] = ScriptCommnadTurnAction;
            _delegateScriptCommand[(int)AIScriptDef.Command.Random] = ScriptCommandRandom;
            _delegateScriptCommand[(int)AIScriptDef.Command.RecTimer] = ScriptCommandRecodeTime;
            _delegateScriptCommand[(int)AIScriptDef.Command.RecHpRate] = ScriptCommandRecodeHpRate;
            _delegateScriptCommand[(int)AIScriptDef.Command.AliveNum] = ScriptCommandAliveNum;
            _delegateScriptCommand[(int)AIScriptDef.Command.Jump] = ScriptCommandJump;
            _delegateScriptCommand[(int)AIScriptDef.Command.Wake] = ScriptCommandWake;
            _delegateScriptCommand[(int)AIScriptDef.Command.ClearDmgCnt] = ScriptCommandClearDamageCounter;
            _delegateScriptCommand[(int)AIScriptDef.Command.UnusualPosture] = ScriptCommnadUnusualPosture;
            _delegateScriptCommand[(int)AIScriptDef.Command.FromActionSet] = ScriptCommnadFromActionSet;
            _delegateScriptCommand[(int)AIScriptDef.Command.OrderCloser] = ScriptCommandOrderCloser;
            _delegateScriptCommand[(int)AIScriptDef.Command.OrderAliveFather] = ScriptCommandOrderAliveFather;

            //ゲームマスターAI用
            _delegateScriptCommand[(int)AIScriptDef.Command.GM_SetTurnEvent] = ScriptCommnadGM_SetTurnEvent;
            _delegateScriptCommand[(int)AIScriptDef.Command.GM_CompleteTurnEvent] = ScriptCommnadGM_CompleteTurnEvent;
            _delegateScriptCommand[(int)AIScriptDef.Command.GM_SetTurnMax] = ScriptCommnadGM_SetTurnMax;
            _delegateScriptCommand[(int)AIScriptDef.Command.GM_SetSuddenEvent] = ScriptCommnadGM_SetSuddenEvent;
            _delegateScriptCommand[(int)AIScriptDef.Command.GM_SetBanditEvent] = ScriptCommnadGM_SetBanditEvent;


            //システム変数の作成
            _context.AddLocalValue(AIScriptDef._GameTime, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._AwakeTime, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._QuestTimeLimit, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._HpRate, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._TrgHpRate, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._RecHpRate, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._CountTime, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._TrgLength, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._PivotLength, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._IsSearchAction, new AIScriptValue(0.0f));
            _context.AddLocalValue(AIScriptDef._IndirectDmgCnt, new AIScriptValue(0));
            _context.AddLocalValue(AIScriptDef._DirectDmgCnt, new AIScriptValue(0));
            _context.AddLocalValue(AIScriptDef._LastActionId, new AIScriptValue(0));
            _context.AddLocalValue(AIScriptDef._QuesetDifficulty, new AIScriptValue(0));
            _context.AddLocalValue(AIScriptDef._MarkerCnt, new AIScriptValue(0));
            _context.AddLocalValue(AIScriptDef._ParamId, new AIScriptValue(0));
            _context.AddLocalValue(AIScriptDef._ActionSetId, new AIScriptValue(0));
            _context.AddLocalValue(AIScriptDef._GM_TurnCnt, new AIScriptValue(0));

            // 制御情報保持
            //_enemyCtrl = gameObject.GetComponent<EnemyCtrl>();

            InitTermSubDictionary();
        }
        /// <summary>
        /// AI構築処理
        /// </summary>
        public void Create(string scriptFile)
        {
            Debug.Assert(!string.IsNullOrEmpty(scriptFile));

            //_script = MainGameCtrl.instance.aiScriptLoader.Load(scriptFile);
            _script = aiScriptLoader.Load(scriptFile);
            if (_script != null)
            {
                //script初期化
#if UNITY_EDITOR && GLUON_DEBUG
                _log[0] = new DebugLog();
                _log[1] = new DebugLog();
#endif
                _isEndScript = false;
                UpdateAction(_script.GetScriptFunction(AIScriptDef.Init));
                _updateAwakeContainer = _script.IsScriptFunction(AIScriptDef.UpdateWake) ? _script.GetScriptFunction(AIScriptDef.UpdateWake) : null;
                _updateAttackContainer = _script.IsScriptFunction(AIScriptDef.UpdateAttack) ? _script.GetScriptFunction(AIScriptDef.UpdateAttack) : null;
                _updateIdleContainer = _script.IsScriptFunction(AIScriptDef.UpdateIdle) ? _script.GetScriptFunction(AIScriptDef.UpdateIdle) : null;
            }
        }

#if UNITY_EDITOR && GLUON_DEBUG
        /// <summary>
        /// デバッグ用内部使用変数情報の取得
        /// </summary>
        /// <returns></returns>                     
        public void SetDebugValues()
        {
            _log[1].values.Clear();
            Dictionary<string, string> values = _log[1].values;
            values.Add("ActionID", _enemyCtrl.actionID.ToString());
            values.Add("MoveAction", _enemyCtrl.moveAction.ToString());
            values.Add("TurnAction", _enemyCtrl.turnAction.ToString());
            values.Add(AIScriptDef.TARGET, (_target != null) ? _target.cacheTransform.parent.name : "none");
            foreach (var key in _context.AllLocalValueNames)
            {
                values.Add(key, GetScriptVariable(key).ToString());
            }
        }
        public void UpdateDebugLog()
        {
            SetDebugValues();
        }

        public void DebugConsole(string str)
        {
            _log[1].console.Add(str);
        }

        //
        public void AssertAIError(string msg)
        {
            if (!DebugAIScript._isShow)
            {
                return;
            }

            if (DebugAIScript._isEnableAssert)
            {
                Debug.Assert(false, msg);
            }
            else
            {
                Debug.LogWarning(msg);
            }
        }
#endif
        private void UpdateAction(List<AIScriptContainer> scriptContainer)
        {
            if (scriptContainer != null)
            {
#if UNITY_EDITOR && GLUON_DEBUG
                // デバッグ用ログの保持
                if (DebugAIScript._isShow)
                {
                    _log[0].values = new Dictionary<string, string>(_log[1].values);
                    _log[0].command = new List<string>(_log[1].command);
                    _log[0].console = new List<string>(_log[1].console);
                    _log[1].Clear();
                    _tabCnt = 0;
                }
#endif
                _context.isSearchAction = false;
                _isEndScript = false;

                ExecScript(scriptContainer);
#if UNITY_EDITOR && GLUON_DEBUG
                if (DebugAIScript._isShow)
                {
                    SetDebugValues();
                }
#endif
            }
        }
        /// <summary>
        /// 攻撃処理用AI制御
        /// </summary>
        public void UpdateAttackAI()
        {
            UpdateAction(_updateAttackContainer);
        }
        /// <summary>
        /// 待機処理用AI制御
        /// </summary>
        public void UpdateIdleAI()
        {
            UpdateAction(_updateIdleContainer);
        }

        // Update is called once per frame
        void Update()
        {
            //if (_enemyCtrl.isAwake)
            //{
            //    //起床時間カウント
            //    _context.awakeTime += Time.deltaTime;
            //}
            ////起床判定チェック
            //else if (_updateAwakeContainer != null && !_enemyCtrl.character.isDead)
            //{
            //    ExecScript(_updateAwakeContainer);
            //}

            ////計測タイマーカウント    
            //if (_context.isCountTime)
            //{
            //    _context.countTime += Time.deltaTime;
            //}
        }


        //--------------------------------------
        /// 条件判定処理の分岐別のデリゲート
        //--------------------------------------
        //delegate bool TermSubDelegate(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare);
        //Dictionary<string, TermSubDelegate> termsSubDictionary = new Dictionary<string, TermSubDelegate>();

        void InitTermSubDictionary()
        {
            //termsSubDictionary.Add(AIScriptDef._IsFrontTarget, TermIsFrontTarget);
            //termsSubDictionary.Add(AIScriptDef._IsBreakRecovery, TermIsBreakRecovery);
            //termsSubDictionary.Add(AIScriptDef._IsRage, TermIsRage);
            //termsSubDictionary.Add(AIScriptDef._IsActiveParts, TermIsActiveParts);
            //termsSubDictionary.Add(AIScriptDef._IsActiveAbility, TermIsActiveAbility);
            //termsSubDictionary.Add(AIScriptDef._IsSpecialMode, TermIsSpecialMode);

            //termsSubDictionary.Add(AIScriptDef._GM_CanSetTurn, TermGM_CanSetTurn);
            //termsSubDictionary.Add(AIScriptDef._GM_CanSetSuddenEvent, TermGM_CanSetSuddenEvent);
            //termsSubDictionary.Add(AIScriptDef._GM_CanSetBandit, TermGM_CanSetBandit);
        }

        /*bool TermIsFrontTarget(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            if (_target == null)
            {
                return false;
            }
            ProfileChecker.BeginSample("_IsFrontTarget");

            // 自分から見てtaegetがいる方向の判定
            Vector3 front = character.cacheTransform.forward;
            Vector3 dir = _target.cacheTransform.position - character.cacheTransform.position;
            float angle = Vector3.Angle(front, dir);

            bool isSuccess = false;
            if (compare == TermsCompare.eCompare.none)
            {
                isSuccess = angle < 90.0f;
            }
            else
            {
                float tmp = 0.0f;
                if (strDt.Length >= 2 && strDt[1].valType != AIScriptValue.ValueType.String)
                {
                    tmp = strDt[1].valFloat * 0.5f;
                }
                isSuccess = TermsCompare.Compare(compare, angle, Mathf.Clamp(tmp, 0.0f, 180.0f));
            }
            ProfileChecker.EndSample();

            return isSuccess;
        }

        bool TermIsBreakRecovery(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            return false;
            //bool isSuccess = false;

            //ProfileChecker.BeginSample("_IsBreakRecovery");

            //EnemyCharacter enemy = character as EnemyCharacter;
            ////ブレイク復調判定
            //if (enemy != null)
            //{
            //    if (compare == TermsCompare.eCompare.none)
            //    {
            //        isSuccess = enemy.IsBreakRecovery;
            //    }
            //    else
            //    {
            //        Debug.Assert(strDt[1].valType == AIScriptValue.ValueType.Int);
            //        isSuccess = TermsCompare.Compare(compare, (enemy.IsBreakRecovery ? 1 : 0), strDt[1].valInt);
            //    }
            //}

            //ProfileChecker.EndSample();

            //return isSuccess;
        }

        bool TermIsRage(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            return false;
            //bool isSuccess = false;

            //ProfileChecker.BeginSample("_IsRage");
            //EnemyCharacter enemy = character as EnemyCharacter;

            ////オーバードライブモード中かどうか判定
            //if (enemy != null)
            //{
            //    if (compare == TermsCompare.eCompare.none)
            //    {
            //        isSuccess = enemy.isOverdriveMode;
            //    }
            //    else
            //    {
            //        Debug.Assert(strDt[1].valType == AIScriptValue.ValueType.Int);
            //        isSuccess = TermsCompare.Compare(compare, (enemy.isOverdriveMode ? 1 : 0), strDt[1].valInt);
            //    }
            //}
            //ProfileChecker.EndSample();

            //return isSuccess;
        }

        //部位キャラクタが生きているか判定
        bool TermIsActiveParts(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            EnemyCharacter enemy = character as EnemyCharacter;
            if (enemy == null)
            {
                Debug.Assert(false, "AI Script Error [_isActiveParts] : キャラクタがエネミーでない");
                return true;
            }

            string[] cmd = strDt[0].splitValStrings;
            bool ret = false;

            //どれかでも生きていたらtrueを返す
            for (int i = 1; i < cmd.Length; ++i)
            {
                int idx;
                bool canParse = int.TryParse(cmd[i], out idx);
                if (!canParse || idx <= 0)
                {
                    Debug.Assert(false, "AI Script Error [_isActiveParts] : 引数が異常");
                    continue;
                }

                //子機登録順とインデクスはズレてる場合がある
                foreach (var parts in enemy.PartsCharacters)
                {
                    if (parts == null || parts.partsIdx != idx)
                    {
                        continue;
                    }

                    if (!parts.isDead)
                    {
                        ret |= true;
                        break;
                    }
                }
            }
            return ret;
        }

        //エネミーアビリティ発動判定
        bool TermIsActiveAbility(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            EnemyCharacter enemy = character as EnemyCharacter;
            if (enemy == null)
            {
                Debug.Assert(false, "AI Script Error [_isActiveParts] : キャラクタがエネミーでない");
                return false;
            }

            string[] cmd = strDt[0].splitValStrings;

            int abilityId;
            bool canParse = int.TryParse(cmd[1], out abilityId);
            Debug.Assert(canParse && abilityId > 0, "AI Script Error [_isActiveAbility] : 引数が異常");

            if (enemy.enemyAbility != null)
            {
                return enemy.enemyAbility.IsActive(abilityId);
            }
            return false;
        }

        //特殊モードフラグが立っているかどうか判定
        bool TermIsSpecialMode(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            EnemyCharacter enemy = character as EnemyCharacter;
            if (enemy == null)
            {
                Debug.Assert(false, "AI Script Error [_isSpecialMode] : キャラクタがエネミーでない");
                return false;
            }

            bool isSuccess = false;

            if (compare == TermsCompare.eCompare.none)
            {
                isSuccess = enemy.isAiSpecialMode;
            }
            else
            {
                Debug.Assert(strDt[1].valType == AIScriptValue.ValueType.Int);
                isSuccess = TermsCompare.Compare(compare, (enemy.isAiSpecialMode ? 1 : 0), strDt[1].valInt);
            }
            return isSuccess;
        }

        //ゲームマスターAI用：ターン実行可能判定
        bool TermGM_CanSetTurn(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            EnemyCharacter enemy = character as EnemyCharacter;
            if (enemy == null)
            {
                Debug.Assert(false, "AI Script Error [_GM_canSetTurn] : キャラクタがエネミーでない");
                return false;
            }
            GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            if (gm == null)
            {
                Debug.Assert(false, "AI Script Error [_GM_canSetTurn] : キャラクタがゲームマスターAIでない");
                return false;
            }

            return gm.CanSetTurn();
        }

        //ゲームマスターAI用：突発イベント実行可能判定
        bool TermGM_CanSetSuddenEvent(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            EnemyCharacter enemy = character as EnemyCharacter;
            if (enemy == null)
            {
                Debug.Assert(false, "AI Script Error [_GM_canSetSuddenEvent] : キャラクタがエネミーでない");
                return false;
            }
            GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            if (gm == null)
            {
                Debug.Assert(false, "AI Script Error [_GM_canSetSuddenEvent] : キャラクタがゲームマスターAIでない");
                return false;
            }

            return gm.CanSetSyncEvent();
        }

        //ゲームマスターAI用：山賊イベント実行可能判定
        bool TermGM_CanSetBandit(CharacterBase character, AIScriptValue[] strDt, TermsCompare.eCompare compare)
        {
            EnemyCharacter enemy = character as EnemyCharacter;
            if (enemy == null)
            {
                Debug.Assert(false, "AI Script Error [_GM_canSetBandit] : キャラクタがエネミーでない");
                return false;
            }
            GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            if (gm == null)
            {
                Debug.Assert(false, "AI Script Error [_GM_canSetBandit] : キャラクタがゲームマスターAIでない");
                return false;
            }

            return gm.CanSetBanditEvent();
        }
        
        */

        /// <summary>
        /// 条件判定処理
        /// "valu<100"な文字列
        /// </summary>
        /// <param name="termsStrs">条件判定文字列</param>
        /// <returns></returns>
        private bool IsTerms(ref AIScriptParam.Column[] termsStrs)
        {
            //CharacterBase character = _enemyCtrl.character;
            for (int i = 0; i < termsStrs.Length; ++i)
            {
                bool isSuccess = true;
                AIScriptValue[] strDt = termsStrs[i].values;
                if (strDt != null)
                {
                    //条件情報あり
                    TermsCompare.eCompare compare = termsStrs[i].compare;
                    //TermSubDelegate termSubDelegate;

                    ProfileChecker.BeginSample("AIScript.IsTerms,switch");

                    string[] cmd = strDt[0].splitValStrings;

                    //if (termsSubDictionary.TryGetValue(cmd[0], out termSubDelegate))
                    //{
                    //    isSuccess = termSubDelegate(character, strDt, compare);
                    //}
                    //else 
                    if (strDt.Length == 2)
                    {
                        ProfileChecker.BeginSample("default");
                        {
                            //AI用ローカル変数
                            AIScriptValue locValue0 = strDt[0];
                            AIScriptValue locValue1 = strDt[1];

                            if (IsScriptVariable(strDt[0].valString))
                            {
                                locValue0 = GetScriptVariable(strDt[0].valString);
                            }
                            if (IsScriptVariable(strDt[1].valString))
                            {
                                locValue1 = GetScriptVariable(strDt[1].valString);
                            }
                            // 文字列か floatで判定を行う
                            if (locValue0.Type == AIScriptValue.ValueType.String)
                            {
                                isSuccess = TermsCompare.Compare(compare, locValue0.valString, locValue1.valString);
                            }
                            else if (locValue0.Type == AIScriptValue.ValueType.Int)
                            {
                                isSuccess = TermsCompare.Compare(compare, locValue0.valInt, locValue1.valInt);
                            }
                            else if (locValue0.Type == AIScriptValue.ValueType.Float)
                            {
                                isSuccess = TermsCompare.Compare(compare, locValue0.valFloat, locValue1.valFloat);
                            }

                        }
                        ProfileChecker.EndSample();
                    }
                    ProfileChecker.EndSample();

                    if (!isSuccess)
                    {
                        return false;
                    }
                }
            }
            //　ここに来ることはすべての条件が有効
            return true;
        }


        /// <summary>
        /// スクリプト制御実行
        /// </summary>
        /// <param name="scripts"></param>
        private void ExecScript(List<AIScriptContainer> scripts)
        {
            ProfileChecker.BeginSample("AIScript.ExecScript");

            for (int step = 0; step < scripts.Count;)
            {
                AIScriptContainer script = scripts[step];
#if UNITY_EDITOR && GLUON_DEBUG
                //実行コマンドログ保持
                if (DebugAIScript._isShow)
                {
                    // debug用実行command log 保持        
                    string tabStr = "";
                    for (int i = 0; i < _tabCnt; ++i)
                    {
                        tabStr += "    ";
                    }
                    _log[1].command.Add(tabStr + script.ToString());
                }
#endif

#if UNITY_EDITOR && GLUON_DEBUG
                ProfileChecker.BeginSample(AIScriptDef.commandNames[(int)script._command]);
#endif
                step += _delegateScriptCommand[(int)script._command](ref script);
#if UNITY_EDITOR && GLUON_DEBUG
                ProfileChecker.EndSample();
#endif
                //強制スクリプト終了
                if (_isEndScript)
                {
                    break;
                }
            }

            ProfileChecker.EndSample();
        }



        /// <summary>
        /// スクリプト変数判定
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool IsScriptVariable(string name)
        {
            return _context.ContainsLocalValue(name);
        }
        /// <summary>
        /// スクリプト変数の取得
        /// </summary>
        /// <param name="name">変数名</param>
        /// <returns></returns>
        private AIScriptValue GetScriptVariable(string name)
        {
            ProfileChecker.BeginSample("AIScript.GetScriptVariable");

            AIScriptValue result = null;
            if (!IsScriptVariable(name))
            {
                ProfileChecker.BeginSample("AIScript.GetScriptVariable.new AIScriptValue");
                //新規変数生成
                _context.AddLocalValue(name, new AIScriptValue(0.0f));
                ProfileChecker.EndSample();
            }

            result = _context.GetLocalValue(name);
            switch (name)
            {
                // ゲーム時間の更新
                case AIScriptDef._GameTime:
                    {
                        //if (MainGameCtrl.instance)
                        //{
                        //    result.SetValue(MainGameCtrl.instance.GetGamePlayTime());
                        //}
                    }
                    break;

                //起床経過時間の更新
                case AIScriptDef._AwakeTime:
                    {
                        result.SetValue(_context.awakeTime);
                    }
                    break;

                //クエスト制限時間の更新
                case AIScriptDef._QuestTimeLimit:
                    {
                        //result.SetValue(MainGameCtrl.instance.QuestFailureTimeElapsed);
                    }
                    break;

                //HP率の更新
                case AIScriptDef._HpRate:
                    {
                        //result.SetValue(_enemyCtrl.character.hpRate);
                    }
                    break;

                //ターゲットのHP率
                case AIScriptDef._TrgHpRate:
                    {
                        result.SetValue(_context.trgHpRate);
                    }
                    break;

                // HP率の記録
                case AIScriptDef._RecHpRate:
                    {
                        result.SetValue(_context.hpRecode);
                    }
                    break;

                //計測タイマー更新
                case AIScriptDef._CountTime:
                    {
                        result.SetValue(_context.countTime);
                    }
                    break;
                case AIScriptDef._TrgLength:
                    {
                        result.SetValue(_context.trgLength);
                    }
                    break;
                case AIScriptDef._IsSearchAction:
                    {
                        result.SetValue(_context.isSearchAction.ToString());
                    }
                    break;

                // 初期ポップ位置からの距離
                case AIScriptDef._PivotLength:
                    {
                        //result.SetValue((_enemyCtrl.transform.position - _enemyCtrl.character.pivotBase).magnitude);
                    }
                    break;

                //間接攻撃ダメージ回数
                case AIScriptDef._IndirectDmgCnt:
                    {
                        //result.SetValue(_enemyCtrl.character.GetDamageHateCounter(EnemyCharacter.DamageHateType.Indirect));
                    }
                    break;

                //直接攻撃ダメージ回数
                case AIScriptDef._DirectDmgCnt:
                    {
                        //result.SetValue(_enemyCtrl.character.GetDamageHateCounter(EnemyCharacter.DamageHateType.Direct));
                    }
                    break;

                //最後に実行したアクションのID
                case AIScriptDef._LastActionId:
                    {
                        //result.SetValue(_enemyCtrl.character.lastActionId);
                    }
                    break;
                //クエストの難易度
                case AIScriptDef._QuesetDifficulty:
                    {
                        //result.SetValue(EnemyCharacter.GetQuesetDifficulty());
                    }
                    break;
                //起動中のマーカー数
                case AIScriptDef._MarkerCnt:
                    {
                        //result.SetValue(ChargeMarkerManager.instance.GetActiveMarkerNum);
                    }
                    break;
                //EnemyParamのID
                case AIScriptDef._ParamId:
                    {
                        //result.SetValue(_enemyCtrl.character.ParamElm.Id);
                    }
                    break;
                //ActionSetのID
                case AIScriptDef._ActionSetId:
                    {
                        //result.SetValue(_enemyCtrl.character.ParamElm.ActionSet);
                    }
                    break;
                //ゲームマスター専用：現在のターン数
                case AIScriptDef._GM_TurnCnt:
                    {
                        //GameMasterUniqueCtrl gm = EnemyManage.FindGameMaster();
                        //result.SetValue((gm != null ? gm.turnCount : 0));
                    }
                    break;
                default:
                    {
                    }
                    break;
            }

            ProfileChecker.EndSample();

            return result;
        }

        /// <summary>
        /// if処理
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandIf(ref AIScriptContainer script)
        {
            // 変数名+比較+判定情報の文字列でくる
            if (IsTerms(ref script._params[0].columns))
            {
#if UNITY_EDITOR && GLUON_DEBUG 
                ++_tabCnt;
#endif
                return AIScriptContainer._outStep;
            }
            return script._jumpStep;
        }
        /// <summary>
        /// elseif処理
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandElseIf(ref AIScriptContainer script)
        {
            return ScriptCommandIf(ref script);
        }
        /// <summary>
        /// else処理
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandElse(ref AIScriptContainer script)
        {
#if UNITY_EDITOR && GLUON_DEBUG
            ++_tabCnt;
#endif
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// if終了命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandEndIf(ref AIScriptContainer script)
        {
#if UNITY_EDITOR && GLUON_DEBUG
            --_tabCnt;
            _tabCnt = Mathf.Max(0, _tabCnt);
#endif
            return AIScriptContainer._outStep;
        }


        /// <summary>
        /// 変数登録命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandSet(ref AIScriptContainer script)
        {
            //変数設定
            AIScriptValue scriptValue = GetScriptVariable(script._params[0].ValueString);

            if (IsScriptVariable(script._params[1].ValueString))
            {
                //既存変数からの値を設定
                scriptValue.SetValue(GetScriptVariable(script._params[1].ValueString));
            }
            else
            {
                //直接登録
                scriptValue.SetValue(script._params[1].Value);
            }
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// 加算命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandAdd(ref AIScriptContainer script)
        {
            //格納変数取得
            AIScriptValue scriptValue = GetScriptVariable(script._params[0].ValueString);

            //変数加算
            if (script._params[1].IsValueString)
            {
                scriptValue.AddValue(GetScriptVariable(script._params[1].ValueString));
            }
            else
            {
                scriptValue.AddValue(script._params[1].Value);
            }
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// 減算命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandSub(ref AIScriptContainer script)
        {
            //格納変数取得
            AIScriptValue scriptValue = GetScriptVariable(script._params[0].ValueString);

            //変数減算
            if (script._params[1].IsValueString)
            {
                scriptValue.SubValue(GetScriptVariable(script._params[1].ValueString));
            }
            else
            {
                scriptValue.SubValue(script._params[1].Value);
            }
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// 乗算命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandMul(ref AIScriptContainer script)
        {
            //格納変数取得
            AIScriptValue scriptValue = GetScriptVariable(script._params[0].ValueString);

            //変数乗算
            if (script._params[1].IsValueString)
            {
                scriptValue.MulValue(GetScriptVariable(script._params[1].ValueString));
            }
            else
            {
                scriptValue.MulValue(script._params[1].Value);
            }
            return AIScriptContainer._outStep;
        }

        /// <summary>
        /// ターゲット指定命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandSetTarget(ref AIScriptContainer script)
        {
            return 0;
            //// アクションターゲット設定
            //int type = (int)script._params[0].Value.valInt;
            //int subType = (int)ActionTarget.NONE;
            //// 2つ以上の設定をサポートするなら
            //if (script._params.Length > 1)
            //{
            //    // 2番目の指定を取得
            //    subType = (int)script._params[1].Value.valInt;
            //}
            //_enemyCtrl.SetActionTarget((ActionTarget)type, (ActionTarget)subType);

            ////ターゲットの距離計算
            //_context.trgLength = 0.0f;
            //_target = _enemyCtrl.character.targetObject;
            //if (_target != null)
            //{
            //    _context.trgLength = (_target.cacheTransform.position - _enemyCtrl.character.cacheTransform.position).magnitude;
            //    // ターゲットのHP率保持
            //    _context.trgHpRate = (_target as CharacterBase).hpRate;
            //}
            //return AIScriptContainer._outStep;
        }
        /// <summary>
        /// アクション実行命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandAction(ref AIScriptContainer script)
        {
            // アクション設定
            int actionId = 0;
            if (_context.ContainsLocalValue(script._params[0].ValueString))
            {
                //変数からの変換
                actionId = (int)GetScriptVariable(script._params[0].ValueString).valInt;
            }
            else
            {
                actionId = (int)script._params[0].Value.valInt;
            }
            //_enemyCtrl.SetAIAction(actionId);
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// スクリプト終了処理命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandEndScript(ref AIScriptContainer script)
        {
            _isEndScript = true;
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// スクリプト関数の実行
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandFunction(ref AIScriptContainer script)
        {
            List<AIScriptContainer> funcScript = _script.GetScriptFunction(script._params[0].ValueString);
            ExecScript(funcScript);
            return AIScriptContainer._outStep;
        }

        /// <summary>
        /// 現在のHP比率保持コマンド
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandRecodeHpRate(ref AIScriptContainer script)
        {
            //_context.hpRecode = _enemyCtrl.character.hpRate;
            return AIScriptContainer._outStep;
        }

        /// <summary>
        /// タイマー計測コマンド
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandRecodeTime(ref AIScriptContainer script)
        {
            if (script._params[0].ValueString.Contains("true"))
            {
                _context.isCountTime = true;
                _context.countTime = 0;
            }
            else
            {
                _context.isCountTime = false;
            }
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// 移動行動コマンド
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadMoveAction(ref AIScriptContainer script)
        {
            float dist = (script._params.Length > 1) ? script._params[1].Value.valFloat : 0.0f;

            Vector3 offset = Vector3.zero;
            if (script._params.Length > 3)
            {
                offset.x = script._params[2].Value.valFloat;
                offset.z = script._params[3].Value.valFloat;
            }

            //_enemyCtrl.SetAIMoveAction(script._params[0].Value.valInt, offset, dist);
            return AIScriptContainer._outStep;
        }

        /// <summary>
        /// 振り向き行動コマンド
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadTurnAction(ref AIScriptContainer script)
        {
            //_enemyCtrl.SetAITurnAction((int)script._params[0].Value.valInt);
            return AIScriptContainer._outStep;
        }

        /// <summary>
        /// ランダム値取得コマンド
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandRandom(ref AIScriptContainer script)
        {
            /*
            _params[0]...保持変数名
            _params[1]...最小値
            _params[2]...最大値
            */
            //格納変数取得
            AIScriptValue scriptValue = GetScriptVariable(script._params[0].ValueString);

            float value = 0.0f;
            if (script._params.Length > 2)
            {
                //最大最小値のランダム
                value = Random.Range((float)script._params[1].Value.valFloat, (float)script._params[2].Value.valFloat);
            }
            else
            {
                value = Random.value * (float)script._params[1].Value.valFloat;
            }
            scriptValue.SetValue(value);
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// 生存数の取得命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandAliveNum(ref AIScriptContainer script)
        {
            AIScriptDef.eTargetType targetType = (AIScriptDef.eTargetType)(script._params[0].Value.valInt);

            //格納変数取得
            AIScriptValue scriptValue = GetScriptVariable(script._params[1].ValueString);
            switch (targetType)
            {
                case AIScriptDef.eTargetType.ally:
                    {
                        //味方生存数取得
                        //scriptValue.SetValue(EnemyManage.instance.GetAliveNum());
                    }
                    break;
                case AIScriptDef.eTargetType.allyChild:
                    {
                        //子機生存数の取得
                        //int num = 0;
                        //for (int i = 0; i < _enemyCtrl.character._subCharacters.Count; ++i)
                        //{
                        //    CharacterBase child = _enemyCtrl.character._subCharacters[i];
                        //    if (child != null && !child.isDead && child.gameObject.activeInHierarchy)
                        //    {
                        //        ++num;
                        //    }
                        //}
                        //scriptValue.SetValue(num);
                    }
                    break;
                case AIScriptDef.eTargetType.hostile:
                    {
                        //敵生存数の取得
                        //scriptValue.SetValue(MainGameCtrl.instance.charaManager.GetAliveCharacters().Count);
                    }
                    break;
                case AIScriptDef.eTargetType.minion:
                    {
                        //scriptValue.SetValue(EnemyManage.instance.GetMinionAliveNum(_enemyCtrl.character));
                    }
                    break;
                //ゲームマスター用ターンで排出した残存エネミー
                case AIScriptDef.eTargetType.gm_turn:
                    {
                        //GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
                        //Debug.Assert(gm != null, "AI Script Error [AliveNum] : キャラクタがゲームマスターAIでない");

                        //int num = 0;
                        //if (gm != null)
                        //{
                        //    num = gm.GetAliveFromGM(GameMasterUniqueCtrl.EventType.TurnPop);
                        //}
                        //scriptValue.SetValue(num);
                    }
                    break;

            }
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// 内部制御用スクリプトStepJump命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandJump(ref AIScriptContainer script)
        {
            return script._jumpStep;
        }
        /// <summary>
        /// エネミー目覚め命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandWake(ref AIScriptContainer script)
        {
            //_enemyCtrl.SetAwake();
            return AIScriptContainer._outStep;
        }
        /// <summary>
        /// ダメージヘイトカウンターリセット命令
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandClearDamageCounter(ref AIScriptContainer script)
        {
            //_enemyCtrl.character.InitDamageHateCounter();
            return AIScriptContainer._outStep;
        }

        /// <summary>
        /// 特殊姿勢中か否かコマンド
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadUnusualPosture(ref AIScriptContainer script)
        {
            return 0;
            //bool oldValue = _enemyCtrl.character.isUnusualPosture;
            //_enemyCtrl.character.isUnusualPosture = (script._params[0].ValueString.Contains("true"));
            //if (MultiPlayManager.instance != null && MultiPlayManager.instance.IsMultiPlayHost)
            //{
            //    if (_enemyCtrl.character.IsNetworkSynchronizedEnemy && _enemyCtrl.character.isUnusualPosture != oldValue)
            //    {
            //        // マルチプレイ中、ボスの値が変更されたら、同期する
            //        _tmpChangeBossState.type = (byte)Gluon.Event.CharacterSpecialStateType.UnusualPosture;
            //        _tmpChangeBossState.target = _enemyCtrl.character.multiPlayId;
            //        _tmpChangeBossState.value = _enemyCtrl.character.isUnusualPosture ? 1 : 0;
            //        PhotonManager.instance.Client.RaiseEventToOthers(Common.EventCode.CharacterSpecialState, _tmpChangeBossState);
            //    }
            //}
            //return AIScriptContainer._outStep;
        }

        /// <summary>
        /// マスターActionSetから指定カラムのアクションIDを取得
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadFromActionSet(ref AIScriptContainer script)
        {
            return 0;
            //EnemyCharacter owner = _enemyCtrl.character;

            //string columunName = script._params[0].Value.valString;
            ////頭文字大文字化
            //columunName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(columunName);

            //PropertyInfo pi = owner.ActionSetElm.GetType().GetProperty(columunName);
            //if (pi != null)
            //{
            //    //格納変数取得
            //    AIScriptValue aiVal = GetScriptVariable(script._params[1].ValueString);

            //    int actionId = (int)pi.GetValue(owner.ActionSetElm, null);
            //    Debug.Assert(actionId != 0,
            //                 "AIコマンド：FromActionSetで指定したアクションセットのカラム[" + columunName + "]が空です");

            //    aiVal.SetValue(actionId);
            //}
            //else
            //{
            //    Debug.Assert(false,
            //                 "AIコマンド：FromActionSetで指定したアクションセットのカラム名[" + columunName + "]が不正です");
            //}

            //return AIScriptContainer._outStep;
        }

        /// <summary>
        /// プレイヤーキャラクタを距離の近い順に登録保存
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandOrderCloser(ref AIScriptContainer script)
        {
            //_enemyCtrl.RegisterOrderCloser();

            return AIScriptContainer._outStep;
        }

        /// <summary>
        /// 生きているプレイヤーキャラクタを距離の遠い順に登録保存
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommandOrderAliveFather(ref AIScriptContainer script)
        {
            //_enemyCtrl.RegisterOrderAliveFarther();

            return AIScriptContainer._outStep;
        }


        /// <summary>
        /// ゲームマスター専用：ターン設定
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadGM_SetTurnEvent(ref AIScriptContainer script)
        {
            return 0;
            //GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            //Debug.Assert(gm != null, "AI Script Error [GM_SetTurnEvent] : キャラクタがゲームマスターAIでない");

            ////ポップしたいエネミージェネレータのラベル
            //string eg_label = script._params[0].Value.valString;

            //gm.SetTempEvent(GameMasterUniqueCtrl.EventType.TurnPop, eg_label);

            //return AIScriptContainer._outStep;
        }

        /// <summary>
        /// ゲームマスター専用：ターンイベント完了
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadGM_CompleteTurnEvent(ref AIScriptContainer script)
        {
            return 0;
            //GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            //Debug.Assert(gm != null, "AI Script Error [GM_CompleteTurnEvent] : キャラクタがゲームマスターAIでない");

            //gm.CompleateTurnEventTemporary();

            //return AIScriptContainer._outStep;
        }

        /// <summary>
        /// ゲームマスター専用：最大ターン数の設定
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadGM_SetTurnMax(ref AIScriptContainer script)
        {
            return 0;
            //GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            //Debug.Assert(gm != null, "AI Script Error [GM_SetTurnMax] : キャラクタがゲームマスターAIでない");

            //if (!gm.hasSetTurnMax)
            //{
            //    gm.SetTurnMax(script._params[0].Value.valInt);
            //}
            //else
            //{
            //    Debug.Assert(false, "AI Script Error [GM_SetTurnMax] : ゲームマスターAIがターン最大数を設定できるのは一回だけです");
            //}

            //return AIScriptContainer._outStep;
        }

        /// <summary>
        /// ゲームマスター専用：突発イベント設定
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadGM_SetSuddenEvent(ref AIScriptContainer script)
        {
            return 0;
            //GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            //Debug.Assert(gm != null, "AI Script Error [GM_SetSuddenEvent] : キャラクタがゲームマスターAIでない");

            ////ポップしたいエネミージェネレータのラベル
            //string eg_label = script._params[0].Value.valString;

            //gm.SetTempEvent(GameMasterUniqueCtrl.EventType.Sudden, eg_label);

            //return AIScriptContainer._outStep;
        }

        /// <summary>
        /// ゲームマスター専用：山賊イベント設定
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ScriptCommnadGM_SetBanditEvent(ref AIScriptContainer script)
        {
            return 0;
            //GameMasterUniqueCtrl gm = _enemyCtrl.GetComponent<GameMasterUniqueCtrl>();
            //Debug.Assert(gm != null, "AI Script Error [GM_SetBanditEvent] : キャラクタがゲームマスターAIでない");

            ////ポップしたいエネミージェネレータのラベル
            //string eg_label = script._params[0].Value.valString;

            //gm.SetTempEvent(GameMasterUniqueCtrl.EventType.Bandit, eg_label);

            //return AIScriptContainer._outStep;
        }

        /// <summary>
        /// コンテキストをロード
        /// </summary>
        /// <param name="context"></param>
        public void LoadContext(AIScriptContext context)
        {
            _context.CopyTo(context);
        }

        /// <summary>
        /// コンテキストをストア
        /// </summary>
        /// <param name="context"></param>
        public void StoreContext(AIScriptContext context)
        {
            if (context == null)
            {
                return;
            }

            context.CopyTo(_context);
        }

        /// <summary>
        /// Initialize後のActionを設定
        /// </summary>
        /// <param name="action"></param>
        public void SetPostInitializeAction(System.Action action)
        {
            if (gameObject.activeInHierarchy)
            {
                // 既にアクティブな場合、Awake処理は完了しているはずなので即座に処理
                action();
            }
            else
            {
                // 非アクティブの場合はAwake後に処理されるように保持
                _postInitializeAction = action;
            }
        }

        /// <summary>
        /// _awakeTimeを0にリセット
        /// </summary>
        public void ResetAwakeTime()
        {
            _context.awakeTime = 0;
        }

        /// <summary>
        /// AI系変数の全リセット
        /// </summary>
        public void ResetValue()
        {
            //システム変数のクリア
            _context.awakeTime = 0;
            _context.hpRecode = 0f;
            //↓意図的にフラグは下ろさない　※def initで「recTimer true」にしていた場合に下ろされると困るので
            //_context.isCountTime = false;
            _context.countTime = 0f;    //カウントはリセット
            _context.trgLength = 0f;
            _context.trgHpRate = 0f;
            _context.isSearchAction = false;

            //スクリプト内リセット関数呼ぶ
            if (_script != null && _script.IsScriptFunction(AIScriptDef.Reset))
            {
                UpdateAction(_script.GetScriptFunction(AIScriptDef.Reset));
            }
        }
    }
}