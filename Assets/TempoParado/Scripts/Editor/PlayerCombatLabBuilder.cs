using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class PlayerCombatLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar ataque do jogador")]
        public static void EnablePlayerAttack()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de ativar o ataque."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            EnemyChaser enemy = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
                if (enemy == null) enemy = root.GetComponentInChildren<EnemyChaser>(true);
            }
            if (player == null || enemy == null || player.GetComponent<PlayerHealth>() == null)
            {
                Debug.LogError("Abra TimeLab com o combate do zelador configurado antes de ativar o ataque do jogador.");
                return;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader da arma provisoria nao encontrado."); return; }
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Ativar ataque do jogador");
            if (enemy.GetComponent<EnemyHealth>() == null) Undo.AddComponent<EnemyHealth>(enemy.gameObject);
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat == null) combat = Undo.AddComponent<PlayerCombat>(player.gameObject);
            if (combat.weaponPivot == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/TempoParado/Materials"))
                    AssetDatabase.CreateFolder("Assets/TempoParado", "Materials");
                const string path = "Assets/TempoParado/Materials/PlayerWeapon.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader); material.color = new Color(0.8f, 0.85f, 0.9f);
                    AssetDatabase.CreateAsset(material, path);
                }
                GameObject pivot = new GameObject("Arma provisoria");
                pivot.transform.SetParent(player.transform, false);
                pivot.transform.localPosition = new Vector3(0.55f, 0.15f, 0.25f);
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Bastao";
                visual.transform.SetParent(pivot.transform, false);
                visual.transform.localPosition = Vector3.forward * 0.55f;
                visual.transform.localScale = new Vector3(0.14f, 0.14f, 1.1f);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().sharedMaterial = material;
                Undo.RegisterCreatedObjectUndo(pivot, "Criar arma provisoria");
                Undo.RecordObject(combat, "Vincular arma provisoria");
                combat.weaponPivot = pivot.transform;
                EditorUtility.SetDirty(combat);
            }
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Ataque ativado. Salve, entre em Play e clique com o botao esquerdo olhando para o zelador.");
        }
    }
}
