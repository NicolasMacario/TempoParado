using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class DodgeLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar esquiva do jogador")]
        public static void EnableDodge()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de ativar a esquiva."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
            if (player == null || player.GetComponent<PlayerHealth>() == null)
            {
                Debug.LogError("Abra a cena TimeLab com o combate configurado. Se necessario, use Ativar combate do zelador primeiro.");
                return;
            }
            if (player.GetComponent<PlayerDodge>() == null) Undo.AddComponent<PlayerDodge>(player.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Esquiva ativada. Salve a cena, entre em Play e use WASD + Espaco. Sem direcao, o personagem esquiva para tras.");
        }
    }
}
