using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class GameHUDLabBuilder
    {
        [MenuItem("Tempo Parado/Ativar HUD compacto")]
        public static void EnableCleanHUD()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play para configurar o HUD."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            OrphanageRoute route = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
                if (route == null) route = root.GetComponentInChildren<OrphanageRoute>(true);
            }
            if (player == null || player.GetComponent<PlayerHealth>() == null)
            {
                Debug.LogError("Abra uma cena com jogador e vida configurados.");
                return;
            }
            GameHUD hud = player.GetComponent<GameHUD>();
            if (hud == null) hud = Undo.AddComponent<GameHUD>(player.gameObject);
            Undo.RecordObject(hud, "Ativar HUD compacto");
            hud.enabled = true;
            hud.showDebug = false;
            hud.route = route;
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = player.gameObject;
            Debug.Log("HUD compacto ativado. Ctrl+S para salvar; F1 alterna os detalhes durante o Play.");
        }
    }
}
