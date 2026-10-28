using UnityEngine;

namespace TempoParado
{
    public sealed class TimeBody : MonoBehaviour
    {
        public bool ignoreZones;
        [Range(0f, 3f)] public float defaultScale = 0f;
        private TimeBody clockOwner;
        // Usado por objetos carregados: apenas o jogador e fonte, evitando ciclos.
        public void FollowClock(TimeBody owner) { clockOwner = owner == this ? null : owner; }
        // Calculado ao consultar, evitando dependencia da ordem de Update.
        public float LocalTimeScale
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
        public float DeltaTime => Time.deltaTime * LocalTimeScale;
    }
}
