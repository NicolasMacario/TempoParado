using UnityEngine;

namespace TempoParado
{
    [DefaultExecutionOrder(-110)]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RouteCheckpoint : MonoBehaviour
    {
        public PlayerMotor player;
        public Transform respawnPoint;
        public bool IsActivated { get; private set; }
        private BoxCollider bounds;
        private PlayerHealth health;
        private string message;
        private float messageUntil;
        private void Awake()
        {
            bounds = GetComponent<BoxCollider>();
            health = player == null ? null : player.GetComponent<PlayerHealth>();
        }
        private void Update()
        {
            if (player == null || health == null || respawnPoint == null) return;
            if (LabReset.Requested)
            {
                if (LabInput.ResetPressed)
                {
                    IsActivated = false;
                    player.ClearCheckpoint();
                    messageUntil = 0f;
                }
                else if (IsActivated) Notify("Nova tentativa: retome a luta contra o zelador.");
                return;
            }
            if (IsActivated || health.IsDead || !bounds.enabled) return;
            Vector3 point = transform.InverseTransformPoint(player.transform.position) - bounds.center;
            Vector3 half = bounds.size * 0.5f;
            if (Mathf.Abs(point.x) > half.x || Mathf.Abs(point.y) > half.y || Mathf.Abs(point.z) > half.z) return;
            player.SetCheckpoint(respawnPoint.position, respawnPoint.rotation);
            IsActivated = true;
            Notify("Ponto de retomada ativado antes do chefe.");
        }
        private void Notify(string text) { message = text; messageUntil = Time.unscaledTime + 3f; }
        private void OnGUI()
        {
            if (Time.unscaledTime >= messageUntil || health == null || health.IsDead) return;
            float width = Mathf.Min(280f, Screen.width - 24f);
            GUI.Box(new Rect(16f, 82f, width, 50f), message);
        }
        private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
    }
}
