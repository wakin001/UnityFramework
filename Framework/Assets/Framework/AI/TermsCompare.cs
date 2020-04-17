using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 条件判定クラス
    /// </summary>
    public static class TermsCompare
    {
        //判定タイプ
        public enum eCompare
        {
            largeEqual,     // <=
            smallEqual,     // >=
            repudiation,    // !=
            equal,          // ==
            large,          // <
            small,          // >
            none,           //error
        };
        static private string[] _compareTbl = { "<=", ">=", "!=", "==", "<", ">" };

        static public string CompareStr(eCompare type) { return _compareTbl[(int)type]; }

        public static string[] TermsSplit(string str)
        {
            for (int i = 0; i < _compareTbl.Length; ++i)
            {
                if (str.Contains(_compareTbl[i]))
                {
                    string[] termsStr = str.Split(_compareTbl, StringSplitOptions.None);
                    for (int ii = 0; ii < termsStr.Length; ++ii)
                    {
                        termsStr[ii] = termsStr[ii].Trim();
                    }
                    return termsStr;
                }

            }
            Debug.Assert(false, "----Error TermsCompare is Parser Terms" + str);
            return null;
        }
        public static eCompare CompareType(string str)
        {
            for (int i = 0; i < _compareTbl.Length; ++i)
            {
                if (str.Contains(_compareTbl[i]))
                {
                    return (eCompare)i;
                }
            }
            return eCompare.none;
        }


        /// <summary>
        /// 比較判定
        /// </summary>
        /// <param name="type">判定タイプ</param>
        /// <param name="val0">比較データ</param>
        /// <param name="val1">比較データ</param>
        /// <returns></returns>
        public static bool Compare<T>(eCompare type, T val0, T val1) where T : IComparable<T>
        {
            switch (type)
            {
                case eCompare.largeEqual:     // <=
                    {
                        return (val0.CompareTo(val1) <= 0) ? true : false;
                    }
                case eCompare.smallEqual:     // >=
                    {
                        return (val0.CompareTo(val1) >= 0) ? true : false;
                    }
                case eCompare.large:
                    {
                        return (val0.CompareTo(val1) < 0) ? true : false;
                    }
                case eCompare.small:
                    {
                        return (val0.CompareTo(val1) > 0) ? true : false;
                    }
                case eCompare.equal:
                    {
                        return (val0.CompareTo(val1) == 0) ? true : false;
                    }
                case eCompare.repudiation:
                    {
                        return !((val0.CompareTo(val1) != 0) ? false : true);
                    }
            }
            return false;
        }
    }
}
