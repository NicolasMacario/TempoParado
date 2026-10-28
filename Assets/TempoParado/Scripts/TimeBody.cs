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
        private void Update() { if (LabReset.Requested) ClearTemporarySlow(); }
        private void OnDisable() { ClearTemporarySlow(); }
        // Usado por objetos carregados: apenas o jogador e fonte, evitando ciclos.
        public void FollowClock(TimeBody owner) { clockOwner = owner == this ? null : owner; }
        // Calculado ao consultar, evitando dependencia da ordem de Update.
        private float BaseTimeScale
        {
            get
            {
                if (clockOwner != null) return clockOwner.LocalTimeScale;
                if (ignoreZones) return 1f;
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
        public float LocalTimeScale => HasTemporarySlow ? Mathf.Min(BaseTimeScale, temporaryCap) : BaseTimeScale;
        public float DeltaTime => Time.deltaTime * LocalTimeScale;
    }
}
