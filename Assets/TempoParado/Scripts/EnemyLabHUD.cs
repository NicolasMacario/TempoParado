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
            GUI.Box(new Rect(x, 12f, width, 124f), "ZELADOR | teste de perseguicao");
            GUI.Label(new Rect(x + 12f, 40f, width - 24f, 24f), "Estado: " + enemy.StateLabel);
            GUI.Label(new Rect(x + 12f, 65f, width - 24f, 24f), "Ritmo local: " + enemy.LocalScale.ToString("0.0") + "x");
            GUI.Label(new Rect(x + 12f, 90f, width - 24f, 36f), "Atraia-o para os pisos coloridos.\nR: reiniciar | Ainda nao causa dano.");
        }
    }
}
