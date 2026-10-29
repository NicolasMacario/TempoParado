using UnityEngine;

namespace TempoParado
{
    // Progresso depois dos Updates de combate e reinicio; nao usa eventos de trigger.
    public sealed class OrphanageRoute : MonoBehaviour
    {
        public PlayerMotor player;
        public EnemyHealth guardian;
        public Transform gate;
        public Collider gateCollider;
        public BoxCollider exitBounds;
        public float corridorStartZ = 18f;
        public float arenaStartZ = 38f;
        [Min(0.1f)] public float gateOpeningDuration = 0.8f;
        [Min(2f)] public float gateRise = 4f;
        public bool IsCompleted { get; private set; }
        private PlayerHealth health;
        private Vector3 gateClosedPosition;
        private float opening;

        private void Awake()
        {
            health = player == null ? null : player.GetComponent<PlayerHealth>();
            if (gate != null) gateClosedPosition = gate.localPosition;
        }
        private void Start()
        {
            WoodenBird bird = player == null ? null : player.GetComponent<WoodenBird>();
            if (bird != null) bird.EnterArea("OrfanatoPrototipo");
        }
        private void LateUpdate()
        {
            if (LabReset.Requested)
            {
                IsCompleted = false;
                opening = 0f;
                ApplyGate();
                return;
            }
            if (player == null || health == null || health.IsDead) return;
            bool defeated = guardian != null && guardian.IsDead;
            opening = Mathf.MoveTowards(opening, defeated ? 1f : 0f,
                Time.unscaledDeltaTime / Mathf.Max(0.1f, gateOpeningDuration));
            ApplyGate();
            if (!IsCompleted && defeated && opening >= 1f && Contains(exitBounds, player.transform.position))
                IsCompleted = true;
        }
        private void ApplyGate()
        {
            if (gate != null) gate.localPosition = gateClosedPosition + Vector3.up * (Mathf.SmoothStep(0f, 1f, opening) * gateRise);
            if (gateCollider != null) gateCollider.enabled = opening < 1f;
        }
        private static bool Contains(BoxCollider box, Vector3 position)
        {
            if (box == null || !box.enabled || !box.gameObject.activeInHierarchy) return false;
            Vector3 p = box.transform.InverseTransformPoint(position) - box.center;
            Vector3 h = box.size * 0.5f;
            return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
        }
        public string ObjectiveText
        {
            get
            {
                if (player == null) return "Explore o orfanato.";
                bool defeated = guardian != null && guardian.IsDead;
                float z = player.transform.position.z;
                return IsCompleted ? "Percurso concluido!" : defeated ? "Atravesse o portao e alcance a saida." :
                    z < corridorStartZ ? "Saia do patio e entre no corredor." : z < arenaStartZ ?
                    "Escolha seu ritmo e atravesse o corredor." : "Derrote o zelador para abrir o portao.";
            }
        }
        private void OnGUI()
        {
            if (player == null) return;
            if (GameHUD.ShowDebug)
            {
                GUI.Box(new Rect(12f, 12f, 390f, 190f), "TEMPO PARADO | ORFANATO - PROTOTIPO");
                GUI.Label(new Rect(24f, 40f, 365f, 42f), "OBJETIVO: " + ObjectiveText);
                GUI.Label(new Rect(24f, 85f, 365f, 30f), "WASD: andar | Mouse: olhar | Espaco: esquiva");
                GUI.Label(new Rect(24f, 115f, 365f, 30f), "Clique: atacar | Q: Pulso | E: pegar / soltar");
                GUI.Label(new Rect(24f, 145f, 365f, 30f), "F: passaro (1 uso no percurso) | R: reiniciar");
                GUI.Label(new Rect(24f, 173f, 365f, 25f), "Azul: lento | Laranja: rapido | Verde: recuperar");
            }
            if (IsCompleted && health != null && !health.IsDead)
            {
                float width = Mathf.Min(500f, Screen.width - 24f);
                GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height * 0.3f, width, 100f),
                    "PERCURSO CONCLUIDO\nVoce atravessou o orfanato e derrotou o zelador.\nR: reiniciar o percurso | Esc: liberar cursor");
            }
        }
    }
}
