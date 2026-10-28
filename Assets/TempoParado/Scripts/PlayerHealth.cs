using UnityEngine;

namespace TempoParado
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Min(1)] public int maxHealth = 100;
        [Min(0.1f)] public float restartDelay = 2f;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        private float flashUntil;
        private float restartAt;
        private bool restartQueued;
        private void Awake() { Restore(); }
        private void Update()
        {
            if (LabReset.Requested) { Restore(); return; }
            if (IsDead && !restartQueued && Time.unscaledTime >= restartAt)
            {
                restartQueued = true;
                LabReset.RequestNextFrame();
            }
        }
        public bool TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || LabReset.Requested) return false;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            flashUntil = Time.unscaledTime + 0.18f;
            if (IsDead) restartAt = Time.unscaledTime + Mathf.Max(0.1f, restartDelay);
            return true;
        }
        private void Restore()
        {
            CurrentHealth = Mathf.Max(1, maxHealth);
            flashUntil = 0f;
            restartAt = 0f;
            restartQueued = false;
        }
        private void OnGUI()
        {
            Color original = GUI.color;
            GUI.Box(new Rect(12f, 214f, 300f, 58f), "VIDA: " + CurrentHealth + " / " + Mathf.Max(1, maxHealth));
            GUI.color = new Color(0.15f, 0.8f, 0.35f);
            GUI.DrawTexture(new Rect(24f, 246f, 276f * CurrentHealth / Mathf.Max(1, maxHealth), 12f), Texture2D.whiteTexture);
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
