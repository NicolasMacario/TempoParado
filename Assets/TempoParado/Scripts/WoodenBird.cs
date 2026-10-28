using System.Collections.Generic;
using UnityEngine;

namespace TempoParado
{
    // Depois do desgaste e antes do motor: pode interromper o colapso neste frame.
    [DefaultExecutionOrder(-150)]
    [RequireComponent(typeof(TemporalStrain), typeof(PlayerHealth))]
    public sealed class WoodenBird : MonoBehaviour
    {
        [Range(0f, 1f)] public float healingFraction = 0.4f;
        [SerializeField] private string initialAreaId = "TimeLab";
        public string CurrentAreaId { get; private set; }
        public bool IsAvailable => !spentAreas.Contains(CurrentAreaId);
        private readonly HashSet<string> spentAreas = new HashSet<string>();
        private TemporalStrain strain;
        private PlayerHealth health;
        private string message;
        private float messageUntil;

        private void Awake()
        {
            strain = GetComponent<TemporalStrain>();
            health = GetComponent<PlayerHealth>();
            CurrentAreaId = string.IsNullOrWhiteSpace(initialAreaId) ? "TimeLab" : initialAreaId.Trim();
        }
        private void Update()
        {
            // Reiniciar o laboratorio representa uma nova tentativa completa da area.
            if (LabReset.Requested)
            {
                spentAreas.Clear();
                messageUntil = 0f;
                return;
            }
            if (health.IsDead || !LabInput.BirdPressed || !Application.isFocused ||
                Cursor.lockState != CursorLockMode.Locked || LabInput.ReleaseCameraPressed) return;
            TryUse();
        }
        public bool TryUse()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || health == null || health.IsDead ||
                strain == null || !strain.isActiveAndEnabled || LabReset.Requested) return false;
            if (!IsAvailable)
            {
                Notify("Esgotado: voce ja usou o passaro nesta area.");
                return false;
            }
            if (Mathf.Abs(strain.Value) < 0.0001f && !strain.IsCollapsed && health.CurrentHealth >= health.EffectiveMaxHealth)
            {
                Notify("Vida cheia e barra zerada. O uso foi preservado.");
                return false;
            }
            // Consome antes de aplicar os dois efeitos; nao concede invulnerabilidade.
            spentAreas.Add(CurrentAreaId);
            int before = health.CurrentHealth;
            strain.ResetState();
            health.HealFractionOfMax(healingFraction);
            Notify("Equilibrio recuperado | Vida +" + Mathf.Max(0, health.CurrentHealth - before));
            return true;
        }
        // Ponto de integracao para a futura transicao de areas. Voltar a uma area
        // ja consumida nao renova o uso; reiniciar o laboratorio limpa o historico.
        public bool EnterArea(string areaId)
        {
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(areaId)) return false;
            CurrentAreaId = areaId.Trim();
            messageUntil = 0f;
            return true;
        }
        private void Notify(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 2.5f;
        }
        private void OnGUI()
        {
            if (health == null || health.IsDead) return;
            float width = Mathf.Min(400f, Screen.width - 24f);
            float x = (Screen.width - width) * 0.5f;
            GUI.Box(new Rect(x, 12f, width, 78f), "F: PASSARO DE MADEIRA | " + (IsAvailable ? "DISPONIVEL" : "ESGOTADO"));
            string text = Time.unscaledTime < messageUntil ? message : IsAvailable ?
                "1 uso nesta area | Equilibrio + cura de " + (Mathf.Clamp01(healingFraction) * 100f).ToString("0") + "%" :
                "Ja utilizado nesta area.\nR: reiniciar o laboratorio e renovar o uso.";
            GUI.Label(new Rect(x + 12f, 40f, width - 24f, 46f), text);
        }
    }
}
