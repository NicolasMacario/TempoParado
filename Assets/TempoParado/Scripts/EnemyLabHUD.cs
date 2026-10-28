using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(EnemyChaser))]
    public sealed class EnemyLabHUD : MonoBehaviour
    {
        private EnemyChaser enemy;
        private EnemyHealth health;
        private TimeBody clock;
        private void Awake()
        {
            enemy = GetComponent<EnemyChaser>();
            health = GetComponent<EnemyHealth>();
            clock = GetComponent<TimeBody>();
        }
        private void OnGUI()
        {
            if (enemy == null) return;
            float width = Mathf.Min(310f, Screen.width - 24f);
            float x = Mathf.Max(12f, Screen.width - width - 12f);
            bool slowed = clock != null && clock.HasTemporarySlow;
            float height = (health == null ? 176f : 226f) + (slowed ? 28f : 0f);
            GUI.Box(new Rect(x, 12f, width, height), "ZELADOR | perseguicao e ataque");
            GUI.Label(new Rect(x + 12f, 40f, width - 24f, 24f), "Estado: " + enemy.StateLabel);
            GUI.Label(new Rect(x + 12f, 65f, width - 24f, 24f), "Ritmo local: " + enemy.LocalScale.ToString("0.0") + "x");
            GUI.Label(new Rect(x + 12f, 90f, width - 24f, 24f), enemy.LastAttackLabel);
            GUI.Label(new Rect(x + 12f, 115f, width - 24f, 24f), "Fase restante: " + enemy.PhaseRemaining.ToString("0.0") + "s locais");
            GUI.Label(new Rect(x + 12f, 140f, width - 24f, 36f), health != null && health.IsDead ?
                "Zelador derrotado!\nR: enfrentar novamente" : enemy.HasCombat ?
                "Saia do setor colorido antes do golpe.\nR: reiniciar" : "Use o menu Ativar combate do zelador.");
            if (health != null)
            {
                GUI.Label(new Rect(x + 12f, 180f, width - 24f, 24f), "Vida: " + health.CurrentHealth + " / " + Mathf.Max(1, health.maxHealth));
                Color original = GUI.color;
                GUI.color = new Color(0.95f, 0.3f, 0.1f);
                if (health.CurrentHealth > 0)
                    GUI.DrawTexture(new Rect(x + 12f, 211f, (width - 24f) * health.CurrentHealth / Mathf.Max(1, health.maxHealth), 12f), Texture2D.whiteTexture);
                GUI.color = original;
            }
            if (slowed)
                GUI.Label(new Rect(x + 12f, health == null ? 180f : 236f, width - 24f, 24f),
                    "Efeito temporal: " + clock.TemporarySlowRemaining.ToString("0.0") + "s reais");
        }
    }
}
