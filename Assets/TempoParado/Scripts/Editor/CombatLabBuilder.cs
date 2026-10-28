using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class CombatLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar combate do zelador")]
        public static void EnableCombat()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de ativar o combate."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            EnemyChaser enemy = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
                if (enemy == null) enemy = root.GetComponentInChildren<EnemyChaser>(true);
            }
            if (player == null || enemy == null)
            {
                Debug.LogError("Abra a cena TimeLab com Jogador e Zelador Prototipo. Se necessario, use Adicionar primeiro zelador.");
                return;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader para o aviso de ataque nao encontrado."); return; }
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Ativar combate do zelador");
            if (player.GetComponent<PlayerHealth>() == null) Undo.AddComponent<PlayerHealth>(player.gameObject);
            AttackTelegraph telegraph = enemy.GetComponent<AttackTelegraph>();
            if (telegraph == null) telegraph = Undo.AddComponent<AttackTelegraph>(enemy.gameObject);
            if (telegraph.sourceMaterial == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/TempoParado/Materials"))
                    AssetDatabase.CreateFolder("Assets/TempoParado", "Materials");
                const string path = "Assets/TempoParado/Materials/ZeladorTelegraph.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader); mat.color = new Color(1f, 0.75f, 0.1f);
                    AssetDatabase.CreateAsset(mat, path);
                }
                Undo.RecordObject(telegraph, "Configurar aviso de ataque");
                telegraph.sourceMaterial = mat;
                EditorUtility.SetDirty(telegraph);
            }
            if (enemy.target != player.transform)
            {
                Undo.RecordObject(enemy, "Configurar alvo do ataque");
                enemy.target = player.transform;
                EditorUtility.SetDirty(enemy);
            }
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = enemy.gameObject;
            Debug.Log("Combate ativado. Salve a cena, entre em Play e observe a area de aviso antes do golpe.");
        }
    }
}
