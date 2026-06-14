using System;
using System.IO;
using System.Text;
using UnityEngine;
using Modding;
using ACMu.Core.Config;
using ACMu.Core.Lifecycle;

namespace ACMu.Adapter
{
    public class ModIoConfigStore : MonoBehaviour, IConfigStore, ILifecycleParticipant
    {
        public int InitOrder { get { return 0; } }

        public T LoadOrCreate<T>(string fileName) where T : class, new()
        {
            try
            {
                if (!Modding.ModIO.ExistsFile(fileName))
                    return new T();

                using (var stream = Modding.ModIO.Open(fileName, FileMode.Open, false, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        int length = reader.ReadInt32();
                        byte[] bytes = reader.ReadBytes(length);
                        string json = Encoding.UTF8.GetString(bytes);
                        return JsonUtility.FromJson<T>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ACMu:WARN] LoadOrCreate failed for " + fileName + ": " + ex.Message);
                return new T();
            }
        }

        public void Save<T>(string fileName, T value) where T : class
        {
            try
            {
                string json = JsonUtility.ToJson(value);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                using (var stream = Modding.ModIO.Open(fileName, FileMode.Create, false, FileAccess.Write))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(bytes.Length);
                        writer.Write(bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[ACMu:ERR] Save failed for " + fileName + ": " + ex);
            }
        }

        public void OnModLoad() { }
        public void OnSimulationStart(bool isMultiplayer) { }
        public void OnSimulationStop() { }
    }
}
