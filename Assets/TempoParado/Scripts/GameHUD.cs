using UnityEngine;

namespace TempoParado
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class GameHUD : MonoBehaviour
    {
        public OrphanageRoute route;
        public bool showDebug;
        private static GameHUD active;
        // Cenas sem este componente continuam com os paineis antigos.
        public static bool ShowDebug => active == null || active.showDebug;
        private PlayerHealth health;
        private TemporalStrain strain;
        private WoodenBird bird;
        private PulseAbility pulse;
        private PlayerDodge dodge;
        private PlayerInteractor interactor;
        private GUIStyle textStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() { active = null; }
        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            strain = GetComponent<TemporalStrain>();
            bird = GetComponent<WoodenBird>();
            pulse = GetComponent<PulseAbility>();
            dodge = GetComponent<PlayerDodge>();
            interactor = GetComponent<PlayerInteractor>();
        }
        private void OnEnable() { active = this; }
        private void OnDisable() { if (active == this) active = null; }
        private void Update()
        {
            if (Application.isFocused && LabInput.ToggleHUDPressed) showDebug = !showDebug;
        }
        private void OnGUI()
        {
            if (showDebug)
            {
                GUI.Label(new Rect(12f, Screen.height - 28f, 260f, 24f), "F1: voltar ao HUD compacto");
                return;
            }
            if (health == null || health.IsDead) return;
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            // Mantem a disposicao em janelas menores, sem depender de 1280x720.
            float scale = Mathf.Max(0.1f, Mathf.Min(1f, Mathf.Min(Screen.width / 1024f, Screen.height / 576f)));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            try
            {
                float width = Screen.width / scale;
                float height = Screen.height / scale;
                if (textStyle == null)
                {
                    textStyle = new GUIStyle(GUI.skin.label);
                    textStyle.fontSize = 14;
                    textStyle.alignment = TextAnchor.MiddleLeft;
                    textStyle.wordWrap = true;
                }
                GUI.Box(new Rect(16f, 16f, 220f, 54f), GUIContent.none);
                Label(new Rect(28f, 20f, 196f, 24f), "VIDA  " + health.CurrentHealth + " / " + health.EffectiveMaxHealth);
                Bar(new Rect(28f, 50f, 196f, 8f), (float)health.CurrentHealth / health.EffectiveMaxHealth, new Color(0.2f, 0.8f, 0.4f));

                float center = width * 0.5f;
                GUI.Box(new Rect(center - 200f, 16f, 400f, 48f), GUIContent.none);
                Label(new Rect(center - 188f, 18f, 376f, 44f), route == null ? "Explore o laboratorio temporal." : route.ObjectiveText);
                if (route != null && route.guardian != null && !route.guardian.IsDead &&
                    (route.guardian.transform.position - transform.position).sqrMagnitude < 196f)
                {
                    EnemyChaser enemy = route.guardian.GetComponent<EnemyChaser>();
                    if (enemy != null && enemy.IsBossEncounter)
                    {
                        GUI.Box(new Rect(center - 200f, 76f, 400f, 66f), GUIContent.none);
                        Label(new Rect(center - 188f, 77f, 376f, 22f), "ZELADOR DO ORFANATO | FASE " + enemy.BossPhase);
                        Bar(new Rect(center - 188f, 103f, 376f, 7f),
                            (float)route.guardian.CurrentHealth / Mathf.Max(1, route.guardian.maxHealth), new Color(0.95f, 0.3f, 0.15f));
                        Label(new Rect(center - 188f, 114f, 376f, 24f),
                            enemy.IsRecovering ? "EXPOSTO: cause mais dano agora" : enemy.StateLabel + " | ARMADURA");
                    }
                    else
                    {
                        GUI.Box(new Rect(center - 120f, 76f, 240f, 38f), GUIContent.none);
                        Label(new Rect(center - 108f, 77f, 216f, 22f), "ZELADOR");
                        Bar(new Rect(center - 108f, 103f, 216f, 5f),
                            (float)route.guardian.CurrentHealth / Mathf.Max(1, route.guardian.maxHealth), new Color(0.95f, 0.35f, 0.2f));
                    }
                }

                GUI.Box(new Rect(width - 246f, 16f, 230f, 80f), GUIContent.none);
                string birdState = bird == null || !bird.isActiveAndEnabled ? "--" : bird.IsAvailable ? "disponivel" : "esgotado";
                string pulseState = pulse == null || !pulse.isActiveAndEnabled ? "--" : pulse.CooldownRemaining > 0f ? pulse.CooldownRemaining.ToString("0.0") + "s" : "pronto";
                string dodgeState = dodge == null || !dodge.isActiveAndEnabled ? "--" : dodge.CooldownRemaining > 0f ? dodge.CooldownRemaining.ToString("0.0") + "s" : "pronta";
                if (strain != null && strain.isActiveAndEnabled && strain.IsCollapsed)
                {
                    pulseState = "bloqueado";
                    dodgeState = "bloqueada";
                }
                Label(new Rect(width - 234f, 20f, 206f, 22f), "F  Passaro: " + birdState);
                Label(new Rect(width - 234f, 44f, 206f, 22f), "Q  Pulso: " + pulseState);
                Label(new Rect(width - 234f, 68f, 206f, 22f), "Espaco  Esquiva: " + dodgeState);

                if (strain != null && strain.isActiveAndEnabled)
                {
                    GUI.Box(new Rect(center - 180f, height - 104f, 360f, 56f), GUIContent.none);
                    string condition = Mathf.Abs(strain.Value) < 0.05f ? "EQUILIBRIO" : strain.Value < 0f ? "LENTA" : "ACELERADA";
                    Label(new Rect(center - 166f, height - 101f, 332f, 26f), condition + " | " + strain.StageLabel);
                    Rect bar = new Rect(center - 164f, height - 68f, 328f, 8f);
                    Fill(new Rect(bar.x, bar.y, bar.width * 0.5f, bar.height), new Color(0.2f, 0.45f, 0.85f));
                    Fill(new Rect(bar.x + bar.width * 0.5f, bar.y, bar.width * 0.5f, bar.height), new Color(0.95f, 0.4f, 0.1f));
                    Fill(new Rect(bar.x + bar.width * 0.5f - 1f, bar.y - 2f, 2f, 12f), Color.black);
                    Fill(new Rect(bar.x + (strain.Value + 1f) * 0.5f * (bar.width - 4f), bar.y - 3f, 4f, 14f), Color.white);
                }
                string hint = interactor == null || !interactor.isActiveAndEnabled || (strain != null && strain.IsCollapsed) ? null : interactor.ActiveHint;
                if (!string.IsNullOrEmpty(hint)) Label(new Rect(center - 220f, height - 40f, 440f, 30f), hint);
                if (bird != null && bird.isActiveAndEnabled && !string.IsNullOrEmpty(bird.ActiveMessage))
                {
                    GUI.Box(new Rect(center - 220f, height - 152f, 440f, 36f), GUIContent.none);
                    Label(new Rect(center - 208f, height - 150f, 416f, 32f), bird.ActiveMessage);
                }
                Label(new Rect(16f, height - 32f, 250f, 24f), "F1: detalhes  |  R: reiniciar");
            }
            finally { GUI.matrix = previous; GUI.color = previousColor; }
        }
        private void Label(Rect rect, string text) { GUI.Label(rect, text, textStyle); }
        private static void Fill(Rect rect, Color color)
        {
            Color original = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = original;
        }
        private static void Bar(Rect rect, float amount, Color color)
        {
            Fill(rect, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(amount), rect.height), color);
        }
    }
}
