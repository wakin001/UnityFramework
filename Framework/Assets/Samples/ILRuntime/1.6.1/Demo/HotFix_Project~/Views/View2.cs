using System;
using DG.Tweening;
using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace HotFix_Project
{
    public class View2
    {
        private Button button;
        private GameObject mainGo;
        private CanvasGroup canvasGroup;

        public View2(string prefabPath)
        {
            var prefab = AssetManager.Load(prefabPath);
            UnityEngine.Debug.Assert(prefab != null, prefabPath + " is null");
            mainGo = (GameObject)GameObject.Instantiate(prefab);
            canvasGroup = mainGo.GetComponent<CanvasGroup>();
            //if (canvasGroup == null)
            //{
            //    canvasGroup = mainGo.AddComponent<CanvasGroup>();
            //}

            canvasGroup.alpha = 0f;
            
        }

        public void Start()
        {
            canvasGroup.DOFade(1f, 3.5f);
        }
    }
}
