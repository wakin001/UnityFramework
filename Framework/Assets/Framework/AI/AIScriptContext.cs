using UnityEngine;
using System.Collections.Generic;

namespace Framework
{
    public class AIScriptContext
    {
        public float awakeTime;         // 目覚めからの経過時間
        public float hpRecode;          //hp記録
        public bool isCountTime;        //計測タイマーon/off
        public float countTime;         //計測タイマー
        public float trgLength;         //ターゲットとの距離
        public float trgHpRate;         //ターゲットのHP率
        public bool isSearchAction;     //推測アクションが定義されたかflag

        private Dictionary<string, AIScriptValue> _localValueDic = new Dictionary<string, AIScriptValue>();

        public void AddLocalValue(string name, AIScriptValue value)
        {
            _localValueDic.Add(name, value);
        }

        public Dictionary<string, AIScriptValue>.KeyCollection AllLocalValueNames
        {
            get
            {
                return _localValueDic.Keys;
            }
        }

        public bool ContainsLocalValue(string name)
        {
            return _localValueDic.ContainsKey(name);
        }

        public AIScriptValue GetLocalValue(string name)
        {
            return _localValueDic[name];
        }

        public void ClearAllLocalValues()
        {
            _localValueDic.Clear();
        }

        public void CopyTo(AIScriptContext context)
        {
            if (context == null)
            {
                return;
            }

            context.awakeTime = awakeTime;
            context.hpRecode = hpRecode;
            context.isCountTime = isCountTime;
            context.countTime = countTime;
            context.trgLength = trgLength;
            context.trgHpRate = trgHpRate;
            context.isSearchAction = isSearchAction;

            foreach (var pair in _localValueDic)
            {
                if (!context.ContainsLocalValue(pair.Key))
                {
                    // 新規作成
                    var value = new AIScriptValue(string.Empty);
                    value.SetValue(pair.Value);
                    context.AddLocalValue(pair.Key, value);
                }
                else
                {
                    // 上書き
                    context.GetLocalValue(pair.Key).SetValue(pair.Value);
                }
            }
        }


        public static void EncodeDeltaCompression(DeltaCompressionEncoder encoder, AIScriptContext prev, AIScriptContext next, out byte[] result)
        {
            encoder.Clear();
            encoder.WriteFloat(prev == null ? (float?)null : prev.awakeTime, next.awakeTime);
            encoder.WriteFloat(prev == null ? (float?)null : prev.hpRecode, next.hpRecode);
            encoder.WriteBool(prev == null ? (bool?)null : prev.isCountTime, next.isCountTime);
            encoder.WriteFloat(prev == null ? (float?)null : prev.countTime, next.countTime);
            encoder.WriteFloat(prev == null ? (float?)null : prev.trgLength, next.trgLength);
            encoder.WriteFloat(prev == null ? (float?)null : prev.trgHpRate, next.trgHpRate);
            encoder.WriteBool(prev == null ? (bool?)null : prev.isSearchAction, next.isSearchAction);

            encoder.WriteInteger(prev == null ? 0 : prev._localValueDic.Count, next._localValueDic.Count);

            // 変数は追加されるのみなので、prevにあって、nextに格納されていない変数はない前提
            var e = next._localValueDic.GetEnumerator();
            while (e.MoveNext())
            {
                var variableName = e.Current.Key;
                var nextLocalValue = e.Current.Value;
                var prevLocalValue = (prev == null || !prev.ContainsLocalValue(variableName)) ? null : prev.GetLocalValue(variableName);

                // 変数名、型、値を書き込む。新規変数の場合は常に最新の状態を書き込む
                // 通常はなさそうだが、型が途中で変化することも念のため考慮している
                bool isNewVariable = prevLocalValue == null;
                isNewVariable |= encoder.WriteString(isNewVariable ? null : variableName, variableName);
                isNewVariable |= encoder.WriteByte(isNewVariable ? null : (byte?)prevLocalValue.valType, (byte)nextLocalValue.valType);

                switch (nextLocalValue.valType)
                {
                    case AIScriptValue.ValueType.Int:
                        encoder.WriteInteger(isNewVariable ? null : (int?)prevLocalValue.valInt, nextLocalValue.valInt);
                        break;
                    case AIScriptValue.ValueType.Float:
                        encoder.WriteFloat(isNewVariable ? null : (float?)prevLocalValue.valFloat, nextLocalValue.valFloat);
                        break;
                    case AIScriptValue.ValueType.String:
                        encoder.WriteString(isNewVariable ? null : prevLocalValue.valString, nextLocalValue.valString);
                        break;
                    default:
                        Debug.Assert(false, "AIScriptValueの型" + nextLocalValue.valType + "が未対応です");
                        break;
                }
            }

            result = encoder.GetBinary();
        }

        public static bool DecodeDeltaCompression(DeltaCompressionDecoder decoder, AIScriptContext prev, byte[] nextBinary, AIScriptContext outNext)
        {
            decoder.SetBinary(nextBinary);
            if (!decoder.ReadFloat(prev == null ? (float?)null : prev.awakeTime, out outNext.awakeTime))
            {
                return false;
            }
            if (!decoder.ReadFloat(prev == null ? (float?)null : prev.hpRecode, out outNext.hpRecode))
            {
                return false;
            }
            if (!decoder.ReadBool(prev == null ? (bool?)null : prev.isCountTime, out outNext.isCountTime))
            {
                return false;
            }
            if (!decoder.ReadFloat(prev == null ? (float?)null : prev.countTime, out outNext.countTime))
            {
                return false;
            }
            if (!decoder.ReadFloat(prev == null ? (float?)null : prev.trgLength, out outNext.trgLength))
            {
                return false;
            }
            if (!decoder.ReadFloat(prev == null ? (float?)null : prev.trgHpRate, out outNext.trgHpRate))
            {
                return false;
            }
            if (!decoder.ReadBool(prev == null ? (bool?)null : prev.isSearchAction, out outNext.isSearchAction))
            {
                return false;
            }

            int prevVariableLength = prev == null ? 0 : prev._localValueDic.Count;
            int nextVariableLength = 0;
            if (!decoder.ReadInteger(prevVariableLength, out nextVariableLength))
            {
                return false;
            }

            outNext.ClearAllLocalValues();
            var e = prev == null
                ? new Dictionary<string, AIScriptValue>().GetEnumerator()   // この値は使用されない
                : prev._localValueDic.GetEnumerator();
            for (int i = 0; i < nextVariableLength; ++i)
            {
                string prevVariableKey;
                AIScriptValue prevVariableValue;
                if (i < prevVariableLength)
                {
                    e.MoveNext();
                    prevVariableKey = e.Current.Key;
                    prevVariableValue = e.Current.Value;
                }
                else
                {
                    prevVariableKey = null;
                    prevVariableValue = null;
                }

                string variableName;
                if (!decoder.ReadString(prevVariableKey == null ? null : prevVariableKey, out variableName))
                {
                    return false;
                }
                byte type;
                if (!decoder.ReadByte(prevVariableValue == null ? null : (byte?)prevVariableValue.valType, out type))
                {
                    return false;
                }

                AIScriptValue value = new AIScriptValue(string.Empty);
                value.valType = (AIScriptValue.ValueType)type;

                // 型が変化しているときは、必ず値も書き込まれていることが保証されている
                switch (value.valType)
                {
                    case AIScriptValue.ValueType.Int:
                        int resultInt;
                        if (!decoder.ReadInteger(prevVariableValue == null ? null : (int?)prevVariableValue.valInt, out resultInt))
                        {
                            return false;
                        }
                        else
                        {
                            value.SetValue(resultInt);
                        }
                        break;
                    case AIScriptValue.ValueType.Float:
                        float resultFloat;
                        if (!decoder.ReadFloat(prevVariableValue == null ? null : (float?)prevVariableValue.valFloat, out resultFloat))
                        {
                            return false;
                        }
                        else
                        {
                            value.SetValue(resultFloat);
                        }
                        break;
                    case AIScriptValue.ValueType.String:
                        string resultString;
                        if (!decoder.ReadString(prevVariableValue == null ? null : prevVariableValue.valString, out resultString))
                        {
                            return false;
                        }
                        else
                        {
                            value.SetValue(resultString);
                        }
                        break;
                    default:
                        Debug.Assert(false, "AIScriptValueの型" + value.valType + "が未対応です");
                        break;
                }

                outNext.AddLocalValue(variableName, value);
            }
            return true;
        }

#if GLUON_DEBUG
        public void DebugCompare(AIScriptContext ctx)
        {
            Debug.Assert(awakeTime == ctx.awakeTime, "DebugCompare awakeTime " + awakeTime + ", " + ctx.awakeTime);
            Debug.Assert(hpRecode == ctx.hpRecode, "DebugCompare hpRecode " + hpRecode + ", " + ctx.hpRecode);
            Debug.Assert(isCountTime == ctx.isCountTime, "DebugCompare isCountTime " + isCountTime + ", " + ctx.isCountTime);
            Debug.Assert(countTime == ctx.countTime, "DebugCompare countTime " + countTime + ", " + ctx.countTime);
            Debug.Assert(trgLength == ctx.trgLength, "DebugCompare trgLength " + trgLength + ", " + ctx.trgLength);
            Debug.Assert(trgHpRate == ctx.trgHpRate, "DebugCompare trgHpRate " + trgHpRate + ", " + ctx.trgHpRate);
            Debug.Assert(isSearchAction == ctx.isSearchAction, "DebugCompare isSearchAction " + isSearchAction + ", " + ctx.isSearchAction);

            Debug.Assert(_localValueDic.Count == ctx._localValueDic.Count, "DebugCompare _localValueDic.Count" + _localValueDic.Count + ", " + ctx._localValueDic.Count);
            var e = _localValueDic.GetEnumerator();
            while (e.MoveNext())
            {
                var variableName = e.Current.Key;
                var value = e.Current.Value;

                if (!ctx.ContainsLocalValue(variableName))
                {
                    Debug.Assert(false, "DebugCompare " + variableName + " not found");
                }
                else
                {
                    var ctxValue = ctx.GetLocalValue(variableName);
                    Debug.Assert(value.valType == ctxValue.valType, "DebugCompare " + variableName + ".valType " + value.valType + ", " + ctxValue.valType);
                    Debug.Assert(value.ToString() == ctxValue.ToString(), "DebugCompare " + variableName + ".value " + value.ToString() + ", " + ctxValue.ToString());
                }
            }
        }

#endif

    }
}