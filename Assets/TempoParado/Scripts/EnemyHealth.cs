using System.Collections.Generic;
using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(EnemyChaser), typeof(TimeBody))]
    public sealed class EnemyHealth : MonoBehaviour
    {
        [Min(1)] public int maxHealth = 100;
        [Min(0f)] public float hitReactionDuration = 0.18f;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public int LastDamageTaken { get; private set; }
        private ZeladorBoss boss;
        private CombatFeedback feedback;
        private EnemyChaser brain;
        private TimeBody body;
        private Renderer[] visuals;
        private bool[] originalEnabled;
        private MaterialPropertyBlock[] originalBlocks;
        private MaterialPropertyBlock[] hitBlocks;
        private float flashRemaining;
        private bool flashing;

        private void Awake()
        {
            brain = GetComponent<EnemyChaser>();
            boss = GetComponent<ZeladorBoss>();
            feedback = GetComponent<CombatFeedback>();
            body = GetComponent<TimeBody>();
            AttackTelegraph telegraph = GetComponent<AttackTelegraph>();
            var list = new List<Renderer>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                if (telegraph == null || !telegraph.OwnsRenderer(renderer)) list.Add(renderer);
            visuals = list.ToArray();
            originalEnabled = new bool[visuals.Length];
            originalBlocks = new MaterialPropertyBlock[visuals.Length];
            hitBlocks = new MaterialPropertyBlock[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                originalEnabled[i] = visuals[i].enabled;
                originalBlocks[i] = new MaterialPropertyBlock();
                hitBlocks[i] = new MaterialPropertyBlock();
                visuals[i].GetPropertyBlock(originalBlocks[i]);
                visuals[i].GetPropertyBlock(hitBlocks[i]);
            }
            Restore();
        }
        private void Update()
        {
            if (LabReset.Requested) { Restore(); return; }
            if (IsDead || !flashing) return;
            flashRemaining = Mathf.Max(0f, flashRemaining - body.DeltaTime);
            if (flashRemaining <= 0f) RestoreColors();
        }
        public bool TakeDamage(int damage)
        {
            if (!isActiveAndEnabled || IsDead || damage <= 0 || LabReset.Requested) return false;
            float multiplier = boss != null && boss.isActiveAndEnabled ?
                (brain.IsRecovering ? Mathf.Max(1f, boss.exposedMultiplier) : Mathf.Clamp(boss.armorMultiplier, 0.1f, 1f)) : 1f;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
            LastDamageTaken = Mathf.Min(CurrentHealth, finalDamage);
            CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
            if (feedback != null && feedback.isActiveAndEnabled)
                feedback.PlayHit(boss != null && boss.isActiveAndEnabled && brain.IsRecovering);
            if (IsDead)
            {
                brain.Defeat();
                RestoreColors();
                foreach (Renderer renderer in visuals) if (renderer != null) renderer.enabled = false;
            }
            else
            {
                flashRemaining = Mathf.Max(0.05f, hitReactionDuration);
                ApplyHitColor();
                brain.ReactToHit(hitReactionDuration);
            }
            return true;
        }
        private void ApplyHitColor()
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] == null || visuals[i].sharedMaterial == null) continue;
                Material material = visuals[i].sharedMaterial;
                Color hitColor = new Color(1f, 0.18f, 0.12f);
                if (material.HasProperty("_BaseColor")) hitBlocks[i].SetColor("_BaseColor", hitColor);
                if (material.HasProperty("_Color")) hitBlocks[i].SetColor("_Color", hitColor);
                visuals[i].SetPropertyBlock(hitBlocks[i]);
            }
            flashing = true;
        }
        private void RestoreColors()
        {
            if (visuals == null) return;
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null) visuals[i].SetPropertyBlock(originalBlocks[i]);
            flashing = false;
            flashRemaining = 0f;
        }
        private void Restore()
        {
            CurrentHealth = Mathf.Max(1, maxHealth);
            LastDamageTaken = 0;
            RestoreColors();
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null) visuals[i].enabled = originalEnabled[i];
        }
        private void OnDisable() { RestoreColors(); }
    }
}
