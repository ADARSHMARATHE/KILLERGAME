using UnityEngine;

namespace StrategyGame
{
    public class SupplyCrate : MonoBehaviour
    {
        [SerializeField] float dropHeight = 8f;
        [SerializeField] float fallDurationSeconds = 1.1f;

        GridManager grid;
        GridCoordinates coordinates;
        bool hasLanded;
        bool collected;
        float fallTimer;
        Vector3 landingPosition;

        public static SupplyCrate CreateSupplyCratePrefabInstance()
        {
            var crateObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crateObject.name = "SupplyCrate";
            crateObject.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);

            var renderer = crateObject.GetComponent<Renderer>();
            renderer.sharedMaterial.color = new Color(0.95f, 0.65f, 0.15f);

            var collider = crateObject.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var parachute = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            parachute.name = "Parachute";
            parachute.transform.SetParent(crateObject.transform, false);
            parachute.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            parachute.transform.localScale = new Vector3(0.9f, 0.03f, 0.9f);
            parachute.GetComponent<Renderer>().sharedMaterial.color = new Color(0.9f, 0.9f, 0.95f, 0.85f);
            DestroyCollider(parachute);

            return crateObject.AddComponent<SupplyCrate>();
        }

        static void DestroyCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        public void Launch(GridManager gridManager, GridCoordinates targetCoordinates)
        {
            grid = gridManager;
            coordinates = targetCoordinates;
            landingPosition = grid.GridToWorld(targetCoordinates) + Vector3.up * 0.25f;
            transform.position = landingPosition + Vector3.up * dropHeight;
            fallTimer = 0f;
            hasLanded = false;
            collected = false;
        }

        void Update()
        {
            if (collected || grid == null)
            {
                return;
            }

            if (!hasLanded)
            {
                fallTimer += Time.deltaTime;
                var t = Mathf.Clamp01(fallTimer / fallDurationSeconds);
                transform.position = Vector3.Lerp(landingPosition + Vector3.up * dropHeight, landingPosition, t);
                if (t >= 1f)
                {
                    hasLanded = true;
                }

                return;
            }

            TryCollectNearbyUnit();
        }

        void TryCollectNearbyUnit()
        {
            if (EventGameManager.Instance == null)
            {
                return;
            }

            foreach (var unit in EventGameManager.Instance.GetActiveUnits())
            {
                if (unit == null || !unit.IsAlive || !unit.IsActiveInArena)
                {
                    continue;
                }

                if (!unit.Position.Equals(coordinates))
                {
                    continue;
                }

                Collect(unit);
                break;
            }
        }

        void Collect(Unit unit)
        {
            collected = true;

            var buffs = unit.GetComponent<UnitBuffs>();
            if (buffs == null)
            {
                buffs = unit.gameObject.AddComponent<UnitBuffs>();
            }

            var buffType = buffs.ApplyRandomSupplyBuff();
            var label = UnitBuffs.GetBuffLabel(buffType);
            var color = UnitBuffs.GetBuffColor(buffType);
            FloatingTextPopup.Show(unit.transform.position, label, color);

            Destroy(gameObject);
        }
    }
}
