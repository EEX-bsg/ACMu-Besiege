using System.Collections.Generic;
using ACMu.Core.Game;
using ACMu.Core.Lifecycle;
using Modding.Blocks;
using UnityEngine;

namespace ACMu.Adapter
{
    public class BlockAccessorFactoryAdapter : MonoBehaviour, IBlockAccessorFactory, ILifecycleParticipant
    {
        private readonly Dictionary<GameObject, IBlockAccessor> _cache = new Dictionary<GameObject, IBlockAccessor>();

        public int InitOrder { get { return 0; } }

        public IBlockAccessor FromGameObject(GameObject blockObject)
        {
            if (blockObject == null) return null;
            IBlockAccessor cached;
            if (_cache.TryGetValue(blockObject, out cached)) return cached;
            try
            {
                var block = Block.From(blockObject);
                if (block == null)
                {
                    Debug.LogWarning("[ACMu] BlockAccessorFactory: Block.From returned null for " + blockObject.name);
                    return null;
                }
                var accessor = new BlockAccessorAdapter(block.InternalObject);
                _cache[blockObject] = accessor;
                return accessor;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ACMu] BlockAccessorFactory: FromGameObject failed: " + ex.Message);
                return null;
            }
        }

        public void OnModLoad() { }

        public void OnSimulationStart(bool isMultiplayer) { }

        public void OnSimulationStop()
        {
            _cache.Clear();
        }
    }
}
