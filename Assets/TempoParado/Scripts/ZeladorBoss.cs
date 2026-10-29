using UnityEngine;

namespace TempoParado
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyChaser), typeof(EnemyHealth))]
    public sealed class ZeladorBoss : MonoBehaviour
    {
        [Header("Fases e perseguicao")]
        [Range(0.1f, 0.9f)] public float secondPhaseThreshold = 0.5f;
        [Min(0.1f)] public float movementSpeed = 3.4f;
        [Min(0.1f)] public float secondPhaseSpeed = 4.1f;
        [Range(0.5f, 1f)] public float secondPhaseWindupMultiplier = 0.85f;
        [Range(0f, 0.8f)] public float trackingFraction = 0.45f;
        [Header("Recebimento de dano")]
        [Range(0.1f, 1f)] public float armorMultiplier = 0.5f;
        [Range(1f, 3f)] public float exposedMultiplier = 1.25f;
        [Header("Varredura")]
        [Min(1)] public int sweepDamage = 26;
        [Min(0.3f)] public float sweepWindup = 0.95f;
        [Header("Sequencia: dois golpes, tres na fase 2")]
        [Min(1)] public int comboDamage = 22;
        [Min(0.3f)] public float comboFirstWindup = 0.9f;
        [Min(0.3f)] public float comboNextWindup = 0.65f;
        [Header("Impacto circular")]
        [Min(1)] public int slamDamage = 38;
        [Min(0.6f)] public float slamWindup = 1.4f;
        private EnemyHealth health;
        public bool IsSecondPhase => health != null && !health.IsDead &&
            health.CurrentHealth <= Mathf.Max(1, health.maxHealth) * secondPhaseThreshold;
        private void Awake() { health = GetComponent<EnemyHealth>(); }
    }
}
