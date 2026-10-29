using UnityEngine;
using UnityEngine.Rendering;

namespace TempoParado
{
    [RequireComponent(typeof(EnemyChaser))]
    public sealed class AttackTelegraph : MonoBehaviour
    {
        public Material sourceMaterial;
        private GameObject marker;
        private Mesh mesh;
        private Material runtimeMaterial;
        public bool OwnsRenderer(Renderer renderer)
        {
            return marker != null && renderer != null && renderer.transform.IsChildOf(marker.transform);
        }
        private void Awake()
        {
            EnemyChaser enemy = GetComponent<EnemyChaser>();
            mesh = new Mesh { name = "Area do golpe do zelador" };
            SetArea(enemy.attackRange, enemy.attackArc);
            marker = new GameObject("Aviso de ataque");
            marker.transform.SetParent(transform, false);
            CharacterController cc = GetComponent<CharacterController>();
            marker.transform.localPosition = cc == null ? Vector3.down * 0.92f :
                cc.center + Vector3.down * (cc.height * 0.5f - 0.08f);
            marker.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = marker.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (sourceMaterial != null)
            {
                runtimeMaterial = new Material(sourceMaterial);
                renderer.sharedMaterial = runtimeMaterial;
            }
            else Debug.LogWarning("Execute Tempo Parado > Ativar combate do zelador para configurar o material do aviso.", this);
            marker.SetActive(false);
        }
        private float currentRange = -1f;
        private float currentArc = -1f;
        public void SetArea(float range, float arc)
        {
            if (mesh == null) return;
            range = Mathf.Max(0.1f, range);
            arc = Mathf.Clamp(arc, 10f, 360f);
            if (Mathf.Approximately(range, currentRange) && Mathf.Approximately(arc, currentArc)) return;
            currentRange = range;
            currentArc = arc;
            const int segments = 48;
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, (float)i / segments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * range;
                if (i == segments) continue;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
        public void Show(float progress, bool impact = false)
        {
            if (marker == null) return;
            marker.SetActive(true);
            if (runtimeMaterial != null) runtimeMaterial.color = impact ? Color.white :
                Color.Lerp(currentArc >= 359f ? new Color(0.7f, 0.25f, 1f) : new Color(1f, 0.75f, 0.1f), new Color(1f, 0.12f, 0.05f), Mathf.Clamp01(progress));
        }
        public void Hide() { if (marker != null) marker.SetActive(false); }
        private void OnDisable() { Hide(); }
        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
