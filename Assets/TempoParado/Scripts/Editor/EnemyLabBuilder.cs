using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class EnemyLabBuilder
    {
        [MenuItem("Tempo Parado/Adicionar primeiro zelador")]
        public static void AddEnemy()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de adicionar o zelador."); return; }
            Scene scene = SceneManager.GetActiveScene();
            PlayerMotor player = null;
            EnemyChaser existing = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerMotor>(true);
                if (existing == null) existing = root.GetComponentInChildren<EnemyChaser>(true);
            }
            if (player == null) { Debug.LogError("Abra a cena TimeLab com o Jogador antes de usar este comando."); return; }
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("A cena ja contem um zelador. Ele foi selecionado na Hierarchy.");
                return;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader Lit ou Standard nao encontrado."); return; }
            if (!AssetDatabase.IsValidFolder("Assets/TempoParado/Materials"))
                AssetDatabase.CreateFolder("Assets/TempoParado", "Materials");
            Material shell = MaterialFor("ZeladorShell", new Color(0.25f, 0.3f, 0.37f), shader);
            Material core = MaterialFor("ZeladorCore", new Color(1f, 0.35f, 0.08f), shader);
            GameObject enemy = new GameObject("Zelador Prototipo");
            SceneManager.MoveGameObjectToScene(enemy, scene);
            // Ponto livre na cena padrao, atras dos tres pisos coloridos.
            enemy.transform.position = new Vector3(0f, 1f, 8f);
            CharacterController cc = enemy.AddComponent<CharacterController>();
            cc.radius = 0.45f; cc.height = 2f; cc.stepOffset = 0.25f;
            TimeBody time = enemy.AddComponent<TimeBody>();
            time.defaultScale = 1f; // Zeladores continuam ativos fora dos bolsoes.
            time.ignoreZones = false;
            EnemyChaser chaser = enemy.AddComponent<EnemyChaser>();
            chaser.target = player.transform;
            enemy.AddComponent<EnemyLabHUD>();
            Part(enemy.transform, "Corpo", PrimitiveType.Capsule, Vector3.zero,
                new Vector3(0.85f, 0.95f, 0.85f), shell);
            Part(enemy.transform, "Nucleo", PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0.43f),
                Vector3.one * 0.36f, core);
            Part(enemy.transform, "Estilhaco esquerdo", PrimitiveType.Cube, new Vector3(-0.55f, 0.3f, 0f),
                new Vector3(0.2f, 0.85f, 0.25f), shell);
            Part(enemy.transform, "Estilhaco direito", PrimitiveType.Cube, new Vector3(0.55f, 0.1f, 0f),
                new Vector3(0.2f, 0.65f, 0.25f), shell);
            Undo.RegisterCreatedObjectUndo(enemy, "Adicionar primeiro zelador");
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = enemy;
            Debug.Log("Zelador adicionado. Salve com Ctrl+S, entre em Play e aproxime-se dos pisos coloridos.");
        }
        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
        private static Material MaterialFor(string name, Color color, Shader shader)
        {
            string path = "Assets/TempoParado/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(shader); mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
