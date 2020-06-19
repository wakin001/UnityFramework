using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;

namespace Cute.Master
{
    public static class MasterUtil
    {
        /// <summary>
        /// Master内で「localize」指定されたカラムを変換する関数コールバック
        /// 「localize」カラムを使用する場合は適切な関数を指定すること
        /// </summary>
        public static Func<string, string> Localize;

        /// <summary>
        /// csv読み込み時に定義されてるカラムが全て存在することを要求する.
        /// 存在しないカラムがあった場合はExceptionを吐くようになる.
        /// デフォルトはfalse.
        /// </summary>
        public static bool IsStrictImport { get; set; }

        public static string SnakeToUpperCamel(string src)
        {
            return src
                .Split(new[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
                .Aggregate(string.Empty, (s1, s2) => s1 + s2);
        }

        /// <summary>ファイル名が複数インスタンス用かチェック</summary>
        public static bool IsGroupInstance(string path, out string baseName, out int id)
        {
            string filename = Path.GetFileNameWithoutExtension(path);

            Regex r = new Regex(@"(.*)_(\d+)");
            var match = r.Match(filename);
            if (match.Success)
            {
                baseName = match.Groups[1].Value;
                id = int.Parse(match.Groups[2].Value);
                return true;
            }
            else
            {
                baseName = filename;
                id = 0;
                return false;
            }
        }

        public static string[] ToStringArray(string src)
        {
            return src.Split(',')
                .Where(s => s != "") // 空の要素は許されない
                .ToArray();

        }

        public static int[] ToIntArray(string src)
        {
            var split_strings = ToStringArray(src);

            return split_strings
                .Select(s =>
                {
                    int value = 0;
                    int.TryParse(s, out value);
                    return value;
                })
                .ToArray();
        }

        public static float[] ToFloatArray(string src)
        {
            var split_strings = ToStringArray(src);

            return split_strings
                .Select(s =>
                {
                    float value = 0;
                    float.TryParse(s, out value);
                    return value;
                })
                .ToArray();
        }

        public static bool[] ToBoolArray(string src)
        {
            var split_strings = ToStringArray(src);

            return split_strings
                .Select(s =>
                {
                    bool value = false;
                    bool.TryParse(s, out value);
                    return value;
                })
                .ToArray();
        }

        public static object[] ToEnumArray(string src, Type enumType)
        {
            var split_strings = ToStringArray(src);

            return split_strings
                .Select(s =>
                {
                    if (!Enum.IsDefined(enumType, s))
                    {
                        Debug.LogError(s + " is not defined in " + enumType);
                    }
                    return Enum.Parse(enumType, s);
                })
                .ToArray();
        }

        public static int ToInt(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                int ret = 0;
                if (int.TryParse(values[indexes[index]], out ret))
                {
                    return ret;
                }
            }
            return 0;
        }

        public static float ToFloat(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                float ret = 0;
                if (float.TryParse(values[indexes[index]], out ret))
                {
                    return ret;
                }
            }
            return 0;
        }

        public static string ToString(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                return values[indexes[index]];
            }
            return "";
        }

        public static bool ToBool(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                return bool.Parse(values[indexes[index]]);
            }
            return default(bool);
        }

        public static T ToEnum<T>(string[] values, int[] indexes, int index) where T : struct
        {
            if (indexes[index] >= 0)
            {
                if (Enum.IsDefined(typeof(T), values[indexes[index]]))
                {
                    return (T)Enum.Parse(typeof(T), values[indexes[index]]);
                }
            }
            return default(T);
        }

        public static int[] ToIntArray(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                return ToIntArray(values[indexes[index]]);
            }
            return default(int[]);
        }

        public static float[] ToFloatArray(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                return ToFloatArray(values[indexes[index]]);
            }
            return default(float[]);
        }

        public static string[] ToStringArray(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                return ToStringArray(values[indexes[index]]);
            }
            return default(string[]);
        }

        public static bool[] ToBoolArray(string[] values, int[] indexes, int index)
        {
            if (indexes[index] >= 0)
            {
                return ToBoolArray(values[indexes[index]]);
            }
            return default(bool[]);
        }

        public static T[] ToEnumArray<T>(string[] values, int[] indexes, int index) where T : struct
        {
            if (indexes[index] >= 0)
            {
                string[] split_strings = ToStringArray(values[indexes[index]]);

                return split_strings
                    .Select(s =>
                    {
                        if (!Enum.IsDefined(typeof(T), s))
                        {
                            Debug.LogError(s + " is not defined in " + typeof(T));
                        }
                        return (T)Enum.Parse(typeof(T), s);
                    })
                    .ToArray();
            }
            return default(T[]);
        }
    }
}