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
        [Header("Ataque: segundos do relogio local")]
        [Min(0.1f)] public float attackRange = 2.2f;
        [Range(10f, 180f)] public float attackArc = 90f;
        [Min(0.05f)] public float windupDuration = 1f;
        [Min(0.05f)] public float impactDuration = 0.15f;
        [Min(0.05f)] public float recoveryDuration = 1.1f;
        [Min(1)] public int attackDamage = 20;
        public string StateLabel { get; private set; } = "Vigiando";
        public string LastAttackLabel { get; private set; } = "Nenhum golpe ainda";
        public bool HasCombat => targetHealth != null && telegraph != null && telegraph.sourceMaterial != null;
        public float PhaseRemaining => phaseTimer;
        public float LocalScale => body == null ? 1f : body.LocalTimeScale;
        private CharacterController controller;
        private TimeBody body;
        private Vector3 spawn;
        private Quaternion spawnRotation;
        private float velocityY;
        private bool alerted;
        private enum Phase { Approach, Windup, Impact, Recover }
        private Phase phase;
        private float phaseTimer;
        private PlayerHealth targetHealth;
        private AttackTelegraph telegraph;
        private EnemyHealth enemyHealth;
        private float hitReactionRemaining;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<TimeBody>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            targetHealth = target == null ? null : target.GetComponent<PlayerHealth>();
            telegraph = GetComponent<AttackTelegraph>();
            enemyHealth = GetComponent<EnemyHealth>();
        }
        private void Update()
        {
            if (LabReset.Requested)
            {
                controller.enabled = false;
                transform.SetPositionAndRotation(spawn, spawnRotation);
                controller.enabled = true;
                velocityY = 0f;
                alerted = false;
                hitReactionRemaining = 0f;
                CancelAttack();
                LastAttackLabel = "Nenhum golpe ainda";
                StateLabel = "Vigiando";
                return;
            }
            if (enemyHealth != null && enemyHealth.IsDead) { Defeat(); return; }
            if (target == null) { StateLabel = "Sem alvo"; return; }
            if (targetHealth != null && targetHealth.IsDead)
            {
                CancelAttack();
                alerted = false;
                StateLabel = "Aguardando reinicio";
                return;
            }
            float dt = body.DeltaTime;
            if (dt <= 0f) { StateLabel = "Congelado"; return; }
            if (hitReactionRemaining > 0f)
            {
                hitReactionRemaining = Mathf.Max(0f, hitReactionRemaining - dt);
                StateLabel = "Reagindo ao golpe";
                Move(Vector3.zero, 0f, dt);
                return;
            }
            if (phase != Phase.Approach)
            {
                TickAttack(dt);
                Move(Vector3.zero, 0f, dt);
                return;
            }
            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (!alerted && distance <= detectionRange) alerted = true;
            else if (alerted && distance > Mathf.Max(loseRange, detectionRange)) alerted = false;
            Vector3 direction = Vector3.zero;
            if (!alerted) StateLabel = "Vigiando";
            else if (HasCombat && distance <= attackRange && HasClearHitPath())
            {
                if (offset.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(offset);
                phase = Phase.Windup;
                phaseTimer = Mathf.Max(0.05f, windupDuration);
                StateLabel = "Preparando golpe";
                telegraph.Show(0f);
            }
            else if (distance <= stopDistance) StateLabel = HasCombat ? "Golpe bloqueado" : "Ao alcance (ative combate)";
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
            Move(direction, horizontalStep, dt);
        }
        private void TickAttack(float dt)
        {
            StateLabel = phase == Phase.Windup ? "Preparando golpe" : phase == Phase.Impact ? "Golpe" : "Recuperando";
            phaseTimer = Mathf.Max(0f, phaseTimer - dt);
            if (phase == Phase.Windup)
            {
                telegraph.Show(1f - phaseTimer / Mathf.Max(0.05f, windupDuration));
                if (phaseTimer > 0f) return;
                TryHit(); // Um unico teste de dano por golpe, no fim da preparacao.
                phase = Phase.Impact;
                phaseTimer = Mathf.Max(0.05f, impactDuration);
                StateLabel = "Golpe";
                telegraph.Show(1f, true);
            }
            else if (phase == Phase.Impact && phaseTimer <= 0f)
            {
                phase = Phase.Recover;
                phaseTimer = Mathf.Max(0.05f, recoveryDuration);
                StateLabel = "Recuperando";
                telegraph.Hide();
            }
            else if (phase == Phase.Recover && phaseTimer <= 0f)
            {
                phase = Phase.Approach;
                StateLabel = "Retomando perseguicao";
            }
        }
        private void TryHit()
        {
            Vector3 offset = target.position - transform.position;
            float verticalDistance = Mathf.Abs(offset.y);
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance > attackRange || verticalDistance > 1.5f)
            {
                LastAttackLabel = "Errou: fora do alcance";
                return;
            }
            float minimumDot = Mathf.Cos(attackArc * 0.5f * Mathf.Deg2Rad);
            if (distance > 0.001f && Vector3.Dot(transform.forward, offset / distance) < minimumDot)
            {
                LastAttackLabel = "Errou: saiu da area";
                return;
            }
            if (!HasClearHitPath()) { LastAttackLabel = "Bloqueado por obstaculo"; return; }
            LastAttackLabel = targetHealth.TakeDamage(attackDamage) ? "Acertou: -" + attackDamage + " de vida" :
                targetHealth.IsInvulnerable ? "Esquiva: sem dano" : "Sem dano";
        }
        private bool HasClearHitPath()
        {
            Vector3 start = transform.position + Vector3.up * 0.25f;
            Vector3 offset = target.position + Vector3.up * 0.25f - start;
            if (offset.sqrMagnitude < 0.0001f) return true;
            foreach (RaycastHit hit in Physics.RaycastAll(start, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(target)) continue;
                return false;
            }
            return true;
        }
        private void Move(Vector3 direction, float horizontalStep, float dt)
        {
            if (controller.isGrounded && velocityY < 0f) velocityY = -2f;
            velocityY = Mathf.Max(velocityY - 20f * dt, -30f);
            controller.Move(direction * horizontalStep + Vector3.up * velocityY * dt);
        }
        public void ReactToHit(float duration)
        {
            // Golpes preparados continuam: nao e possivel impedir todo ataque apenas clicando.
            if (phase == Phase.Windup || phase == Phase.Impact) return;
            hitReactionRemaining = Mathf.Max(hitReactionRemaining, Mathf.Max(0f, duration));
        }
        public void Defeat()
        {
            CancelAttack();
            alerted = false;
            hitReactionRemaining = 0f;
            StateLabel = "Derrotado";
            if (controller != null) controller.enabled = false;
        }
        private void CancelAttack()
        {
            phase = Phase.Approach;
            phaseTimer = 0f;
            if (telegraph != null) telegraph.Hide();
        }
        private void OnDisable() { CancelAttack(); }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
