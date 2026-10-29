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
        private Quaternion spawnRotation;
        private Vector3 checkpointPosition;
        private Quaternion checkpointRotation;
        public bool HasCheckpoint { get; private set; }
        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            checkpointPosition = position;
            checkpointRotation = rotation;
            HasCheckpoint = true;
        }
        public void ClearCheckpoint() { HasCheckpoint = false; }
        private float velocityY;
        private PlayerHealth health;
        private PlayerDodge dodge;
        private PlayerCombat combat;
        private TemporalStrain strain;
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<TimeBody>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            health = GetComponent<PlayerHealth>();
            dodge = GetComponent<PlayerDodge>();
            combat = GetComponent<PlayerCombat>();
            strain = GetComponent<TemporalStrain>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }
        private void Update()
        {
            // Comandos de depuracao continuam funcionando quando o corpo congela.
            if (LabInput.ToggleProtection) body.ignoreZones = !body.ignoreZones;
            if (LabReset.Requested)
            {
                if (LabInput.ResetPressed) ClearCheckpoint();
                controller.enabled = false;
                transform.SetPositionAndRotation(HasCheckpoint ? checkpointPosition : spawn,
                    HasCheckpoint ? checkpointRotation : spawnRotation);
                controller.enabled = true;
                body.ignoreZones = true;
                velocityY = 0f;
                if (dodge != null) dodge.ResetState();
                if (combat != null) combat.ResetState();
                return;
            }
            if (health != null && health.IsDead)
            {
                if (dodge != null) dodge.ResetState();
                if (combat != null) combat.ResetState();
                return;
            }
            float dt = body.DeltaTime;
            if (strain != null && strain.isActiveAndEnabled && strain.IsCollapsed)
            {
                if (dodge != null && dodge.isActiveAndEnabled)
                {
                    dodge.InterruptDodge();
                    dodge.Step(dt, Vector3.zero, false); // A recarga continua sem dar invulnerabilidade.
                }
                if (combat != null) combat.InterruptAttack("Ataque interrompido pelo colapso");
                MoveWithGravity(Vector3.zero, dt);
                return;
            }
            Vector2 input = LabInput.Move;
            Vector3 move = new Vector3(input.x, 0f, input.y);
            if (cameraTransform != null)
            {
                Quaternion heading = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
                move = heading * move;
            }
            float conditionSpeed = strain == null ? 1f : strain.MovementMultiplier;
            Vector3 displacement = move * speed * conditionSpeed * dt;
            Vector3 facing = move;
            bool isDodging = false;
            if (dodge != null && dodge.isActiveAndEnabled)
            {
                Vector3 dodgeMotion = dodge.Step(dt, move, LabInput.DodgePressed && controller.isGrounded);
                if (dodge.IsDodging)
                {
                    isDodging = true;
                    displacement = dodgeMotion;
                    facing = dodge.FaceDirection ? dodge.Direction : Vector3.zero;
                }
            }
            if (combat != null && combat.isActiveAndEnabled)
            {
                Vector3 aim = cameraTransform == null ? transform.forward :
                    Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * Vector3.forward;
                combat.Step(dt, aim, LabInput.AttackPressed && controller.isGrounded, isDodging);
                if (combat.IsAttacking && !isDodging)
                {
                    displacement *= Mathf.Clamp01(combat.movementMultiplier);
                    facing = combat.LockedDirection;
                }
            }
            if (dt <= 0f) return;
            if (facing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(facing), turnSpeed * dt);
            MoveWithGravity(displacement, dt);
        }
        private void MoveWithGravity(Vector3 displacement, float dt)
        {
            if (dt <= 0f) return;
            if (controller.isGrounded && velocityY < 0f) velocityY = -2f;
            velocityY += -20f * dt;
            controller.Move(displacement + Vector3.up * velocityY * dt);
        }
    }
}
