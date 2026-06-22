using System.Collections.Generic;
using UnityEngine;

namespace KillerGame
{
    /// <summary>
    /// Swaps City3D primitive cubes for imported Quaternius medieval FBX models (CC0).
    /// </summary>
    public static class CityModelReplacer
    {
        static readonly Dictionary<string, string> SlotToModel = new Dictionary<string, string>
        {
            { "Tower_N", "Bell_Tower" },
            { "Tower_L", "Bell_Tower" },
            { "Tower_W", "Bell_Tower" },
            { "Tower_R", "Bell_Tower" },
            { "Tower_E", "Bell_Tower" },
            { "House_L1", "House_1" },
            { "House_R1", "House_2" },
            { "House_L2", "House_3" },
            { "House_R2", "House_4" },
            { "House_L3", "Stable" },
            { "Workshop", "Blacksmith" },
            { "BG_L1", "Mill" },
            { "BG_L2", "Inn" },
            { "BG_C", "Mill" },
            { "BG_R1", "Sawmill" },
            { "BG_R2", "Mill" },
        };

        static readonly Dictionary<string, Vector3> SlotTargetSize = new Dictionary<string, Vector3>
        {
            { "Keep", new Vector3(14f, 20f, 14f) },
            { "Tower_N", new Vector3(5f, 12f, 5f) },
            { "Tower_L", new Vector3(5f, 12f, 5f) },
            { "Tower_W", new Vector3(5f, 12f, 5f) },
            { "Tower_R", new Vector3(5f, 12f, 5f) },
            { "Tower_E", new Vector3(5f, 12f, 5f) },
            { "House_L1", new Vector3(6f, 5f, 6f) },
            { "House_R1", new Vector3(6f, 5f, 6f) },
            { "House_L2", new Vector3(6f, 5f, 6f) },
            { "House_R2", new Vector3(6f, 5f, 6f) },
            { "House_L3", new Vector3(7f, 5f, 7f) },
            { "Workshop", new Vector3(8f, 6f, 8f) },
            { "BG_L1", new Vector3(7f, 5f, 7f) },
            { "BG_L2", new Vector3(8f, 6f, 8f) },
            { "BG_C", new Vector3(6f, 4f, 6f) },
            { "BG_R1", new Vector3(8f, 6f, 8f) },
            { "BG_R2", new Vector3(7f, 5f, 7f) },
        };

        static readonly HashSet<string> HideSlots = new HashSet<string>
        {
            "Roof_L1", "Roof_R1", "Roof_Keep", "Spire",
            "Win_Keep1", "Win_Keep2", "Win_Keep3", "CastleGlow",
            "Box", "Fill", "AccentBorder", "FuelBarBG", "FireGlow2",
            "Win_L1", "Win_R1",
        };

        const string ModelRoot = "Models/MedievalVillage/";
        const float GroundY = 0f;

        public static void Replace(GameObject city, Material snowMat, Material stoneMat, Material windowMat)
        {
            if (city == null) return;

            var toProcess = new List<Transform>();
            foreach (Transform slot in city.transform)
                toProcess.Add(slot);

            foreach (var slot in toProcess)
            {
                var name = slot.name;
                if (HideSlots.Contains(name))
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                if (name == "Keep")
                {
                    HidePrimitive(slot.gameObject);
                    var existingKeep = slot.Find("Keep_Model");
                    if (existingKeep != null) Object.Destroy(existingKeep.gameObject);
                    slot.localScale = Vector3.one;
                    slot.localRotation = Quaternion.identity;
                    slot.localPosition = new Vector3(slot.localPosition.x, GroundY, slot.localPosition.z);
                    var darkMat = Resources.Load<Material>("Materials/Mat_Stone_Dark") ?? stoneMat;
                    BuildWOSFurnaceTower(slot, stoneMat, darkMat, windowMat, snowMat);
                    continue;
                }

                if (!SlotToModel.TryGetValue(name, out var modelName))
                    continue;

                var src = slot.GetComponent<Renderer>();
                if (src == null) continue;

                var originalScale = slot.localScale;
                var targetSize = ResolveTargetSize(name, slot, originalScale);

                var prefab = Resources.Load<GameObject>(ModelRoot + modelName);
                if (prefab == null)
                {
                    Debug.LogWarning($"CityModelReplacer: missing Resources/{ModelRoot}{modelName}");
                    continue;
                }

                var anchor = new Vector3(slot.position.x, GroundY, slot.position.z);

                HidePrimitive(slot.gameObject);

                var existing = slot.Find(name + "_Model");
                if (existing != null)
                    Object.Destroy(existing.gameObject);

                slot.localScale = Vector3.one;
                slot.localRotation = Quaternion.identity;
                slot.localPosition = new Vector3(slot.localPosition.x, GroundY, slot.localPosition.z);

                var model = Object.Instantiate(prefab, slot);
                model.name = name + "_Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                FitModel(model, targetSize, anchor);
                ApplyMaterials(model, name, snowMat, stoneMat, windowMat);
            }

            PlaceProp(city.transform, "Bonfire_Lit", new Vector3(0f, 0f, 2f), 8f);
            PlaceEdgeTrees(city.transform, snowMat, stoneMat);
            HideLeftoverPrimitives(city.transform);
        }

        static readonly (Vector3 pos, float height)[] EdgeTreeSpots =
        {
            (new Vector3(-32f, 0f, -14f), 9f),
            (new Vector3(32f, 0f, -14f), 9f),
            (new Vector3(-32f, 0f, 4f), 8f),
            (new Vector3(32f, 0f, 4f), 8f),
            (new Vector3(-22f, 0f, 10f), 7f),
            (new Vector3(22f, 0f, 10f), 7f),
            (new Vector3(0f, 0f, -18f), 8.5f),
        };

        static void PlaceEdgeTrees(Transform city, Material snowMat, Material trunkMat)
        {
            var root = city.Find("EdgeTrees");
            if (root == null)
            {
                var go = new GameObject("EdgeTrees");
                go.transform.SetParent(city, false);
                root = go.transform;
            }

            for (int i = 0; i < EdgeTreeSpots.Length; i++)
            {
                var spot = EdgeTreeSpots[i];
                var name = $"PineTree_{i}";
                if (root.Find(name) != null) continue;
                CreatePineTree(root, name, spot.pos, spot.height, snowMat, trunkMat);
            }
        }

        static void CreatePineTree(Transform parent, string name, Vector3 localPos, float height, Material foliageMat, Material trunkMat)
        {
            var tree = new GameObject(name);
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = localPos;

            float trunkH = height * 0.35f;
            float foliageH = height * 0.65f;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localScale = new Vector3(0.6f, trunkH, 0.6f);
            trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
            if (trunkMat != null) trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            Object.Destroy(trunk.GetComponent<Collider>());

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foliage.name = "Foliage";
            foliage.transform.SetParent(tree.transform, false);
            foliage.transform.localScale = new Vector3(2.2f, foliageH, 2.2f);
            foliage.transform.localPosition = new Vector3(0f, trunkH + foliageH * 0.45f, 0f);
            if (foliageMat != null) foliage.GetComponent<Renderer>().sharedMaterial = foliageMat;
            Object.Destroy(foliage.GetComponent<Collider>());
        }

        static void BuildWOSFurnaceTower(Transform slot, Material stoneMat, Material darkMat, Material glowMat, Material snowMat)
        {
            var tower = new GameObject("Keep_Model");
            tower.transform.SetParent(slot, false);
            tower.transform.localPosition = Vector3.zero;

            Material stone = stoneMat;
            Material dark = darkMat ?? stoneMat;
            Material glow = MakeForgeGlowMaterial(glowMat);
            Material snow = snowMat ?? stoneMat;

            // Wide stone foundation + stepped tiers (WOS industrial base)
            AddPart(tower.transform, "Foundation", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, 0f), new Vector3(14f, 1.1f, 14f), dark);
            AddPart(tower.transform, "Tier1", PrimitiveType.Cube,
                new Vector3(0f, 2f, 0f), new Vector3(12f, 1.8f, 12f), stone);
            AddPart(tower.transform, "Tier2", PrimitiveType.Cube,
                new Vector3(0f, 4.2f, 0f), new Vector3(10.5f, 2.2f, 10.5f), stone);

            // Main furnace chamber block
            AddPart(tower.transform, "Chamber", PrimitiveType.Cube,
                new Vector3(0f, 8.5f, 0f), new Vector3(9.5f, 5.5f, 9.5f), dark);
            AddPart(tower.transform, "UpperHousing", PrimitiveType.Cube,
                new Vector3(0f, 13.5f, 0f), new Vector3(7.5f, 3f, 7.5f), stone);

            // Tall chimney / smokestack
            AddPart(tower.transform, "Chimney", PrimitiveType.Cube,
                new Vector3(0f, 17.5f, 0f), new Vector3(4.2f, 5.5f, 4.2f), dark);
            AddPart(tower.transform, "ChimneyRim", PrimitiveType.Cube,
                new Vector3(0f, 20.8f, 0f), new Vector3(5f, 0.6f, 5f), dark);
            AddPart(tower.transform, "SnowCap", PrimitiveType.Cube,
                new Vector3(0f, 21.6f, 0f), new Vector3(5.5f, 0.9f, 5.5f), snow);

            // Metal bands around chamber
            AddPart(tower.transform, "BandLower", PrimitiveType.Cube,
                new Vector3(0f, 6.2f, 0f), new Vector3(10.6f, 0.45f, 10.6f), dark);
            AddPart(tower.transform, "BandMid", PrimitiveType.Cube,
                new Vector3(0f, 10.8f, 0f), new Vector3(9.7f, 0.4f, 9.7f), dark);

            // Front forge mouth — faces +Z toward camera
            AddPart(tower.transform, "ForgeFrame", PrimitiveType.Cube,
                new Vector3(0f, 4.8f, 5.15f), new Vector3(4.6f, 4f, 0.5f), dark);
            AddPart(tower.transform, "ForgeMouth", PrimitiveType.Cube,
                new Vector3(0f, 4.8f, 5.35f), new Vector3(3.6f, 3.2f, 0.35f), glow);
            AddPart(tower.transform, "ForgeCore", PrimitiveType.Cube,
                new Vector3(0f, 4.8f, 5.5f), new Vector3(2.6f, 2.4f, 0.2f), glow);

            // Glowing vent slits around chamber mid-line
            const float ventY = 9.2f;
            const float ventHalf = 4.85f;
            AddPart(tower.transform, "VentFront", PrimitiveType.Cube,
                new Vector3(0f, ventY, ventHalf), new Vector3(5.5f, 0.55f, 0.18f), glow);
            AddPart(tower.transform, "VentBack", PrimitiveType.Cube,
                new Vector3(0f, ventY, -ventHalf), new Vector3(5.5f, 0.55f, 0.18f), glow);
            AddPart(tower.transform, "VentLeft", PrimitiveType.Cube,
                new Vector3(-ventHalf, ventY, 0f), new Vector3(0.18f, 0.55f, 5.5f), glow);
            AddPart(tower.transform, "VentRight", PrimitiveType.Cube,
                new Vector3(ventHalf, ventY, 0f), new Vector3(0.18f, 0.55f, 5.5f), glow);

            // Side exhaust pipes
            AddPart(tower.transform, "PipeL", PrimitiveType.Cylinder,
                new Vector3(-5.8f, 11f, 1.5f), new Vector3(1.4f, 3.5f, 1.4f), dark);
            AddPart(tower.transform, "PipeR", PrimitiveType.Cylinder,
                new Vector3(5.8f, 11f, 1.5f), new Vector3(1.4f, 3.5f, 1.4f), dark);

            // Front coal platform / ramp
            AddPart(tower.transform, "CoalPlatform", PrimitiveType.Cube,
                new Vector3(0f, 0.25f, 6.5f), new Vector3(5.5f, 0.45f, 2.5f), stone);
            AddPart(tower.transform, "CoalPile", PrimitiveType.Cube,
                new Vector3(0f, 0.75f, 6.8f), new Vector3(3f, 0.8f, 1.8f), dark);

            AddFurnaceLight(tower.transform, "ForgeLight", new Vector3(0f, 4.8f, 7f), 45f, 22f);
            AddFurnaceLight(tower.transform, "ChamberLight", new Vector3(0f, 9f, 0f), 22f, 28f);
            AddFurnaceLight(tower.transform, "ChimneyLight", new Vector3(0f, 18f, 0f), 12f, 14f);
        }

        static Material MakeForgeGlowMaterial(Material source)
        {
            if (source == null) return null;
            var m = new Material(source);
            m.SetColor("_BaseColor", new Color(0.95f, 0.38f, 0.06f));
            m.SetColor("_EmissionColor", new Color(12f, 4.5f, 0.5f));
            return m;
        }

        static void AddFurnaceLight(Transform parent, string name, Vector3 localPos, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.48f, 0.08f);
            l.intensity = intensity;
            l.range = range;
        }

        static void AddPart(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(go.GetComponent<Collider>());
        }

        static readonly HashSet<string> KeepCityObjects = new HashSet<string>
        {
            "SnowGround", "SnowLayer", "Bonfire_Lit",
        };

        static void HideLeftoverPrimitives(Transform city)
        {
            foreach (Transform child in city)
            {
                var n = child.name;
                if (KeepCityObjects.Contains(n) || SlotToModel.ContainsKey(n) || HideSlots.Contains(n))
                    continue;
                if (n.EndsWith("_Model") || child.Find(n + "_Model") != null)
                    continue;
                child.gameObject.SetActive(false);
            }
        }

        static Vector3 ResolveTargetSize(string slotName, Transform slot, Vector3 originalScale)
        {
            if (SlotTargetSize.TryGetValue(slotName, out var preset))
                return preset;

            var meshFilter = slot.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                return Vector3.Scale(meshFilter.sharedMesh.bounds.size, originalScale);

            var renderer = slot.GetComponent<Renderer>();
            if (renderer != null)
                return renderer.bounds.size;

            return new Vector3(6f, 5f, 6f);
        }

        static void HidePrimitive(GameObject go)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        static void FitModel(GameObject model, Vector3 targetSize, Vector3 anchor)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float targetH = Mathf.Max(targetSize.y, 2f);
            float targetW = Mathf.Max(targetSize.x, targetSize.z, 2f);
            float modelH = Mathf.Max(bounds.size.y, 0.001f);
            float modelW = Mathf.Max(bounds.size.x, bounds.size.z, 0.001f);

            float scale = targetH / modelH;
            scale = Mathf.Clamp(scale, 1f, 800f);
            model.transform.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var delta = new Vector3(anchor.x - bounds.center.x, GroundY - bounds.min.y, anchor.z - bounds.center.z);
            model.transform.position += delta;
        }

        static void ApplyMaterials(GameObject model, string slotName, Material snowMat, Material stoneMat, Material windowMat)
        {
            bool isKeep = slotName == "Keep";
            bool isTower = slotName.StartsWith("Tower");
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var n = r.gameObject.name.ToLowerInvariant();
                if (IsRoofMesh(n) && snowMat != null)
                    r.sharedMaterial = snowMat;
                else if (n.Contains("window") || n.Contains("glass"))
                {
                    if (windowMat != null) r.sharedMaterial = windowMat;
                }
                else if (isKeep && windowMat != null && (n.Contains("bell") || n.Contains("forge") || n.Contains("fire") || n.Contains("glow") || n.Contains("door")))
                    r.sharedMaterial = windowMat;
                else if (isTower && stoneMat != null && !IsRoofMesh(n))
                    r.sharedMaterial = stoneMat;
            }
        }

        static bool IsRoofMesh(string meshName)
        {
            return meshName.Contains("roof") || meshName.Contains("shingle")
                || meshName.Contains("thatch") || meshName.Contains("_top")
                || meshName.EndsWith("top") || meshName.Contains("roof_");
        }

        static void PlaceProp(Transform parent, string modelName, Vector3 localPos, float targetHeight)
        {
            if (parent.Find(modelName) != null) return;
            var prefab = Resources.Load<GameObject>(ModelRoot + modelName);
            if (prefab == null) return;
            var go = Object.Instantiate(prefab, parent);
            go.name = modelName;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            float modelH = Mathf.Max(bounds.size.y, 0.01f);
            float scale = Mathf.Clamp(targetHeight / modelH, 1f, 800f);
            go.transform.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            go.transform.position += new Vector3(0f, GroundY - bounds.min.y, 0f);
        }
    }
}
