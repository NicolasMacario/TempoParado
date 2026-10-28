using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class PulseLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar Pulso temporal")]
        public static void EnablePulse()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de ativar o Pulso."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
            if (player == null || player.GetComponent<PlayerHealth>() == null)
            {
                Debug.LogError("Abra TimeLab com o jogador e o combate configurados primeiro.");
                return;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader do Pulso nao encontrado."); return; }
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Ativar Pulso temporal");
            PulseAbility ability = player.GetComponent<PulseAbility>();
            if (ability == null) ability = Undo.AddComponent<PulseAbility>(player.gameObject);
            if (ability.pulseMaterial == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/TempoParado/Materials"))
                    AssetDatabase.CreateFolder("Assets/TempoParado", "Materials");
                const string path = "Assets/TempoParado/Materials/PulseWave.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader); material.color = new Color(0.25f, 0.65f, 1f);
                    AssetDatabase.CreateAsset(material, path);
                }
                Undo.RecordObject(ability, "Configurar visual do Pulso");
                ability.pulseMaterial = material;
                EditorUtility.SetDirty(ability);
            }
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Pulso ativado. Salve a cena, entre em Play e pressione Q perto do zelador.");
        }
    }
}
