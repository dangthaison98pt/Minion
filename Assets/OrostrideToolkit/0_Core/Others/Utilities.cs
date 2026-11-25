using System;
using UnityEngine;

namespace OrostrideToolkit.Core
{
    public static class Utilities
    {
        public static void SetDateTimeToPlayerPrefs(string key, DateTime value)
        {
            PlayerPrefs.SetString(key, value.ToBinary().ToString());
        }

        public static DateTime GetDateTimeFromPlayerPrefs(string key)
        {
            string data = PlayerPrefs.GetString(key, string.Empty);
            if (long.TryParse(data, out long binaryTime))
            {
                return DateTime.FromBinary(binaryTime);
            }
        
            return DateTime.MinValue; 
        }
    }
}
