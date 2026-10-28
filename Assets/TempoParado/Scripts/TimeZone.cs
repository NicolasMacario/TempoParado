using System.Collections.Generic;
using UnityEngine;

namespace TempoParado
{
    // Consulta geometrica: nao depende de triggers ou de um Rigidbody no jogador.
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TimeZone : MonoBehaviour
    {
        public static readonly HashSet<TimeZone> Active = new HashSet<TimeZone>();
        [Range(0f, 3f)] public float scale = 1f;
        public int priority = 10;
        private BoxCollider bounds;
        private void OnEnable() { bounds = GetComponent<BoxCollider>(); Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
        public bool Contains(Vector3 point)
        {
            if (bounds == null || !bounds.enabled) return false;
            Vector3 p = transform.InverseTransformPoint(point) - bounds.center;
            Vector3 h = bounds.size * 0.5f;
            return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
        }
        private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
