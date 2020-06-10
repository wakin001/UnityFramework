using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;
using UnityEngine.Events;

namespace HotFix_Project
{
    /// <summary>
    /// UIAnimationにイベントを追加
    /// </summary>
    public class UIAnimationPublisherEventAttacher : MonoBehaviour
    {
        [SerializeField]
        public UIAnimationPublisher publisher;
        public UnityEvent onEnterEvent;
        public UnityEvent onExitEvent;

        private void Start()
        {
            if (publisher == null)
            {
                publisher = GetComponent<UIAnimationPublisher>();
            }
            if (publisher == null)
            {
                return;
            }
            publisher.onEnterEvent = JoinActions(publisher.onEnterEvent, onEnterEvent);
            publisher.onExitEvent = JoinActions(publisher.onExitEvent, onExitEvent);
        }

        /// <summary>
        /// CombineUnityEvent
        /// </summary>
        /// <param name="baseEvent"></param>
        /// <param name="attachmentEvent"></param>
        private UnityEvent JoinActions(UnityEvent baseEvent, UnityEvent attachmentEvent)
        {
            if (attachmentEvent == null)
            {
                return baseEvent;
            }

            if (baseEvent == null)
            {
                baseEvent = new UnityEvent();
            }

            int count = attachmentEvent.GetPersistentEventCount();

            for (int i = 0; i < count; ++i)
            {
                Action action = (Action)System.Delegate.CreateDelegate(typeof(Action), attachmentEvent.GetPersistentTarget(i), attachmentEvent.GetPersistentMethodName(i));
                baseEvent.AddListener(() => action.Invoke());
            }
            return baseEvent;
        }
    }
}
