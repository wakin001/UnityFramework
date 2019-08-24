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
        IEnumerator Download(string path, string filename);
        string error { get; }

        DownloadErrorType errorType { get; }
    }

    public class Downloader : IDownloader
    {
        public string error { get; private set; } = null;

        public DownloadErrorType errorType { get; private set; } = DownloadErrorType.None;

        public IEnumerator Download(string path, string toPath)
        {
            //UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(path);
            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();

                if (!request.isHttpError && !request.isNetworkError)
                {
                    byte[] data = request.downloadHandler.data;

                    try
                    {
                        FileUtil.Save(toPath, data);
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
