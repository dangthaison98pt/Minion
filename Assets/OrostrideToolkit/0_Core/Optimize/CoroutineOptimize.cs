using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrostrideToolkit.Core
{
    public class CoroutineOptimize : Singleton<CoroutineOptimize>
    {
        #region WaitForSeconds
        private readonly Dictionary<int, WaitForSeconds> _timeInterval = new(32);

        public WaitForSeconds WaitForSeconds(float seconds)
        {
            int msKey = Mathf.RoundToInt(seconds * 1000f);

            if (!_timeInterval.TryGetValue(msKey, out var waitObj))
            {
                waitObj = new WaitForSeconds(seconds);
                _timeInterval.Add(msKey, waitObj);
            }
    
            return waitObj;
        }
        #endregion
        
        #region WaitForFixedUpdate
        private static readonly WaitForFixedUpdate fixedUpdate = new();
        public WaitForFixedUpdate WaitForFixedUpdate => fixedUpdate;
        #endregion
        
        #region WaitForEndOfFrame
        private static readonly WaitForEndOfFrame endOfFrame = new();
        public WaitForEndOfFrame WaitForEndOfFrame => endOfFrame;
        #endregion
    }
}