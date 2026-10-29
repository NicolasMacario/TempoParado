using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class CombatFeedbackLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar efeitos de acerto do zelador")]
        public static void EnableFeedback()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play para configurar os efeitos."); return; }
            Scene scene = SceneManager.GetActiveScene();
            EnemyHealth enemy = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (enemy == null) enemy = root.GetComponentInChildren<EnemyHealth>(true);
            if (enemy == null) { Debug.LogError("Abra a cena com o zelador e o combate configurados."); return; }
            CombatFeedback feedback = enemy.GetComponent<CombatFeedback>();
            if (feedback == null) feedback = Undo.AddComponent<CombatFeedback>(enemy.gameObject);
            Undo.RecordObject(feedback, "Configurar efeitos de acerto");
            feedback.enabled = true;
            if (feedback.core == null)
                foreach (Renderer renderer in enemy.GetComponentsInChildren<Renderer>(true))
                    if (renderer.gameObject.name == "Nucleo") { feedback.core = renderer; break; }
            EditorUtility.SetDirty(feedback);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = enemy.gameObject;
            Debug.Log("Efeitos ativados. Ctrl+S para salvar. Acerte a armadura e compare com a janela EXPOSTO.");
        }
    }
}
