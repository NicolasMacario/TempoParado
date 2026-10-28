using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerDodge : MonoBehaviour
    {
        [Header("Segundos do relogio local do jogador")]
        [Min(0.05f)] public float duration = 0.32f;
        [Min(0f)] public float invulnerableDuration = 0.16f;
        [Min(0.05f)] public float cooldown = 0.9f;
        [Min(0.1f)] public float distance = 3f;
        public bool IsDodging { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool FaceDirection { get; private set; }
        public Vector3 Direction { get; private set; }
        public float CooldownRemaining { get; private set; }
        public int DodgeSequence { get; private set; }
        public Vector3 DodgeStartPosition { get; private set; }
        private float remaining;
        private float elapsed;
        private float currentDuration;
        private float currentInvulnerability;
        private float currentSpeed;
        private PlayerHealth health;
        private TemporalStrain strain;
        private Renderer[] renderers;
        private MaterialPropertyBlock[] originalBlocks;
        private MaterialPropertyBlock[] feedbackBlocks;
        private bool feedbackApplied;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            strain = GetComponent<TemporalStrain>();
            renderers = GetComponentsInChildren<Renderer>(true);
            originalBlocks = new MaterialPropertyBlock[renderers.Length];
            feedbackBlocks = new MaterialPropertyBlock[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalBlocks[i] = new MaterialPropertyBlock();
                feedbackBlocks[i] = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(originalBlocks[i]);
                renderers[i].GetPropertyBlock(feedbackBlocks[i]);
            }
        }
        // Chamado apenas por PlayerMotor: uma unica movimentacao por frame.
        public Vector3 Step(float dt, Vector3 moveDirection, bool requested)
        {
            IsDodging = false;
            IsInvulnerable = false;
            if (dt <= 0f) { RestoreFeedback(); return Vector3.zero; }
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - dt);
            if (requested && remaining <= 0f && CooldownRemaining <= 0f)
            {
                DodgeSequence++;
                DodgeStartPosition = transform.position;
                FaceDirection = moveDirection.sqrMagnitude > 0.001f;
                Direction = FaceDirection ? moveDirection.normalized : -transform.forward;
                Direction = new Vector3(Direction.x, 0f, Direction.z).normalized;
                currentDuration = Mathf.Max(0.05f, duration);
                currentInvulnerability = Mathf.Clamp(invulnerableDuration, 0f, currentDuration);
                float conditionDistance = strain == null ? 1f : strain.DodgeDistanceMultiplier;
                currentSpeed = Mathf.Max(0.1f, distance) * conditionDistance / currentDuration;
                remaining = currentDuration;
                elapsed = 0f;
                // O intervalo conta desde o inicio, mas nao permite sobrepor esquivas.
                CooldownRemaining = Mathf.Max(currentDuration, cooldown);
            }
            if (remaining <= 0f) { RestoreFeedback(); return Vector3.zero; }
            IsDodging = true;
            IsInvulnerable = elapsed < currentInvulnerability;
            float step = Mathf.Min(dt, remaining);
            remaining = Mathf.Max(0f, remaining - step);
            elapsed += step;
            ApplyFeedback();
            return Direction * currentSpeed * step;
        }
        public void ResetState()
        {
            InterruptDodge();
            CooldownRemaining = 0f;
        }
        public void InterruptDodge()
        {
            remaining = 0f;
            elapsed = 0f;
            IsDodging = false;
            IsInvulnerable = false;
            FaceDirection = false;
            Direction = Vector3.zero;
            RestoreFeedback();
        }
        private void ApplyFeedback()
        {
            if (renderers == null) return;
            Color color = IsInvulnerable ? new Color(0.1f, 1f, 1f) : new Color(0.15f, 0.4f, 1f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Material material = renderers[i].sharedMaterial;
                if (material == null) continue;
                if (material.HasProperty("_BaseColor")) feedbackBlocks[i].SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) feedbackBlocks[i].SetColor("_Color", color);
                renderers[i].SetPropertyBlock(feedbackBlocks[i]);
            }
            feedbackApplied = true;
        }
        private void RestoreFeedback()
        {
            if (!feedbackApplied || renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].SetPropertyBlock(originalBlocks[i]);
            feedbackApplied = false;
        }
        private void OnDisable() { ResetState(); }
        private void OnGUI()
        {
            if (health != null && health.IsDead) return;
            string state = strain != null && strain.isActiveAndEnabled && strain.IsCollapsed ? "COLAPSO" :
                IsInvulnerable ? "INVULNERAVEL" : IsDodging ? "ESQUIVANDO (vulneravel)" :
                CooldownRemaining > 0f ? "RECARGA: " + CooldownRemaining.ToString("0.0") + "s" : "PRONTA";
            GUI.Box(new Rect(12f, 284f, 300f, 64f), "ESPACO: ESQUIVA | " + state);
            GUI.Label(new Rect(24f, 315f, 276f, 25f), "WASD + Espaco: direcao | Parado: para tras");
        }
    }
}
