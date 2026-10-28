using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(TimeBody))]
    public sealed class TemporalProbe : MonoBehaviour
    {
        private TimeBody body;
        private Vector3 origin;
        private float elapsed;
        private void Awake() { body = GetComponent<TimeBody>(); origin = transform.position; }
        private void Update()
        {
            if (LabReset.Requested) elapsed = 0f;
            elapsed += body.DeltaTime;
            // Animacao procedural limitada ao interior da zona, sem simular queda.
            transform.position = origin + Vector3.up * (Mathf.Sin(elapsed * 2f) * 0.5f);
            transform.rotation = Quaternion.Euler(20f, elapsed * 90f, 15f);
        }
    }
}
