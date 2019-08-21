using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cute
{
    /// <summary>
    /// base of all Custom Message.
    /// Inherit it for your own implemented message.
    /// </summary>
    public class Message
    {
        public int type { get; set; } = -1;
    }

    /// <summary>
    /// Message System.
    /// 使い方:
    /// 0. typeを定義します。
    /// 1.  RegisterListener:　MessageSystem.instance.RegisterListener((int)MessageType.ShowQuestSelectEventCanvas, OnGotMessage);
    ///     OnDestroy()MessageSystem.instance.RemoveListener()をしたほうがいいです。
    /// 2. メセッジを送るクラスで
    ///     MessageSystem.instance.PostMessage((int)MessageType.ShowQuestSelectEventCanvas, false);
    /// 
    /// </summary>
    public class MessageSystem : SingletonComponent<MessageSystem>
    {
        public delegate void MessageHandlerDelegate(Message message);

        // Dictionary key enum will cause boxing, so we use int as key.
        // Case Customtype into int when register listeners.
        private Dictionary<int, List<MessageHandlerDelegate>> _messageListeners = new Dictionary<int, List<MessageHandlerDelegate>>();

        private Queue<Message> _messageQueue = new Queue<Message>();
        private const int _maxQueueProcessingTime = 16667;
        private System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// Listenerを登録します。
        /// </summary>
        public bool RegisterListener(int type, MessageHandlerDelegate handler)
        {
            if (!_messageListeners.ContainsKey(type))
            {
                _messageListeners.Add(type, new List<MessageHandlerDelegate>());
            }
            var listeners = _messageListeners[type];
            if (listeners.Contains(handler))
            {
                return false; //listener already in list
            }
            listeners.Add(handler);
            return true;
        }

        /// <summary>
        /// Listenerを削除します。
        /// </summary>
        public void RemoveListener(int type, MessageHandlerDelegate handler)
        {
            if (_messageListeners.ContainsKey(type))
            {
                _messageListeners[type].Remove(handler);
            }
        }
        
        /// <summary>
        /// 同じタイプのListenerを全部削除します。
        /// </summary>
        public void RemoveAllListeners(int type)
        {
            if (_messageListeners.ContainsKey(type))
            {
                _messageListeners[type].Clear();
            }
        }

        /// <summary>
        /// Listenerを全部削除します。
        /// </summary>
        public void Clear()
        {
            _messageListeners.Clear();
        }

        /// <summary>
        /// メセッジを送ります。
        /// </summary>
        public bool PostMessage(Message message)
        {
            int type = message.type;
            if (!_messageListeners.ContainsKey(type))
            {
                Debug.Log("Cannot find message targets.");
                return false;
            }

            for (int i = 0; i < _messageListeners[type].Count; ++i)
            {
                _messageListeners[type][i](message);
            }
            return true;
        }

        public bool QueueMessage(Message message)
        {
            if (!_messageListeners.ContainsKey(message.type))
            {
                return false;
            }
            _messageQueue.Enqueue(message);
            return true;
        }

        private void Update()
        {
            timer.Start();
            while (_messageQueue.Count > 0)
            {
                if (_maxQueueProcessingTime > 0f && timer.Elapsed.Milliseconds > _maxQueueProcessingTime)
                {
                    timer.Stop();
                    return;
                }

                Message message = _messageQueue.Dequeue();
                if (!PostMessage(message))
                {
                    Debug.Log("Error when processing message: " + message.type);
                }
            }
        }
    }
}