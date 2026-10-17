using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Runtime pedestrians and traffic for the island city. All routes are cached from the road grid;
/// the actors use simple transforms, so the scene does not need a NavMesh.
/// </summary>
[DisallowMultipleComponent]
public sealed class IslandCityLife : MonoBehaviour
{
    public enum CityLifeMode
    {
        Normal,
        Assembly,
        Shelter
    }

    [Header("Traffic prefabs")]
    public GameObject[] vehiclePrefabs;

    [Header("Runtime population")]
    [Min(1)] public int citizenCount = 48;
    [Range(0, 12)] public int movingVehicleCount = 12;

    [Header("Scene references")]
    public DisasterController disasterController;
    public Bounds landBounds;
    public BuildingVisuals[] houses;
    public Transform[] gatheringAreas;

    private const int XNodeCount = 5;
    private const int ZNodeCount = 4;
    private const int RoadNodeCount = XNodeCount * ZNodeCount;
    private const float SidewalkOffset = 3.3f;
    private const float LaneOffset = 1.1f;
    private const int CitizensPerVehicleRoute = 3;
    private const float RoadSurfaceClearance = .34f;

    private static readonly float[] RoadX = { -60f, -30f, 0f, 30f, 60f };
    private static readonly float[] RoadZ = { -42f, -14f, 14f, 40f };
    private static readonly Color[] ShirtColors =
    {
        new Color(.18f, .48f, .76f), new Color(.78f, .29f, .22f), new Color(.94f, .62f, .16f),
        new Color(.25f, .59f, .38f), new Color(.60f, .36f, .67f), new Color(.88f, .72f, .48f),
        new Color(.22f, .62f, .66f), new Color(.78f, .43f, .30f)
    };
    private static readonly Color[] SkinColors =
    {
        new Color(.91f, .72f, .56f), new Color(.70f, .47f, .32f), new Color(.96f, .81f, .68f),
        new Color(.48f, .31f, .22f)
    };

    private sealed class CitizenActor
    {
        public GameObject gameObject;
        public Transform root;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public readonly List<int> path = new List<int>(RoadNodeCount);
        public int pathIndex;
        public int currentNode;
        public int targetNode;
        public float speed;
        public float emergencySpeed;
        public float gaitPhase;
        public float waitRemaining;
        public bool hasInitialLeg;
        public Vector3 initialLegTarget;
        public bool hasWorldGoal;
        public Vector3 worldGoal;
        public bool resumeAtNode;
        public CityLifeMode mode;
        public bool reachedGoal;
        public BuildingVisuals shelterHouse;
    }

    private sealed class VehicleRoute
    {
        public Vector3[] points;
        public float[] segmentLengths;
        public float totalLength;
    }

    private sealed class VehicleActor
    {
        public Transform root;
        public VehicleRoute route;
        public float distance;
        public float speed;
    }

    private sealed class CitizenMaterialBatch
    {
        public Material material;
        public Mesh mesh;
        public readonly List<Transform> parts = new List<Transform>(48);
        public readonly List<MeshRenderer> renderers = new List<MeshRenderer>(48);
        public Matrix4x4[] matrices;
        public RenderParams renderParams;
    }

    private readonly List<CitizenActor> citizens = new List<CitizenActor>(48);
    private readonly List<VehicleActor> vehicles = new List<VehicleActor>(12);
    private readonly List<CitizenMaterialBatch> citizenBatches = new List<CitizenMaterialBatch>(16);
    private readonly Dictionary<Material, CitizenMaterialBatch> citizenBatchesByMaterial = new Dictionary<Material, CitizenMaterialBatch>(16);
    private Vector3[] roadNodePositions;
    private int[,] roadNeighbors;
    private int[] roadNeighborCounts;
    private int[] bfsParents;
    private int[] bfsQueue;
    private int[] reversePath;
    private VehicleRoute[] vehicleRoutes;
    private Transform cityLifeRoot;
    private GameManager gameManager;
    private bool runtimeBuilt;
    private bool incidentActive;
    private bool instancingSupported;
    private bool instanceRenderingEnabled;
    private AfetTuru incidentType;
    private int shelteredCount;
    private int gatheredCount;

    public int CitizenCount { get { return citizens.Count; } }
    public int VehicleCount { get { return vehicles.Count; } }
    public int ShelteredCount { get { return shelteredCount; } }
    public int GatheredCount { get { return gatheredCount; } }
    public int InstancedPartCount { get; private set; }
    public int InstancedBatchCount { get; private set; }
    public CityLifeMode CurrentMode { get; private set; } = CityLifeMode.Normal;

    private void Awake()
    {
        gameManager = GameManager.Instance;
    }

    private void Start()
    {
        if (!runtimeBuilt) BuildRuntimeCityLife();
    }

    /// <summary>Wires the active disaster, the walkable land bounds, houses, and safe assembly areas.</summary>
    public void Configure(DisasterController controller, Bounds land, BuildingVisuals[] houseBuildings, Transform[] areas)
    {
        disasterController = controller;
        landBounds = land;
        houses = houseBuildings ?? new BuildingVisuals[0];
        gatheringAreas = areas ?? new Transform[0];
        if (Application.isPlaying)
        {
            gameManager = GameManager.Instance;
            if (runtimeBuilt)
            {
                ClearRuntimeActors();
                runtimeBuilt = false;
            }
            BuildRuntimeCityLife();
        }
    }

    private void BuildRuntimeCityLife()
    {
        if (!Application.isPlaying || runtimeBuilt) return;
        ClearRuntimeActors();
        BuildRoadGraph();

        GameObject rootObject = new GameObject("SehirYasami");
        cityLifeRoot = rootObject.transform;
        cityLifeRoot.SetParent(transform, false);
        SpawnCitizens();
        BuildCitizenMaterialBatches();
        BuildVehicleRoutes();
        SpawnVehicles();
        runtimeBuilt = true;
    }

    private void ClearRuntimeActors()
    {
        SetCitizenRenderersEnabled(true);
        citizenBatches.Clear();
        citizenBatchesByMaterial.Clear();
        InstancedPartCount = 0;
        InstancedBatchCount = 0;
        instanceRenderingEnabled = false;
        IslandEffectsFactory.ClearCitizenRuntimeCache();
        citizens.Clear();
        vehicles.Clear();
        if (cityLifeRoot != null)
        {
            cityLifeRoot.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(cityLifeRoot.gameObject);
            else DestroyImmediate(cityLifeRoot.gameObject);
            cityLifeRoot = null;
        }
    }

    private void BuildCitizenMaterialBatches()
    {
        instancingSupported = SystemInfo.supportsInstancing;
        if (!instancingSupported) return;

        for (int i = 0; i < citizens.Count; i++)
        {
            GameObject model = citizens[i].gameObject;
            if (model == null) continue;
            MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                MeshRenderer renderer = renderers[r];
                if (renderer == null || renderer.sharedMaterial == null) continue;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                Material material = renderer.sharedMaterial;
                CitizenMaterialBatch batch;
                if (!citizenBatchesByMaterial.TryGetValue(material, out batch))
                {
                    batch = new CitizenMaterialBatch
                    {
                        material = material,
                        mesh = filter.sharedMesh,
                        renderParams = new RenderParams(material)
                    };
                    batch.renderParams.layer = renderer.gameObject.layer;
                    batch.renderParams.shadowCastingMode = renderer.shadowCastingMode;
                    batch.renderParams.receiveShadows = renderer.receiveShadows;
                    batch.renderParams.lightProbeUsage = renderer.lightProbeUsage;
                    batch.renderParams.reflectionProbeUsage = renderer.reflectionProbeUsage;
                    batch.renderParams.renderingLayerMask = renderer.renderingLayerMask;
                    citizenBatchesByMaterial.Add(material, batch);
                    citizenBatches.Add(batch);
                }

                batch.parts.Add(renderer.transform);
                batch.renderers.Add(renderer);
            }
        }

        bool valid = citizenBatches.Count > 0;
        for (int i = 0; i < citizenBatches.Count; i++)
        {
            CitizenMaterialBatch batch = citizenBatches[i];
            if (batch.mesh == null || batch.material == null || !batch.material.enableInstancing)
            {
                valid = false;
                break;
            }
            batch.matrices = new Matrix4x4[batch.parts.Count];
        }

        if (!valid)
        {
            citizenBatches.Clear();
            citizenBatchesByMaterial.Clear();
            return;
        }

        instanceRenderingEnabled = true;
        SetCitizenRenderersEnabled(false);
    }

    private void SetCitizenRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < citizenBatches.Count; i++)
        {
            List<MeshRenderer> renderers = citizenBatches[i].renderers;
            for (int r = 0; r < renderers.Count; r++)
            {
                MeshRenderer renderer = renderers[r];
                if (renderer != null) renderer.enabled = enabled;
            }
        }
    }

    private void OnEnable()
    {
        if (runtimeBuilt && instancingSupported && citizenBatches.Count > 0)
        {
            instanceRenderingEnabled = true;
            SetCitizenRenderersEnabled(false);
        }
    }

    private void OnDisable()
    {
        instanceRenderingEnabled = false;
        SetCitizenRenderersEnabled(true);
        InstancedPartCount = 0;
        InstancedBatchCount = 0;
    }

    private void LateUpdate()
    {
        if (!instanceRenderingEnabled || !instancingSupported) return;

        int submittedParts = 0;
        int submittedBatches = 0;
        for (int b = 0; b < citizenBatches.Count; b++)
        {
            CitizenMaterialBatch batch = citizenBatches[b];
            int count = 0;
            bool hasBounds = false;
            Bounds bounds = default;
            Bounds meshBounds = batch.mesh.bounds;
            Vector3 localExtents = meshBounds.extents;

            for (int i = 0; i < batch.parts.Count; i++)
            {
                Transform part = batch.parts[i];
                if (part == null || !part.gameObject.activeInHierarchy) continue;
                Matrix4x4 matrix = part.localToWorldMatrix;
                batch.matrices[count++] = matrix;

                Vector3 center = matrix.MultiplyPoint3x4(meshBounds.center);
                Vector3 extents = new Vector3(
                    Mathf.Abs(matrix.m00) * localExtents.x + Mathf.Abs(matrix.m01) * localExtents.y + Mathf.Abs(matrix.m02) * localExtents.z,
                    Mathf.Abs(matrix.m10) * localExtents.x + Mathf.Abs(matrix.m11) * localExtents.y + Mathf.Abs(matrix.m12) * localExtents.z,
                    Mathf.Abs(matrix.m20) * localExtents.x + Mathf.Abs(matrix.m21) * localExtents.y + Mathf.Abs(matrix.m22) * localExtents.z);
                Bounds partBounds = new Bounds(center, extents * 2f);
                if (hasBounds) bounds.Encapsulate(partBounds);
                else { bounds = partBounds; hasBounds = true; }
            }

            if (count == 0 || !hasBounds) continue;
            batch.renderParams.worldBounds = bounds;
            for (int first = 0; first < count; first += 1023)
            {
                int instanceCount = Mathf.Min(1023, count - first);
                Graphics.RenderMeshInstanced(in batch.renderParams, batch.mesh, 0, batch.matrices, instanceCount, first);
                submittedBatches++;
            }
            submittedParts += count;
        }

        InstancedPartCount = submittedParts;
        InstancedBatchCount = submittedBatches;
    }

    private void BuildRoadGraph()
    {
        float groundY = RoadSurfaceY;
        roadNodePositions = new Vector3[RoadNodeCount];
        roadNeighbors = new int[RoadNodeCount, 4];
        roadNeighborCounts = new int[RoadNodeCount];
        bfsParents = new int[RoadNodeCount];
        bfsQueue = new int[RoadNodeCount];
        reversePath = new int[RoadNodeCount];

        for (int x = 0; x < XNodeCount; x++)
        {
            for (int z = 0; z < ZNodeCount; z++)
            {
                int node = NodeIndex(x, z);
                roadNodePositions[node] = IslandPoint(RoadX[x] + SidewalkOffset, RoadZ[z] + SidewalkOffset);
                roadNodePositions[node].y = groundY;

                if (x + 1 < XNodeCount) ConnectNodes(node, NodeIndex(x + 1, z));
                if (z + 1 < ZNodeCount && !(x == 2 && z == 1))
                    ConnectNodes(node, NodeIndex(x, z + 1));
            }
        }
    }

    private static int NodeIndex(int x, int z)
    {
        return x * ZNodeCount + z;
    }

    private void ConnectNodes(int a, int b)
    {
        roadNeighbors[a, roadNeighborCounts[a]++] = b;
        roadNeighbors[b, roadNeighborCounts[b]++] = a;
    }

    private void SpawnCitizens()
    {
        int count = Mathf.Max(1, citizenCount);
        for (int i = 0; i < count; i++)
        {
            int startNode = (i * 7) % RoadNodeCount;
            Color shirt = ShirtColors[Random.Range(0, ShirtColors.Length)];
            Color skin = SkinColors[Random.Range(0, SkinColors.Length)];
            GameObject model = IslandEffectsFactory.CreateCitizen(cityLifeRoot, shirt, skin);
            model.name = "Yaya_" + (i + 1).ToString("00");
            Transform root = model.transform;
            root.position = roadNodePositions[startNode];

            CitizenActor actor = new CitizenActor
            {
                gameObject = model,
                root = root,
                leftArm = root.Find("SolKol"),
                rightArm = root.Find("SagKol"),
                leftLeg = root.Find("SolBacak"),
                rightLeg = root.Find("SagBacak"),
                currentNode = startNode,
                targetNode = startNode,
                speed = Random.Range(1.5f, 2.41f),
                emergencySpeed = Random.Range(10.5f, 12f),
                gaitPhase = Random.Range(0f, Mathf.PI * 2f),
                mode = CityLifeMode.Normal
            };
            citizens.Add(actor);
            ChooseWanderRoute(actor);
        }
    }

    private void BuildVehicleRoutes()
    {
        Vector3[] outer =
        {
            RoadPoint(-60f, -42f), RoadPoint(60f, -42f),
            RoadPoint(60f, 40f), RoadPoint(-60f, 40f)
        };
        Vector3[] inner =
        {
            RoadPoint(-30f, -14f), RoadPoint(30f, -14f),
            RoadPoint(30f, 14f), RoadPoint(-30f, 14f)
        };

        vehicleRoutes = new[]
        {
            CreateVehicleRoute(outer, LaneOffset),
            CreateVehicleRoute(ReverseCorners(outer), LaneOffset),
            CreateVehicleRoute(inner, LaneOffset),
            CreateVehicleRoute(ReverseCorners(inner), LaneOffset)
        };
    }

    private Vector3 RoadPoint(float x, float z)
    {
        Vector3 point = IslandPoint(x, z);
        point.y = RoadSurfaceY;
        return point;
    }

    private Vector3 IslandPoint(float x, float z)
    {
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }

    private float RoadSurfaceY { get { return landBounds.max.y + RoadSurfaceClearance; } }

    private static Vector3[] ReverseCorners(Vector3[] corners)
    {
        Vector3[] reversed = new Vector3[corners.Length];
        for (int i = 0; i < corners.Length; i++) reversed[i] = corners[corners.Length - 1 - i];
        return reversed;
    }

    private static VehicleRoute CreateVehicleRoute(Vector3[] corners, float laneOffset)
    {
        Vector3[] lanePoints = new Vector3[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 previous = corners[(i + corners.Length - 1) % corners.Length];
            Vector3 current = corners[i];
            Vector3 next = corners[(i + 1) % corners.Length];
            Vector3 incoming = (current - previous).normalized;
            Vector3 outgoing = (next - current).normalized;
            Vector3 incomingSide = new Vector3(-incoming.z, 0f, incoming.x);
            Vector3 outgoingSide = new Vector3(-outgoing.z, 0f, outgoing.x);
            Vector3 miter = (incomingSide + outgoingSide).normalized;
            float denominator = Mathf.Max(.25f, Vector3.Dot(miter, outgoingSide));
            lanePoints[i] = current + miter * (laneOffset / denominator);
        }

        float[] lengths = new float[lanePoints.Length];
        float total = 0f;
        for (int i = 0; i < lanePoints.Length; i++)
        {
            lengths[i] = Vector3.Distance(lanePoints[i], lanePoints[(i + 1) % lanePoints.Length]);
            total += lengths[i];
        }
        return new VehicleRoute { points = lanePoints, segmentLengths = lengths, totalLength = total };
    }

    private void SpawnVehicles()
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0 || movingVehicleCount <= 0) return;
        int count = Mathf.Min(12, movingVehicleCount);
        for (int i = 0; i < count; i++)
        {
            int routeIndex = i % vehicleRoutes.Length;
            int onRoute = i / vehicleRoutes.Length;
            VehicleRoute route = vehicleRoutes[routeIndex];
            float distance = route.totalLength * onRoute / CitizensPerVehicleRoute;
            Vector3 position, direction;
            EvaluateRoute(route, distance, out position, out direction);

            GameObject prefab = vehiclePrefabs[i % vehiclePrefabs.Length];
            if (prefab == null) continue;
            GameObject car = Instantiate(prefab, cityLifeRoot, false);
            car.name = "AdaAraci_" + (i + 1).ToString("00");
            car.transform.position = position;
            car.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            RemoveVehicleColliders(car);
            NormalizeVehicle(car, direction);
            PlaceVehicleOnGround(car.transform);

            vehicles.Add(new VehicleActor
            {
                root = car.transform,
                route = route,
                distance = distance,
                speed = Random.Range(4.2f, 5.6f)
            });
        }
    }

    private static void RemoveVehicleColliders(GameObject vehicle)
    {
        Collider[] colliders = vehicle.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) UnityEngine.Object.Destroy(colliders[i]);
        Collider2D[] colliders2D = vehicle.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++) UnityEngine.Object.Destroy(colliders2D[i]);
    }

    private static void NormalizeVehicle(GameObject vehicle, Vector3 forward)
    {
        Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Vector3 horizontalForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, horizontalForward).normalized;
        float minRight = float.PositiveInfinity, maxRight = float.NegativeInfinity;
        float minForward = float.PositiveInfinity, maxForward = float.NegativeInfinity;
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds bounds = renderers[i].bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f));
                float projectedRight = Vector3.Dot(point - vehicle.transform.position, right);
                float projectedForward = Vector3.Dot(point - vehicle.transform.position, horizontalForward);
                minRight = Mathf.Min(minRight, projectedRight);
                maxRight = Mathf.Max(maxRight, projectedRight);
                minForward = Mathf.Min(minForward, projectedForward);
                maxForward = Mathf.Max(maxForward, projectedForward);
            }
        }

        float width = maxRight - minRight;
        float length = maxForward - minForward;
        if (width <= .001f || length <= .001f) return;
        Vector3 scale = vehicle.transform.localScale;
        scale.x *= 1.65f / width;
        scale.z *= 3.3f / length;
        vehicle.transform.localScale = scale;
    }

    private void PlaceVehicleOnGround(Transform vehicle)
    {
        Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        float minY = renderers[0].bounds.min.y;
        for (int i = 1; i < renderers.Length; i++) minY = Mathf.Min(minY, renderers[i].bounds.min.y);
        vehicle.position += Vector3.up * (RoadSurfaceY - minY);
    }

    private void Update()
    {
        if (!runtimeBuilt) return;
        if (gameManager == null) gameManager = GameManager.Instance;
        UpdateIncidentState();

        bool activeHazard = disasterController != null && disasterController.AfetDevamEdiyorMu;
        bool paused = PauseMenu.DurdurulduMu ||
                      (gameManager != null &&
                       (gameManager.oyunDurduMu || gameManager.afetOlduMu || gameManager.oyunBittiMi ||
                        (gameManager.kullaniciDurdurdu && !activeHazard)));
        if (paused) return;

        float deltaTime = Time.deltaTime;
        bool stopVehicles = incidentActive || paused;
        if (!stopVehicles) UpdateVehicles(deltaTime);
        UpdateCitizens(deltaTime);
    }

    private void UpdateIncidentState()
    {
        bool disasterRunning = disasterController != null && disasterController.AfetDevamEdiyorMu;
        bool reportOpen = gameManager != null && gameManager.afetOlduMu;
        bool hasIncident = disasterRunning || reportOpen;
        if (hasIncident)
        {
            AfetTuru type = disasterRunning ? disasterController.AktifAfetTuru : gameManager.sonAfetTuru;
            if (!incidentActive || incidentType != type) BeginIncident(type);
        }
        else if (incidentActive)
        {
            EndIncident();
        }
    }

    private void BeginIncident(AfetTuru type)
    {
        incidentActive = true;
        incidentType = type;
        CurrentMode = type == AfetTuru.Yangin || type == AfetTuru.Deprem
            ? CityLifeMode.Assembly
            : CityLifeMode.Shelter;
        shelteredCount = 0;
        gatheredCount = 0;

        for (int i = 0; i < citizens.Count; i++)
        {
            CitizenActor actor = citizens[i];
            actor.mode = CurrentMode;
            actor.reachedGoal = false;
            actor.waitRemaining = 0f;
            actor.shelterHouse = null;
            Vector3 destination;
            if (CurrentMode == CityLifeMode.Assembly)
            {
                destination = NearestGatheringPoint(actor.root.position, i);
            }
            else
            {
                BuildingVisuals house = NearestHouse(actor.root.position);
                actor.shelterHouse = house;
                destination = house != null
                    ? house.transform.position + new Vector3(0f, 0f, -3f)
                    : roadNodePositions[FindNearestNode(actor.root.position)];
            }
            PlanRouteToWorldPoint(actor, destination);
        }
    }

    private void EndIncident()
    {
        incidentActive = false;
        CurrentMode = CityLifeMode.Normal;
        for (int i = 0; i < citizens.Count; i++)
        {
            CitizenActor actor = citizens[i];
            if (actor.mode == CityLifeMode.Assembly)
            {
                if (actor.reachedGoal) gatheredCount = Mathf.Max(0, gatheredCount - 1);
                actor.mode = CityLifeMode.Normal;
                actor.reachedGoal = false;
                PlanReturnToSidewalk(actor);
            }
            else if (actor.mode == CityLifeMode.Shelter)
            {
                if (actor.reachedGoal) shelteredCount = Mathf.Max(0, shelteredCount - 1);
                actor.mode = CityLifeMode.Normal;
                actor.reachedGoal = false;
                if (actor.gameObject != null && !actor.gameObject.activeSelf)
                {
                    actor.gameObject.SetActive(true);
                    if (actor.shelterHouse != null)
                    {
                        actor.root.position = actor.shelterHouse.transform.position + new Vector3(0f, 0f, -3f);
                        actor.root.position = new Vector3(actor.root.position.x, RoadSurfaceY, actor.root.position.z);
                    }
                }
                PlanReturnToSidewalk(actor);
                actor.shelterHouse = null;
            }
        }
        gatheredCount = 0;
        shelteredCount = 0;
    }

    private Vector3 NearestGatheringPoint(Vector3 from, int citizenIndex)
    {
        Transform nearest = null;
        float nearestDistance = float.PositiveInfinity;
        if (gatheringAreas != null)
        {
            for (int i = 0; i < gatheringAreas.Length; i++)
            {
                Transform area = gatheringAreas[i];
                if (area == null) continue;
                float distance = (area.position - from).sqrMagnitude;
                if (distance < nearestDistance) { nearest = area; nearestDistance = distance; }
            }
        }
        Vector3 target = nearest != null ? nearest.position : roadNodePositions[FindNearestNode(from)];
        float population = Mathf.Max(1, citizens.Count);
        float radius = Mathf.Lerp(.5f, 3f, Mathf.Sqrt((citizenIndex + .5f) / population));
        float angle = citizenIndex * 2.39996323f;
        target += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        target.y = RoadSurfaceY;
        return target;
    }

    private BuildingVisuals NearestHouse(Vector3 from)
    {
        BuildingVisuals nearest = null;
        float nearestDistance = float.PositiveInfinity;
        if (houses == null) return null;
        for (int i = 0; i < houses.Length; i++)
        {
            BuildingVisuals house = houses[i];
            if (house == null || house.binaTuru != BinaTuru.TemelBina) continue;
            float distance = (house.transform.position - from).sqrMagnitude;
            if (distance < nearestDistance) { nearest = house; nearestDistance = distance; }
        }
        return nearest;
    }

    private void PlanReturnToSidewalk(CitizenActor actor)
    {
        actor.path.Clear();
        actor.pathIndex = 0;
        actor.hasWorldGoal = false;
        actor.resumeAtNode = true;
        actor.currentNode = FindNearestNode(actor.root.position);
        actor.initialLegTarget = roadNodePositions[actor.currentNode];
        actor.hasInitialLeg = HorizontalDistance(actor.root.position, actor.initialLegTarget) > .15f;
        if (!actor.hasInitialLeg) FinishResumeAtNode(actor);
    }

    private void PlanRouteToWorldPoint(CitizenActor actor, Vector3 target)
    {
        target.y = RoadSurfaceY;
        int start = FindNearestNode(actor.root.position);
        int destination = FindNearestNode(target);
        actor.currentNode = start;
        actor.targetNode = destination;
        actor.pathIndex = 0;
        actor.path.Clear();
        BuildPath(start, destination, actor.path);
        actor.hasInitialLeg = HorizontalDistance(actor.root.position, roadNodePositions[start]) > .15f;
        actor.initialLegTarget = roadNodePositions[start];
        actor.hasWorldGoal = true;
        actor.worldGoal = target;
        actor.resumeAtNode = false;
    }

    private void ChooseWanderRoute(CitizenActor actor)
    {
        int destination = actor.currentNode;
        for (int tries = 0; tries < RoadNodeCount * 2 && destination == actor.currentNode; tries++)
            destination = Random.Range(0, RoadNodeCount);
        if (destination == actor.currentNode)
            destination = (actor.currentNode + 1) % RoadNodeCount;

        actor.targetNode = destination;
        actor.pathIndex = 0;
        actor.path.Clear();
        BuildPath(actor.currentNode, destination, actor.path);
        actor.hasInitialLeg = false;
        actor.hasWorldGoal = false;
        actor.resumeAtNode = false;
        actor.waitRemaining = 0f;
    }

    private void BuildPath(int start, int destination, List<int> output)
    {
        output.Clear();
        for (int i = 0; i < RoadNodeCount; i++) bfsParents[i] = -2;
        int head = 0, tail = 0;
        bfsQueue[tail++] = start;
        bfsParents[start] = -1;
        while (head < tail && bfsParents[destination] == -2)
        {
            int node = bfsQueue[head++];
            for (int i = 0; i < roadNeighborCounts[node]; i++)
            {
                int next = roadNeighbors[node, i];
                if (bfsParents[next] != -2) continue;
                bfsParents[next] = node;
                bfsQueue[tail++] = next;
            }
        }

        if (bfsParents[destination] == -2 || start == destination) return;
        int reverseCount = 0;
        int step = destination;
        while (step != start && step >= 0 && reverseCount < RoadNodeCount)
        {
            reversePath[reverseCount++] = step;
            step = bfsParents[step];
        }
        for (int i = reverseCount - 1; i >= 0; i--) output.Add(reversePath[i]);
    }

    private int FindNearestNode(Vector3 position)
    {
        int nearest = 0;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < roadNodePositions.Length; i++)
        {
            float distance = HorizontalDistanceSquared(position, roadNodePositions[i]);
            if (distance < nearestDistance) { nearestDistance = distance; nearest = i; }
        }
        return nearest;
    }

    private void UpdateCitizens(float deltaTime)
    {
        for (int i = 0; i < citizens.Count; i++)
        {
            CitizenActor actor = citizens[i];
            if (actor.gameObject == null || !actor.gameObject.activeSelf) continue;
            if (actor.waitRemaining > 0f && actor.mode == CityLifeMode.Normal)
            {
                actor.waitRemaining = Mathf.Max(0f, actor.waitRemaining - deltaTime);
                AnimateCitizen(actor, false, deltaTime);
                if (actor.waitRemaining > 0f) continue;
                ChooseWanderRoute(actor);
            }

            bool moving = AdvanceCitizen(actor, deltaTime);
            AnimateCitizen(actor, moving, deltaTime);
        }
    }

    private bool AdvanceCitizen(CitizenActor actor, float deltaTime)
    {
        float speed = actor.mode == CityLifeMode.Normal ? actor.speed : actor.emergencySpeed;
        if (actor.hasInitialLeg)
        {
            if (!MoveCitizen(actor, actor.initialLegTarget, speed, deltaTime)) return true;
            actor.hasInitialLeg = false;
            if (actor.resumeAtNode)
            {
                FinishResumeAtNode(actor);
                return false;
            }
        }

        if (actor.pathIndex < actor.path.Count)
        {
            int node = actor.path[actor.pathIndex];
            if (MoveCitizen(actor, roadNodePositions[node], speed, deltaTime))
            {
                actor.currentNode = node;
                actor.pathIndex++;
            }
            return true;
        }

        if (actor.hasWorldGoal)
        {
            if (!MoveCitizen(actor, actor.worldGoal, speed, deltaTime)) return true;
            actor.hasWorldGoal = false;
            CompleteWorldGoal(actor);
            return false;
        }

        if (actor.mode == CityLifeMode.Normal)
        {
            actor.waitRemaining = Random.Range(.25f, 1.1f);
        }
        return false;
    }

    private bool MoveCitizen(CitizenActor actor, Vector3 destination, float speed, float deltaTime)
    {
        Vector3 current = actor.root.position;
        destination.y = RoadSurfaceY;
        Vector3 next = Vector3.MoveTowards(current, destination, speed * deltaTime);
        Vector3 direction = next - current;
        direction.y = 0f;
        actor.root.position = next;
        if (direction.sqrMagnitude > .0001f)
            actor.root.rotation = Quaternion.Slerp(actor.root.rotation, Quaternion.LookRotation(direction.normalized, Vector3.up),
                1f - Mathf.Exp(-12f * deltaTime));
        return HorizontalDistanceSquared(next, destination) <= .0025f;
    }

    private void CompleteWorldGoal(CitizenActor actor)
    {
        if (actor.mode == CityLifeMode.Assembly)
        {
            if (!actor.reachedGoal) gatheredCount++;
            actor.reachedGoal = true;
        }
        else if (actor.mode == CityLifeMode.Shelter)
        {
            if (actor.shelterHouse != null)
            {
                if (!actor.reachedGoal) shelteredCount++;
                actor.reachedGoal = true;
                actor.gameObject.SetActive(false);
            }
            else
            {
                actor.reachedGoal = true;
                shelteredCount++;
            }
        }
    }

    private void FinishResumeAtNode(CitizenActor actor)
    {
        actor.currentNode = FindNearestNode(actor.root.position);
        actor.resumeAtNode = false;
        ChooseWanderRoute(actor);
    }

    private static void AnimateCitizen(CitizenActor actor, bool walking, float deltaTime)
    {
        float blend = 1f - Mathf.Exp(-10f * deltaTime);
        float swing = 0f;
        if (walking)
        {
            actor.gaitPhase += deltaTime * (actor.mode == CityLifeMode.Normal ? 8.5f : 12.5f);
            swing = Mathf.Sin(actor.gaitPhase) * (actor.mode == CityLifeMode.Normal ? 24f : 34f);
        }
        SetLimbRotation(actor.leftArm, Quaternion.Euler(swing, 0f, 0f), blend);
        SetLimbRotation(actor.rightArm, Quaternion.Euler(-swing, 0f, 0f), blend);
        SetLimbRotation(actor.leftLeg, Quaternion.Euler(-swing * .8f, 0f, 0f), blend);
        SetLimbRotation(actor.rightLeg, Quaternion.Euler(swing * .8f, 0f, 0f), blend);
    }

    private static void SetLimbRotation(Transform limb, Quaternion target, float blend)
    {
        if (limb != null) limb.localRotation = Quaternion.Slerp(limb.localRotation, target, blend);
    }

    private void UpdateVehicles(float deltaTime)
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            VehicleActor actor = vehicles[i];
            if (actor.root == null) continue;
            actor.distance = Mathf.Repeat(actor.distance + actor.speed * deltaTime, actor.route.totalLength);
            Vector3 position, direction;
            EvaluateRoute(actor.route, actor.distance, out position, out direction);
            position.y = actor.root.position.y;
            actor.root.position = position;
            if (direction.sqrMagnitude > .001f)
            {
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                actor.root.rotation = Quaternion.RotateTowards(actor.root.rotation, rotation, 150f * deltaTime);
            }
        }
    }

    private static void EvaluateRoute(VehicleRoute route, float distance, out Vector3 position, out Vector3 direction)
    {
        float remaining = distance;
        int segment = 0;
        while (segment < route.segmentLengths.Length - 1 && remaining >= route.segmentLengths[segment])
        {
            remaining -= route.segmentLengths[segment];
            segment++;
        }
        Vector3 from = route.points[segment];
        Vector3 to = route.points[(segment + 1) % route.points.Length];
        direction = (to - from).normalized;
        position = Vector3.Lerp(from, to, route.segmentLengths[segment] > 0f ? remaining / route.segmentLengths[segment] : 0f);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(HorizontalDistanceSquared(a, b));
    }

    private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying) ClearRuntimeActors();
    }
}
