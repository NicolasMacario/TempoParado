using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class StrainLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar desgaste temporal")]
        public static void EnableStrain()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de ativar o desgaste."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
            if (player == null || player.GetComponent<PlayerHealth>() == null)
            {
                Debug.LogError("Abra TimeLab com o jogador e o combate configurados primeiro.");
                return;
            }
            if (player.GetComponent<TemporalStrain>() == null) Undo.AddComponent<TemporalStrain>(player.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Desgaste ativado. Salve com Ctrl+S e teste nos pisos azul, laranja e verde.");
        }
    }
}
