using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(CharacterController), typeof(TimeBody))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public float speed = 5f;
        public float turnSpeed = 720f;
        public Transform cameraTransform;
        private CharacterController controller;
        private TimeBody body;
        private Vector3 spawn;
        private float velocityY;
        private PlayerHealth health;
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<TimeBody>();
            spawn = transform.position;
            health = GetComponent<PlayerHealth>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }
        private void Update()
        {
            // Comandos de depuracao continuam funcionando quando o corpo congela.
            if (LabInput.ToggleProtection) body.ignoreZones = !body.ignoreZones;
            if (LabReset.Requested)
            {
                controller.enabled = false;
                transform.position = spawn;
                controller.enabled = true;
                body.ignoreZones = true;
                velocityY = 0f;
                return;
            }
            if (health != null && health.IsDead) return;
            float dt = body.DeltaTime;
            if (dt <= 0f) return;
            Vector2 input = LabInput.Move;
            Vector3 move = new Vector3(input.x, 0f, input.y);
            if (cameraTransform != null)
            {
                Quaternion heading = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
                move = heading * move;
            }
            if (move.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(move), turnSpeed * dt);
            if (controller.isGrounded && velocityY < 0f) velocityY = -2f;
            velocityY += -20f * dt;
            controller.Move((move * speed + Vector3.up * velocityY) * dt);
        }
    }
}
