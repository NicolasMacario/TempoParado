using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class InteractionLabBuilder
    {
        [MenuItem("Tempo Parado/Adicionar teste de interacao")]
        public static void AddInteraction()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de adicionar a interacao."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            TemporalPickup existing = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
                if (existing == null) existing = root.GetComponentInChildren<TemporalPickup>(true);
            }
            if (player == null) { Debug.LogError("Abra a cena TimeLab que contem o Jogador antes de usar este comando."); return; }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader Lit ou Standard nao encontrado."); return; }
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Adicionar teste de interacao temporal");
            if (player.GetComponent<PlayerInteractor>() == null) Undo.AddComponent<PlayerInteractor>(player.gameObject);
            if (existing == null)
            {
                GameObject item = new GameObject("Esfera temporal");
                SceneManager.MoveGameObjectToScene(item, scene);
                item.transform.position = player.transform.position + player.transform.forward * 1.6f + Vector3.up * 0.5f;
                CharacterController cc = item.AddComponent<CharacterController>();
                cc.stepOffset = 0.05f; cc.radius = 0.32f; cc.height = 0.7f; cc.skinWidth = 0.01f;
                item.AddComponent<TimeBody>();
                TemporalPickup pickup = item.AddComponent<TemporalPickup>();
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Visual";
                visual.transform.SetParent(item.transform, false);
                visual.transform.localScale = Vector3.one * 0.6f;
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                // Listras permitem perceber a rotacao da esfera ao descongelar.
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Faixa";
                stripe.transform.SetParent(visual.transform, false);
                stripe.transform.localScale = new Vector3(1.02f, 0.12f, 0.12f);
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
                string materialPath = "Assets/TempoParado/Materials/TemporalPickup.mat";
                if (!AssetDatabase.IsValidFolder("Assets/TempoParado/Materials"))
                    AssetDatabase.CreateFolder("Assets/TempoParado", "Materials");
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat == null)
                {
                    mat = new Material(shader); mat.color = new Color(0.65f, 0.2f, 0.9f);
                    AssetDatabase.CreateAsset(mat, materialPath);
                }
                visual.GetComponent<Renderer>().sharedMaterial = mat;
                pickup.visual = visual.transform;
                Undo.RegisterCreatedObjectUndo(item, "Criar esfera temporal");
                existing = pickup;
            }
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("Interacao adicionada. Salve a cena com Ctrl+S, entre em Play e use E perto da esfera roxa.");
        }
    }
}
