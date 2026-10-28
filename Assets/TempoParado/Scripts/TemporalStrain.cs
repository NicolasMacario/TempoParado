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
        [Header("Condicao lenta: multiplicadores leve / medio / critico")]
        public Vector3 slowDamageTaken = new Vector3(0.8f, 0.65f, 0.5f);
        public Vector3 slowAttackSpeed = new Vector3(1f, 0.7f, 0.45f);
        public Vector3 slowDodgeDistance = new Vector3(1f, 0.7f, 0.45f);
        [Header("Condicao acelerada: multiplicadores leve / medio / critico")]
        public Vector3 fastMovement = new Vector3(1.2f, 1.45f, 1.75f);
        public Vector3 fastAttackSpeed = new Vector3(1.2f, 1.45f, 1.75f);
        public Vector3 fastMaxHealth = new Vector3(1f, 0.9f, 0.75f);
        public float Value { get; private set; }
        public float CollapseRemaining { get; private set; }
        public bool IsCollapsed => CollapseRemaining > 0f;
        private float collapseSide;
        private TimeBody body;
        private PlayerHealth health;
        private PlayerMotor motor;
        private PlayerCombat combat;
        private PlayerDodge dodge;

        // O ambiente altera a barra; o estado da barra determina os efeitos.
        private int ConditionSide
        {
            get
            {
                if (!isActiveAndEnabled || LabReset.Requested || Mathf.Abs(Value) < 0.05f) return 0;
                return Value < 0f ? -1 : 1;
            }
        }
        private int EffectTier
        {
            get
            {
                float magnitude = Mathf.Abs(Value);
                return magnitude < 0.35f ? 0 : magnitude < 0.7f ? 1 : 2;
            }
        }
        private float Multiplier(Vector3 levels, float minimum, float maximum)
        {
            float value = levels[EffectTier];
            return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp(value, minimum, maximum);
        }
        public float MovementMultiplier => ConditionSide > 0 ? Multiplier(fastMovement, 0.1f, 3f) : 1f;
        public float AttackSpeedMultiplier => ConditionSide < 0 ? Multiplier(slowAttackSpeed, 0.1f, 3f) :
            ConditionSide > 0 ? Multiplier(fastAttackSpeed, 0.1f, 3f) : 1f;
        public float DodgeDistanceMultiplier => ConditionSide < 0 ? Multiplier(slowDodgeDistance, 0.1f, 3f) : 1f;
        public float DamageTakenMultiplier => ConditionSide < 0 ? Multiplier(slowDamageTaken, 0.1f, 3f) : 1f;
        public float MaxHealthMultiplier => ConditionSide > 0 ? Multiplier(fastMaxHealth, 0.1f, 1f) : 1f;

        private void Awake()
        {
            body = GetComponent<TimeBody>();
            health = GetComponent<PlayerHealth>();
            motor = GetComponent<PlayerMotor>();
            combat = GetComponent<PlayerCombat>();
            dodge = GetComponent<PlayerDodge>();
        }
        public void ApplyNoticeableBalance()
        {
            slowDamageTaken = new Vector3(0.8f, 0.65f, 0.5f);
            slowAttackSpeed = new Vector3(1f, 0.7f, 0.45f);
            slowDodgeDistance = new Vector3(1f, 0.7f, 0.45f);
            fastMovement = new Vector3(1.2f, 1.45f, 1.75f);
            fastAttackSpeed = new Vector3(1.2f, 1.45f, 1.75f);
            fastMaxHealth = new Vector3(1f, 0.9f, 0.75f);
        }
        public bool SetConditionForTesting(float value)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || health == null || health.IsDead) return false;
            Value = Mathf.Clamp(value, -0.95f, 0.95f);
            CollapseRemaining = 0f;
            collapseSide = 0f;
            return true;
        }
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
        private static void EffectLine(Rect rect, string label, float multiplier, bool lowerIsBetter)
        {
            Color original = GUI.color;
            int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
            bool beneficial = lowerIsBetter ? multiplier < 1f : multiplier > 1f;
            GUI.color = percent == 0 ? Color.white : beneficial ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.4f);
            GUI.Label(rect, label + ": " + (percent > 0 ? "+" : "") + percent + "%");
            GUI.color = original;
        }
        private void OnGUI()
        {
            if (health == null || health.IsDead || body == null) return;
            float width = Mathf.Min(400f, Screen.width - 24f);
            float x = (Screen.width - width) * 0.5f;
            float y = Mathf.Max(12f, Screen.height - 304f);
            string condition = ConditionSide == 0 ? "EQUILIBRIO" : ConditionSide < 0 ? "LENTA" : "ACELERADA";
            GUI.Box(new Rect(x, y, width, 224f), "CONDICAO TEMPORAL: " + condition + " | " + StageLabel);
            float environment = body.EnvironmentTimeScale;
            string region = environment <= 0f ? "Parado" : Mathf.Approximately(environment, 1f) ? "Normal" : environment < 1f ? "Lento" : "Rapido";
            GUI.Label(new Rect(x + 12f, y + 26f, width - 24f, 24f),
                "Ambiente: " + region + " | Barra: " + (Value * 100f).ToString("0") + "%");
            float left = x + 16f;
            float barWidth = width - 32f;
            Color original = GUI.color;
            GUI.color = new Color(0.15f, 0.4f, 0.8f);
            GUI.DrawTexture(new Rect(left, y + 54f, barWidth * 0.5f, 10f), Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.45f, 0.1f);
            GUI.DrawTexture(new Rect(left + barWidth * 0.5f, y + 54f, barWidth * 0.5f, 10f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float marker = left + (Value + 1f) * 0.5f * (barWidth - 4f);
            GUI.DrawTexture(new Rect(marker, y + 51f, 4f, 16f), Texture2D.whiteTexture);
            GUI.color = original;
            float baseSpeed = motor == null ? 0f : Mathf.Max(0f, motor.speed);
            float baseDistance = dodge == null ? 0f : Mathf.Max(0.1f, dodge.distance);
            if (ConditionSide < 0)
            {
                EffectLine(new Rect(left, y + 74f, barWidth, 24f), "Dano recebido", DamageTakenMultiplier, true);
                EffectLine(new Rect(left, y + 100f, barWidth, 24f), "Velocidade dos ataques", AttackSpeedMultiplier, false);
                EffectLine(new Rect(left, y + 126f, barWidth, 24f), "Distancia da esquiva", DodgeDistanceMultiplier, false);
            }
            else if (ConditionSide > 0)
            {
                EffectLine(new Rect(left, y + 74f, barWidth, 24f), "Velocidade da caminhada", MovementMultiplier, false);
                EffectLine(new Rect(left, y + 100f, barWidth, 24f), "Velocidade dos ataques", AttackSpeedMultiplier, false);
                EffectLine(new Rect(left, y + 126f, barWidth, 24f), "Vida maxima", MaxHealthMultiplier, false);
            }
            else
            {
                GUI.Label(new Rect(left, y + 74f, barWidth, 24f), "Sem buffs ou debuffs ativos.");
                GUI.Label(new Rect(left, y + 100f, barWidth, 24f), "Leve: bonus | Medio / Critico: bonus e penalidades");
                GUI.Label(new Rect(left, y + 126f, barWidth, 24f), "Os efeitos seguem a barra, mesmo fora das zonas.");
            }
            GUI.Label(new Rect(left, y + 156f, barWidth, 24f),
                "Caminhada: " + (baseSpeed * MovementMultiplier).ToString("0.00") +
                " m/s | Esquiva: " + (baseDistance * DodgeDistanceMultiplier).ToString("0.00") + " m");
            float attackDuration = combat == null ? 0f :
                (Mathf.Max(0.05f, combat.windupDuration) + Mathf.Max(0.05f, combat.impactDuration) +
                 Mathf.Max(0.05f, combat.recoveryDuration)) / AttackSpeedMultiplier;
            GUI.Label(new Rect(left, y + 182f, barWidth, 30f),
                "Proximo ataque ~" + attackDuration.ToString("0.00") + "s | Vida: " + health.CurrentHealth + "/" + health.EffectiveMaxHealth);
            if (IsCollapsed)
            {
                float overlayWidth = Mathf.Min(440f, Screen.width - 24f);
                GUI.Box(new Rect((Screen.width - overlayWidth) * 0.5f, Screen.height * 0.36f, overlayWidth, 70f),
                    "COLAPSO TEMPORAL\nAguarde " + CollapseRemaining.ToString("0.0") + "s | R: reiniciar\nVoce continua vulneravel aos ataques.");
            }
        }
    }
}
