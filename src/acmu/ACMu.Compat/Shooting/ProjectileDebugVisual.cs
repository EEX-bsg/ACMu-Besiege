using System;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // デバッグモード時に弾頭コライダーを半透明メッシュで可視化する。
    // Adapter(BesiegeColliderVisuals)からBootstrapがプロバイダーを注入する。
    internal static class ProjectileDebugVisual
    {
        private static Func<Material>   _materialProvider;
        private static Func<GameObject> _gridSphereProvider;

        private const int    DebugLayer   = 25;
        private const string VisualGoName = "[ACMu:ColliderDebug]";

        internal static void SetProviders(
            Func<Material>   materialProvider,
            Func<GameObject> gridSphereProvider)
        {
            _materialProvider   = materialProvider;
            _gridSphereProvider = gridSphereProvider;
        }

        internal static void Attach(GameObject projectileGo)
        {
            if (projectileGo == null) return;
            RemoveExisting(projectileGo);

            Material mat = _materialProvider != null ? _materialProvider() : null;

            Collider[] colliders;
            var physSetup = projectileGo.GetComponent<ProjectilePhysicsSetup>();
            if (physSetup != null)
                colliders = physSetup.GetEffectiveColliders();
            else
                colliders = projectileGo.GetComponents<Collider>();

            foreach (var col in colliders)
            {
                if (col == null) continue;
                if (col is SphereCollider)
                    AttachSphere(projectileGo.transform, (SphereCollider)col, mat);
                else if (col is CapsuleCollider)
                    AttachCapsule(projectileGo.transform, (CapsuleCollider)col, mat);
                else if (col is BoxCollider)
                    AttachBox(projectileGo.transform, (BoxCollider)col, mat);
            }
        }

        private static void RemoveExisting(GameObject go)
        {
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = go.transform.GetChild(i);
                if (child.name == VisualGoName)
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static void AttachSphere(Transform parent, SphereCollider col, Material mat)
        {
            GameObject prefab = _gridSphereProvider != null ? _gridSphereProvider() : null;
            GameObject vis;
            if (prefab != null)
            {
                vis = UnityEngine.Object.Instantiate(prefab);
                var c = vis.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }
            else
            {
                vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.DestroyImmediate(vis.GetComponent<Collider>());
            }
            vis.name = VisualGoName;
            ApplyMaterial(vis, mat);
            vis.transform.SetParent(parent, false);
            vis.transform.localPosition = col.center;
            vis.transform.localScale    = Vector3.one * (col.radius * 2f);
            SetLayerRecursively(vis, DebugLayer);
        }

        private static void AttachCapsule(Transform parent, CapsuleCollider col, Material mat)
        {
            float radius     = col.radius;
            float height     = Mathf.Max(col.height, radius * 2f);
            float bodyLength = height - radius * 2f;

            Vector3 axis =
                col.direction == 0 ? Vector3.right  :
                col.direction == 2 ? Vector3.forward :
                                     Vector3.up;

            // 両端: カプセルの端部はコライダー定義上まさに球体なので、球体プリミティブが正確
            CreateSphereVis(parent, col.center + axis * (bodyLength * 0.5f), radius, mat);
            CreateSphereVis(parent, col.center - axis * (bodyLength * 0.5f), radius, mat);

            // 胴体シリンダー (Unityの Cylinder は Y軸方向・高さ2・半径1)
            if (bodyLength > 0f)
            {
                var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                UnityEngine.Object.DestroyImmediate(cyl.GetComponent<Collider>());
                cyl.name = VisualGoName;
                ApplyMaterial(cyl, mat);
                cyl.transform.SetParent(parent, false);
                cyl.transform.localPosition = col.center;
                cyl.transform.localRotation = CapsuleBodyRotation(col.direction);
                cyl.transform.localScale    = new Vector3(radius * 2f, bodyLength * 0.5f, radius * 2f);
                SetLayerRecursively(cyl, DebugLayer);
            }
        }

        private static void CreateSphereVis(Transform parent, Vector3 localPos, float radius, Material mat)
        {
            var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.name = VisualGoName;
            ApplyMaterial(vis, mat);
            vis.transform.SetParent(parent, false);
            vis.transform.localPosition = localPos;
            vis.transform.localScale    = Vector3.one * (radius * 2f);
            SetLayerRecursively(vis, DebugLayer);
        }

        private static Quaternion CapsuleBodyRotation(int direction)
        {
            if (direction == 0) return Quaternion.Euler(0f, 0f, 90f);
            if (direction == 2) return Quaternion.Euler(90f, 0f, 0f);
            return Quaternion.identity;
        }

        private static void AttachBox(Transform parent, BoxCollider col, Material mat)
        {
            var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.name = VisualGoName;
            ApplyMaterial(vis, mat);
            vis.transform.SetParent(parent, false);
            vis.transform.localPosition = col.center;
            vis.transform.localScale    = col.size;
            SetLayerRecursively(vis, DebugLayer);
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
            foreach (Renderer child in go.GetComponentsInChildren<Renderer>())
                child.sharedMaterial = mat;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
