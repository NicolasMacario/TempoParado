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
            const int segments = 24;
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-enemy.attackArc * 0.5f, enemy.attackArc * 0.5f, (float)i / segments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * enemy.attackRange;
                if (i == segments) continue;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            mesh = new Mesh { name = "Area do golpe do zelador" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
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
        public void Show(float progress, bool impact = false)
        {
            if (marker == null) return;
            marker.SetActive(true);
            if (runtimeMaterial != null) runtimeMaterial.color = impact ? Color.white :
                Color.Lerp(new Color(1f, 0.75f, 0.1f), new Color(1f, 0.12f, 0.05f), Mathf.Clamp01(progress));
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
