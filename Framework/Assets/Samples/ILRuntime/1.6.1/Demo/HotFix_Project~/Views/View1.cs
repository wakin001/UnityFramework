using System;
using DG.Tweening;
using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace HotFix_Project
{
    public class View1
    {
        private Button button;
        private GameObject mainGo;
        private CanvasGroup canvasGroup;

        public View1(string prefabPath)
        {
            var prefab = AssetManager.Load(prefabPath);
            UnityEngine.Debug.Assert(prefab != null, prefabPath + " is null");
            mainGo = (GameObject)GameObject.Instantiate(prefab);
            canvasGroup = mainGo.GetComponent<CanvasGroup>();
        }

        public void Start()
        {
            var buttonTrans = mainGo.transform.Find("Button_next");
            Debug.Assert(buttonTrans != null, "Button_next is null");
            button = buttonTrans.GetComponent<Button>();

            button.onClick.AddListener(() =>
            {
                FadeOut();
                Debug.Log("button is pressed.");
                View2 view2 = new View2("Prefabs/Views/View2");
                view2.Start();
            });
        }

        private void FadeOut()
        {
            canvasGroup.DOFade(0f, 0.5f);
        }
    }
}
