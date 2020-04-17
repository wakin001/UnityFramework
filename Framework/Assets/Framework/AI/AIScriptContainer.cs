using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// AIスクリプトコマンド保持クラス
    /// </summary>
    [System.Serializable]
    public class AIScriptContainer
    {
        public AIScriptDef.Command _command;
        public AIScriptParam[] _params;        //コマンド変数
        public int _jumpStep = 1;           //if命令によるStep実行処理
        [NonSerialized]
        public const int _outStep = 1;    //次のコマンドステップ

#if UNITY_EDITOR
        /// <summary> デバッグ表示用script line 文字列　</summary>
        public override string ToString()
        {
            string str = _command.ToString() + " ";
            if (_params != null)
            {
                for (int i = 0; i < _params.Length; ++i)
                {
                    str += _params[i].ToString() + ", ";
                }
            }
            return str;
        }
        public AIScriptContainer(string str)
        {
            Parser(str);
        }

        /// <summary>
        /// script line解析
        /// コマンド,制御命令で分離
        /// 制御命令は","で判定分離しておく
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private void Parser(string str)
        {
            string[] column = str.Split(' ');
            try
            {
                _command = (AIScriptDef.Command)System.Enum.Parse(typeof(AIScriptDef.Command), column[0], true);
            }
            catch
            {
                Debug.Assert(false, "-----Error No Script Function " + column[0]);
            }
            //制御命令あり
            int idx = str.IndexOf(' ');
            if (idx != -1)
            {
                string[] valuesStr = str.Substring(idx + 1).Split(',');
                _params = new AIScriptParam[valuesStr.Length];
                for (int i = 0; i < valuesStr.Length; ++i)
                {
                    AIScriptParam valObj = new AIScriptParam();
                    valObj.Parser(valuesStr[i]);
                    _params[i] = valObj;
                }
            }
            _jumpStep = 1;
        }
#endif
    }
    /// <summary>
    /// script用変数class
    /// </summary>
    [System.Serializable]
    public class AIScriptValue
    {
        public string valString;
        public int valInt;              // 主にactionID情報管理用
        public float valFloat;         //
        public ValueType valType;

        public string[] splitValStrings = null;

        public enum ValueType
        {
            String,
            Int,
            Float,
        }
        public ValueType Type { get { return valType; } }

        //コンストラクタ
        public AIScriptValue(string str)
        {
            SetValue(str, true);
        }
        public AIScriptValue(int val)
        {
            valString = "";
            valType = ValueType.Int;
            valInt = val;
            valFloat = val;
            splitValStrings = null;
        }
        public AIScriptValue(float val)
        {
            valString = "";
            valType = ValueType.Float;
            valInt = (int)val;
            valFloat = val;
            splitValStrings = null;
        }

        //セット
        public void SetValue(string str, bool isParser = false)
        {
            valString = str;
            valType = ValueType.String;
            if (isParser)
            {
                //memo:型解析（parseするとGCがでるので注意）
                if (int.TryParse(str, out valInt))
                {
                    valType = ValueType.Int;
                    valFloat = valInt;      //値によっては桁オーバーして情報ロストあり
                }
                else if (float.TryParse(str, out valFloat))
                {
                    valType = ValueType.Float;
                    valInt = (int)valFloat;
                }
            }
            splitValStrings = valString.Split(' ');
        }

        public void SetValue(int val)
        {
            valType = ValueType.Int;
            valFloat = val;
            valInt = val;
            valString = "";
            splitValStrings = null;
        }
        public void SetValue(float val)
        {
            valType = ValueType.Float;
            valFloat = val;
            valInt = (int)valFloat;
            valString = "";
            splitValStrings = null;
        }
        public void SetValue(AIScriptValue value)
        {
            valType = value.valType;
            valString = value.valString;
            valInt = value.valInt;
            valFloat = value.valFloat;
            if (valType == ValueType.String)
            {
                splitValStrings = valString.Split(' ');
            }
            else
            {
                splitValStrings = null;
            }
        }

        //加算
        public void AddValue(AIScriptValue value)
        {
            if (valType == ValueType.String)
            {
                return;
            }
            valFloat += value.valFloat;
            valInt += value.valInt;
        }

        //減算
        public void SubValue(AIScriptValue value)
        {
            if (valType == ValueType.String)
            {
                return;
            }
            valFloat -= value.valFloat;
            valInt -= value.valInt;
        }

        //乗算
        public void MulValue(AIScriptValue value)
        {
            if (valType == ValueType.String)
            {
                return;
            }
            valFloat *= value.valFloat;
            valInt *= value.valInt;
        }

        //文字列化
        public override string ToString()
        {
            switch (valType)
            {
                case ValueType.String:
                    return valString;
                case ValueType.Int:
                    return valInt.ToString();
                case ValueType.Float:
                    return valFloat.ToString();
            }
            return "";
        }
    }

    //-----------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------

    [System.Serializable]
    public class AIScriptParam
    {
        [System.Serializable]
        public class Column
        {
            public AIScriptValue[] values;
            public TermsCompare.eCompare compare;
#if UNITY_EDITOR
            public override string ToString()
            {
                if (values.Length == 1)
                {
                    // 単一情報
                    return values[0].ToString();
                }
                else
                {
                    // 比較情報
                    return values[0].ToString() + TermsCompare.CompareStr(compare) + values[1].ToString();
                }
            }
            public void CreateValue(string[] datas)
            {
                values = new AIScriptValue[datas.Length];
                for (int i = 0; i < datas.Length; ++i)
                {
                    values[i] = new AIScriptValue(datas[i]);
                }
            }
            public void CreateValue(string data)
            {
                values = new AIScriptValue[1];
                values[0] = new AIScriptValue(data);
            }

#endif
        }

        public Column[] columns;

        public string ValueString
        {
            get { return columns[0].values[0].valString; }
        }
        public AIScriptValue Value
        {
            get { return columns[0].values[0]; }
        }
        public bool IsValueString { get { return (columns[0].values[0].Type == AIScriptValue.ValueType.String); } }

#if UNITY_EDITOR
        public override string ToString()
        {
            string str = "";
            for (int i = 0; i < columns.Length; ++i)
            {
                if (i != 0)
                {
                    str += ":";
                }
                str += columns[i];
            }
            return str;
        }

        public void Parser(string str)
        {
            string[] column = str.Split(':');
            columns = new Column[column.Length];
            for (int i = 0; i < column.Length; ++i)
            {
                columns[i] = new Column();
                columns[i].compare = TermsCompare.CompareType(column[i]);
                if (columns[i].compare != TermsCompare.eCompare.none)
                {
                    //比較情報込の複数変数
                    columns[i].CreateValue(TermsCompare.TermsSplit(column[i]));
                }
                else
                {
                    //単一変数
                    columns[i].CreateValue(column[i].Trim());
                }
            }
        }

#endif
    }
}