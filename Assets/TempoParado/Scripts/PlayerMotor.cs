using UnityEngine;

namespace TempoParado
{
    // A entrada da esquiva e a movimentacao sao resolvidas antes dos golpes do inimigo.
    [DefaultExecutionOrder(-100)]
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
        private PlayerDodge dodge;
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<TimeBody>();
            spawn = transform.position;
            health = GetComponent<PlayerHealth>();
            dodge = GetComponent<PlayerDodge>();
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
                if (dodge != null) dodge.ResetState();
                return;
            }
            if (health != null && health.IsDead)
            {
                if (dodge != null) dodge.ResetState();
                return;
            }
            float dt = body.DeltaTime;
            Vector2 input = LabInput.Move;
            Vector3 move = new Vector3(input.x, 0f, input.y);
            if (cameraTransform != null)
            {
                Quaternion heading = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
                move = heading * move;
            }
            Vector3 displacement = move * speed * dt;
            Vector3 facing = move;
            if (dodge != null && dodge.isActiveAndEnabled)
            {
                Vector3 dodgeMotion = dodge.Step(dt, move, LabInput.DodgePressed && controller.isGrounded);
                if (dodge.IsDodging)
                {
                    displacement = dodgeMotion;
                    facing = dodge.FaceDirection ? dodge.Direction : Vector3.zero;
                }
            }
            if (dt <= 0f) return;
            if (facing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(facing), turnSpeed * dt);
            if (controller.isGrounded && velocityY < 0f) velocityY = -2f;
            velocityY += -20f * dt;
            controller.Move(displacement + Vector3.up * velocityY * dt);
        }
    }
}
