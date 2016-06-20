using System.Collections.Generic;
using Engine.Common.Singleton;
using UnityEngine;

namespace Engine.Common.EventMessage
{
    public class MessageCenter :
#if UNITY_EDITOR
        DDOSingleton<MessageCenter>
#else
        NormalSingleton<MessageCenter>
#endif
        
    {

        /// <summary>
        /// 消息系统，委托与监听，如多个UI注册了对金币的改变，一旦客户端接收到金币改变消息，直接发送一个金币改变事件，对应添加了监听的UI将会改变(实际上委托并不是UI的View层添加，而是Model层添加)
        /// </summary>
        /// <param name="message"></param>
        public delegate void MessageEvent(object message);


        //为什么用List而不用委托的 += 因为：方便扩展，事件优先级，事件需要循环发送，某个事件添加有相应
        private Dictionary<int, List<MessageEvent>> dicMessageEvents = new Dictionary<int, List<MessageEvent>>();

#if UNITY_EDITOR
        // 所有事件发送次数
        public int totalSendCount = 0;

        // 所有事件处理次数
        public int totalProcessCount = 0;


        // 记录当前注册的事件类型数量
        public List<int> EventTypes = new List<int>(); 
        // 记录当前注册的所有事件数量
        public List<string> SubEventTypes = new List<string>(); 
#endif

        #region Add & Remove Listener

        /// <summary>
        /// OnEnable / OnRegister (OnAwake 最好别在OnAwake中添加)
        /// </summary>
        /// <param name="messageKey"></param>
        /// <param name="messageEvent"></param>
        public void AddListener(int messageKey, MessageEvent messageEvent)
        {
            Debug.Log("AddListener Name : " + messageKey);
            List<MessageEvent> list = null;
            if (dicMessageEvents.ContainsKey(messageKey))
            {
                list = dicMessageEvents[messageKey];
            }
            else
            {
                list = new List<MessageEvent>();
                dicMessageEvents.Add(messageKey, list);
#if UNITY_EDITOR
                EventTypes.Add(messageKey);
#endif
            }
            // no same messageEvent then add
            if (!list.Contains(messageEvent))
            {
                list.Add(messageEvent);

#if UNITY_EDITOR
                SubEventTypes.Add(messageKey + "_" + messageEvent.Method);
#endif

    }
}

        /// <summary>
        /// OnDisable / OnRemove
        /// </summary>
        /// <param name="messageKey"></param>
        /// <param name="messageEvent"></param>
        public void RemoveListener(int messageKey, MessageEvent messageEvent)
        {
            Debug.Log("RemoveListener Name : " + messageKey);
            if (dicMessageEvents.ContainsKey(messageKey))
            {
                List<MessageEvent> list = dicMessageEvents[messageKey];
                if (list.Contains(messageEvent))
                {
                    list.Remove(messageEvent);
#if UNITY_EDITOR
                    EventTypes.Remove(messageKey);
#endif
                }
                if (list.Count <= 0)
                {
                    dicMessageEvents.Remove(messageKey);
#if UNITY_EDITOR
                    SubEventTypes.Remove(messageKey + "_" + messageEvent.Method);
#endif
                }
            }
        }

        public void RemoveAllListener()
        {
            dicMessageEvents.Clear();
#if UNITY_EDITOR
            EventTypes.Clear();
            SubEventTypes.Clear();
#endif
        }

        #endregion

        #region Send Message

        public void SendMessage(int messageKey, object message)
        {
#if UNITY_EDITOR
            totalSendCount++;
#endif
            if (dicMessageEvents == null || !dicMessageEvents.ContainsKey(messageKey)) return;
            List<MessageEvent> list = dicMessageEvents[messageKey];
            for (int i = 0; i < list.Count; i++)
            {
                MessageEvent messageEvent = list[i];
                if (null != messageEvent)
                {
                    // 委托函数执行
                    messageEvent(message);
#if UNITY_EDITOR
                    totalProcessCount++;
#endif
                }
            }
        }

#endregion


//        void FixedUpdate()
//        {
//            ThreadMessage threadMessage = MessageCenterThread.Instance.Dequeue();
//            if (threadMessage != null)
//            {
//                SendMessage(threadMessage.id, threadMessage.msg);
//            }
//        }

    }
}