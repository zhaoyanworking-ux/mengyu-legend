using UnityEngine;

namespace ChibiARPG
{
    public enum ChibiKind
    {
        Hero,
        Swordsman,
        Mage,
        Archer,
        Imp,
        Slime,
        MiniBoss,
        NpcElder,
        NpcHerbalist,
        NpcGuard
    }

    /// <summary>
    /// 用基础几何体拼出大头小人，不依赖外部模型。
    /// </summary>
    public static class ChibiFactory
    {
        public static ChibiAnim Assemble(Transform root, ChibiKind kind)
        {
            switch (kind)
            {
                case ChibiKind.Imp:
                    return BuildColoredSlime(root, Hex("A06AD8"), Hex("E8C8FF"), Hex("4A2080"), 1f);
                case ChibiKind.Slime:
                    return BuildColoredSlime(root, Hex("7BE36A"), Hex("D8FF9A"), Hex("2A1C28"), 1f);
                case ChibiKind.MiniBoss:
                    return BuildMiniBoss(root);
                case ChibiKind.NpcElder:
                    return BuildNpc(root, Hex("F3D4B0"), Hex("E8E0F0"), Hex("6B4A9E"), Hex("F4F0E6"), false);
                case ChibiKind.NpcHerbalist:
                    return BuildNpc(root, Hex("FFD9B8"), Hex("3CB87A"), Hex("F2F7E8"), Hex("7BC86A"), true);
                case ChibiKind.NpcGuard:
                    return BuildNpc(root, Hex("E8C4A0"), Hex("4A5568"), Hex("C9A36A"), Hex("6B7280"), false);
                case ChibiKind.Mage:
                    return BuildPlayer(root, "mage");
                case ChibiKind.Archer:
                    return BuildPlayer(root, "archer");
                case ChibiKind.Swordsman:
                case ChibiKind.Hero:
                default:
                    return BuildPlayer(root, "swordsman");
            }
        }

        static ChibiAnim BuildHero(Transform root)
        {
            Color skin = Hex("FFD4B0");
            Color hair = Hex("3B7CFF");
            Color cloth = Hex("FF7A4A");
            Color clothDark = Hex("3A4BD8");
            Color shoe = Hex("5A3E36");
            Color eyeWhite = Color.white;
            Color eyeDark = Hex("2A1C28");

            var body = Capsule(root, "Body", new Vector3(0f, 0.38f, 0f), new Vector3(0.38f, 0.22f, 0.32f), cloth, 0.02f);
            var hips = Sphere(root, "Hips", new Vector3(0f, 0.22f, 0f), 0.22f, clothDark, 0.018f);

            var head = Sphere(root, "Head", new Vector3(0f, 0.92f, 0f), 0.42f, skin, 0.024f);
            Sphere(head, "Hair", new Vector3(0f, 0.16f, -0.02f), 0.44f, hair, 0.026f);
            Sphere(head, "BangL", new Vector3(-0.16f, 0.22f, 0.22f), 0.14f, hair, 0.016f);
            Sphere(head, "BangR", new Vector3(0.16f, 0.22f, 0.22f), 0.14f, hair, 0.016f);
            Sphere(head, "HairTuft", new Vector3(0.02f, 0.42f, -0.05f), 0.16f, hair, 0.016f);

            var eyeL = Sphere(head, "EyeL", new Vector3(-0.13f, 0.02f, 0.34f), 0.1f, eyeWhite, 0.008f);
            var eyeR = Sphere(head, "EyeR", new Vector3(0.13f, 0.02f, 0.34f), 0.1f, eyeWhite, 0.008f);
            Sphere(eyeL, "PupilL", new Vector3(0f, -0.01f, 0.06f), 0.055f, eyeDark, 0.004f);
            Sphere(eyeR, "PupilR", new Vector3(0f, -0.01f, 0.06f), 0.055f, eyeDark, 0.004f);
            Sphere(head, "CheekL", new Vector3(-0.22f, -0.1f, 0.3f), 0.07f, Hex("FF9AA0"), 0.004f);
            Sphere(head, "CheekR", new Vector3(0.22f, -0.1f, 0.3f), 0.07f, Hex("FF9AA0"), 0.004f);

            var armL = Limb(root, "ArmL", new Vector3(-0.28f, 0.5f, 0f), new Vector3(0.11f, 0.2f, 0.11f), skin, 0.012f);
            var armR = Limb(root, "ArmR", new Vector3(0.28f, 0.5f, 0f), new Vector3(0.11f, 0.2f, 0.11f), skin, 0.012f);
            Sphere(armL, "HandL", new Vector3(0f, -0.22f, 0f), 0.09f, skin, 0.01f);
            Sphere(armR, "HandR", new Vector3(0f, -0.22f, 0f), 0.09f, skin, 0.01f);

            var legL = Limb(root, "LegL", new Vector3(-0.11f, 0.16f, 0f), new Vector3(0.11f, 0.16f, 0.11f), clothDark, 0.012f);
            var legR = Limb(root, "LegR", new Vector3(0.11f, 0.16f, 0f), new Vector3(0.11f, 0.16f, 0.11f), clothDark, 0.012f);
            Sphere(legL, "ShoeL", new Vector3(0f, -0.18f, 0.03f), 0.1f, shoe, 0.01f);
            Sphere(legR, "ShoeR", new Vector3(0f, -0.18f, 0.03f), 0.1f, shoe, 0.01f);

            // 忽略碰撞，只作视觉
            DisableAllColliders(root);

            var anim = root.gameObject.AddComponent<ChibiAnim>();
            anim.body = body;
            anim.head = head;
            anim.armL = armL;
            anim.armR = armR;
            anim.legL = legL;
            anim.legR = legR;
            anim.CaptureRests();
            return anim;
        }

        static ChibiAnim BuildImp(Transform root)
        {
            Color skin = Hex("FF8B73");
            Color cloth = Hex("8B2E62");
            Color horn = Hex("FFE38A");
            Color eye = Hex("FFE96A");

            var body = Capsule(root, "Body", new Vector3(0f, 0.34f, 0f), new Vector3(0.36f, 0.2f, 0.3f), cloth, 0.02f);
            var head = Sphere(root, "Head", new Vector3(0f, 0.82f, 0f), 0.4f, skin, 0.024f);
            Cone(head, "HornL", new Vector3(-0.16f, 0.32f, 0f), 0.16f, 0.08f, horn, 0.01f, -18f);
            Cone(head, "HornR", new Vector3(0.16f, 0.32f, 0f), 0.16f, 0.08f, horn, 0.01f, 18f);

            var eyeL = Sphere(head, "EyeL", new Vector3(-0.13f, 0.04f, 0.32f), 0.1f, Color.white, 0.008f);
            var eyeR = Sphere(head, "EyeR", new Vector3(0.13f, 0.04f, 0.32f), 0.1f, Color.white, 0.008f);
            Sphere(eyeL, "PupilL", new Vector3(0f, -0.01f, 0.06f), 0.055f, Hex("3A1020"), 0.004f);
            Sphere(eyeR, "PupilR", new Vector3(0f, -0.01f, 0.06f), 0.055f, Hex("3A1020"), 0.004f);
            Sphere(head, "GlowL", new Vector3(-0.13f, 0.04f, 0.38f), 0.03f, eye, 0.002f);
            Sphere(head, "GlowR", new Vector3(0.13f, 0.04f, 0.38f), 0.03f, eye, 0.002f);

            var armL = Limb(root, "ArmL", new Vector3(-0.28f, 0.44f, 0f), new Vector3(0.1f, 0.18f, 0.1f), skin, 0.012f);
            var armR = Limb(root, "ArmR", new Vector3(0.28f, 0.44f, 0f), new Vector3(0.1f, 0.18f, 0.1f), skin, 0.012f);
            Sphere(armL, "HandL", new Vector3(0f, -0.2f, 0f), 0.08f, skin, 0.01f);
            Sphere(armR, "HandR", new Vector3(0f, -0.2f, 0f), 0.08f, skin, 0.01f);
            var legL = Limb(root, "LegL", new Vector3(-0.1f, 0.14f, 0f), new Vector3(0.1f, 0.14f, 0.1f), Hex("4A1A32"), 0.012f);
            var legR = Limb(root, "LegR", new Vector3(0.1f, 0.14f, 0f), new Vector3(0.1f, 0.14f, 0.1f), Hex("4A1A32"), 0.012f);
            Sphere(legL, "FootL", new Vector3(0f, -0.16f, 0.03f), 0.09f, Hex("3A1020"), 0.01f);
            Sphere(legR, "FootR", new Vector3(0f, -0.16f, 0.03f), 0.09f, Hex("3A1020"), 0.01f);

            // 小尾巴
            var tail = Limb(root, "Tail", new Vector3(0f, 0.28f, -0.16f), new Vector3(0.07f, 0.16f, 0.07f), cloth, 0.01f);
            tail.localRotation = Quaternion.Euler(55f, 0f, 0f);
            Sphere(tail, "TailTip", new Vector3(0f, -0.2f, 0f), 0.07f, Hex("FF5A6A"), 0.008f);

            DisableAllColliders(root);
            var anim = root.gameObject.AddComponent<ChibiAnim>();
            anim.body = body;
            anim.head = head;
            anim.armL = armL;
            anim.armR = armR;
            anim.legL = legL;
            anim.legR = legR;
            anim.CaptureRests();
            return anim;
        }

        static ChibiAnim BuildSlime(Transform root)
        {
            return BuildColoredSlime(root, Hex("7BE36A"), Hex("D8FF9A"), Hex("2A1C28"), 1f);
        }

        public static ChibiAnim BuildColoredSlime(Transform root, Color bodyCol, Color belly, Color iris, float scale)
        {
            var body = Sphere(root, "Body", new Vector3(0f, 0.42f * scale, 0f), 0.48f * scale, bodyCol, 0.028f * scale);
            body.localScale = new Vector3(1.15f, 0.95f, 1.1f);
            Sphere(body, "Belly", new Vector3(0f, -0.12f, 0.28f), 0.28f, belly, 0.01f);
            var eyeL = Sphere(body, "EyeL", new Vector3(-0.16f, 0.16f, 0.36f), 0.13f, Color.white, 0.008f);
            var eyeR = Sphere(body, "EyeR", new Vector3(0.16f, 0.16f, 0.36f), 0.13f, Color.white, 0.008f);
            Sphere(eyeL, "IrisL", new Vector3(0f, -0.02f, 0.06f), 0.07f, iris, 0.003f);
            Sphere(eyeR, "IrisR", new Vector3(0f, -0.02f, 0.06f), 0.07f, iris, 0.003f);
            Sphere(eyeL, "PupilL", new Vector3(0f, -0.02f, 0.09f), 0.035f, Hex("1A1220"), 0.002f);
            Sphere(eyeR, "PupilR", new Vector3(0f, -0.02f, 0.09f), 0.035f, Hex("1A1220"), 0.002f);
            Sphere(eyeL, "ShineL", new Vector3(-0.03f, 0.04f, 0.12f), 0.03f, Color.white, 0.001f);
            Sphere(eyeR, "ShineR", new Vector3(-0.03f, 0.04f, 0.12f), 0.03f, Color.white, 0.001f);
            Sphere(body, "Highlight", new Vector3(-0.18f, 0.28f, 0.22f), 0.12f, Color.white, 0.004f);

            DisableAllColliders(root);
            if (scale != 1f)
                root.localScale = Vector3.one * scale;
            var anim = root.gameObject.AddComponent<ChibiAnim>();
            anim.body = body;
            anim.head = body;
            anim.CaptureRests();
            return anim;
        }

        public static Color Hex(string hex)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out c))
                c = Color.magenta;
            return c;
        }

        static Transform Sphere(Transform parent, string name, Vector3 localPos, float radius, Color color, float outline)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * (radius * 2f);
            ApplyMat(go, color, outline);
            return go.transform;
        }

        static Transform Capsule(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, float outline)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ApplyMat(go, color, outline);
            return go.transform;
        }

        static Transform Limb(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, float outline)
        {
            return Capsule(parent, name, localPos, scale, color, outline);
        }

        static Transform Cone(Transform parent, string name, Vector3 localPos, float height, float radius, Color color, float outline, float tiltZ)
        {
            // 用拉长的胶囊近似角
            var t = Capsule(parent, name, localPos, new Vector3(radius, height * 0.5f, radius), color, outline);
            t.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
            t.localPosition = localPos;
            return t;
        }


        public static ChibiAnim AssemblePlayer(Transform root, string classId)
        {
            return BuildPlayer(root, classId);
        }

        static ChibiAnim BuildPlayer(Transform root, string classId)
        {
            string id = (classId ?? "swordsman").ToLowerInvariant();
            Color skin = Hex("FFD6B8");
            if (id != "mage" && id != "archer")
                id = "swordsman";

            // 按概念设定：战士银蓝甲+红披风、法师白紫袍+尖帽、弓手绿风帽+皮质装
            Color hair, cloth, clothDark, shoe, accent, iris;
            if (id == "mage")
            {
                hair = Hex("C4A8F0");
                cloth = Hex("F7F2FF");
                clothDark = Hex("D4C2F2");
                shoe = Hex("F5F0E8");
                accent = Hex("E8C85A");
                iris = Hex("8B5AD8");
            }
            else if (id == "archer")
            {
                hair = Hex("C8A06A");
                cloth = Hex("F3EBDA");
                clothDark = Hex("7A4A2A");
                shoe = Hex("6A3E28");
                accent = Hex("4F9A45");
                iris = Hex("3DAA4A");
            }
            else
            {
                hair = Hex("3A2418");
                cloth = Hex("8FA4BC");
                clothDark = Hex("6A7F98");
                shoe = Hex("9AACC0");
                accent = Hex("E0323A");
                iris = Hex("3A7CFF");
            }

            var body = Capsule(root, "Body", new Vector3(0f, 0.38f, 0f), new Vector3(0.38f, 0.22f, 0.32f), cloth, 0.02f);
            Sphere(root, "Hips", new Vector3(0f, 0.22f, 0f), 0.22f, clothDark, 0.018f);
            var head = Sphere(root, "Head", new Vector3(0f, 0.95f, 0f), 0.46f, skin, 0.026f);

            if (id == "archer")
            {
                // 风帽遮住大部分头发，只露刘海
                Sphere(head, "BangL", new Vector3(-0.12f, 0.08f, 0.32f), 0.1f, hair, 0.01f);
                Sphere(head, "BangR", new Vector3(0.12f, 0.08f, 0.32f), 0.1f, hair, 0.01f);
            }
            else
            {
                Sphere(head, "Hair", new Vector3(0f, 0.16f, -0.04f), 0.48f, hair, 0.026f);
                Sphere(head, "BangL", new Vector3(-0.18f, 0.18f, 0.24f), 0.15f, hair, 0.016f);
                Sphere(head, "BangR", new Vector3(0.18f, 0.18f, 0.24f), 0.15f, hair, 0.016f);
                Sphere(head, "HairTuft", new Vector3(0.02f, 0.42f, -0.06f), 0.16f, hair, 0.016f);
            }

            if (id == "mage")
                AttachMageHat(head);
            else if (id == "archer")
                AttachArcherHood(head, accent);
            else
            {
                // 战士头带 + 蓝宝石
                Cube(head, "Headband", new Vector3(0f, 0.14f, 0.06f), new Vector3(0.72f, 0.08f, 0.62f), Hex("F4F0EA"), 0.008f);
                Sphere(head, "HeadGem", new Vector3(0f, 0.16f, 0.38f), 0.07f, Hex("3A7CFF"), 0.006f);
            }

            CuteEyes(head, iris);

            Color armCol = id == "swordsman" ? cloth : skin;
            if (id == "archer") armCol = cloth;
            var armL = Limb(root, "ArmL", new Vector3(-0.28f, 0.5f, 0f), new Vector3(0.11f, 0.2f, 0.11f), armCol, 0.012f);
            var armR = Limb(root, "ArmR", new Vector3(0.28f, 0.5f, 0f), new Vector3(0.11f, 0.2f, 0.11f), armCol, 0.012f);
            var handL = Sphere(armL, "HandL", new Vector3(0f, -0.22f, 0f), 0.09f, skin, 0.01f);
            var handR = Sphere(armR, "HandR", new Vector3(0f, -0.22f, 0f), 0.09f, skin, 0.01f);

            Color legCol = id == "swordsman" ? Hex("2A2A32") : (id == "mage" ? cloth : Hex("E8D8C0"));
            var legL = Limb(root, "LegL", new Vector3(-0.11f, 0.16f, 0f), new Vector3(0.11f, 0.16f, 0.11f), legCol, 0.012f);
            var legR = Limb(root, "LegR", new Vector3(0.11f, 0.16f, 0f), new Vector3(0.11f, 0.16f, 0.11f), legCol, 0.012f);
            Sphere(legL, "ShoeL", new Vector3(0f, -0.18f, 0.03f), 0.1f, shoe, 0.01f);
            Sphere(legR, "ShoeR", new Vector3(0f, -0.18f, 0.03f), 0.1f, shoe, 0.01f);

            if (id == "swordsman")
            {
                // 胸甲宝石、红围巾、红披风、左手圆盾、右手银剑金柄
                Sphere(body, "ChestGem", new Vector3(0f, 0.15f, 0.55f), 0.07f, Hex("3A7CFF"), 0.006f);
                Cube(root, "Scarf", new Vector3(0f, 0.62f, 0.08f), new Vector3(0.42f, 0.14f, 0.36f), accent, 0.012f);
                var cape = Cube(root, "Cape", new Vector3(0f, 0.45f, -0.28f), new Vector3(0.7f, 0.85f, 0.08f), accent, 0.016f);
                cape.localRotation = Quaternion.Euler(12f, 0f, 0f);
                AttachShield(handL);
                AttachSword(handR, Hex("E8EEF8"), Hex("E8C85A"));
            }
            else if (id == "mage")
            {
                // 淡紫下摆与金星装饰
                Sphere(body, "RobeTrim", new Vector3(0f, -0.4f, 0.1f), 0.18f, clothDark, 0.01f);
                Sphere(body, "StarGem", new Vector3(0f, 0.2f, 0.55f), 0.06f, Hex("B48CFF"), 0.005f);
                AttachStaff(handR, Hex("4EC4FF"), Hex("E8C85A"));
            }
            else
            {
                // 棕色皮甲、绿披风、箭袋、木弓
                Cube(body, "Leather", new Vector3(0f, 0.05f, 0.2f), new Vector3(0.85f, 0.7f, 0.7f), clothDark, 0.012f);
                var cloak = Cube(root, "Cloak", new Vector3(0f, 0.45f, -0.22f), new Vector3(0.65f, 0.75f, 0.08f), accent, 0.016f);
                cloak.localRotation = Quaternion.Euler(10f, 0f, 0f);
                AttachQuiver(root, Hex("6A3E28"));
                AttachBow(handR, Hex("8B5A2B"), Hex("F4E8C8"));
            }

            DisableAllColliders(root);
            var anim = root.gameObject.AddComponent<ChibiAnim>();
            anim.body = body;
            anim.head = head;
            anim.armL = armL;
            anim.armR = armR;
            anim.legL = legL;
            anim.legR = legR;
            anim.CaptureRests();
            return anim;
        }

        static void CuteEyes(Transform head, Color iris)
        {
            var eyeL = Sphere(head, "EyeL", new Vector3(-0.14f, 0.04f, 0.36f), 0.115f, Color.white, 0.007f);
            var eyeR = Sphere(head, "EyeR", new Vector3(0.14f, 0.04f, 0.36f), 0.115f, Color.white, 0.007f);
            Sphere(eyeL, "IrisL", new Vector3(0f, -0.02f, 0.07f), 0.07f, iris, 0.003f);
            Sphere(eyeR, "IrisR", new Vector3(0f, -0.02f, 0.07f), 0.07f, iris, 0.003f);
            Sphere(eyeL, "PupilL", new Vector3(0f, -0.02f, 0.1f), 0.035f, Hex("1A1220"), 0.002f);
            Sphere(eyeR, "PupilR", new Vector3(0f, -0.02f, 0.1f), 0.035f, Hex("1A1220"), 0.002f);
            Sphere(eyeL, "ShineL", new Vector3(-0.03f, 0.04f, 0.12f), 0.028f, Color.white, 0.001f);
            Sphere(eyeR, "ShineR", new Vector3(-0.03f, 0.04f, 0.12f), 0.028f, Color.white, 0.001f);
            Sphere(head, "CheekL", new Vector3(-0.24f, -0.12f, 0.32f), 0.07f, Hex("FF9AA8"), 0.003f);
            Sphere(head, "CheekR", new Vector3(0.24f, -0.12f, 0.32f), 0.07f, Hex("FF9AA8"), 0.003f);
        }

        static void AttachMageHat(Transform head)
        {
            Color white = Hex("F8F5FF");
            Color band = Hex("D4C2F2");
            var brim = Sphere(head, "HatBrim", new Vector3(0f, 0.28f, -0.02f), 0.42f, white, 0.016f);
            brim.localScale = new Vector3(1.55f, 0.16f, 1.55f);
            var cone = Cone(head, "HatCone", new Vector3(0f, 0.55f, -0.05f), 0.42f, 0.22f, white, 0.018f, 8f);
            cone.localRotation = Quaternion.Euler(0f, 0f, 10f);
            Cube(head, "HatBand", new Vector3(0f, 0.34f, 0f), new Vector3(0.7f, 0.08f, 0.7f), band, 0.006f);
            Sphere(head, "HatGem", new Vector3(0f, 0.36f, 0.32f), 0.07f, Hex("5AA8FF"), 0.005f);
        }

        static void AttachArcherHood(Transform head, Color green)
        {
            var hood = Sphere(head, "Hood", new Vector3(0f, 0.20f, -0.12f), 0.5f, green, 0.022f);
            hood.localScale = new Vector3(1.15f, 1.05f, 1.0f);
            // 带着耳尖的风帽
            var earL = Cone(head, "EarL", new Vector3(-0.22f, 0.42f, -0.02f), 0.16f, 0.09f, green, 0.01f, -22f);
            var earR = Cone(head, "EarR", new Vector3(0.22f, 0.42f, -0.02f), 0.16f, 0.09f, green, 0.01f, 22f);
            Sphere(head, "LeafL", new Vector3(-0.18f, 0.28f, 0.2f), 0.07f, Hex("E8C85A"), 0.004f);
            Sphere(head, "LeafR", new Vector3(0.18f, 0.28f, 0.2f), 0.07f, Hex("E8C85A"), 0.004f);
        }

        static void AttachShield(Transform hand)
        {
            var shield = Sphere(hand, "Shield", new Vector3(-0.12f, -0.02f, 0.02f), 0.22f, Hex("4A8AFF"), 0.012f);
            shield.localScale = new Vector3(0.85f, 0.95f, 0.18f);
            Cube(shield, "Emblem", new Vector3(0f, 0.05f, 0.6f), new Vector3(0.35f, 0.4f, 0.12f), Hex("E8C85A"), 0.004f);
        }

        static void AttachQuiver(Transform root, Color leather)
        {
            var q = Cube(root, "Quiver", new Vector3(-0.22f, 0.62f, -0.22f), new Vector3(0.12f, 0.35f, 0.12f), leather, 0.01f);
            q.localRotation = Quaternion.Euler(12f, 0f, -12f);
            for (int i = 0; i < 3; i++)
            {
                var a = Cube(q, "Arrow" + i, new Vector3((i - 1) * 0.15f, 0.7f, 0f), new Vector3(0.08f, 0.7f, 0.08f), Hex("E8D8A8"), 0.003f);
            }
        }

        static ChibiAnim BuildNpc(Transform root, Color skin, Color hair, Color cloth, Color accent, bool braid)
        {
            var body = Capsule(root, "Body", new Vector3(0f, 0.38f, 0f), new Vector3(0.4f, 0.22f, 0.34f), cloth, 0.02f);
            var head = Sphere(root, "Head", new Vector3(0f, 0.9f, 0f), 0.4f, skin, 0.024f);
            Sphere(head, "Hair", new Vector3(0f, 0.14f, -0.04f), 0.42f, hair, 0.024f);
            if (braid)
                Capsule(head, "Braid", new Vector3(0.22f, -0.1f, -0.1f), new Vector3(0.08f, 0.2f, 0.08f), hair, 0.01f);
            var eyeL = Sphere(head, "EyeL", new Vector3(-0.12f, 0.02f, 0.33f), 0.09f, Color.white, 0.008f);
            var eyeR = Sphere(head, "EyeR", new Vector3(0.12f, 0.02f, 0.33f), 0.09f, Color.white, 0.008f);
            Sphere(eyeL, "PupilL", new Vector3(0f, 0f, 0.05f), 0.05f, Hex("2A1C28"), 0.004f);
            Sphere(eyeR, "PupilR", new Vector3(0f, 0f, 0.05f), 0.05f, Hex("2A1C28"), 0.004f);
            var armL = Limb(root, "ArmL", new Vector3(-0.28f, 0.5f, 0f), new Vector3(0.1f, 0.18f, 0.1f), skin, 0.012f);
            var armR = Limb(root, "ArmR", new Vector3(0.28f, 0.5f, 0f), new Vector3(0.1f, 0.18f, 0.1f), skin, 0.012f);
            var legL = Limb(root, "LegL", new Vector3(-0.1f, 0.16f, 0f), new Vector3(0.11f, 0.15f, 0.11f), accent, 0.012f);
            var legR = Limb(root, "LegR", new Vector3(0.1f, 0.16f, 0f), new Vector3(0.11f, 0.15f, 0.11f), accent, 0.012f);
            DisableAllColliders(root);
            var anim = root.gameObject.AddComponent<ChibiAnim>();
            anim.body = body;
            anim.head = head;
            anim.armL = armL;
            anim.armR = armR;
            anim.legL = legL;
            anim.legR = legR;
            anim.CaptureRests();
            return anim;
        }

        static ChibiAnim BuildMiniBoss(Transform root)
        {
            // 大型蚀雾紫史莱姆
            var anim = BuildColoredSlime(root, Hex("8A3AD0"), Hex("E0B0FF"), Hex("FFE96A"), 1.0f);
            root.localScale = Vector3.one * 2.15f;
            if (anim.head != null)
            {
                Sphere(anim.head, "CorruptBlob", new Vector3(0.15f, 0.35f, 0.05f), 0.18f, Hex("4A1A68"), 0.01f);
                Sphere(anim.head, "CorruptGlow", new Vector3(0f, 0.42f, 0f), 0.12f, Hex("C45CFF"), 0.008f);
            }
            return anim;
        }

        static void AttachSword(Transform hand, Color blade, Color hilt)
        {
            var guard = Cube(hand, "Guard", new Vector3(0f, -0.02f, 0f), new Vector3(0.18f, 0.05f, 0.06f), hilt, 0.008f);
            var bladeT = Cube(hand, "Blade", new Vector3(0f, -0.28f, 0f), new Vector3(0.07f, 0.42f, 0.03f), blade, 0.008f);
            Cube(hand, "Pommel", new Vector3(0f, 0.06f, 0f), new Vector3(0.07f, 0.07f, 0.07f), hilt, 0.006f);
            guard.localRotation = Quaternion.identity;
            bladeT.localRotation = Quaternion.identity;
        }

        static void AttachStaff(Transform hand, Color crystal, Color wood)
        {
            Cube(hand, "Shaft", new Vector3(0f, -0.18f, 0f), new Vector3(0.055f, 0.72f, 0.055f), wood, 0.008f);
            Cube(hand, "ForkL", new Vector3(-0.07f, -0.55f, 0f), new Vector3(0.04f, 0.16f, 0.04f), wood, 0.006f).localRotation = Quaternion.Euler(0f, 0f, 28f);
            Cube(hand, "ForkR", new Vector3(0.07f, -0.55f, 0f), new Vector3(0.04f, 0.16f, 0.04f), wood, 0.006f).localRotation = Quaternion.Euler(0f, 0f, -28f);
            Sphere(hand, "Orb", new Vector3(0f, -0.58f, 0f), 0.1f, crystal, 0.01f);
            Sphere(hand, "OrbGlow", new Vector3(0f, -0.58f, 0f), 0.055f, Color.white, 0.003f);
            Sphere(hand, "FloatCrystal", new Vector3(0.05f, -0.78f, 0f), 0.05f, Hex("A8E0FF"), 0.004f);
        }

        static void AttachBow(Transform hand, Color wood, Color stringCol)
        {
            var bow = Cube(hand, "BowBody", new Vector3(0.08f, -0.05f, 0f), new Vector3(0.04f, 0.5f, 0.08f), wood, 0.008f);
            bow.localRotation = Quaternion.Euler(0f, 0f, 12f);
            Cube(hand, "BowTipT", new Vector3(0.02f, 0.22f, 0f), new Vector3(0.03f, 0.14f, 0.05f), wood, 0.006f).localRotation = Quaternion.Euler(0f, 0f, 40f);
            Cube(hand, "BowTipB", new Vector3(0.02f, -0.32f, 0f), new Vector3(0.03f, 0.14f, 0.05f), wood, 0.006f).localRotation = Quaternion.Euler(0f, 0f, -40f);
            Cube(hand, "String", new Vector3(-0.02f, -0.05f, 0f), new Vector3(0.015f, 0.46f, 0.015f), stringCol, 0.004f);
        }

        static Transform Cube(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, float outline)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ApplyMat(go, color, outline);
            return go.transform;
        }

        static void ApplyMat(GameObject go, Color color, float outline)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null)
                return;
            r.sharedMaterial = ToonMaterialUtil.Get(color, outline);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void DisableAllColliders(Transform root)
        {
            var cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = false;
        }
    }
}
