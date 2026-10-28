using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class StrainLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar desgaste temporal")]
        [MenuItem("Tempo Parado/Ativar buffs e debuffs das zonas")]
        [MenuItem("Tempo Parado/Ativar condicao temporal")]
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
            Debug.Log("Condicao temporal ativada. A barra determina os efeitos, e a vida suprimida retorna com a recuperacao. Salve com Ctrl+S.");
        }
    }
}
