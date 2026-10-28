using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class TimeLabBuilder
    {
        private const string Root = "Assets/TempoParado";
        [MenuItem("Tempo Parado/Criar laboratorio temporal")]
        public static void CreateLab()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de criar o laboratorio."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Materials");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader Lit ou Standard nao encontrado."); return; }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material ground = MaterialFor("Ground", new Color(0.17f, 0.19f, 0.23f), shader);
            Material white = MaterialFor("Probe", new Color(0.95f, 0.93f, 0.8f), shader);
            Material hero = MaterialFor("Player", new Color(0.95f, 0.75f, 0.2f), shader);
            Cube("Chao", new Vector3(0f, -0.5f, 0f), new Vector3(36f, 1f, 24f), ground);
            Cube("Parede Norte", new Vector3(0f, 1f, 12f), new Vector3(36f, 2f, 1f), ground);
            Cube("Parede Sul", new Vector3(0f, 1f, -12f), new Vector3(36f, 2f, 1f), ground);
            Cube("Parede Leste", new Vector3(18f, 1f, 0f), new Vector3(1f, 2f, 24f), ground);
            Cube("Parede Oeste", new Vector3(-18f, 1f, 0f), new Vector3(1f, 2f, 24f), ground);
            Zone("Normal", -10f, 1f, new Color(0.2f, 0.65f, 0.4f), shader, white);
            Zone("Lento", 0f, 0.2f, new Color(0.2f, 0.45f, 0.85f), shader, white);
            Zone("Rapido", 10f, 2f, new Color(0.95f, 0.4f, 0.15f), shader, white);
            Probe("Objeto congelado", new Vector3(-5f, 2f, -6f), white);
            GameObject player = new GameObject("Jogador");
            player.transform.position = new Vector3(0f, 1f, -7f);
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.stepOffset = 0.3f;
            TimeBody body = player.AddComponent<TimeBody>();
            body.ignoreZones = true;
            player.AddComponent<PlayerMotor>();
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = hero;
            GameObject cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(LabCamera));
            cameraGO.tag = "MainCamera";
            cameraGO.GetComponent<Camera>().farClipPlane = 150f;
            cameraGO.GetComponent<LabCamera>().target = player.transform;
            cameraGO.transform.position = player.transform.position + new Vector3(0f, 17f, -14f);
            cameraGO.transform.LookAt(player.transform.position);
            GameObject lightGO = new GameObject("Sol", typeof(Light));
            lightGO.GetComponent<Light>().type = LightType.Directional;
            lightGO.GetComponent<Light>().intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.55f);
            new GameObject("HUD", typeof(LabHUD)).GetComponent<LabHUD>().player = body;
            string path = AssetDatabase.GenerateUniqueAssetPath(Root + "/Scenes/TimeLab.unity");
            if (!EditorSceneManager.SaveScene(scene, path)) { Debug.LogError("Nao foi possivel salvar a cena."); return; }
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("TimeLab criado em " + path + ". Aperte Play e clique na aba Game.");
        }
        private static void Zone(string name, float x, float scale, Color color, Shader shader, Material probeMaterial)
        {
            Material mat = MaterialFor(name, color, shader);
            Cube("Piso " + name, new Vector3(x, 0.02f, 2f), new Vector3(7f, 0.04f, 8f), mat);
            GameObject zone = new GameObject("Zona " + name);
            zone.transform.position = new Vector3(x, 3f, 2f);
            BoxCollider box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(7f, 8f, 8f);
            zone.AddComponent<TimeZone>().scale = scale;
            Probe("Teste " + name, new Vector3(x, 2f, 3f), probeMaterial);
        }
        private static void Probe(string name, Vector3 pos, Material mat)
        {
            GameObject probe = Cube(name, pos, new Vector3(1.5f, 0.4f, 0.4f), mat);
            Object.DestroyImmediate(probe.GetComponent<Collider>());
            probe.AddComponent<TimeBody>();
            probe.AddComponent<TemporalProbe>();
        }
        private static GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.position = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
        private static Material MaterialFor(string name, Color color, Shader shader)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(shader); mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
