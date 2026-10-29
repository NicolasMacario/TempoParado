using UnityEngine;
using UnityEngine.Rendering;

namespace TempoParado
{
    [RequireComponent(typeof(PlayerDodge), typeof(PlayerHealth))]
    public sealed class PerfectDodge : MonoBehaviour
    {
        [Range(0f, 1f)] public float enemyTimeCap = 0.1f;
        [Min(0.05f)] public float slowDuration = 0.5f;
        public Material ringMaterial;
        public int SuccessCount { get; private set; }
        private PlayerDodge dodge;
        private PlayerHealth health;
        private int rewardedSequence = -1;
        private float notificationUntil;
        private float lastDuration;
        private TimeBody affectedEnemy;
        private CharacterController enemyController;
        private GameObject ring;
        private Material runtimeMaterial;

        private void Awake()
        {
            dodge = GetComponent<PlayerDodge>();
            health = GetComponent<PlayerHealth>();
            // Fora da hierarquia do inimigo: o flash de dano nao altera este sinal.
            ring = new GameObject("Esquiva perfeita - efeito visual");
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = 0.06f;
            line.positionCount = 48;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 1.1f);
            }
            if (ringMaterial != null)
            {
                runtimeMaterial = new Material(ringMaterial);
                line.sharedMaterial = runtimeMaterial;
            }
            else Debug.LogWarning("Use Tempo Parado > Ativar esquiva perfeita para configurar o efeito visual.", this);
            ring.SetActive(false);
        }
        private void Update()
        {
            if (LabReset.Requested) { ResetState(); return; }
            if (health.IsDead) { ClearEffect(); notificationUntil = 0f; }
        }
        public bool TryTrigger(EnemyChaser attacker)
        {
            if (!isActiveAndEnabled || LabReset.Requested || health.IsDead || attacker == null) return false;
            if (!dodge.isActiveAndEnabled || !dodge.IsInvulnerable || rewardedSequence == dodge.DodgeSequence) return false;
            TimeBody enemyClock = attacker.GetComponent<TimeBody>();
            EnemyHealth enemyHealth = attacker.GetComponent<EnemyHealth>();
            if (enemyClock == null || (enemyHealth != null && enemyHealth.IsDead)) return false;
            ClearEffect();
            affectedEnemy = enemyClock;
            enemyController = attacker.GetComponent<CharacterController>();
            lastDuration = Mathf.Max(0.05f, slowDuration);
            enemyClock.ApplyTemporarySlow(enemyTimeCap, lastDuration);
            rewardedSequence = dodge.DodgeSequence;
            SuccessCount++;
            notificationUntil = Time.unscaledTime + 1.2f;
            return true;
        }
        private void LateUpdate()
        {
            if (ring == null) return;
            if (affectedEnemy == null || !affectedEnemy.HasTemporarySlow) { ring.SetActive(false); return; }
            Vector3 offset = enemyController == null ? Vector3.down * 0.9f :
                enemyController.center + Vector3.down * (enemyController.height * 0.5f - 0.1f);
            ring.transform.position = affectedEnemy.transform.position + offset;
            ring.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 24f) * 0.08f);
            ring.SetActive(true);
        }
        private void ClearEffect()
        {
            if (affectedEnemy != null) affectedEnemy.ClearTemporarySlow();
            affectedEnemy = null;
            enemyController = null;
            if (ring != null) ring.SetActive(false);
        }
        private void ResetState()
        {
            ClearEffect();
            rewardedSequence = -1;
            SuccessCount = 0;
            notificationUntil = 0f;
        }
        private void OnDisable() { ResetState(); }
        private void OnDestroy()
        {
            if (ring != null) Destroy(ring);
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
        private void OnGUI()
        {
            if (health == null || health.IsDead) return;
            if (GameHUD.ShowDebug) GUI.Box(new Rect(12f, 438f, 330f, 32f), "ESQUIVAS PERFEITAS: " + SuccessCount);
            if (Time.unscaledTime >= notificationUntil) return;
            float width = Mathf.Min(420f, Screen.width - 24f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height * (GameHUD.ShowDebug ? 0.22f : 0.28f), width, 60f),
                "ESQUIVA PERFEITA!\nZelador desacelerado por " + lastDuration.ToString("0.0") + "s");
        }
    }
}
