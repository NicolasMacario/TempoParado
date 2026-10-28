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
            TemporalStrain strain = player.GetComponent<TemporalStrain>();
            if (strain == null) strain = Undo.AddComponent<TemporalStrain>(player.gameObject);
            Undo.RecordObject(strain, "Ativar condicao temporal");
            strain.enabled = true;
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Condicao temporal ativada. A barra determina os efeitos, e a vida suprimida retorna com a recuperacao. Salve com Ctrl+S.");
        }
        [MenuItem("Tempo Parado/Aplicar buffs e debuffs mais perceptiveis")]
        public static void ApplyNoticeableBalance()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play para aplicar o balanceamento."); return; }
            EnableStrain();
            TemporalStrain strain = FindStrain();
            if (strain == null) return;
            Undo.RecordObject(strain, "Aplicar balanceamento perceptivel");
            strain.ApplyNoticeableBalance();
            EditorUtility.SetDirty(strain);
            EditorSceneManager.MarkSceneDirty(strain.gameObject.scene);
            Debug.Log("Novos valores aplicados ao componente da cena. Ctrl+S para salvar; Ctrl+Z para desfazer.");
        }
        [MenuItem("Tempo Parado/Testar condicao no Play/Equilibrio")]
        public static void TestEquilibrium() { TestCondition(0f); }
        [MenuItem("Tempo Parado/Testar condicao no Play/Lenta critica")]
        public static void TestSlow() { TestCondition(-0.8f); }
        [MenuItem("Tempo Parado/Testar condicao no Play/Acelerada critica")]
        public static void TestFast() { TestCondition(0.8f); }
        private static void TestCondition(float value)
        {
            if (!Application.isPlaying) { Debug.LogWarning("Entre no Play para testar a condicao."); return; }
            TemporalStrain strain = FindStrain();
            if (strain == null || !strain.SetConditionForTesting(value))
            {
                Debug.LogWarning("Use um jogador vivo com TemporalStrain ativo na cena aberta.");
                return;
            }
            Debug.Log("Condicao de teste aplicada. Fora das zonas, a barra conserva esse estado. R reinicia.");
        }
        private static TemporalStrain FindStrain()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                PlayerMotor player = root.GetComponentInChildren<PlayerMotor>(true);
                if (player != null) return player.GetComponent<TemporalStrain>();
            }
            return null;
        }
    }
}
