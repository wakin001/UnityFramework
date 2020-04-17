namespace Framework
{
    /// <summary>スクリプトコマンド</summary>
    static public class AIScriptDef
    {
        // パラメーター
        public const string TARGET = "target";

        // 固定変数
        public const string _HpRate = "_hpRate";                //HP比率(0.0-1.0)
        public const string _TrgHpRate = "_trgHpRate";          //ターゲットのHP比率
        public const string _TrgLength = "_trgLength";          //ターゲットの距離
        public const string _IsFrontTarget = "_isFrontTarget";  //ターゲットが前にいるか
        public const string _GameTime = "_gameTime";            //ゲーム時間
        public const string _AwakeTime = "_awakeTime";          //目覚め時間
        public const string _QuestTimeLimit = "_questTimeLimit";//クエスト制限時間
        public const string _RecHpRate = "_recHpRate";          //HP比率記録
        public const string _CountTime = "_countTime";          //タイマーカウント
        public const string _IsSearchAction = "_isSearchAction";//索敵アクション有効
        public const string _PivotLength = "_pivotLength";      //popPivot距離
        public const string _DirectDmgCnt = "_directDmgCnt";    //直接攻撃ダメージ回数
        public const string _IndirectDmgCnt = "_indirectDmgCnt";//間接攻撃ダメージ回数
        public const string _IsBreakRecovery = "_isBreakRecovery";  //ブレイク復調判定
        public const string _LastActionId = "_lastActionId";    //最後に実行したアクションのID
        public const string _IsRage = "_isRage";                //怒り中判定
        public const string _QuesetDifficulty = "_quesetDifficulty";//難易度判定
        public const string _MarkerCnt = "_markerCnt";          //起動中のマーカー数
        public const string _IsActiveParts = "_isActiveParts";  //部位キャラクタが生きている判定
        public const string _ParamId = "_paramId";              //EnemyParamのID
        public const string _ActionSetId = "_actionSetId";      //ActionSetのID
        public const string _IsActiveAbility = "_isActiveAbility";  //エネミーアビリティ発動判定
        public const string _IsSpecialMode = "_isSpecialMode";  //特殊モード判定

        //ゲームマスターAI用
        public const string _GM_CanSetTurn = "_GM_canSetTurn";      //ターン実行可能判定
        public const string _GM_TurnCnt = "_GM_turnCnt";            //現在のターン数
        public const string _GM_CanSetSuddenEvent = "_GM_canSetSuddenEvent";  //突発イベント実行可能判定
        public const string _GM_CanSetBandit = "_GM_canSetBandit";  //山賊イベント実行可能判定

        // function
        public const string Init = "init";
        public const string Reset = "reset";
        public const string UpdateWake = "updateWake";
        public const string UpdateAttack = "updateAttack";
        public const string UpdateIdle = "updateIdle";


        //↓↓enum順変更不可
        public enum Command
        {
            Def,
            EndDef,
            If,
            Else,
            ElseIF,
            EndIf,
            Set,
            Add,
            Sub,
            SetTarget,          //action target setting
            EndScript,
            Action,
            Function,           //関数実行
            MoveAction,         //アクション移動命令
            TurnAction,         //アクション回転命令
            Random,
            RecTimer,           //タイマー測定
            RecHpRate,          //HP率記録
            AliveNum,           //生存数
            Jump,               //内部制御用
            Wake,
            ClearDmgCnt,        //ダメージカウンター初期化
            UnusualPosture,     //特殊姿勢
            FromActionSet,      //マスターActionSetから指定カラムのアクションIDを取得
            GM_SetTurnEvent,    //ゲームマスター専用：ターンの設定
            GM_CompleteTurnEvent,    //ゲームマスター専用：全てのターン終了
            GM_SetTurnMax,      //ゲームマスター専用：最大ターン数の設定
            GM_SetSuddenEvent,  //ゲームマスター専用：突発イベントの設定
            GM_SetBanditEvent,  //ゲームマスター専用：山賊イベントの設定
            Mul,
            OrderCloser,        //距離の近い順にプレイヤーキャラクタを登録
            OrderAliveFather,   //生きているプレイヤーキャラクタを遠い順に登録
            Reserve06,
            Reserve07,
            Reserve08,
            Reserve09,
            Reserve10,
        };
        //↑↑enum順変更不可

#if UNITY_EDITOR || PROFILE
        public static readonly string[] commandNames = System.Enum.GetNames(typeof(Command));
#endif

        //移動アクションタイプ
        public enum eMoveAction
        {
            none,
            approch,
            escape,
            escapeTL,           //escape target look
            pivot,
            anchor,
        };
        //回転アクションタイプ
        public enum eTurnAction
        {
            none,
            target,
            warldCenter,
            north,
            east,
            south,
            west,
            pivot,
            anchor,
        };
        // ターゲットタイプ
        public enum eTargetType
        {
            ally,       //味方（つまりエネミー）
            hostile,    //敵対（つまりプレイヤー）
            allyChild,  //エネミー子機
            minion,     //初期配置、およびCallSummonで配置されたエネミー
            gm_turn,    //ゲームマスター用ターンで排出したエネミー
        }
    }
}
