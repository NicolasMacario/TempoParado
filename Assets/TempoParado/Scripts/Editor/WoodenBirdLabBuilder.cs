using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class WoodenBirdLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar passaro de madeira")]
        public static void EnableBird()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de ativar o passaro de madeira.");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
            if (player == null || player.GetComponent<PlayerHealth>() == null || player.GetComponent<TemporalStrain>() == null)
            {
                Debug.LogError("Abra TimeLab com vida e condicao temporal configuradas primeiro.");
                return;
            }
            WoodenBird bird = player.GetComponent<WoodenBird>();
            if (bird == null) bird = Undo.AddComponent<WoodenBird>(player.gameObject);
            Undo.RecordObject(bird, "Ativar passaro de madeira");
            bird.enabled = true;
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Passaro ativado. Ctrl+S para salvar. No Play, F recupera equilibrio e cura 40% da vida maxima; R renova o teste.");
        }
    }
}
