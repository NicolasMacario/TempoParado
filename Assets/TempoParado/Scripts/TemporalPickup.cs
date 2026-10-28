using System.Collections.Generic;
using UnityEngine;

namespace TempoParado
{
    [RequireComponent(typeof(TimeBody), typeof(CharacterController))]
    public sealed class TemporalPickup : MonoBehaviour
    {
        public static readonly HashSet<TemporalPickup> Active = new HashSet<TemporalPickup>();
        public string displayName = "Esfera temporal";
        public Transform visual;
        public PlayerInteractor Holder { get; private set; }
        public TimeBody Body { get; private set; }
        private CharacterController controller;
        private Vector3 spawn;
        private Quaternion spawnRotation;
        private Quaternion visualRotation;
        private float velocityY;

        private void Awake()
        {
            Body = GetComponent<TimeBody>();
            controller = GetComponent<CharacterController>();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            if (visual != null) visualRotation = visual.localRotation;
        }
        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
        private void Update()
        {
            float dt = Body.DeltaTime;
            if (dt <= 0f) return;
            if (visual != null) visual.Rotate(25f * dt, 70f * dt, 0f, Space.Self);
            if (Holder != null || !controller.enabled) return;
            // Subpassos limitam o salto da gravidade em ritmos rapidos e frames longos.
            float remaining = Mathf.Min(dt, 0.2f);
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, 0.02f);
                velocityY = Mathf.Max(velocityY - 9.81f * step, -30f);
                CollisionFlags flags = controller.Move(Vector3.up * velocityY * step);
                if ((flags & CollisionFlags.Below) != 0) velocityY = 0f;
                remaining -= step;
            }
        }
        public bool PickUp(PlayerInteractor holder, TimeBody holderClock)
        {
            if (Holder != null || holder == null || holderClock == null) return false;
            Holder = holder;
            controller.enabled = false;
            velocityY = 0f;
            Body.FollowClock(holderClock);
            transform.SetParent(holder.transform, false);
            transform.localPosition = new Vector3(0f, 0.25f, 0.9f);
            transform.localRotation = Quaternion.identity;
            return true;
        }
        public void Drop(Vector3 position)
        {
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, Quaternion.identity);
            Body.FollowClock(null);
            Holder = null;
            velocityY = 0f;
            controller.enabled = true;
        }
        public bool HasSpaceAt(Vector3 position, Transform player)
        {
            // Objetos do laboratorio usam escala 1 e centro do controller em zero.
            float halfLine = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
            Collider[] colliders = Physics.OverlapCapsule(position + Vector3.up * halfLine,
                position - Vector3.up * halfLine, controller.radius + 0.02f,
                ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider other in colliders)
            {
                if (other.transform.IsChildOf(transform) || other.transform.IsChildOf(player)) continue;
                return false;
            }
            return true;
        }
        public void ResetToSpawn()
        {
            controller.enabled = false;
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(spawn, spawnRotation);
            Body.FollowClock(null);
            Holder = null;
            velocityY = 0f;
            if (visual != null) visual.localRotation = visualRotation;
            controller.enabled = true;
        }
    }
}
