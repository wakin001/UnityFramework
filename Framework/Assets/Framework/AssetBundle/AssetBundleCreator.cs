using System;
using UnityEngine;
using System.IO;
using System.Security.Cryptography;

namespace Framework
{
    /// <summary>
    /// AssetBundle生成関数のラッパーclass
    /// 暗号化と通常版のAssetBundle生成を行う
    /// </summary>
    public class AssetBundleCreator
    {
        /// <summary>
        /// 復号化関数
        /// 暗号化をカスタマイズしたい場合はこの関数を上書きすること
        /// </summary>
        //public static Func<byte[], byte[]> DecryptMethod
        //{
        //    get
        //    {
        //        return decryptMethod;
        //    }
        //    set
        //    {
        //        decryptMethod = value;
        //    }
        //}
        //private static Func<byte[], byte[]> decryptMethod = DefaultEncryption.Decrypt;

        /// <summary>
        /// ManifestをLoadする.
        /// 暗号化必須.
        /// ファイルが壊れてるとnullを返す.
        /// </summary>
        public static AssetBundle LoadManifest(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = new byte[stream.Length - sha256.HashSize / 8];
                byte[] hash = new byte[sha256.HashSize / 8];
                stream.Read(bytes, 0, bytes.Length);
                stream.Read(hash, 0, hash.Length);

                var filehash = sha256.ComputeHash(bytes);
                if (!Equal(filehash, hash))
                {
                    return null;
                }

                //var decrypted = DecryptMethod(bytes);
                //return AssetBundle.LoadFromMemory(decrypted);
                return AssetBundle.LoadFromMemory(bytes);
            }
        }

        /// <summary>
        /// ManifestをLoadする.
        /// 暗号化必須.
        /// ファイルが壊れてるとnullを返す.
        /// </summary>
        public static AssetBundle LoadManifest(byte[] src)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = new byte[src.Length - sha256.HashSize / 8];
                byte[] hash = new byte[sha256.HashSize / 8];
                Array.Copy(src, bytes, bytes.Length);
                Array.Copy(src, bytes.Length, hash, 0, hash.Length);

                var filehash = sha256.ComputeHash(bytes);
                if (!Equal(filehash, hash))
                {
                    return null;
                }

                //var decrypted = DecryptMethod(bytes);
                //return AssetBundle.LoadFromMemory(decrypted);
                return AssetBundle.LoadFromMemory(bytes);
            }
        }

        /// <summary>
        /// Manifestファイルをハッシュを付与して保存する
        /// </summary>
        public static void SaveManifestWithHash(byte[] bytes, string outputManifestPath)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                byte[] outputs = new byte[bytes.Length + hash.Length];
                Array.Copy(bytes, outputs, bytes.Length);
                Array.Copy(hash, 0, outputs, bytes.Length, hash.Length);
                File.WriteAllBytes(outputManifestPath, outputs);
            }
        }

        public static AssetBundle LoadAssetBundle(string path, bool isEncrypted)
        {
            if (isEncrypted)
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                //var decrypted = DecryptMethod(bytes);
                //return AssetBundle.LoadFromMemory(decrypted);
                return AssetBundle.LoadFromMemory(bytes);
            }
            else
            {
                return AssetBundle.LoadFromFile(path);
            }
        }

        public static AssetBundleCreateRequest LoadAssetBundleAsync(string path, bool isEncrypted)
        {
            if (isEncrypted)
            {
                // ここが非同期にならない
                var bytes = System.IO.File.ReadAllBytes(path);
                //var decrypted = DecryptMethod(bytes);
                //return AssetBundle.LoadFromMemoryAsync(decrypted);
                return AssetBundle.LoadFromMemoryAsync(bytes);
            }
            else
            {
                return AssetBundle.LoadFromFileAsync(path);
            }
        }

        private static bool Equal(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}