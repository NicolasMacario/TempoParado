using System.Collections.Generic;
using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("Ataque basico: segundos locais do jogador")]
        [Min(1)] public int damage = 25;
        [Min(0.1f)] public float range = 2.4f;
        [Range(10f, 180f)] public float arc = 100f;
        [Min(0.05f)] public float windupDuration = 0.18f;
        [Min(0.05f)] public float impactDuration = 0.12f;
        [Min(0.05f)] public float recoveryDuration = 0.35f;
        [Range(0f, 1f)] public float movementMultiplier = 0.35f;
        public Transform weaponPivot;
        public bool IsAttacking => phase != Phase.Idle;
        public Vector3 LockedDirection { get; private set; }
        public string LastResult { get; private set; } = "Nenhum golpe ainda";
        private enum Phase { Idle, Windup, Impact, Recover }
        private Phase phase;
        private float timer;
        private float phaseDuration;
        private bool hitApplied;
        private float currentAttackSpeed = 1f;
        private Quaternion restRotation = Quaternion.identity;
        private bool initialized;
        private PlayerInteractor interactor;
        private PlayerHealth health;
        private TemporalStrain strain;
        private readonly HashSet<EnemyHealth> struck = new HashSet<EnemyHealth>();

        private void Awake()
        {
            interactor = GetComponent<PlayerInteractor>();
            health = GetComponent<PlayerHealth>();
            strain = GetComponent<TemporalStrain>();
            restRotation = weaponPivot == null ? Quaternion.identity : weaponPivot.localRotation;
            initialized = true;
        }
        // Chamado pelo motor antes do movimento; a esquiva tem prioridade.
        public void Step(float dt, Vector3 aimDirection, bool requested, bool dodging)
        {
            if (dodging)
            {
                if (IsAttacking) LastResult = "Ataque cancelado pela esquiva";
                CancelAttack();
                return;
            }
            if (dt <= 0f) return;
            if (phase == Phase.Idle)
            {
                if (!requested) return;
                // O primeiro clique depois de Esc captura a camera, sem atacar.
                if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked || LabInput.ReleaseCameraPressed) return;
                if (health != null && health.IsDead) return;
                if (interactor != null && interactor.HeldItem != null)
                {
                    LastResult = "Solte a esfera com E antes de atacar";
                    return;
                }
                aimDirection.y = 0f;
                LockedDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : transform.forward;
                transform.rotation = Quaternion.LookRotation(LockedDirection);
                phase = Phase.Windup;
                // Capturado no inicio: atravessar a borda nao altera um golpe em andamento.
                currentAttackSpeed = strain == null ? 1f : strain.AttackSpeedMultiplier;
                phaseDuration = timer = Mathf.Max(0.05f, windupDuration);
                hitApplied = false;
                LastResult = "Preparando ataque";
                AnimateWeapon();
                return;
            }
            timer = Mathf.Max(0f, timer - dt * currentAttackSpeed);
            if (phase == Phase.Windup && timer <= 0f)
            {
                phase = Phase.Impact;
                phaseDuration = timer = Mathf.Max(0.05f, impactDuration);
            }
            else if (phase == Phase.Impact)
            {
                if (!hitApplied && timer <= phaseDuration * 0.5f)
                {
                    hitApplied = true;
                    ApplyHit();
                }
                if (timer <= 0f)
                {
                    phase = Phase.Recover;
                    phaseDuration = timer = Mathf.Max(0.05f, recoveryDuration);
                }
            }
            else if (phase == Phase.Recover && timer <= 0f) phase = Phase.Idle;
            AnimateWeapon();
        }
        private void ApplyHit()
        {
            struck.Clear();
            int hits = 0;
            int damageDealt = 0;
            bool defeated = false;
            foreach (Collider collider in Physics.OverlapSphere(transform.position, range, ~0, QueryTriggerInteraction.Ignore))
            {
                EnemyHealth enemy = collider.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || !struck.Add(enemy)) continue;
                Vector3 offset = enemy.transform.position - transform.position;
                if (Mathf.Abs(offset.y) > 1.5f) continue;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > range) continue;
                if (distance > 0.001f && Vector3.Dot(LockedDirection, offset / distance) < Mathf.Cos(arc * 0.5f * Mathf.Deg2Rad)) continue;
                if (!HasClearPath(enemy.transform)) continue;
                if (!enemy.TakeDamage(damage)) continue;
                hits++;
                damageDealt += enemy.LastDamageTaken;
                defeated |= enemy.IsDead;
            }
            LastResult = defeated ? "Zelador derrotado! R: repetir" : hits > 0 ? "Acertou: " + damageDealt + " de dano" : "Golpe errou";
        }
        private bool HasClearPath(Transform enemy)
        {
            Vector3 start = transform.position + Vector3.up * 0.25f;
            Vector3 offset = enemy.position + Vector3.up * 0.25f - start;
            if (offset.sqrMagnitude < 0.0001f) return true;
            foreach (RaycastHit hit in Physics.RaycastAll(start, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(enemy)) continue;
                return false;
            }
            return true;
        }
        private void AnimateWeapon()
        {
            if (weaponPivot == null) return;
            float progress = phaseDuration <= 0f ? 1f : 1f - timer / phaseDuration;
            float yaw = phase == Phase.Windup ? Mathf.Lerp(0f, -60f, progress) :
                phase == Phase.Impact ? Mathf.Lerp(-60f, 75f, progress) :
                phase == Phase.Recover ? Mathf.Lerp(75f, 0f, progress) : 0f;
            weaponPivot.localRotation = restRotation * Quaternion.Euler(0f, yaw, 0f);
        }
        private void CancelAttack()
        {
            phase = Phase.Idle;
            timer = 0f;
            hitApplied = false;
            struck.Clear();
            if (initialized && weaponPivot != null) weaponPivot.localRotation = restRotation;
        }
        public void ResetState() { CancelAttack(); LastResult = "Nenhum golpe ainda"; }
        public void InterruptAttack(string reason)
        {
            if (IsAttacking) LastResult = reason;
            CancelAttack();
        }
        private void OnDisable() { CancelAttack(); }
        private void OnGUI()
        {
            if (!GameHUD.ShowDebug) return;
            if (health != null && health.IsDead) return;
            string state = strain != null && strain.isActiveAndEnabled && strain.IsCollapsed ? "COLAPSO" :
                phase == Phase.Idle ? "PRONTO" : phase == Phase.Windup ? "PREPARANDO" : phase == Phase.Impact ? "GOLPE" : "RECUPERANDO";
            GUI.Box(new Rect(12f, 360f, 330f, 66f), "BOTAO ESQUERDO: ATAQUE | " + state);
            GUI.Label(new Rect(24f, 392f, 306f, 25f), LastResult);
        }
    }
}
