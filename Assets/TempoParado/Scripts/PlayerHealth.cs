using UnityEngine;

namespace TempoParado
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Min(1)] public int maxHealth = 100;
        [Min(0.1f)] public float restartDelay = 2f;
        public int CurrentHealth { get; private set; }
        public int EffectiveMaxHealth => Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, maxHealth) *
            (strain == null ? 1f : strain.MaxHealthMultiplier)));
        public bool IsDead => CurrentHealth <= 0;
        public bool IsInvulnerable => dodge != null && dodge.isActiveAndEnabled && dodge.IsInvulnerable;
        private PlayerDodge dodge;
        private TemporalStrain strain;
        private float flashUntil;
        private float restartAt;
        private bool restartQueued;
        private int temporarilyRemovedHealth;
        private int appliedMaxHealth;
        private void Awake()
        {
            dodge = GetComponent<PlayerDodge>();
            strain = GetComponent<TemporalStrain>();
            Restore();
        }
        private void Update()
        {
            if (LabReset.Requested) { Restore(); return; }
            SyncTemporalHealth();
            if (IsDead && !restartQueued && Time.unscaledTime >= restartAt)
            {
                restartQueued = true;
                LabReset.RequestNextFrame();
            }
        }
        public bool TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || LabReset.Requested || IsInvulnerable) return false;
            SyncTemporalHealth();
            float multiplier = strain == null ? 1f : strain.DamageTakenMultiplier;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(amount * multiplier));
            CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
            flashUntil = Time.unscaledTime + 0.18f;
            if (IsDead) restartAt = Time.unscaledTime + Mathf.Max(0.1f, restartDelay);
            return true;
        }
        private void Restore()
        {
            CurrentHealth = Mathf.Max(1, maxHealth);
            temporarilyRemovedHealth = 0;
            appliedMaxHealth = Mathf.Max(1, maxHealth);
            flashUntil = 0f;
            restartAt = 0f;
            restartQueued = false;
        }
        private void SyncTemporalHealth()
        {
            // A recuperacao temporal nao ressuscita nem devolve dano de combate.
            if (IsDead) return;
            int newMax = EffectiveMaxHealth;
            if (newMax < appliedMaxHealth)
            {
                int removed = Mathf.Max(0, CurrentHealth - newMax);
                CurrentHealth -= removed;
                temporarilyRemovedHealth += removed;
            }
            else if (newMax > appliedMaxHealth)
            {
                int restored = Mathf.Min(temporarilyRemovedHealth,
                    Mathf.Min(newMax - appliedMaxHealth, Mathf.Max(0, newMax - CurrentHealth)));
                CurrentHealth += restored;
                temporarilyRemovedHealth -= restored;
            }
            appliedMaxHealth = newMax;
        }
        private void OnGUI()
        {
            Color original = GUI.color;
            GUI.Box(new Rect(12f, 214f, 300f, 58f), "VIDA: " + CurrentHealth + " / " + EffectiveMaxHealth);
            GUI.color = new Color(0.15f, 0.8f, 0.35f);
            GUI.DrawTexture(new Rect(24f, 246f, 276f * CurrentHealth / EffectiveMaxHealth, 12f), Texture2D.whiteTexture);
            GUI.color = original;
            if (Time.unscaledTime < flashUntil)
            {
                GUI.color = new Color(1f, 0.1f, 0.1f, 0.65f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 6f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, Screen.height - 6f, Screen.width, 6f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, 0f, 6f, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - 6f, 0f, 6f, Screen.height), Texture2D.whiteTexture);
                GUI.color = original;
            }
            if (IsDead)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0f, restartAt - Time.unscaledTime));
                float width = Mathf.Min(500f, Screen.width - 24f);
                GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height * 0.4f, width, 70f),
                    "VOCE CAIU\nReiniciando o laboratorio em " + seconds + "s...\nR: reiniciar agora");
            }
        }
    }
}
