using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class ZeladorBossLabBuilder
    {
        [MenuItem("Tempo Parado/Transformar zelador em chefe")]
        public static void ConfigureBoss()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play para configurar o chefe."); return; }
            Scene scene = SceneManager.GetActiveScene();
            EnemyChaser enemy = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (enemy == null) enemy = root.GetComponentInChildren<EnemyChaser>(true);
            if (enemy == null || enemy.GetComponent<EnemyHealth>() == null || enemy.GetComponent<AttackTelegraph>() == null)
            {
                Debug.LogError("Abra a cena com zelador, vida e combate configurados.");
                return;
            }
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            ZeladorBoss boss = enemy.GetComponent<ZeladorBoss>();
            if (boss == null) boss = Undo.AddComponent<ZeladorBoss>(enemy.gameObject);
            Undo.RecordObject(boss, "Ativar chefe");
            Undo.RecordObject(enemy, "Configurar combate do chefe");
            Undo.RecordObject(health, "Configurar vida do chefe");
            boss.enabled = true;
            health.maxHealth = 320;
            enemy.speed = 3.4f;
            enemy.stopDistance = 2f;
            enemy.attackRange = 3.1f;
            enemy.attackArc = 150f;
            enemy.attackDamage = 26;
            enemy.windupDuration = 0.95f;
            enemy.impactDuration = 0.18f;
            enemy.recoveryDuration = 1.2f;
            EditorUtility.SetDirty(enemy);
            EditorUtility.SetDirty(health);
            EditorUtility.SetDirty(boss);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = enemy.gameObject;
            Debug.Log("Zelador configurado como chefe: 320 de vida, tres padroes, armadura e fase 2 em 50%. Ctrl+S para salvar.");
        }
    }
}
