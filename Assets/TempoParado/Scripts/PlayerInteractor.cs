using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(TimeBody))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public float reach = 2.4f;
        public float dropDistance = 1.3f;
        public TemporalPickup HeldItem { get; private set; }
        private TimeBody body;
        private TemporalPickup nearby;
        private string message;
        private float messageUntil;
        private PlayerHealth health;
        private PlayerDodge dodge;
        private PlayerCombat combat;
        private TemporalStrain strain;
        private void Awake()
        {
            body = GetComponent<TimeBody>();
            health = GetComponent<PlayerHealth>();
            dodge = GetComponent<PlayerDodge>();
            combat = GetComponent<PlayerCombat>();
            strain = GetComponent<TemporalStrain>();
        }
        private void Update()
        {
            if (LabReset.Requested)
            {
                foreach (TemporalPickup item in TemporalPickup.Active)
                    if (item != null) item.ResetToSpawn();
                HeldItem = null;
                messageUntil = 0f;
                return;
            }
            if (health != null && health.IsDead) { nearby = null; return; }
            if (strain != null && strain.isActiveAndEnabled && strain.IsCollapsed)
            {
                nearby = null;
                messageUntil = 0f;
                return;
            }
            if (dodge != null && dodge.isActiveAndEnabled && dodge.IsDodging)
            {
                nearby = null;
                if (LabInput.InteractPressed) ShowMessage("Aguarde o fim da esquiva para pegar ou soltar objetos.");
                return;
            }
            nearby = HeldItem == null ? FindNearest() : null;
            if (combat != null && combat.isActiveAndEnabled && combat.IsAttacking)
            {
                nearby = null;
                if (LabInput.InteractPressed) ShowMessage("Aguarde o fim do ataque para pegar objetos.");
                return;
            }
            if (!LabInput.InteractPressed) return;
            if (body.LocalTimeScale <= 0f) { ShowMessage("Voce esta congelado. Pressione I para recuperar a protecao."); return; }
            if (HeldItem != null)
            {
                Vector3 drop = transform.position + transform.forward * dropDistance + Vector3.up * 0.25f;
                if (!HeldItem.HasSpaceAt(drop, transform) || !ClearPath(HeldItem, drop))
                {
                    ShowMessage("Sem espaco para soltar. Afaste-se da parede ou vire o personagem.");
                    return;
                }
                HeldItem.Drop(drop);
                HeldItem = null;
                ShowMessage("Objeto solto: agora ele segue o tempo do ambiente.");
            }
            else if (nearby != null && nearby.PickUp(this, body))
            {
                HeldItem = nearby;
                nearby = null;
                ShowMessage("Na sua mao, o objeto acompanha seu tempo.");
            }
        }
        private TemporalPickup FindNearest()
        {
            TemporalPickup nearest = null;
            float bestDistance = reach * reach;
            foreach (TemporalPickup item in TemporalPickup.Active)
            {
                if (item == null || item.Holder != null) continue;
                float sqrDistance = (item.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance > bestDistance || !ClearPath(item, item.transform.position)) continue;
                nearest = item;
                bestDistance = sqrDistance;
            }
            return nearest;
        }
        private bool ClearPath(TemporalPickup item, Vector3 end)
        {
            Vector3 start = transform.position + Vector3.up * 0.25f;
            Vector3 delta = end - start;
            if (delta.sqrMagnitude < 0.0001f) return true;
            RaycastHit[] hits = Physics.RaycastAll(start, delta.normalized, delta.magnitude,
                ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(item.transform)) continue;
                return false;
            }
            return true;
        }
        private void ShowMessage(string text) { message = text; messageUntil = Time.unscaledTime + 3f; }
        private void OnDisable()
        {
            if (HeldItem != null) HeldItem.ResetToSpawn();
            HeldItem = null;
        }
        public string ActiveHint
        {
            get
            {
                if (Time.unscaledTime < messageUntil) return message;
                if (HeldItem != null) return "E: soltar " + HeldItem.displayName;
                return nearby == null ? null : "E: pegar " + nearby.displayName;
            }
        }
        private void OnGUI()
        {
            if (!GameHUD.ShowDebug) return;
            if (health != null && health.IsDead) return;
            if (strain != null && strain.isActiveAndEnabled && strain.IsCollapsed) return;
            string text;
            if (Time.unscaledTime < messageUntil) text = message;
            else if (HeldItem != null) text = "E: soltar " + HeldItem.displayName + " | Ritmo do objeto: " + HeldItem.Body.LocalTimeScale.ToString("0.0") + "x";
            else if (nearby != null) text = "E: pegar " + nearby.displayName + " | Ritmo do objeto: " + nearby.Body.LocalTimeScale.ToString("0.0") + "x";
            else text = "Aproxime-se da esfera roxa e pressione E.";
            float width = Mathf.Min(620f, Screen.width - 24f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height - 68f, width, 52f), text);
        }
    }
}
