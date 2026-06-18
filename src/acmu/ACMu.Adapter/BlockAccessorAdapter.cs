using System;
using System.Collections.Generic;
using UnityEngine;
using ACMu.Core.Game;

namespace ACMu.Adapter
{
    public class BlockAccessorAdapter : IBlockAccessor
    {
        private readonly BlockBehaviour _bb;

        public BlockAccessorAdapter(BlockBehaviour bb)
        {
            _bb = bb;
        }

        public Guid Guid { get { return _bb.Guid; } }
        public string BlockName { get { return _bb.Prefab != null ? _bb.Prefab.name : _bb.gameObject.name; } }
        public int BlockTypeId { get { return _bb.BlockID; } }
        public GameObject GameObject { get { return _bb.gameObject; } }
        public Rigidbody Rigidbody { get { return _bb.Rigidbody; } }
        public bool IsSimulating { get { return _bb.isSimulating; } }
        public bool IsDestroyed { get { return _bb.IsDestroyed; } }
        public Vector3 Position { get { return _bb.transform.position; } }
        public Quaternion Rotation { get { return _bb.transform.rotation; } }

        public bool TryGetSlider(string key, out float value)
        {
            foreach (MSlider s in _bb.Sliders)
            {
                if (s.Key == key) { value = s.Value; return true; }
            }
            value = 0f;
            return false;
        }

        public bool TryGetToggle(string key, out bool value)
        {
            foreach (MToggle t in _bb.Toggles)
            {
                if (t.Key == key) { value = t.IsActive; return true; }
            }
            value = false;
            return false;
        }

        public bool IsKeyHeld(string key)
        {
            foreach (MKey k in _bb.Keys)
            {
                if (k.Key == key) return k.IsHeld;
            }
            return false;
        }

        public bool IsKeyPressed(string key)
        {
            foreach (MKey k in _bb.Keys)
            {
                if (k.Key == key) return k.IsPressed;
            }
            return false;
        }
    }
}
