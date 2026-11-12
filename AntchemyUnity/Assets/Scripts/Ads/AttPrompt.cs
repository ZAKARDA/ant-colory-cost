using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Antchemy
{
    // Shows Apple's App Tracking Transparency prompt once, before the ad consent flow and SDK start.
    public static class AttPrompt
    {
        public static void Request(Action done)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                if (_ATTStatus() != 0) { done?.Invoke(); return; }
                pending = done;
                _ATTRequest(OnResult);
            }
            catch (Exception e) { Debug.LogWarning("[ColonyAds] ATT prompt unavailable: " + e.Message); done?.Invoke(); }
#else
            done?.Invoke();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        delegate void ResultCallback(int status);
        static Action pending;

        [DllImport("__Internal")] static extern int _ATTStatus();
        [DllImport("__Internal")] static extern void _ATTRequest(ResultCallback callback);

        [AOT.MonoPInvokeCallback(typeof(ResultCallback))]
        static void OnResult(int status)
        {
            Debug.Log("[ColonyAds] ATT status " + status);
            var callback = pending; pending = null; callback?.Invoke();
        }
#endif
    }
}
