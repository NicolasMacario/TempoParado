using UnityEngine;
using UnityEngine.Rendering;

namespace TempoParado
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth), typeof(EnemyChaser))]
    public sealed class CombatFeedback : MonoBehaviour
    {
        public Renderer core;
        public AudioClip armorHitSound;
        public AudioClip exposedHitSound;
        [Range(0f, 1f)] public float volume = 0.3f;
        public Color exposedColor = new Color(1f, 0.8f, 0.15f);
        private EnemyHealth health;
        private EnemyChaser brain;
        private AudioSource audioSource;
        private AudioClip generatedArmor;
        private AudioClip generatedExposed;
        private ParticleSystem sparks;
        private Material sparkMaterial;
        private Texture2D sparkTexture;
        private Light coreLight;
        private MaterialPropertyBlock originalBlock;
        private MaterialPropertyBlock glowBlock;
        private bool glowing;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            brain = GetComponent<EnemyChaser>();
            if (core != null)
            {
                originalBlock = new MaterialPropertyBlock();
                glowBlock = new MaterialPropertyBlock();
                core.GetPropertyBlock(originalBlock);
                GameObject lightObject = new GameObject("Brilho de exposicao");
                lightObject.transform.SetParent(core.transform, false);
                coreLight = lightObject.AddComponent<Light>();
                coreLight.type = LightType.Point;
                coreLight.range = 2f;
                coreLight.shadows = LightShadows.None;
                coreLight.enabled = false;
            }
            GameObject sound = new GameObject("Som dos impactos");
            sound.transform.SetParent(transform, false);
            audioSource = sound.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.7f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 22f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            if (armorHitSound == null) generatedArmor = MakeSound(false);
            if (exposedHitSound == null) generatedExposed = MakeSound(true);
            CreateSparks();
        }
        private void CreateSparks()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) { Debug.LogWarning("Shader de particulas ausente; o som e o brilho continuam ativos.", this); return; }
            sparkTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[256];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                float radius = new Vector2((x - 7.5f) / 7.5f, (y - 7.5f) / 7.5f).magnitude;
                pixels[y * 16 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - radius) * 3f));
            }
            sparkTexture.SetPixels(pixels); sparkTexture.Apply();
            sparkMaterial = new Material(shader);
            if (sparkMaterial.HasProperty("_BaseMap")) sparkMaterial.SetTexture("_BaseMap", sparkTexture);
            if (sparkMaterial.HasProperty("_MainTex")) sparkMaterial.SetTexture("_MainTex", sparkTexture);
            if (sparkMaterial.HasProperty("_Surface")) sparkMaterial.SetFloat("_Surface", 1f);
            if (sparkMaterial.HasProperty("_SrcBlend")) sparkMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (sparkMaterial.HasProperty("_DstBlend")) sparkMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (sparkMaterial.HasProperty("_ZWrite")) sparkMaterial.SetFloat("_ZWrite", 0f);
            sparkMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            sparkMaterial.renderQueue = 3000;
            GameObject effect = new GameObject("Faiscas dos golpes");
            sparks = effect.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.playOnAwake = false; main.loop = false; main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true; main.gravityModifier = 0.5f;
            var emission = sparks.emission; emission.enabled = false;
            var shape = sparks.shape; shape.enabled = false;
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = sparkMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        public void PlayHit(bool exposed)
        {
            if (LabReset.Requested) return;
            Vector3 center = core == null ? transform.position + Vector3.up * 0.3f : core.bounds.center;
            Vector3 direction = brain.target == null ? transform.forward : brain.target.position - center;
            direction.y = 0.3f;
            direction.Normalize();
            if (sparks != null)
            {
                sparks.transform.position = center + direction * 0.35f;
                sparks.Play();
                for (int i = 0; i < (exposed ? 22 : 10); i++)
                {
                    var particle = new ParticleSystem.EmitParams();
                    particle.startColor = exposed ? exposedColor : new Color(0.7f, 0.85f, 1f);
                    particle.startLifetime = Random.Range(0.15f, 0.4f);
                    particle.startSize = exposed ? Random.Range(0.08f, 0.15f) : Random.Range(0.04f, 0.08f);
                    particle.velocity = (direction + Random.insideUnitSphere * 1.5f) * (exposed ? 3f : 2f);
                    sparks.Emit(particle, 1);
                }
            }
            AudioClip clip = exposed ? (exposedHitSound != null ? exposedHitSound : generatedExposed) :
                (armorHitSound != null ? armorHitSound : generatedArmor);
            if (clip != null) audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
        private void LateUpdate()
        {
            if (LabReset.Requested)
            {
                if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (audioSource != null) audioSource.Stop();
            }
            bool exposed = !LabReset.Requested && !health.IsDead && brain.IsBossEncounter && brain.IsRecovering;
            if (coreLight != null)
            {
                coreLight.enabled = exposed;
                coreLight.color = exposedColor;
                coreLight.intensity = 1.3f + Mathf.Sin(Time.unscaledTime * 8f) * 0.3f;
            }
            if (exposed && core != null && core.sharedMaterial != null)
            {
                core.GetPropertyBlock(glowBlock);
                if (core.sharedMaterial.HasProperty("_BaseColor")) glowBlock.SetColor("_BaseColor", exposedColor);
                if (core.sharedMaterial.HasProperty("_Color")) glowBlock.SetColor("_Color", exposedColor);
                core.SetPropertyBlock(glowBlock);
                glowing = true;
            }
            else RestoreCore();
        }
        private void RestoreCore()
        {
            if (glowing && core != null && originalBlock != null) core.SetPropertyBlock(originalBlock);
            glowing = false;
        }
        private static AudioClip MakeSound(bool exposed)
        {
            const int rate = 22050;
            float duration = exposed ? 0.2f : 0.12f;
            float[] samples = new float[Mathf.CeilToInt(duration * rate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float tone = exposed ? Mathf.Sin(2f * Mathf.PI * 180f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * 640f * t) :
                    Mathf.Sin(2f * Mathf.PI * 1150f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 1730f * t);
                samples[i] = tone * Mathf.Exp(-t * (exposed ? 24f : 48f)) * 0.35f;
            }
            AudioClip clip = AudioClip.Create(exposed ? "Impacto exposto provisorio" : "Metal provisorio", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private void OnDisable()
        {
            RestoreCore();
            if (coreLight != null) coreLight.enabled = false;
            if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (audioSource != null) audioSource.Stop();
        }
        private void OnDestroy()
        {
            if (coreLight != null) Destroy(coreLight.gameObject);
            if (audioSource != null) Destroy(audioSource.gameObject);
            if (sparks != null) Destroy(sparks.gameObject);
            if (sparkMaterial != null) Destroy(sparkMaterial);
            if (sparkTexture != null) Destroy(sparkTexture);
            if (generatedArmor != null) Destroy(generatedArmor);
            if (generatedExposed != null) Destroy(generatedExposed);
        }
    }
}
