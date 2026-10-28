using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TempoParado
{
    [DefaultExecutionOrder(-90)]
    [RequireComponent(typeof(TimeBody), typeof(PlayerHealth))]
    public sealed class PulseAbility : MonoBehaviour
    {
        [Min(0.1f)] public float range = 5f;
        [Min(0.05f)] public float effectDuration = 2f;
        [Min(0.1f)] public float cooldown = 4f;
        public Material pulseMaterial;
        public float CooldownRemaining { get; private set; }
        public string LastResult { get; private set; } = "Q: normalizar inimigos proximos";
        private sealed class TargetEffect
        {
            public TimeBody body;
            public CharacterController controller;
            public GameObject marker;
        }
        private TimeBody body;
        private PlayerHealth health;
        private PlayerDodge dodge;
        private TemporalStrain strain;
        private CharacterController controller;
        private GameObject wave;
        private LineRenderer waveLine;
        private Material waveMaterial;
        private Material markerMaterial;
        private float waveStarted = -100f;
        private float waveRange;
        private const float WaveDuration = 0.35f;
        private readonly List<TargetEffect> targets = new List<TargetEffect>();
        private readonly HashSet<EnemyChaser> checkedEnemies = new HashSet<EnemyChaser>();

        private void Awake()
        {
            body = GetComponent<TimeBody>();
            health = GetComponent<PlayerHealth>();
            dodge = GetComponent<PlayerDodge>();
            strain = GetComponent<TemporalStrain>();
            controller = GetComponent<CharacterController>();
            if (pulseMaterial != null)
            {
                waveMaterial = new Material(pulseMaterial);
                markerMaterial = new Material(pulseMaterial);
            }
            else Debug.LogWarning("Use Tempo Parado > Ativar Pulso temporal para configurar o visual.", this);
            wave = CreateRing("Pulso - onda visual", waveMaterial, 0.1f);
            waveLine = wave.GetComponent<LineRenderer>();
            wave.SetActive(false);
        }
        private void Update()
        {
            if (LabReset.Requested || health.IsDead) { ResetState(); return; }
            float dt = body.DeltaTime;
            if (dt > 0f) CooldownRemaining = Mathf.Max(0f, CooldownRemaining - dt);
            if (!LabInput.PulsePressed) return;
            if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked || LabInput.ReleaseCameraPressed) return;
            if (dt <= 0f) { LastResult = "Voce esta congelado. I: recuperar protecao"; return; }
            if (strain != null && strain.isActiveAndEnabled && strain.IsCollapsed)
            {
                LastResult = "Colapso temporal: aguarde";
                return;
            }
            if (dodge != null && dodge.isActiveAndEnabled && dodge.IsDodging)
            {
                LastResult = "Espere a esquiva terminar";
                return;
            }
            if (CooldownRemaining > 0f) { LastResult = "Pulso ainda em recarga"; return; }
            Cast();
        }
        private void Cast()
        {
            float radius = Mathf.Max(0.1f, range);
            checkedEnemies.Clear();
            int count = 0;
            foreach (Collider collider in Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                EnemyChaser enemy = collider.GetComponentInParent<EnemyChaser>();
                if (enemy == null || !enemy.isActiveAndEnabled || !checkedEnemies.Add(enemy)) continue;
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth.IsDead) continue;
                if ((enemy.transform.position - transform.position).sqrMagnitude > radius * radius || !HasClearPath(enemy.transform)) continue;
                TimeBody enemyBody = enemy.GetComponent<TimeBody>();
                if (enemyBody == null || !enemyBody.isActiveAndEnabled) continue;
                enemyBody.ApplyNormalization(Mathf.Max(0.05f, effectDuration));
                TargetEffect effect = targets.Find(item => item.body == enemyBody);
                if (effect == null)
                {
                    effect = new TargetEffect
                    {
                        body = enemyBody,
                        controller = enemy.GetComponent<CharacterController>(),
                        marker = CreateRing("Pulso - alvo normalizado", markerMaterial, 0.05f)
                    };
                    effect.marker.transform.localScale = Vector3.one * 0.78f;
                    targets.Add(effect);
                }
                count++;
            }
            CooldownRemaining = Mathf.Max(0.1f, cooldown);
            LastResult = count == 0 ? "Pulso usado: nenhum alvo no alcance" : "Pulso: " + count + " alvo(s) atingido(s)";
            waveStarted = Time.unscaledTime;
            waveRange = radius;
            Vector3 offset = controller == null ? Vector3.down * 0.9f :
                controller.center + Vector3.down * (controller.height * 0.5f - 0.08f);
            wave.transform.position = transform.position + offset;
            wave.SetActive(true);
        }
        private bool HasClearPath(Transform enemy)
        {
            Vector3 start = transform.position + Vector3.up * 0.25f;
            Vector3 offset = enemy.position + Vector3.up * 0.25f - start;
            if (offset.sqrMagnitude < 0.0001f) return true;
            foreach (RaycastHit hit in Physics.RaycastAll(start, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(enemy)) continue;
                return false;
            }
            return true;
        }
        private void LateUpdate()
        {
            if (wave != null && wave.activeSelf)
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - waveStarted) / WaveDuration);
                wave.transform.localScale = Vector3.one * Mathf.Lerp(Mathf.Min(0.15f, waveRange), waveRange, progress);
                waveLine.widthMultiplier = Mathf.Lerp(0.12f, 0.02f, progress);
                if (waveMaterial != null) waveMaterial.color = Color.Lerp(new Color(0.35f, 0.7f, 1f), new Color(0.1f, 0.2f, 0.35f), progress);
                if (progress >= 1f) wave.SetActive(false);
            }
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                TargetEffect target = targets[i];
                if (target.body == null || !target.body.isActiveAndEnabled || !target.body.HasNormalization)
                {
                    if (target.marker != null) Destroy(target.marker);
                    targets.RemoveAt(i);
                    continue;
                }
                Vector3 offset = target.controller == null ? Vector3.down * 0.9f :
                    target.controller.center + Vector3.down * (target.controller.height * 0.5f - 0.12f);
                if (target.marker != null) target.marker.transform.position = target.body.transform.position + offset;
            }
        }
        private static GameObject CreateRing(string name, Material material, float width)
        {
            GameObject ring = new GameObject(name);
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = width;
            line.positionCount = 64;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            if (material != null) line.sharedMaterial = material;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)));
            }
            return ring;
        }
        private void ResetState()
        {
            foreach (TargetEffect target in targets)
            {
                if (target.body != null) target.body.ClearNormalization();
                if (target.marker != null) Destroy(target.marker);
            }
            targets.Clear();
            checkedEnemies.Clear();
            CooldownRemaining = 0f;
            LastResult = "Q: normalizar inimigos proximos";
            if (wave != null) wave.SetActive(false);
        }
        private void OnDisable() { ResetState(); }
        private void OnDestroy()
        {
            if (wave != null) Destroy(wave);
            if (waveMaterial != null) Destroy(waveMaterial);
            if (markerMaterial != null) Destroy(markerMaterial);
        }
        private void OnGUI()
        {
            if (health == null || health.IsDead) return;
            string state = strain != null && strain.isActiveAndEnabled && strain.IsCollapsed ? "COLAPSO" :
                CooldownRemaining > 0f ? "RECARGA: " + CooldownRemaining.ToString("0.0") + "s" : "PRONTO";
            float width = Mathf.Min(330f, Screen.width - 24f);
            float x = Screen.width - width - 12f;
            GUI.Box(new Rect(x, 310f, width, 66f), "Q: PULSO | " + state);
            GUI.Label(new Rect(x + 12f, 342f, width - 24f, 25f), LastResult);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 0.65f, 1f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
