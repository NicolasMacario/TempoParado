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
        public bool IsRecovering => phase == Phase.Recover;
        public bool IsBossEncounter => boss != null && boss.isActiveAndEnabled;
        public int BossPhase => IsBossEncounter && boss.IsSecondPhase ? 2 : 1;
        public string AttackName { get; private set; } = "Golpe";
        private ZeladorBoss boss;
        private int patternCursor;
        private int comboLength = 1;
        private int comboIndex;
        private float strikeRange;
        private float strikeArc;
        private int strikeDamage;
        private float windupTotal;
        private float followupWindup;
        private float recoveryTotal;
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
        private PlayerDodge targetDodge;
        private PerfectDodge targetPerfectDodge;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<TimeBody>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            targetHealth = target == null ? null : target.GetComponent<PlayerHealth>();
            telegraph = GetComponent<AttackTelegraph>();
            enemyHealth = GetComponent<EnemyHealth>();
            targetDodge = target == null ? null : target.GetComponent<PlayerDodge>();
            targetPerfectDodge = target == null ? null : target.GetComponent<PerfectDodge>();
            boss = GetComponent<ZeladorBoss>();
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
                patternCursor = 0;
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
                BeginAttack();
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
            float chaseSpeed = IsBossEncounter ? (boss.IsSecondPhase ? boss.secondPhaseSpeed : boss.movementSpeed) : speed;
            float horizontalStep = direction == Vector3.zero ? 0f :
                Mathf.Min(chaseSpeed * dt, Mathf.Max(0f, distance - stopDistance));
            Move(direction, horizontalStep, dt);
        }
        private string AttackProgress => comboLength > 1 ? AttackName + " " + (comboIndex + 1) + "/" + comboLength : AttackName;
        private void BeginAttack()
        {
            strikeRange = attackRange;
            strikeArc = attackArc;
            strikeDamage = attackDamage;
            recoveryTotal = recoveryDuration;
            comboLength = 1;
            comboIndex = 0;
            float preparation = windupDuration;
            AttackName = "Golpe";
            if (IsBossEncounter)
            {
                float pace = boss.IsSecondPhase ? Mathf.Clamp(boss.secondPhaseWindupMultiplier, 0.5f, 1f) : 1f;
                int pattern = patternCursor % 3;
                patternCursor = (patternCursor + 1) % 3;
                if (pattern == 0)
                {
                    AttackName = "Varredura";
                    strikeRange = 3.1f; strikeArc = 150f; strikeDamage = boss.sweepDamage;
                    preparation = boss.sweepWindup * pace;
                    recoveryTotal = 1.2f;
                }
                else if (pattern == 1)
                {
                    AttackName = "Sequencia";
                    strikeRange = 3f; strikeArc = 110f; strikeDamage = boss.comboDamage;
                    comboLength = boss.IsSecondPhase ? 3 : 2;
                    preparation = boss.comboFirstWindup * pace;
                    followupWindup = boss.comboNextWindup * pace;
                    recoveryTotal = 1.5f;
                }
                else
                {
                    AttackName = "Impacto circular";
                    strikeRange = 4f; strikeArc = 360f; strikeDamage = boss.slamDamage;
                    preparation = boss.slamWindup * pace;
                    recoveryTotal = 1.9f;
                }
            }
            BeginWindup(preparation);
        }
        private void FaceTarget()
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
        }
        private void BeginWindup(float duration)
        {
            FaceTarget();
            phase = Phase.Windup;
            windupTotal = phaseTimer = Mathf.Max(0.05f, duration);
            StateLabel = "Preparando " + AttackProgress;
            telegraph.SetArea(strikeRange, strikeArc);
            telegraph.Show(0f);
        }
        private void TickAttack(float dt)
        {
            StateLabel = phase == Phase.Windup ? "Preparando " + AttackProgress : phase == Phase.Impact ? "Golpe" :
                IsBossEncounter ? "EXPOSTO: ataque agora" : "Recuperando";
            if (phase == Phase.Windup && IsBossEncounter &&
                phaseTimer > windupTotal * (1f - Mathf.Clamp01(boss.trackingFraction))) FaceTarget();
            phaseTimer = Mathf.Max(0f, phaseTimer - dt);
            if (phase == Phase.Windup)
            {
                telegraph.Show(1f - phaseTimer / windupTotal);
                if (phaseTimer > 0f) return;
                TryHit(); // Um unico teste de dano por golpe, no fim da preparacao.
                phase = Phase.Impact;
                phaseTimer = Mathf.Max(0.05f, impactDuration);
                StateLabel = "Golpe";
                telegraph.Show(1f, true);
            }
            else if (phase == Phase.Impact && phaseTimer <= 0f)
            {
                if (comboIndex + 1 < comboLength)
                {
                    comboIndex++;
                    BeginWindup(followupWindup);
                    return;
                }
                phase = Phase.Recover;
                phaseTimer = Mathf.Max(0.05f, recoveryTotal);
                StateLabel = IsBossEncounter ? "EXPOSTO: ataque agora" : "Recuperando";
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
            // Reconhece o ponto de onde o jogador escapou, apenas na janela ciano.
            if (targetHealth.IsInvulnerable && targetDodge != null && targetPerfectDodge != null &&
                CanStrikePosition(targetDodge.DodgeStartPosition) && targetPerfectDodge.TryTrigger(this))
            {
                LastAttackLabel = "Esquiva perfeita!";
                return;
            }
            Vector3 offset = target.position - transform.position;
            float verticalDistance = Mathf.Abs(offset.y);
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance > strikeRange || verticalDistance > 1.5f)
            {
                LastAttackLabel = "Errou: fora do alcance";
                return;
            }
            float minimumDot = Mathf.Cos(strikeArc * 0.5f * Mathf.Deg2Rad);
            if (distance > 0.001f && Vector3.Dot(transform.forward, offset / distance) < minimumDot)
            {
                LastAttackLabel = "Errou: saiu da area";
                return;
            }
            if (!HasClearHitPath()) { LastAttackLabel = "Bloqueado por obstaculo"; return; }
            if (targetHealth.IsInvulnerable && targetPerfectDodge != null && targetPerfectDodge.TryTrigger(this))
            {
                LastAttackLabel = "Esquiva perfeita!";
                return;
            }
            int before = targetHealth.CurrentHealth;
            LastAttackLabel = targetHealth.TakeDamage(strikeDamage) ? "Acertou: -" + (before - targetHealth.CurrentHealth) + " de vida" :
                targetHealth.IsInvulnerable ? "Esquiva: sem dano" : "Sem dano";
        }
        private bool CanStrikePosition(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            if (Mathf.Abs(offset.y) > 1.5f) return false;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance > strikeRange) return false;
            if (distance > 0.001f && Vector3.Dot(transform.forward, offset / distance) <
                Mathf.Cos(strikeArc * 0.5f * Mathf.Deg2Rad)) return false;
            return HasClearHitPath(position);
        }
        private bool HasClearHitPath() { return HasClearHitPath(target.position); }
        private bool HasClearHitPath(Vector3 position)
        {
            Vector3 start = transform.position + Vector3.up * 0.25f;
            Vector3 offset = position + Vector3.up * 0.25f - start;
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
            if (IsBossEncounter) return; // Armadura impede travar o chefe com cliques repetidos.
            // Golpes preparados continuam: nao e possivel impedir todo ataque apenas clicando.
            if (phase == Phase.Windup || phase == Phase.Impact) return;
            hitReactionRemaining = Mathf.Max(hitReactionRemaining, Mathf.Max(0f, duration));
        }
        public void Defeat()
        {
            if (body != null) body.ClearTemporalEffects();
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
            comboLength = 1;
            comboIndex = 0;
            AttackName = "Golpe";
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
