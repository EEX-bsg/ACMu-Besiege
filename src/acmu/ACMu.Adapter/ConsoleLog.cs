using System;
using UnityEngine;
using ACMu.Core.Logging;
using ACMu.Core.Lifecycle;

namespace ACMu.Adapter
{
    public class ConsoleLog : MonoBehaviour, ILog, ILifecycleParticipant
    {
        public int InitOrder { get { return 0; } }

        public void Info(string message)
        {
            string text = "[ACMu] " + message;
            try { Modding.ModConsole.Log(text); } catch { }
            Debug.Log(text);
        }

        public void Warn(string message)
        {
            string text = "[ACMu:WARN] " + message;
            try { Modding.ModConsole.Log(text); } catch { }
            Debug.LogWarning(text);
        }

        public void Error(string message)
        {
            string text = "[ACMu:ERR] " + message;
            try { Modding.ModConsole.Log(text); } catch { }
            Debug.LogError(text);
        }

        public void Error(string message, Exception ex)
        {
            string text = "[ACMu:ERR] " + message + ": " + ex.Message;
            try { Modding.ModConsole.Log(text); } catch { }
            Debug.LogError(text);
        }

        public void OnModLoad() { }
        public void OnSimulationStart(bool isMultiplayer) { }
        public void OnSimulationStop() { }
    }
}
