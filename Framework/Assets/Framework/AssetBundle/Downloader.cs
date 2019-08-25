using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework
{
    public enum DownloadErrorType
    {
        None,
        ConnectError,
        IOError,
        TimeoutError,
        Other,
    }

    public interface IDownloader
    {
        IEnumerator Download(string path, string name, Hash128 hash, string savePath);
        string error { get; }

        DownloadErrorType errorType { get; }
    }

    public class Downloader : IDownloader
    {
        public string error { get; private set; } = null;

        public DownloadErrorType errorType { get; private set; } = DownloadErrorType.None;

        /// <summary>
        /// Download the specified path, name, hash and toPath.
        /// </summary>
        /// <returns>The download.</returns>
        /// <param name="path">Path.</param>
        /// <param name="name">Name.</param>
        /// <param name="hash">Hash.</param> Ignore hash.
        /// <param name="toPath">To path.</param> Save file to toPath.
        public IEnumerator Download(string path, string name, Hash128 hash, string toPath)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(path + name))
            {
                yield return request.SendWebRequest();

                if (!request.isHttpError && !request.isNetworkError)
                {
                    byte[] data = request.downloadHandler.data;

                    try
                    {
                        FileUtil.Save(toPath + name, data);
                    }
                    catch (IOException e)
                    {
                        error = e.Message;
                        errorType = DownloadErrorType.IOError;
                    }
                    catch (Exception e)
                    {
                        error = e.Message;
                        errorType = DownloadErrorType.Other;
                    }
                }
                else
                {
                    error = request.error;
                    if (request.error.Contains("timeout"))
                    {
                        errorType = DownloadErrorType.TimeoutError;
                    }
                    else
                    {
                        errorType = DownloadErrorType.ConnectError;
                    }
                }
            }

        }
    }

}
