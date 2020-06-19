using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// データを差分圧縮するためのクラス
    /// </summary>
    /// <remarks>
    /// 呼び出し側で前回書き込んだときの値を prev に渡すことを想定している。
    /// prevにnullを渡すと、必ずnextの値が書き込まれる。
    /// データの内容が頻繁に変わるようなものだと、元のデータより大きくなってしまう可能性があるので、注意すること。
    /// </remarks>
    public class DeltaCompressionEncoder
    {
        private MemoryStream _stream = new MemoryStream();
        private BinaryWriter _writer = null;
        private List<bool> _diffFlagList = new List<bool>();

        public DeltaCompressionEncoder()
        {
            _writer = new BinaryWriter(_stream);
        }

        public bool WriteInteger(int? prev, int next)
        {
            if (prev == null || prev != next)
            {
                WriteDiffFlag(true);// 差分あり
                _writer.Write(next);
                return true;
            }
            else
            {
                WriteDiffFlag(false);// 差分なし
                return false;
            }
        }

        public bool WriteByte(byte? prev, byte next)
        {
            if (prev == null || prev != next)
            {
                WriteDiffFlag(true);// 差分あり
                _writer.Write(next);
                return true;
            }
            else
            {
                WriteDiffFlag(false);// 差分なし
                return false;
            }
        }

        public bool WriteFloat(float? prev, float next)
        {
            if (prev == null || prev != next)
            {
                WriteDiffFlag(true);// 差分あり
                _writer.Write(next);
                return true;
            }
            else
            {
                WriteDiffFlag(false);// 差分なし
                return false;
            }
        }

        public bool WriteBool(bool? prev, bool next)
        {
            if (prev == null || prev != next)
            {
                WriteDiffFlag(true);// 差分あり
                _writer.Write(next ? (byte)0xff : (byte)0x00);
                return true;
            }
            else
            {
                WriteDiffFlag(false);// 差分なし
                return false;
            }
        }

        public bool WriteString(string prev, string next)
        {
            if (prev == null || prev != next)
            {
                WriteDiffFlag(true);// 差分あり
                byte[] bytesStr = System.Text.Encoding.UTF8.GetBytes(next);
                if (bytesStr.Length > 255)
                {
                    Debug.LogError("文字列が長すぎます : " + next);
                }
                _writer.Write((byte)bytesStr.Length);
                _writer.Write(bytesStr);
                return true;
            }
            else
            {
                WriteDiffFlag(false);// 差分なし
                return false;
            }
        }

        /// <summary>
        /// 現在の書き込み内容をバイナリ配列にして取り出す
        /// </summary>
        /// <remarks>
        /// 下記のバイナリ構造になっている
        /// ---------------------------------
        /// |差分フラグ部のバイト数 (4byte) |
        /// ---------------------------------
        /// |差分フラグ部                   |
        /// ---------------------------------
        /// |差分データ部                   |
        /// ---------------------------------
        /// </remarks>
        public byte[] GetBinary()
        {
            int diffFlagsLength = (_diffFlagList.Count + 7) / 8;
            var result = new byte[4 + diffFlagsLength + _stream.Length];
            int offset = 0;

            // まず、差分フラグ部のバイト数を書き込み
            Array.Copy(BitConverter.GetBytes(diffFlagsLength), 0, result, 0, 4);
            offset += 4;

            // 差分フラグは1要素1bitで書き込む
            for (int i = 0; i < _diffFlagList.Count; ++i)
            {
                int flag = _diffFlagList[i] ? 1 : 0;
                result[offset + i / 8] |= (byte)(flag << (i % 8));
            }
            offset += diffFlagsLength;

            // データ部を後ろにコピー
            Array.Copy(_stream.GetBuffer(), 0, result, offset, _stream.Length);
            Clear();
            return result;
        }

        public void Clear()
        {
            _stream.SetLength(0);
            _writer.Seek(0, SeekOrigin.Begin);
            _diffFlagList.Clear();
        }

        private void WriteDiffFlag(bool b)
        {
            _diffFlagList.Add(b);
        }
    }

    /// <summary>
    /// 差分圧縮されたデータを復元するためのクラス
    /// </summary>
    /// <remarks>
    /// 呼び出し側で前回読み取ったときの値を prev に渡すことを想定している。
    /// </remarks>
    public class DeltaCompressionDecoder
    {
        private byte[] _bytes = null;
        private int _nextByteIndex = 0;
        private int _nextDiffFlagBitIndex = 0;
        private int _diffFlagLength = 0;

        public DeltaCompressionDecoder()
        {

        }

        public void SetBinary(byte[] bytes)
        {
            _diffFlagLength = BitConverter.ToInt32(bytes, 0);
            _bytes = bytes;
            _nextByteIndex = 4 + _diffFlagLength;
            _nextDiffFlagBitIndex = 0;
        }

        public bool ReadInteger(int? prev, out int result)
        {
            bool isDiff;
            if (ReadDiffFlag(out isDiff))
            {
                if (isDiff)
                {
                    // 差分あり
                    if (_nextByteIndex + sizeof(int) <= _bytes.Length)
                    {
                        result = BitConverter.ToInt32(_bytes, _nextByteIndex);
                        _nextByteIndex += sizeof(int);
                        return true;
                    }
                }
                else
                {
                    // 差分なし
                    result = (int)prev;
                    return true;
                }
            }

            result = 0;
            return false;
        }

        public bool ReadByte(byte? prev, out byte result)
        {
            bool isDiff;
            if (ReadDiffFlag(out isDiff))
            {
                if (isDiff)
                {
                    // 差分あり
                    if (_nextByteIndex + sizeof(byte) <= _bytes.Length)
                    {
                        result = _bytes[_nextByteIndex];
                        _nextByteIndex++;
                        return true;
                    }
                }
                else
                {
                    // 差分なし
                    result = (byte)prev;
                    return true;
                }
            }

            result = 0;
            return false;
        }

        public bool ReadFloat(float? prev, out float result)
        {
            bool isDiff;
            if (ReadDiffFlag(out isDiff))
            {
                if (isDiff)
                {
                    // 差分あり
                    if (_nextByteIndex + sizeof(float) <= _bytes.Length)
                    {
                        result = BitConverter.ToSingle(_bytes, _nextByteIndex);
                        _nextByteIndex += sizeof(float);
                        return true;
                    }
                }
                else
                {
                    // 差分なし
                    result = (float)prev;
                    return true;
                }
            }

            result = 0;
            return false;
        }

        public bool ReadBool(bool? prev, out bool result)
        {
            bool isDiff;
            if (ReadDiffFlag(out isDiff))
            {
                if (isDiff)
                {
                    // 差分あり
                    if (_nextByteIndex + sizeof(byte) <= _bytes.Length)
                    {
                        result = _bytes[_nextByteIndex] == 0xff;
                        _nextByteIndex++;
                        return true;
                    }
                }
                else
                {
                    // 差分なし
                    result = (bool)prev;
                    return true;
                }
            }

            result = false;
            return false;
        }

        public bool ReadString(string prev, out string result)
        {
            bool isDiff;
            if (ReadDiffFlag(out isDiff))
            {
                if (isDiff)
                {
                    // 差分あり
                    if (_nextByteIndex + sizeof(byte) <= _bytes.Length)
                    {
                        int strByteLength = (int)_bytes[_nextByteIndex];
                        _nextByteIndex++;
                        if (_nextByteIndex + strByteLength <= _bytes.Length)
                        {
                            result = System.Text.Encoding.UTF8.GetString(_bytes, _nextByteIndex, strByteLength);
                            _nextByteIndex += strByteLength;
                            return true;
                        }
                    }
                    result = String.Empty;
                    return true;
                }
                else
                {
                    // 差分なし
                    result = (string)prev;
                    return true;
                }
            }

            result = String.Empty;
            return false;
        }

        private bool ReadDiffFlag(out bool result)
        {
            int byteIndex = 4 + _nextDiffFlagBitIndex / 8;
            int bitMask = 1 << (_nextDiffFlagBitIndex % 8);

            if (byteIndex < _diffFlagLength + 4)
            {
                result = (_bytes[byteIndex] & bitMask) != 0;
                _nextDiffFlagBitIndex++;
                return true;
            }
            else
            {
                result = false;
                return false;
            }
        }
    }
}
