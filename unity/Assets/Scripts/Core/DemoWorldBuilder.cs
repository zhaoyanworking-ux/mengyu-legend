using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChibiARPG
{
    /// <summary>
    /// 生成青岚镇 + 雾石坡、灯光、UI 与角色。编辑器菜单与运行时引导共用。
    /// </summary>
    public static class DemoWorldBuilder
    {
        public const float TownHalf = 16f;
        public const float FieldZ0 = 18f;
        public const float FieldZ1 = 46f;

        public static void Build()
        {
            ApplyEnvironment();
            CreateLights();
            CreateGround();
            CreateTownProps();
            CreateFieldProps();
            CreateWalls();
            CreateSystems();
            CreateCamera();
            CreateNpcs();
            CreateSpawners();
            CreateUi();
        }

        static void ApplyEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.88f, 0.98f, 1f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 58f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.86f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.95f, 0.88f, 0.82f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.7f, 0.5f);
            RenderSettings.skybox = null;
            RenderSettings.subtractiveShadowColor = new Color(0.55f, 0.5f, 0.7f);
        }

        static void CreateLights()
        {
            var sunGo = new GameObject("阳光");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.35f;
            sunGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            var fillGo = new GameObject("补光");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.72f, 0.8f, 1f);
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(20f, 140f, 0f);
        }

        static void CreateGround()
        {
            // 整片嫩绿草地（雾石坡不再整块铺紫）
            var meadow = Box("草地", new Vector3(0f, -0.5f, 14f), new Vector3(90f, 1f, 100f), Hex("7EDC7A"));
            meadow.isStatic = true;
            var plaza = Cylinder("古井广场", new Vector3(0f, 0.02f, 0f), new Vector3(9.2f, 0.04f, 9.2f), Hex("E8D7B8"));
            plaza.isStatic = true;
            var path = Box("石板路", new Vector3(0f, 0.03f, 9f), new Vector3(3.2f, 0.06f, 16f), Hex("E2D0A8"));
            path.isStatic = true;
            // 紫色蚀斑只做局部圆盘
            float[] px = { -2f, 3.5f, 0f, 6f, -5f, 1f, -3.5f };
            float[] pz = { 30f, 33f, 38f, 35f, 36f, 28f, 41f };
            float[] pr = { 2.8f, 2.2f, 3.4f, 1.8f, 2.4f, 1.6f, 2.0f };
            for (int i = 0; i < px.Length; i++)
            {
                var disc = Cylinder("蚀斑" + i, new Vector3(px[i], 0.03f, pz[i]), new Vector3(pr[i], 0.03f, pr[i]), Hex("6B4A88"));
                disc.isStatic = true;
            }
        }

        static void CreateTownProps()
        {
            // 中央分层喷泉
            Cylinder("泉座", new Vector3(0f, 0.2f, 0f), new Vector3(3.4f, 0.2f, 3.4f), Hex("D8D0C4"));
            Cylinder("水池", new Vector3(0f, 0.35f, 0f), new Vector3(2.8f, 0.16f, 2.8f), Hex("8EC8EA"));
            Cylinder("泉盘", new Vector3(0f, 0.7f, 0f), new Vector3(1.4f, 0.12f, 1.4f), Hex("E8E0D4"));
            Cylinder("泉柱", new Vector3(0f, 0.85f, 0f), new Vector3(0.35f, 0.45f, 0.35f), Hex("EDE6DA"));
            Sphere("泉水", new Vector3(0f, 1.25f, 0f), 0.38f, Hex("A8D8F0"));
            Cylinder("水柱", new Vector3(0f, 1.05f, 0f), new Vector3(0.12f, 0.35f, 0.12f), Hex("C8ECFA"));

            // 环形小屋（白墙/米色墙 + 蓝/红/紫屋顶 + 烟囱）
            House(new Vector3(-8.6f, 0f, 3.2f), 110f, Hex("FFF7EE"), Hex("5BA8E0"), true);   // 蓝
            House(new Vector3(-7.6f, 0f, 8.0f), 150f, Hex("FFF6EC"), Hex("E06A55"), true);   // 红
            House(new Vector3(-4.2f, 0f, 10.6f), 175f, Hex("FFFDF8"), Hex("5BA8E0"), true);  // 蓝
            House(new Vector3(8.8f, 0f, 4.6f), -110f, Hex("FFF8F0"), Hex("5BA8E0"), true);   // 蓝
            House(new Vector3(8.2f, 0f, -3.8f), -40f, Hex("FFF7FB"), Hex("B07AD0"), true);   // 紫
            House(new Vector3(-8.4f, 0f, -5.5f), 40f, Hex("FFF9F0"), Hex("5BA8E0"), false);

            // 条纹摊位
            Stall(new Vector3(-6.2f, 0f, -7.8f), 20f, Hex("6EC0EE"), Hex("FFFFFF")); // 蓝白
            Stall(new Vector3(6.4f, 0f, -7.2f), -20f, Hex("F0B060"), Hex("FFFFFF")); // 橙白
            Stall(new Vector3(7.6f, 0f, 1.2f), -90f, Hex("6EC0EE"), Hex("FFFFFF"));

            NoticeBoard(new Vector3(-5.4f, 0f, -9.4f), 15f);

            LampPost(new Vector3(-3.6f, 0f, 3.8f));
            LampPost(new Vector3(3.6f, 0f, 3.8f));
            LampPost(new Vector3(-4.0f, 0f, -4.0f));
            LampPost(new Vector3(4.2f, 0f, -3.6f));
            LampPost(new Vector3(0f, 0f, 8.5f));

            // 圆滚滚的树
            Tree(new Vector3(-5.6f, 0f, -10.5f), Hex("7EDC6A"));
            Tree(new Vector3(5.8f, 0f, -10.2f), Hex("8EE07A"));
            Tree(new Vector3(-12.2f, 0f, 2.0f), Hex("6CC85A"));
            Tree(new Vector3(12.5f, 0f, 1.5f), Hex("85D078"));
            Tree(new Vector3(-11.5f, 0f, -8.5f), Hex("7BC96A"));
            Tree(new Vector3(12.0f, 0f, -8.0f), Hex("9AE07A"));
            Tree(new Vector3(-11f, 0f, 12f), Hex("6CB85A"));
            Tree(new Vector3(11.2f, 0f, 12.4f), Hex("7EDC6A"));

            Color[] bloom = { Hex("FFF8F0"), Hex("FFB3C8"), Hex("FFD36A"), Hex("B8D8FF"), Hex("E0B8FF"), Hex("FFFFFF") };
            int n = 0;
            for (int z = -13; z <= 15; z += 2)
            {
                for (int x = -14; x <= 14; x += 2)
                {
                    float d = x * x + z * z;
                    if (d < 28f) continue;
                    if (Mathf.Abs(x) < 2.2f && z > 2f) continue;
                    if ((x + z + n) % 3 != 0) { n++; continue; }
                    Flower(new Vector3(x + 0.3f * (n % 3), 0f, z + 0.2f * ((n * 2) % 3)), bloom[n % bloom.Length]);
                    n++;
                }
            }

            // 石拱门通向雾石坡（北）
            ArchGate(new Vector3(0f, 0f, 15.8f));
        }

        static void CreateFieldProps()
        {
            Tree(new Vector3(-11f, 0f, 24f), Hex("5AA85A"));
            Tree(new Vector3(12f, 0f, 25f), Hex("4A9A58"));
            Tree(new Vector3(-9f, 0f, 38f), Hex("4A8A58"));
            Tree(new Vector3(10f, 0f, 40f), Hex("3A7A48"));
            Tree(new Vector3(-13f, 0f, 32f), Hex("5A9A5A"));
            Tree(new Vector3(13.5f, 0f, 34f), Hex("4A8A50"));
            // 紫色蚀雾（半透明感用浅紫椭圆近似）
            Mist(new Vector3(-3f, 1.4f, 30f), 2.2f);
            Mist(new Vector3(4f, 1.6f, 34f), 2.6f);
            Mist(new Vector3(0f, 2.0f, 40f), 3.2f);
            Mist(new Vector3(6f, 1.3f, 28f), 1.8f);
            Mist(new Vector3(-6f, 1.5f, 36f), 2.4f);
            Box("残石", new Vector3(-6f, 0.3f, 28f), new Vector3(1.4f, 0.6f, 1.1f), Hex("7A6A80"));
            Box("残石2", new Vector3(7f, 0.25f, 36f), new Vector3(1.6f, 0.5f, 1.2f), Hex("6A5A78"));
        }

        static void CreateWalls()
        {
            // 隐形围墙
            float y = 2f;
            Wall("墙南", new Vector3(0f, y, -18f), new Vector3(38f, 6f, 1f));
            Wall("墙西", new Vector3(-18f, y, 8f), new Vector3(1f, 6f, 56f));
            Wall("墙东", new Vector3(18f, y, 8f), new Vector3(1f, 6f, 56f));
            Wall("墙北", new Vector3(0f, y, 47f), new Vector3(38f, 6f, 1f));
            // 石拱两侧的碰撞（中间留出拱门）
            Wall("墙门左", new Vector3(-8.5f, 1f, 15.8f), new Vector3(11f, 2.2f, 0.8f));
            Wall("墙门右", new Vector3(8.5f, 1f, 15.8f), new Vector3(11f, 2.2f, 0.8f));
        }

        static void Wall(string name, Vector3 pos, Vector3 scale)
        {
            var go = Box(name, pos, scale, Hex("7EDC7A"));
            var r = go.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        static void CreateSystems()
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
            gm.AddComponent<QuestSystem>();
            gm.AddComponent<DamagePopupManager>();
            gm.AddComponent<HitStop>();
            var plaza = new GameObject("PlazaPoint");
            plaza.transform.position = new Vector3(0f, 0f, -2.5f);
            GameManager.Instance.PlazaPoint = plaza.transform;
        }

        static void CreateCamera()
        {
            Camera cam = Camera.main;
            GameObject go;
            if (cam == null)
            {
                go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.tag = "MainCamera";
                go.AddComponent<AudioListener>();
            }
            else
                go = cam.gameObject;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.82f, 0.98f);
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 80f;
            cam.fieldOfView = 42f;
            cam.allowMSAA = true;
            cam.allowHDR = false;

            var shake = go.transform.Find("Shake");
            Transform host = shake;
            if (host == null)
            {
                var s = new GameObject("Shake");
                s.transform.SetParent(go.transform, false);
                host = s.transform;
            }

            if (host.GetComponent<CameraImpulse>() == null)
                host.gameObject.AddComponent<CameraImpulse>();
            // 把实际摄像机放到 Shake 子物体上会打乱层级；震动写在子物体本地位移，
            // 跟随脚本放在根上。将 Camera 放到 Shake 下更正确。
            if (cam.transform == go.transform)
            {
                // 保留摄像机在根，震动由 CameraImpulse 改 localPosition（根的本地）
                if (go.GetComponent<CameraImpulse>() == null)
                    go.AddComponent<CameraImpulse>();
            }

            var follow = go.GetComponent<FollowCamera>();
            if (follow == null)
                follow = go.AddComponent<FollowCamera>();
            follow.offset = new Vector3(0f, 11.5f, -9.2f);
            follow.fixedEuler = new Vector3(48f, 0f, 0f);
            go.transform.position = new Vector3(0f, 11.5f, -11.7f);
            go.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
        }

        static void CreateNpcs()
        {
            SpawnNpc(QuestCatalog.NpcMayor, "苏晚晴", new Vector3(-1.8f, 0f, 1.6f), 30f, ChibiKind.NpcElder);
            SpawnNpc(QuestCatalog.NpcHerbalist, "叶青", new Vector3(5.6f, 0f, -6.0f), -20f, ChibiKind.NpcHerbalist);
            SpawnNpc(QuestCatalog.NpcGuard, "石川", new Vector3(1.8f, 0f, 14.2f), 180f, ChibiKind.NpcGuard);
        }

        static void SpawnNpc(string id, string display, Vector3 pos, float yaw, ChibiKind kind)
        {
            var go = new GameObject("NPC_" + display);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.7f, 0f);
            cc.radius = 0.4f;
            cc.height = 1.4f;
            ChibiFactory.Assemble(go.transform, kind);
            var npc = go.AddComponent<NpcCharacter>();
            npc.npcId = id;
            npc.displayName = display;
            npc.marker = QuestMarker.Create(go.transform);
            npc.RefreshMarker();
        }

        static void CreateSpawners()
        {
            var field = new GameObject("蚀斑兽刷新");
            field.transform.position = new Vector3(0f, 0f, 32f);
            var sp = field.AddComponent<EnemySpawner>();
            sp.maxAlive = 6;
            sp.radius = 10f;
            sp.respawnDelay = 4.5f;

            var boss = new GameObject("先锋刷新");
            boss.transform.position = new Vector3(0f, 0f, 40f);
            var bs = boss.AddComponent<EnemySpawner>();
            bs.isBossSpawner = true;
            bs.maxAlive = 1;
            bs.radius = 2f;
            if (GameManager.Instance != null)
                GameManager.Instance.BossSpawner = bs;
        }

        static void CreateUi()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var hud = canvasGo.AddComponent<GameHud>();
            hud.Build(canvasGo.transform);
            hud.ShowClassSelect(true);
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.OnChanged += hud.RefreshQuest;
                hud.RefreshQuest();
            }
        }

        public static GameObject SpawnPlayer(Vector3 pos, PlayerClassDefinition def)
        {
            if (def == null)
                def = PlayerClassCatalog.Get(PlayerClassCatalog.DefaultClassId);
            var go = new GameObject("玩家_" + def.displayName);
            go.tag = "Player";
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.7f, 0f);
            cc.radius = 0.38f;
            cc.height = 1.4f;
            cc.skinWidth = 0.04f;
            var stats = go.AddComponent<CharacterStats>();
            stats.Init(def.maxHp, def.maxMp, true, def.displayName);
            var anim = ChibiFactory.AssemblePlayer(go.transform, def.classId);
            var combat = go.AddComponent<PlayerCombat>();
            var mover = go.AddComponent<PlayerController>();
            combat.Setup(def, stats, anim);
            mover.Setup(def, stats, anim, combat);
            WorldHealthBar.Create(stats, go.transform, new Vector3(0f, 1.7f, 0f), 0.9f);
            stats.OnDamaged += (amt, pt, atk) => DamagePopupManager.Show(pt + Vector3.up * 0.2f, amt, amt > def.attackDamage * 1.3f);
            return go;
        }

        public static GameObject SpawnEnemy(Vector3 pos, EnemyKind kind)
        {
            bool boss = kind == EnemyKind.MiniBoss;
            var go = new GameObject(boss ? "蚀雾史莱姆" : "蚀斑史莱姆");
            go.tag = "Enemy";
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, boss ? 1.0f : 0.65f, 0f);
            cc.radius = boss ? 0.6f : 0.4f;
            cc.height = boss ? 2.0f : 1.3f;
            var stats = go.AddComponent<CharacterStats>();
            if (boss)
                stats.Init(280f, 0f, false, "蚀雾史莱姆");
            else
                stats.Init(45f, 0f, false, "蚀斑史莱姆");
            ChibiAnim anim;
            if (boss)
                anim = ChibiFactory.Assemble(go.transform, ChibiKind.MiniBoss);
            else
            {
                int roll = Random.Range(0, 3);
                if (roll == 0)
                    anim = ChibiFactory.BuildColoredSlime(go.transform, ChibiFactory.Hex("A06AD8"), ChibiFactory.Hex("E8C8FF"), ChibiFactory.Hex("4A2080"), 1f);
                else if (roll == 1)
                    anim = ChibiFactory.BuildColoredSlime(go.transform, ChibiFactory.Hex("5AB4FF"), ChibiFactory.Hex("D0F0FF"), ChibiFactory.Hex("1A4A80"), 1f);
                else
                    anim = ChibiFactory.BuildColoredSlime(go.transform, ChibiFactory.Hex("7BE36A"), ChibiFactory.Hex("D8FF9A"), ChibiFactory.Hex("1A4A20"), 1f);
            }

            var ai = go.AddComponent<EnemyAI>();
            ai.Setup(kind, stats, anim);
            WorldHealthBar.Create(stats, go.transform, new Vector3(0f, boss ? 2.4f : 1.55f, 0f), boss ? 1.3f : 0.85f);
            stats.OnDamaged += (amt, pt, atk) => DamagePopupManager.Show(pt + Vector3.up * 0.15f, amt, false);
            return go;
        }

        static GameObject Box(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            Mat(go, color, 0.018f);
            return go;
        }

        static GameObject Cylinder(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            Mat(go, color, 0.016f);
            return go;
        }

        static GameObject Sphere(string name, Vector3 pos, float r, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (r * 2f);
            Mat(go, color, 0.016f);
            return go;
        }

        static void House(Vector3 pos, float yaw, Color wall, Color roof, bool chimney)
        {
            var root = new GameObject("屋");
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "墙";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            body.transform.localScale = new Vector3(3.4f, 2.1f, 2.7f);
            Mat(body, wall, 0.022f);
            // 人字顶：两块斜板
            var roofL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofL.name = "顶L";
            roofL.transform.SetParent(root.transform, false);
            roofL.transform.localPosition = new Vector3(-0.7f, 2.35f, 0f);
            roofL.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            roofL.transform.localScale = new Vector3(2.3f, 0.18f, 3.1f);
            Mat(roofL, roof, 0.02f);
            var roofR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofR.name = "顶R";
            roofR.transform.SetParent(root.transform, false);
            roofR.transform.localPosition = new Vector3(0.7f, 2.35f, 0f);
            roofR.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            roofR.transform.localScale = new Vector3(2.3f, 0.18f, 3.1f);
            Mat(roofR, roof, 0.02f);
            var gable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gable.name = "山墙";
            gable.transform.SetParent(root.transform, false);
            gable.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            gable.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            gable.transform.localScale = new Vector3(1.7f, 1.1f, 1.7f);
            Mat(gable, wall, 0.016f);
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "门";
            door.transform.SetParent(root.transform, false);
            door.transform.localPosition = new Vector3(0f, 0.7f, 1.38f);
            door.transform.localScale = new Vector3(0.7f, 1.2f, 0.08f);
            Mat(door, Hex("A06A3A"), 0.01f);
            // 深色木梁，让白墙读出概念图里的木框架
            Color timber = Hex("8A5A32");
            void Beam(string n, Vector3 lp, Vector3 sc)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = n;
                b.transform.SetParent(root.transform, false);
                b.transform.localPosition = lp;
                b.transform.localScale = sc;
                Mat(b, timber, 0.008f);
            }
            Beam("梁上", new Vector3(0f, 1.85f, 1.36f), new Vector3(3.4f, 0.09f, 0.1f));
            Beam("梁中", new Vector3(0f, 1.05f, 1.36f), new Vector3(3.4f, 0.08f, 0.08f));
            Beam("柱L", new Vector3(-1.62f, 1.05f, 1.36f), new Vector3(0.1f, 2.0f, 0.1f));
            Beam("柱R", new Vector3(1.62f, 1.05f, 1.36f), new Vector3(0.1f, 2.0f, 0.1f));
            var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            win.name = "窗";
            win.transform.SetParent(root.transform, false);
            win.transform.localPosition = new Vector3(0.95f, 1.3f, 1.38f);
            win.transform.localScale = new Vector3(0.55f, 0.5f, 0.08f);
            Mat(win, Hex("B8E4F8"), 0.01f);
            // 窗台花箱
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "花箱";
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(0.95f, 0.95f, 1.5f);
            box.transform.localScale = new Vector3(0.7f, 0.18f, 0.22f);
            Mat(box, Hex("C48A50"), 0.006f);
            var fl = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fl.name = "窗花";
            fl.transform.SetParent(box.transform, false);
            fl.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            fl.transform.localScale = new Vector3(0.7f, 1.4f, 0.7f);
            Mat(fl, Hex("FFB3C8"), 0.004f);
            if (chimney)
            {
                var ch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ch.name = "烟囱";
                ch.transform.SetParent(root.transform, false);
                ch.transform.localPosition = new Vector3(1.0f, 2.85f, -0.4f);
                ch.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
                Mat(ch, Hex("E8E0D4"), 0.01f);
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cap.name = "烟囱帽";
                cap.transform.SetParent(ch.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                cap.transform.localScale = new Vector3(1.3f, 0.15f, 1.3f);
                Mat(cap, Hex("D8D0C4"), 0.006f);
            }
        }

        static void Stall(Vector3 pos, float yaw, Color stripeA, Color stripeB)
        {
            var root = new GameObject("摊位");
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.transform.SetParent(root.transform, false);
            table.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            table.transform.localScale = new Vector3(1.8f, 0.12f, 1.1f);
            Mat(table, Hex("C4965A"), 0.01f);
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.transform.SetParent(root.transform, false);
            leg.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            leg.transform.localScale = new Vector3(1.5f, 0.44f, 0.9f);
            Mat(leg, Hex("A07A48"), 0.008f);
            // 条纹棚顶
            for (int i = 0; i < 6; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s.transform.SetParent(root.transform, false);
                s.transform.localPosition = new Vector3(-0.75f + i * 0.3f, 1.15f, 0f);
                s.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 8f : -8f);
                s.transform.localScale = new Vector3(0.32f, 0.08f, 1.4f);
                Mat(s, i % 2 == 0 ? stripeA : stripeB, 0.006f);
            }
            var poleL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleL.transform.SetParent(root.transform, false);
            poleL.transform.localPosition = new Vector3(-0.8f, 0.8f, 0.5f);
            poleL.transform.localScale = new Vector3(0.08f, 0.8f, 0.08f);
            Mat(poleL, Hex("C4965A"), 0.006f);
            var poleR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleR.transform.SetParent(root.transform, false);
            poleR.transform.localPosition = new Vector3(0.8f, 0.8f, 0.5f);
            poleR.transform.localScale = new Vector3(0.08f, 0.8f, 0.08f);
            Mat(poleR, Hex("C4965A"), 0.006f);
            var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruit.transform.SetParent(root.transform, false);
            fruit.transform.localPosition = new Vector3(-0.3f, 0.62f, 0.1f);
            fruit.transform.localScale = Vector3.one * 0.22f;
            Mat(fruit, Hex("FF7A6A"), 0.005f);
            var fruit2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruit2.transform.SetParent(root.transform, false);
            fruit2.transform.localPosition = new Vector3(0.25f, 0.62f, 0.05f);
            fruit2.transform.localScale = Vector3.one * 0.2f;
            Mat(fruit2, Hex("FFE08A"), 0.005f);
        }

        static void NoticeBoard(Vector3 pos, float yaw)
        {
            var root = new GameObject("告示牌");
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            post.transform.localScale = new Vector3(1.3f, 1.4f, 0.12f);
            Mat(post, Hex("C4965A"), 0.01f);
            var paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.transform.SetParent(root.transform, false);
            paper.transform.localPosition = new Vector3(0f, 0.8f, 0.08f);
            paper.transform.localScale = new Vector3(1.0f, 1.0f, 0.04f);
            Mat(paper, Hex("FFF6DC"), 0.004f);
        }

        static void LampPost(Vector3 pos)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "灯柱";
            pole.transform.position = pos + new Vector3(0f, 1.15f, 0f);
            pole.transform.localScale = new Vector3(0.1f, 1.15f, 0.1f);
            Mat(pole, Hex("C4A05A"), 0.01f);
            var baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "灯座";
            baseRing.transform.position = pos + new Vector3(0f, 0.08f, 0f);
            baseRing.transform.localScale = new Vector3(0.36f, 0.08f, 0.36f);
            Mat(baseRing, Hex("C4A05A"), 0.008f);
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "灯";
            lamp.transform.position = pos + new Vector3(0f, 2.3f, 0f);
            lamp.transform.localScale = Vector3.one * 0.38f;
            Mat(lamp, Hex("FFE6A0"), 0.01f);
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.transform.position = pos + new Vector3(0f, 2.5f, 0f);
            cap.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);
            Mat(cap, Hex("C4A05A"), 0.006f);
        }

        static void ArchGate(Vector3 pos)
        {
            var root = new GameObject("石拱门");
            root.transform.position = pos;
            Box("柱L", pos + new Vector3(-2.2f, 1.5f, 0f), new Vector3(1.0f, 3.0f, 1.0f), Hex("D8D0C4"));
            Box("柱R", pos + new Vector3(2.2f, 1.5f, 0f), new Vector3(1.0f, 3.0f, 1.0f), Hex("D8D0C4"));
            Box("拱顶", pos + new Vector3(0f, 3.15f, 0f), new Vector3(5.6f, 0.7f, 1.15f), Hex("D0C8BC"));
            // 拱心用扁球体近似
            var arch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            arch.name = "拱圆";
            arch.transform.position = pos + new Vector3(0f, 2.5f, 0f);
            arch.transform.localScale = new Vector3(4.4f, 2.0f, 1.05f);
            Mat(arch, Hex("D8D0C4"), 0.016f);
            // 两侧矮石墙
            Box("石墙L", pos + new Vector3(-6.5f, 0.7f, 0f), new Vector3(7f, 1.4f, 0.55f), Hex("D0C8BC"));
            Box("石墙R", pos + new Vector3(6.5f, 0.7f, 0f), new Vector3(7f, 1.4f, 0.55f), Hex("D0C8BC"));
        }

        static void Tree(Vector3 pos, Color leaf)
        {
            var root = new GameObject("树");
            root.transform.position = pos;
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "干";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            trunk.transform.localScale = new Vector3(0.32f, 0.45f, 0.32f);
            Mat(trunk, Hex("C4A07A"), 0.014f);
            // 圆滚滚的树冠
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "叶";
            s.transform.SetParent(root.transform, false);
            s.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            s.transform.localScale = Vector3.one * 1.7f;
            Mat(s, leaf, 0.022f);
            var s2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s2.name = "叶亮";
            s2.transform.SetParent(root.transform, false);
            s2.transform.localPosition = new Vector3(-0.25f, 1.85f, 0.25f);
            s2.transform.localScale = Vector3.one * 0.7f;
            Mat(s2, Color.Lerp(leaf, Color.white, 0.22f), 0.012f);
        }

        static void Mist(Vector3 pos, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "蚀雾";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size, size * 1.6f, size);
            Mat(go, Hex("A06AD8"), 0.01f);
            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = ToonMaterialUtil.Get(new Color(0.55f, 0.3f, 0.75f, 0.55f), 0.008f, new Color(0.3f, 0.15f, 0.45f), new Color(0.35f, 0.15f, 0.5f));
            Object.Destroy(go.GetComponent<Collider>());
        }

        static void Flower(Vector3 pos, Color col)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "花";
            s.transform.position = pos + Vector3.up * 0.18f;
            s.transform.localScale = Vector3.one * 0.28f;
            Mat(s, col, 0.008f);
            var st = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            st.name = "茎";
            st.transform.SetParent(s.transform, false);
            st.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            st.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            Mat(st, Hex("6CB86A"), 0.004f);
        }

        static void Lantern(Vector3 pos)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "灯柱";
            pole.transform.position = pos + new Vector3(0f, 1.1f, 0f);
            pole.transform.localScale = new Vector3(0.12f, 1.1f, 0.12f);
            Mat(pole, Hex("D0B48A"), 0.01f);
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "灯";
            lamp.transform.position = pos + new Vector3(0f, 2.2f, 0f);
            lamp.transform.localScale = Vector3.one * 0.4f;
            Mat(lamp, Hex("FFE6A0"), 0.01f);
        }

        static void Crystal(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "蚀晶";
            go.transform.position = pos + new Vector3(0f, 0.7f, 0f);
            go.transform.localScale = new Vector3(0.45f, 0.7f, 0.45f);
            go.transform.rotation = Quaternion.Euler(8f, 20f, -10f);
            Mat(go, Hex("A06AD8"), 0.018f);
        }

        static void Mat(GameObject go, Color color, float outline)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            r.sharedMaterial = ToonMaterialUtil.Get(color, outline);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static Color Hex(string h)
        {
            return ChibiFactory.Hex(h);
        }
    }
}
