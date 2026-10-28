using UnityEngine;

namespace TempoParado
{
    public sealed class TimeBody : MonoBehaviour
    {
        public bool ignoreZones;
        [Range(0f, 3f)] public float defaultScale = 0f;
        private TimeBody clockOwner;
        private float temporaryCap = 1f;
        private float temporaryUntil = -1f;
        private float normalizationUntil = -1f;
        public bool HasNormalization => Time.unscaledTime < normalizationUntil;
        public float NormalizationRemaining => HasNormalization ? Mathf.Max(0f, normalizationUntil - Time.unscaledTime) : 0f;
        public void ApplyNormalization(float realDuration)
        {
            if (realDuration > 0f) normalizationUntil = Mathf.Max(normalizationUntil, Time.unscaledTime + realDuration);
        }
        public void ClearNormalization() { normalizationUntil = -1f; }
        public bool HasTemporarySlow => Time.unscaledTime < temporaryUntil;
        public float TemporarySlowRemaining => HasTemporarySlow ? Mathf.Max(0f, temporaryUntil - Time.unscaledTime) : 0f;
        // A expiracao usa tempo real para nao se prolongar pelo proprio efeito.
        public void ApplyTemporarySlow(float cap, float realDuration)
        {
            if (realDuration <= 0f) return;
            float clamped = Mathf.Clamp(cap, 0f, 3f);
            temporaryCap = HasTemporarySlow ? Mathf.Min(temporaryCap, clamped) : clamped;
            temporaryUntil = Mathf.Max(HasTemporarySlow ? temporaryUntil : Time.unscaledTime,
                Time.unscaledTime + realDuration);
        }
        public void ClearTemporarySlow() { temporaryCap = 1f; temporaryUntil = -1f; }
        public void ClearTemporalEffects() { ClearTemporarySlow(); ClearNormalization(); }
        private void Update() { if (LabReset.Requested) ClearTemporalEffects(); }
        private void OnDisable() { ClearTemporalEffects(); }
        // Usado por objetos carregados: apenas o jogador e fonte, evitando ciclos.
        public void FollowClock(TimeBody owner) { clockOwner = owner == this ? null : owner; }
        // Ritmo do ambiente, sem imunidade pessoal, heranca ou efeitos temporarios.
        public float EnvironmentTimeScale
        {
            get
            {
                TimeZone selected = null;
                foreach (TimeZone zone in TimeZone.Active)
                {
                    if (zone == null || !zone.isActiveAndEnabled || !zone.Contains(transform.position)) continue;
                    if (selected == null || zone.priority > selected.priority)
                        selected = zone;
                }
                return Mathf.Max(0f, selected == null ? defaultScale : selected.scale);
            }
        }
        // Calculado ao consultar, evitando dependencia da ordem de Update.
        private float BaseTimeScale
        {
            get
            {
                if (clockOwner != null) return clockOwner.LocalTimeScale;
                if (HasNormalization) return 1f;
                if (ignoreZones) return 1f;
                return EnvironmentTimeScale;
            }
        }
        public float LocalTimeScale => HasTemporarySlow ? Mathf.Min(BaseTimeScale, temporaryCap) : BaseTimeScale;
        public float DeltaTime => Time.deltaTime * LocalTimeScale;
    }
}
