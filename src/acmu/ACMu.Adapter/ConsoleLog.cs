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
            try { Modding.ModConsole.Log("[ACMu] " + message); }
            catch { }
        }

        public void Warn(string message)
        {
            try { Modding.ModConsole.Log("[ACMu:WARN] " + message); }
            catch { }
        }

        public void Error(string message)
        {
            try { Modding.ModConsole.Log("[ACMu:ERR] " + message); }
            catch { }
        }

        public void Error(string message, Exception ex)
        {
            try { Modding.ModConsole.Log("[ACMu:ERR] " + message + "\n" + ex.Message); }
            catch { }
        }

        public void OnModLoad() { }
        public void OnSimulationStart(bool isMultiplayer) { }
        public void OnSimulationStop() { }
    }
}
