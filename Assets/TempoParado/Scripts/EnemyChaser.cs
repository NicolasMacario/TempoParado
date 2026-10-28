using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(CharacterController), typeof(TimeBody))]
    public sealed class EnemyChaser : MonoBehaviour
    {
        public Transform target;
        public float detectionRange = 12f;
        public float loseRange = 16f;
        public float stopDistance = 1.7f;
        public float speed = 2.6f;
        public float turnSpeed = 240f;
        public string StateLabel { get; private set; } = "Vigiando";
        public float LocalScale => body == null ? 1f : body.LocalTimeScale;
        private CharacterController controller;
        private TimeBody body;
        private Vector3 spawn;
        private Quaternion spawnRotation;
        private float velocityY;
        private bool alerted;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<TimeBody>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
        }
        private void Update()
        {
            if (LabInput.ResetPressed)
            {
                controller.enabled = false;
                transform.SetPositionAndRotation(spawn, spawnRotation);
                controller.enabled = true;
                velocityY = 0f;
                alerted = false;
                StateLabel = "Vigiando";
                return;
            }
            if (target == null) { StateLabel = "Sem alvo"; return; }
            float dt = body.DeltaTime;
            if (dt <= 0f) { StateLabel = "Congelado"; return; }
            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (!alerted && distance <= detectionRange) alerted = true;
            else if (alerted && distance > Mathf.Max(loseRange, detectionRange)) alerted = false;
            Vector3 direction = Vector3.zero;
            if (!alerted) StateLabel = "Vigiando";
            else if (distance <= stopDistance) StateLabel = "Ao alcance (sem ataque)";
            else
            {
                StateLabel = "Perseguindo";
                direction = offset / distance;
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), turnSpeed * dt);
            }
            // O deslocamento respeita o relogio local e evita ultrapassar o alvo.
            float horizontalStep = direction == Vector3.zero ? 0f :
                Mathf.Min(speed * dt, Mathf.Max(0f, distance - stopDistance));
            if (controller.isGrounded && velocityY < 0f) velocityY = -2f;
            velocityY = Mathf.Max(velocityY - 20f * dt, -30f);
            controller.Move(direction * horizontalStep + Vector3.up * velocityY * dt);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }
    }
}
