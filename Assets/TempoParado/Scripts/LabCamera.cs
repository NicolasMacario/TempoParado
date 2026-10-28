using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(Camera))]
    public sealed class LabCamera : MonoBehaviour
    {
        public Transform target;
        [Header("Enquadramento")]
        public float pivotHeight = 0.7f;
        public float shoulderOffset = 0.5f;
        public float distance = 4.5f;
        public float minDistance = 1.5f;
        public float maxDistance = 7f;
        public float zoomStep = 0.5f;
        [Header("Orbita: mouse livre; Esc libera o cursor")]
        public float sensitivity = 0.15f;
        public float pitch = 15f;
        public float minPitch = -15f;
        public float maxPitch = 65f;
        public bool invertY;
        [Header("Colisao")]
        public LayerMask obstacleLayers = ~0;
        public float collisionRadius = 0.2f;
        public float collisionPadding = 0.08f;
        public float returnSpeed = 8f;
        private float yaw;
        private float currentDistance;
        private bool dragging;
        private bool cameraCaptured = true;
        private CursorLockMode previousLock;
        private bool previousVisibility;

        private void OnEnable()
        {
            yaw = target == null ? 0f : target.eulerAngles.y;
            cameraCaptured = true;
            currentDistance = distance;
            GetComponent<Camera>().nearClipPlane = 0.08f;
        }
        private void Update()
        {
            if (LabInput.ReleaseCameraPressed) cameraCaptured = false;
            else if (LabInput.CaptureCameraPressed) cameraCaptured = true;
            bool rotate = Application.isFocused && cameraCaptured;
            if (rotate && !dragging)
            {
                previousLock = Cursor.lockState;
                previousVisibility = Cursor.visible;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                dragging = true;
                // Ignora o primeiro delta para evitar salto ao capturar o cursor.
            }
            else if (rotate)
            {
                Vector2 look = LabInput.Look;
                yaw = Mathf.Repeat(yaw + look.x * sensitivity, 360f);
                pitch = Mathf.Clamp(pitch + look.y * sensitivity * (invertY ? 1f : -1f), minPitch, maxPitch);
            }
            else ReleaseCursor();
            distance = Mathf.Clamp(distance - LabInput.Zoom * zoomStep, minDistance, maxDistance);
        }
        private void LateUpdate()
        {
            if (target == null) return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Vector3 offset = rotation * new Vector3(shoulderOffset, 0f, -distance);
            Vector3 direction = offset.normalized;
            float desiredDistance = offset.magnitude;
            // Ignora o proprio jogador e os triggers das zonas temporais.
            RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, direction,
                desiredDistance, obstacleLayers, QueryTriggerInteraction.Ignore);
            float allowedDistance = desiredDistance;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(target)) continue;
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, hit.distance - collisionPadding));
            }
            // Aproxima imediatamente para evitar atravessar a parede; afasta suavemente.
            if (allowedDistance < currentDistance) currentDistance = allowedDistance;
            else currentDistance = Mathf.MoveTowards(currentDistance, allowedDistance, returnSpeed * Time.unscaledDeltaTime);
            transform.SetPositionAndRotation(pivot + direction * currentDistance, rotation);
        }
        private void OnDisable() { ReleaseCursor(); }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) { cameraCaptured = false; ReleaseCursor(); }
        }
        private void ReleaseCursor()
        {
            if (!dragging) return;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisibility;
            dragging = false;
        }
    }
}
