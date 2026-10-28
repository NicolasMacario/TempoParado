using UnityEngine;

namespace TempoParado
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(TimeBody), typeof(PlayerHealth))]
    public sealed class TemporalStrain : MonoBehaviour
    {
        [Header("Fracao da barra por segundo real")]
        [Min(0f)] public float slowBuildRate = 0.06f;
        [Min(0f)] public float fastBuildRate = 0.08f;
        [Min(0f)] public float normalRecoveryRate = 0.12f;
        [Header("Colapso")]
        [Min(0.1f)] public float collapseDuration = 2f;
        [Range(0f, 0.95f)] public float postCollapseMagnitude = 0.4f;
        public float Value { get; private set; }
        public float CollapseRemaining { get; private set; }
        public bool IsCollapsed => CollapseRemaining > 0f;
        private float collapseSide;
        private TimeBody body;
        private PlayerHealth health;

        private void Awake() { body = GetComponent<TimeBody>(); health = GetComponent<PlayerHealth>(); }
        private void Update()
        {
            if (LabReset.Requested || health.IsDead) { ResetState(); return; }
            float dt = Time.unscaledDeltaTime;
            if (IsCollapsed)
            {
                CollapseRemaining = Mathf.Max(0f, CollapseRemaining - dt);
                if (!IsCollapsed) Value = collapseSide * Mathf.Clamp(postCollapseMagnitude, 0f, 0.95f);
                return;
            }
            float environment = body.EnvironmentTimeScale;
            if (Mathf.Approximately(environment, 1f))
                Value = Mathf.MoveTowards(Value, 0f, Mathf.Max(0f, normalRecoveryRate) * dt);
            else if (environment > 0f && environment < 1f)
                Value -= Mathf.Max(0f, slowBuildRate) * dt;
            else if (environment > 1f)
                Value += Mathf.Max(0f, fastBuildRate) * dt;
            // No mundo congelado fora dos bolsoes, o valor e mantido.
            Value = Mathf.Clamp(Value, -1f, 1f);
            if (Mathf.Abs(Value) >= 1f)
            {
                collapseSide = Mathf.Sign(Value);
                CollapseRemaining = Mathf.Max(0.1f, collapseDuration);
            }
        }
        public void ResetState() { Value = 0f; CollapseRemaining = 0f; collapseSide = 0f; }
        private void OnDisable() { ResetState(); }
        private string StageLabel
        {
            get
            {
                float magnitude = Mathf.Abs(Value);
                return IsCollapsed ? "COLAPSO" : magnitude < 0.05f ? "EQUILIBRIO" :
                    magnitude < 0.35f ? "LEVE" : magnitude < 0.7f ? "MEDIO" : "CRITICO";
            }
        }
        private void OnGUI()
        {
            if (health == null || health.IsDead || body == null) return;
            float width = Mathf.Min(330f, Screen.width - 24f);
            float x = Screen.width - width - 12f;
            GUI.Box(new Rect(x, 388f, width, 108f), "DESGASTE TEMPORAL | " + StageLabel);
            float environment = body.EnvironmentTimeScale;
            string region = environment <= 0f ? "Parado" : Mathf.Approximately(environment, 1f) ? "Normal" : environment < 1f ? "Lento" : "Rapido";
            string value = Mathf.Abs(Value) < 0.005f ? "0%" : (Value < 0f ? "-" : "+") + (Mathf.Abs(Value) * 100f).ToString("0") + "%";
            GUI.Label(new Rect(x + 12f, 416f, width - 24f, 24f), "Ambiente: " + region + " | Desgaste: " + value);
            float left = x + 16f;
            float barWidth = width - 32f;
            Color original = GUI.color;
            GUI.color = new Color(0.15f, 0.4f, 0.8f);
            GUI.DrawTexture(new Rect(left, 448f, barWidth * 0.5f, 12f), Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.45f, 0.1f);
            GUI.DrawTexture(new Rect(left + barWidth * 0.5f, 448f, barWidth * 0.5f, 12f), Texture2D.whiteTexture);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(left + barWidth * 0.5f - 1f, 446f, 2f, 16f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float marker = left + (Value + 1f) * 0.5f * (barWidth - 4f);
            GUI.DrawTexture(new Rect(marker, 444f, 4f, 20f), Texture2D.whiteTexture);
            GUI.color = original;
            GUI.Label(new Rect(left, 468f, 80f, 22f), "LENTO");
            GUI.Label(new Rect(left + barWidth * 0.5f - 35f, 468f, 90f, 22f), "EQUILIBRIO");
            GUI.Label(new Rect(left + barWidth - 80f, 468f, 85f, 22f), "ACELERADO");
            if (IsCollapsed)
            {
                float overlayWidth = Mathf.Min(440f, Screen.width - 24f);
                GUI.Box(new Rect((Screen.width - overlayWidth) * 0.5f, Screen.height * 0.36f, overlayWidth, 70f),
                    "COLAPSO TEMPORAL\nAguarde " + CollapseRemaining.ToString("0.0") + "s | R: reiniciar\nVoce continua vulneravel aos ataques.");
            }
        }
    }
}
