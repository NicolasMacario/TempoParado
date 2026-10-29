using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class CheckpointLabBuilder
    {
        [MenuItem("Tempo Parado/Adicionar retomada antes do chefe")]
        public static void AddCheckpoint()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play para adicionar a retomada."); return; }
            Scene scene = SceneManager.GetActiveScene();
            OrphanageRoute route = null;
            RouteCheckpoint existing = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (route == null) route = root.GetComponentInChildren<OrphanageRoute>(true);
                if (existing == null) existing = root.GetComponentInChildren<RouteCheckpoint>(true);
            }
            if (existing != null) { Selection.activeGameObject = existing.gameObject; Debug.Log("O percurso ja possui retomada."); return; }
            if (route == null || route.player == null)
            {
                Debug.LogError("Abra a cena OrfanatoPrototipo com o percurso configurado.");
                return;
            }
            GameObject go = new GameObject("Retomada antes do chefe");
            go.transform.position = new Vector3(0f, 3f, route.arenaStartZ + 1.5f);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(7f, 6f, 2f);
            GameObject point = new GameObject("Ponto de retorno");
            point.transform.SetParent(go.transform, false);
            point.transform.localPosition = new Vector3(0f, -1.9f, 0f);
            RouteCheckpoint checkpoint = go.AddComponent<RouteCheckpoint>();
            checkpoint.player = route.player;
            checkpoint.respawnPoint = point.transform;
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marca de retomada";
            marker.transform.SetParent(go.transform, false);
            marker.transform.localPosition = new Vector3(0f, -2.975f, -0.6f);
            marker.transform.localScale = new Vector3(7f, 0.04f, 0.5f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null)
            {
                const string path = "Assets/TempoParado/Materials/Orfanato/Retomada.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null && AssetDatabase.IsValidFolder("Assets/TempoParado/Materials/Orfanato"))
                {
                    material = new Material(shader); material.color = new Color(0.25f, 0.9f, 0.85f);
                    AssetDatabase.CreateAsset(material, path);
                }
                if (material != null) marker.GetComponent<Renderer>().sharedMaterial = material;
            }
            Undo.RegisterCreatedObjectUndo(go, "Adicionar retomada antes do chefe");
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = go;
            Debug.Log("Retomada adicionada. Ctrl+S para salvar. Morte volta a arena; R reinicia o percurso inteiro.");
        }
    }
}
