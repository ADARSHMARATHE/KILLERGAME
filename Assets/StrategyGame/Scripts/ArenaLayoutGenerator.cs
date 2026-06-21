using UnityEngine;

namespace StrategyGame
{
    public class ArenaLayoutGenerator : MonoBehaviour
    {
        [SerializeField] float arenaSize = 100f;
        [SerializeField] float citadelCubeSize = 6f;
        [SerializeField] float fortressCubeSize = 3.5f;
        [SerializeField] float fortressCaptureRadius = 8f;
        [SerializeField] bool hideGameplayGridVisuals = true;

        static readonly Color GroundColor = new(0.32f, 0.34f, 0.38f);
        static readonly Color CitadelColor = new(0.85f, 0.15f, 0.15f);
        static readonly Color FortressColor = new(0.2f, 0.45f, 0.95f);
        static readonly Color CaptureZoneColor = new(0.25f, 0.55f, 0.95f, 0.28f);

        Transform arenaRoot;

        public float ArenaSize => arenaSize;
        public GameObject CentralCitadel { get; private set; }
        public GameObject[] PeripheralFortresses { get; private set; } = new GameObject[4];

        public void Generate()
        {
            arenaRoot = new GameObject("Arena").transform;
            arenaRoot.SetParent(transform, false);

            CreateGroundPlane();
            CentralCitadel = CreateCentralCitadel();
            CreatePeripheralFortresses();

            if (hideGameplayGridVisuals)
            {
                HideGridTileVisuals();
            }
        }

        void CreateGroundPlane()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.SetParent(arenaRoot, false);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(arenaSize * 0.1f, 1f, arenaSize * 0.1f);

            var renderer = ground.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateOpaqueMaterial(GroundColor);
        }

        GameObject CreateCentralCitadel()
        {
            var citadel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            citadel.name = "CentralCitadel";
            citadel.transform.SetParent(arenaRoot, false);
            citadel.transform.position = new Vector3(0f, citadelCubeSize * 0.5f, 0f);
            citadel.transform.localScale = Vector3.one * citadelCubeSize;

            citadel.GetComponent<Renderer>().sharedMaterial = CreateOpaqueMaterial(CitadelColor);
            AttachCitadelGlow(citadel.transform);
            CreateCitadelCaptureTrigger(citadel.transform);

            return citadel;
        }

        void CreateCitadelCaptureTrigger(Transform citadelTransform)
        {
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trigger.name = "CaptureRadiusTrigger";
            trigger.transform.SetParent(citadelTransform, false);
            trigger.transform.localPosition = new Vector3(0f, -citadelCubeSize * 0.25f, 0f);
            trigger.transform.localScale = new Vector3(12f, 0.08f, 12f);

            var renderer = trigger.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateTransparentMaterial(new Color(0.95f, 0.35f, 0.2f, 0.22f));

            var collider = trigger.GetComponent<CapsuleCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            var captureTrigger = trigger.AddComponent<CaptureRadiusTrigger>();
            captureTrigger.Configure("Central Citadel", CaptureZoneType.Citadel, 6f);
        }

        void CreatePeripheralFortresses()
        {
            var halfExtent = arenaSize * 0.5f;
            var cornerInset = fortressCaptureRadius + fortressCubeSize;
            var corners = new[]
            {
                new Vector3(-halfExtent + cornerInset, 0f, -halfExtent + cornerInset),
                new Vector3(halfExtent - cornerInset, 0f, -halfExtent + cornerInset),
                new Vector3(-halfExtent + cornerInset, 0f, halfExtent - cornerInset),
                new Vector3(halfExtent - cornerInset, 0f, halfExtent - cornerInset)
            };

            var names = new[]
            {
                "PeripheralFortress_NW",
                "PeripheralFortress_NE",
                "PeripheralFortress_SW",
                "PeripheralFortress_SE"
            };

            for (var i = 0; i < corners.Length; i++)
            {
                PeripheralFortresses[i] = CreatePeripheralFortress(corners[i], names[i]);
            }
        }

        GameObject CreatePeripheralFortress(Vector3 cornerPosition, string fortressName)
        {
            var fortressRoot = new GameObject(fortressName);
            fortressRoot.transform.SetParent(arenaRoot, false);
            fortressRoot.transform.position = cornerPosition;

            var fortress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fortress.name = "PeripheralFortress";
            fortress.transform.SetParent(fortressRoot.transform, false);
            fortress.transform.localPosition = new Vector3(0f, fortressCubeSize * 0.5f, 0f);
            fortress.transform.localScale = Vector3.one * fortressCubeSize;
            fortress.GetComponent<Renderer>().sharedMaterial = CreateOpaqueMaterial(FortressColor);

            CreateCaptureRadiusTrigger(fortressRoot.transform, fortressName);

            return fortressRoot;
        }

        void CreateCaptureRadiusTrigger(Transform parent, string zoneName)
        {
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trigger.name = "CaptureRadiusTrigger";
            trigger.transform.SetParent(parent, false);
            trigger.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            trigger.transform.localScale = new Vector3(fortressCaptureRadius * 2f, 0.08f, fortressCaptureRadius * 2f);

            var renderer = trigger.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateTransparentMaterial(CaptureZoneColor);

            var collider = trigger.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = trigger.AddComponent<CapsuleCollider>();
            }

            collider.isTrigger = true;

            var captureTrigger = trigger.AddComponent<CaptureRadiusTrigger>();
            captureTrigger.Configure(zoneName, CaptureZoneType.Fortress, fortressCaptureRadius);
        }

        static void AttachCitadelGlow(Transform citadelTransform)
        {
            var glowObject = new GameObject("CitadelGlow");
            glowObject.transform.SetParent(citadelTransform, false);
            glowObject.transform.localPosition = Vector3.up * 0.35f;

            var particles = glowObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.6f;
            main.startSize = 0.35f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.45f, 0.2f, 0.9f),
                new Color(1f, 0.85f, 0.35f, 0.7f));

            var emission = particles.emission;
            emission.rateOverTime = 24f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = Vector3.one * 1.2f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = 0.8f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.2f, 0.1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = glowObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (renderer.material.shader == null || renderer.material.shader.name.Contains("Hidden"))
            {
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            }

            particles.Play();
        }

        static void HideGridTileVisuals()
        {
            foreach (var tile in FindObjectsByType<GridTile>(FindObjectsInactive.Include))
            {
                var renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        static Material CreateOpaqueMaterial(Color color) => ArenaMaterials.CreateOpaque(color);

        static Material CreateTransparentMaterial(Color color) => ArenaMaterials.CreateTransparent(color);
    }
}
