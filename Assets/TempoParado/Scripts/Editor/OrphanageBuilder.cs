using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TempoParado.Editor
{
    public static class OrphanageBuilder
    {
        private const string Root = "Assets/TempoParado";
        [MenuItem("Tempo Parado/Criar percurso do orfanato")]
        public static void CreateRoute()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play para criar o percurso."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("Shader Lit ou Standard nao encontrado."); return; }
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Materials/Orfanato");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material floor = MaterialFor("Piso", new Color(0.28f, 0.29f, 0.3f), shader);
            Material wall = MaterialFor("Parede", new Color(0.48f, 0.43f, 0.36f), shader);
            Material wood = MaterialFor("Madeira", new Color(0.32f, 0.17f, 0.08f), shader);
            Material hero = MaterialFor("Jogador", new Color(0.95f, 0.75f, 0.2f), shader);
            Material blue = MaterialFor("Lento", new Color(0.15f, 0.4f, 0.85f), shader);
            Material orange = MaterialFor("Acelerado", new Color(0.95f, 0.35f, 0.1f), shader);
            Material green = MaterialFor("Normal", new Color(0.15f, 0.65f, 0.3f), shader);
            Transform structure = new GameObject("Estrutura do orfanato").transform;
            Cube(structure, "Patio", new Vector3(0f, -0.5f, 9f), new Vector3(18f, 1f, 18f), floor);
            Cube(structure, "Corredor", new Vector3(0f, -0.5f, 28f), new Vector3(8f, 1f, 20f), floor);
            Cube(structure, "Sala do zelador", new Vector3(0f, -0.5f, 49f), new Vector3(24f, 1f, 22f), floor);
            Cube(structure, "Saida", new Vector3(0f, -0.5f, 64f), new Vector3(8f, 1f, 8f), floor);
            Wall(structure, "Fundo do patio", 0f, 0f, 19f, 1f, wall);
            Wall(structure, "Patio oeste", -9f, 9f, 1f, 18f, wall);
            Wall(structure, "Patio leste", 9f, 9f, 1f, 18f, wall);
            Wall(structure, "Entrada esquerda", -6.75f, 18f, 5.5f, 1f, wall);
            Wall(structure, "Entrada direita", 6.75f, 18f, 5.5f, 1f, wall);
            Wall(structure, "Corredor oeste", -4.5f, 28f, 1f, 21f, wall);
            Wall(structure, "Corredor leste", 4.5f, 28f, 1f, 21f, wall);
            Wall(structure, "Sala sul esquerda", -8f, 38f, 8f, 1f, wall);
            Wall(structure, "Sala sul direita", 8f, 38f, 8f, 1f, wall);
            Wall(structure, "Sala oeste", -12f, 49f, 1f, 23f, wall);
            Wall(structure, "Sala leste", 12f, 49f, 1f, 23f, wall);
            Wall(structure, "Portao esquerda", -7f, 60f, 10f, 1f, wall);
            Wall(structure, "Portao direita", 7f, 60f, 10f, 1f, wall);
            Wall(structure, "Saida oeste", -4.5f, 64f, 1f, 9f, wall);
            Wall(structure, "Saida leste", 4.5f, 64f, 1f, 9f, wall);
            Wall(structure, "Final", 0f, 68f, 10f, 1f, wall);
            Zone(structure, "Descanso no patio", new Vector3(0f, 0f, 10f), new Vector2(6f, 6f), 1f, 10, green);
            Zone(structure, "Faixa lenta", new Vector3(-2f, 0f, 28f), new Vector2(4f, 19f), 0.2f, 11, blue);
            Zone(structure, "Faixa acelerada", new Vector3(2f, 0f, 28f), new Vector2(4f, 19f), 2f, 12, orange);
            Zone(structure, "Zona lenta da arena", new Vector3(-6f, 0f, 49f), new Vector2(6f, 10f), 0.2f, 11, blue);
            Zone(structure, "Zona acelerada da arena", new Vector3(6f, 0f, 49f), new Vector2(6f, 10f), 2f, 12, orange);
            Zone(structure, "Respiro da arena", new Vector3(0f, 0f, 40.5f), new Vector2(4f, 3f), 1f, 10, green);
            Sign(structure, "PATIO | SIGA EM FRENTE", new Vector3(0f, 3f, 16f));
            Sign(structure, "ESCOLHA SEU RITMO", new Vector3(0f, 3f, 27f));
            Sign(structure, "DERROTE O ZELADOR", new Vector3(0f, 3f, 53f));
            Sign(structure, "SAIDA", new Vector3(0f, 3.3f, 67f));
            // Decoracao nas laterais; centro da arena livre para a IA sem navegacao.
            Cube(structure, "Banco esquerdo", new Vector3(-6.5f, 0.4f, 9f), new Vector3(1f, 0.8f, 4f), wood);
            Cube(structure, "Banco direito", new Vector3(6.5f, 0.4f, 9f), new Vector3(1f, 0.8f, 4f), wood);
            PlayerMotor player = CreatePlayer(hero);
            CreateCamera(player);
            GameObject sun = new GameObject("Luz", typeof(Light));
            sun.GetComponent<Light>().type = LightType.Directional;
            sun.GetComponent<Light>().intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.5f, 0.48f, 0.45f);
            InteractionLabBuilder.AddInteraction();
            EnemyLabBuilder.AddEnemy();
            CombatLabBuilder.EnableCombat();
            DodgeLabBuilder.EnableDodge();
            PlayerCombatLabBuilder.EnablePlayerAttack();
            PerfectDodgeLabBuilder.EnablePerfectDodge();
            PulseLabBuilder.EnablePulse();
            StrainLabBuilder.ApplyNoticeableBalance();
            WoodenBirdLabBuilder.EnableBird();
            EnemyChaser enemy = FindInScene<EnemyChaser>(scene);
            if (enemy == null || player.GetComponent<WoodenBird>() == null)
            {
                Debug.LogError("Configuracao incompleta. Confira os erros anteriores antes de salvar.");
                return;
            }
            enemy.transform.SetPositionAndRotation(new Vector3(0f, 1f, 51f), Quaternion.Euler(0f, 180f, 0f));
            enemy.detectionRange = 10f;
            enemy.loseRange = 18f;
            GameObject gate = Cube(structure, "Portao bloqueado", new Vector3(0f, 1.75f, 60f), new Vector3(4f, 3.5f, 0.5f), wood);
            BoxCollider exit = Volume("Destino do percurso", new Vector3(0f, 3f, 64.5f), new Vector3(7f, 6f, 5f));
            OrphanageRoute route = new GameObject("Objetivo do percurso").AddComponent<OrphanageRoute>();
            route.player = player;
            route.guardian = enemy.GetComponent<EnemyHealth>();
            route.gate = gate.transform;
            route.gateCollider = gate.GetComponent<Collider>();
            route.exitBounds = exit;
            GameHUDLabBuilder.EnableCleanHUD();
            string path = AssetDatabase.GenerateUniqueAssetPath(Root + "/Scenes/OrfanatoPrototipo.unity");
            if (!EditorSceneManager.SaveScene(scene, path)) { Debug.LogError("Nao foi possivel salvar o percurso."); return; }
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Percurso criado em " + path + ". Play: atravesse o corredor, derrote o zelador e alcance a saida. R reinicia tudo.");
        }
        private static PlayerMotor CreatePlayer(Material material)
        {
            GameObject go = new GameObject("Jogador");
            go.transform.position = new Vector3(0f, 1f, 3f);
            CharacterController controller = go.AddComponent<CharacterController>();
            controller.height = 2f; controller.radius = 0.4f; controller.stepOffset = 0.3f;
            go.AddComponent<TimeBody>().ignoreZones = true;
            PlayerMotor motor = go.AddComponent<PlayerMotor>();
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = material;
            return motor;
        }
        private static void CreateCamera(PlayerMotor player)
        {
            GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(LabCamera));
            go.tag = "MainCamera";
            go.GetComponent<Camera>().farClipPlane = 180f;
            go.GetComponent<LabCamera>().target = player.transform;
            go.transform.position = player.transform.position + new Vector3(0.5f, 1.8f, -4.5f);
            go.transform.LookAt(player.transform.position + Vector3.up * 0.7f);
            player.cameraTransform = go.transform;
        }
        private static void Zone(Transform parent, string name, Vector3 center, Vector2 size, float scale, int priority, Material material)
        {
            Cube(parent, "Piso - " + name, center + Vector3.up * 0.02f, new Vector3(size.x, 0.04f, size.y), material);
            BoxCollider volume = Volume(name, center + Vector3.up * 3f, new Vector3(size.x, 8f, size.y));
            volume.transform.SetParent(parent, true);
            TimeZone zone = volume.gameObject.AddComponent<TimeZone>();
            zone.scale = scale;
            zone.priority = priority;
        }
        private static BoxCollider Volume(string name, Vector3 position, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = size;
            return box;
        }
        private static void Wall(Transform parent, string name, float x, float z, float width, float depth, Material material)
        {
            Cube(parent, name, new Vector3(x, 1.75f, z), new Vector3(width, 3.5f, depth), material);
        }
        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        private static void Sign(Transform parent, string text, Vector3 position)
        {
            GameObject go = new GameObject(text);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = text; mesh.fontSize = 48; mesh.characterSize = 0.09f;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
        }
        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null) return result;
            }
            return null;
        }
        private static Material MaterialFor(string name, Color color, Shader shader)
        {
            string path = Root + "/Materials/Orfanato/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader); material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
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
