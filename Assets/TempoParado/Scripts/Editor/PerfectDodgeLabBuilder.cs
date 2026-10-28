using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class PerfectDodgeLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar esquiva perfeita")]
        public static void EnablePerfectDodge()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de ativar a esquiva perfeita."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
            if (player == null || player.GetComponent<PlayerDodge>() == null || player.GetComponent<PlayerHealth>() == null)
            {
                Debug.LogError("Abra TimeLab com a esquiva basica e o combate configurados primeiro.");
                return;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader para o efeito temporal nao encontrado."); return; }
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Ativar esquiva perfeita");
            PerfectDodge ability = player.GetComponent<PerfectDodge>();
            if (ability == null) ability = Undo.AddComponent<PerfectDodge>(player.gameObject);
            if (ability.ringMaterial == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/TempoParado/Materials"))
                    AssetDatabase.CreateFolder("Assets/TempoParado", "Materials");
                const string path = "Assets/TempoParado/Materials/PerfectDodgeRing.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader); material.color = new Color(0.1f, 1f, 1f);
                    AssetDatabase.CreateAsset(material, path);
                }
                Undo.RecordObject(ability, "Configurar efeito da esquiva perfeita");
                ability.ringMaterial = material;
                EditorUtility.SetDirty(ability);
            }
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Esquiva perfeita ativada. Salve a cena e esquive no instante anterior ao golpe.");
        }
    }
}
