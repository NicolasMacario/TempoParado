using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(EnemyChaser))]
    public sealed class EnemyLabHUD : MonoBehaviour
    {
        private EnemyChaser enemy;
        private void Awake() { enemy = GetComponent<EnemyChaser>(); }
        private void OnGUI()
        {
            if (enemy == null) return;
            float width = Mathf.Min(310f, Screen.width - 24f);
            float x = Mathf.Max(12f, Screen.width - width - 12f);
            GUI.Box(new Rect(x, 12f, width, 176f), "ZELADOR | perseguicao e ataque");
            GUI.Label(new Rect(x + 12f, 40f, width - 24f, 24f), "Estado: " + enemy.StateLabel);
            GUI.Label(new Rect(x + 12f, 65f, width - 24f, 24f), "Ritmo local: " + enemy.LocalScale.ToString("0.0") + "x");
            GUI.Label(new Rect(x + 12f, 90f, width - 24f, 24f), enemy.LastAttackLabel);
            GUI.Label(new Rect(x + 12f, 115f, width - 24f, 24f), "Fase restante: " + enemy.PhaseRemaining.ToString("0.0") + "s locais");
            GUI.Label(new Rect(x + 12f, 140f, width - 24f, 36f), enemy.HasCombat ?
                "Saia do setor colorido antes do golpe.\nR: reiniciar" : "Use o menu Ativar combate do zelador.");
        }
    }
}
