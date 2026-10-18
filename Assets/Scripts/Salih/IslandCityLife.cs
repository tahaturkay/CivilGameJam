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
        Shelter,
        Panic
    }

    [Header("Traffic prefabs")]
    public GameObject[] vehiclePrefabs;

    [Header("Runtime population")]
    [Min(1)] public int citizenCount = 95;
    [Range(0,5)] public int animalCount = 5;
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
    private const float VehicleHalfLength = 1.75f;
    private const float VehicleHalfWidth = .9f;
    private const float VehicleSafetyMargin = .18f;
    // Leave room for a leading low-poly car's rear corner to swing through tight turns.
    private const float VehicleRouteHeadway = 6.5f;
    private const float IntersectionReservationRadius = 6.5f;
    private const float TrafficClearanceRadius = IntersectionReservationRadius + VehicleHalfLength + .55f;
    // Route vertices are miter-offset from the road centerline. Keep red queues beyond
    // the entire offset and a little clear of the physical junction footprint.
    private const float TrafficStopLineDistance = TrafficClearanceRadius + LaneOffset * 1.5f + .75f;
    private const float TrafficGreenSeconds = 8f;
    private const float TrafficAmberSeconds = 1f;
    private const float TrafficAllRedSeconds = .45f;

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
        public float tsunamiEvacuationSpeed;
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
        public bool isAnimal, panicAtAssembly, unsafeResponse;
        public Vector3 assemblyAnchor;
        public float panicTimer;
        public float waterDriftBlend;
        public Vector3 lastWaterCurrent,sweptOrigin;
        public int id;
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
        public int routeIndex;
        public float distance;
        public float speed;
        public float returnDistance;
        public bool displaced;
        public bool returningToRoute;
        public Vector3 curbTarget;
        public float tiltAngle;
        public int committedJunction = -1;
        public bool waitingAtSignal;
        public float stalledScaledSeconds;
    }

    private enum TrafficPhase { GreenA, AmberA, ClearA, GreenB, AmberB, ClearB }

    private sealed class TrafficJunction
    {
        public int ringIndex;
        public int cornerIndex;
        public Vector3 center;
        public TrafficPhase phase;
        public float remaining;
        public IslandTrafficSignal signalA;
        public IslandTrafficSignal signalB;
    }

    private sealed class RoofShelter
    {
        public Bounds bounds;
        public float roofY;
    }

    private sealed class CitizenMaterialBatch
    {
        public Material material;
        public Mesh mesh;
        public int submesh;
        public readonly List<Transform> parts = new List<Transform>(48);
        public readonly List<MeshRenderer> renderers = new List<MeshRenderer>(48);
        public Matrix4x4[] matrices;
        public RenderParams renderParams;
    }

    private readonly List<CitizenActor> citizens = new List<CitizenActor>(48);
    private readonly List<VehicleActor> vehicles = new List<VehicleActor>(12);
    private readonly List<CitizenMaterialBatch> citizenBatches = new List<CitizenMaterialBatch>(16);
    private readonly Dictionary<(Material,Mesh,int), CitizenMaterialBatch> citizenBatchesByMaterial = new Dictionary<(Material,Mesh,int), CitizenMaterialBatch>(32);
    private Vector3[] roadNodePositions;
    private int[,] roadNeighbors;
    private int[] roadNeighborCounts;
    private int[] bfsParents;
    private int[] bfsQueue;
    private int[] reversePath;
    private VehicleRoute[] vehicleRoutes;
    private bool[,] vehicleRouteConflicts;
    private Vector3[,] vehicleRouteConflictPoints;
    private TrafficJunction[] trafficJunctions;
    private RoofShelter[] roofShelters;
    private Transform cityLifeRoot;
    private GameManager gameManager;
    private IslandFireResponse fireResponse;
    private bool runtimeBuilt;
    private bool incidentActive;
    private bool instancingSupported;
    private bool instanceRenderingEnabled;
    private AfetTuru incidentType;
    private int shelteredCount;
    private int gatheredCount;
    private bool tsunamiShelterOrdered;
    private float simulationElapsed;

    public int CitizenCount { get { return citizens.Count-AnimalCount; } }
    public int AnimalCount { get; private set; }
    public int LivingActorCount => citizens.Count;
    public int AwarenessBandAtIncident { get; private set; }
    public int WrongResponseCount { get; private set; }
    public int PanickedAssemblyCount { get; private set; }
    public int SweptCitizenCount {get{int n=0;foreach(var a in citizens)if(a.waterDriftBlend>0)n++;return n;}}
    public int DisplacedVehicleCount {get{int n=0;foreach(var a in vehicles)if(a.displaced)n++;return n;}}
    public bool TrafficBlocked => incidentActive || tsunamiShelterOrdered || (gameManager != null && gameManager.altyapiHasarli);
    public int VehicleCount { get { return vehicles.Count; } }
    public int TrafficSignalCount { get { return trafficJunctions == null ? 0 : trafficJunctions.Length * 2; } }
    public int TrafficCrossingCount { get; private set; }
    public int TrafficStuckVehicleCount
    {
        get { int count = 0; foreach (VehicleActor actor in vehicles) if (actor.stalledScaledSeconds >= 20f) count++; return count; }
    }
    public int TrafficWaitingForSignalCount
    {
        get { int count = 0; foreach (VehicleActor actor in vehicles) if (actor.waitingAtSignal) count++; return count; }
    }
    public float TrafficTotalTravelDistance { get; private set; }
    public int ShelteredCount { get { return shelteredCount; } }
    public int GatheredCount { get { return gatheredCount; } }
    public bool TsunamiShelterOrdered => tsunamiShelterOrdered;
    public float MovementSpeedMultiplier => gameManager!=null?Mathf.Clamp(gameManager.hizCarpani,1,3):1;
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
        CacheRoofShelters();

        GameObject rootObject = new GameObject("SehirYasami");
        cityLifeRoot = rootObject.transform;
        cityLifeRoot.SetParent(transform, false);
        BuildVehicleRoutes();
        BuildVehicleRouteConflicts();
        BuildTrafficSignals();
        SpawnCitizens();
        BuildCitizenMaterialBatches();
        SpawnVehicles();
        fireResponse=GetComponent<IslandFireResponse>();
        if(fireResponse==null)fireResponse=gameObject.AddComponent<IslandFireResponse>();
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
        IslandActorArt.ClearRuntimeCache();
        citizens.Clear();
        AnimalCount=0;
        vehicles.Clear();
        trafficJunctions = null;
        TrafficCrossingCount = 0;
        TrafficTotalTravelDistance = 0f;
        tsunamiShelterOrdered=incidentActive=false;
        simulationElapsed=0;shelteredCount=gatheredCount=0;
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

                Material[] materials=renderer.sharedMaterials;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount&&sub<materials.Length;sub++)
                {
                Material material = materials[sub];if(material==null)continue;
                CitizenMaterialBatch batch;
                var key=(material,filter.sharedMesh,sub);
                if (!citizenBatchesByMaterial.TryGetValue(key, out batch))
                {
                    batch = new CitizenMaterialBatch
                    {
                        material = material,
                        mesh = filter.sharedMesh,
                        submesh = sub,
                        renderParams = new RenderParams(material)
                    };
                    batch.renderParams.layer = renderer.gameObject.layer;
                    batch.renderParams.shadowCastingMode = renderer.shadowCastingMode;
                    batch.renderParams.receiveShadows = renderer.receiveShadows;
                    batch.renderParams.lightProbeUsage = renderer.lightProbeUsage;
                    batch.renderParams.reflectionProbeUsage = renderer.reflectionProbeUsage;
                    batch.renderParams.renderingLayerMask = renderer.renderingLayerMask;
                    citizenBatchesByMaterial.Add(key, batch);
                    citizenBatches.Add(batch);
                }

                batch.parts.Add(renderer.transform);
                batch.renderers.Add(renderer);
                }
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
            // Non-uniform body-part scales require both object/world matrices in Lit's instance buffer.
            for (int first = 0; first < count; first += 511)
            {
                int instanceCount = Mathf.Min(511, count - first);
                Graphics.RenderMeshInstanced(in batch.renderParams, batch.mesh, batch.submesh, batch.matrices, instanceCount, first);
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
        AnimalCount=Mathf.Clamp(animalCount,0,5);
        int people = Mathf.Clamp(citizenCount, 1, Mathf.Max(1, 100-AnimalCount));
        int count = people+AnimalCount;
        for (int i = 0; i < count; i++)
        {
            int startNode = (i * 7) % RoadNodeCount;
            Color shirt = ShirtColors[Random.Range(0, ShirtColors.Length)];
            Color skin = SkinColors[Random.Range(0, SkinColors.Length)];
            bool animal=i>=people;
            GameObject model = animal ? IslandActorArt.CreateAnimal(cityLifeRoot,(i-people)%2==0,i-people)
                : IslandEffectsFactory.CreateCitizen(cityLifeRoot, shirt, skin);
            if(!animal)IslandActorArt.EnhanceCitizen(model,i);
            model.name = animal?"SokakHayvani_"+(i-people+1):"Yaya_" + (i + 1).ToString("00");
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
                emergencySpeed = Random.Range(18f, 21f),
                gaitPhase = Random.Range(0f, Mathf.PI * 2f),
                mode = CityLifeMode.Normal
            };
            actor.isAnimal=animal;actor.id=i;
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

    private void BuildVehicleRouteConflicts()
    {
        int segmentCount = vehicleRoutes.Length * 4;
        vehicleRouteConflicts = new bool[segmentCount, segmentCount];
        vehicleRouteConflictPoints = new Vector3[segmentCount, segmentCount];
        for (int routeA = 0; routeA < vehicleRoutes.Length; routeA++)
        for (int segmentA = 0; segmentA < 4; segmentA++)
        for (int routeB = routeA; routeB < vehicleRoutes.Length; routeB++)
        for (int segmentB = routeB == routeA ? segmentA + 1 : 0; segmentB < 4; segmentB++)
        {
            Vector3 a0 = vehicleRoutes[routeA].points[segmentA];
            Vector3 a1 = vehicleRoutes[routeA].points[(segmentA + 1) % 4];
            Vector3 b0 = vehicleRoutes[routeB].points[segmentB];
            Vector3 b1 = vehicleRoutes[routeB].points[(segmentB + 1) % 4];
            Vector3 closestA, closestB;
            float distance = SegmentDistance(a0, a1, b0, b1, out closestA, out closestB);
            if (distance > VehicleHalfWidth * 2f + VehicleSafetyMargin) continue;
            int indexA = routeA * 4 + segmentA;
            int indexB = routeB * 4 + segmentB;
            Vector3 point = (closestA + closestB) * .5f;
            vehicleRouteConflicts[indexA, indexB] = true;
            vehicleRouteConflicts[indexB, indexA] = true;
            vehicleRouteConflictPoints[indexA, indexB] = point;
            vehicleRouteConflictPoints[indexB, indexA] = point;
        }
    }

    private void BuildTrafficSignals()
    {
        trafficJunctions = new TrafficJunction[8];
        for (int ring = 0; ring < 2; ring++)
        {
            VehicleRoute routeA = vehicleRoutes[ring * 2];
            VehicleRoute routeB = vehicleRoutes[ring * 2 + 1];
            for (int corner = 0; corner < 4; corner++)
            {
                int junctionIndex = ring * 4 + corner;
                Vector3 center = (routeA.points[corner] + routeB.points[3 - corner]) * .5f;
                TrafficJunction junction = new TrafficJunction
                {
                    ringIndex = ring,
                    cornerIndex = corner,
                    center = center,
                    phase = TrafficPhase.GreenA,
                    remaining = TrafficGreenSeconds
                };
                trafficJunctions[junctionIndex] = junction;
                junction.signalA = CreateTrafficSignal(ring, corner, 0, routeA, corner);
                junction.signalB = CreateTrafficSignal(ring, corner, 1, routeB, 3 - corner);
                RefreshTrafficSignalAspects(junction);
            }
        }
    }

    private IslandTrafficSignal CreateTrafficSignal(int ring, int corner, int group,
        VehicleRoute route, int routeCorner)
    {
        int incomingSegment = (routeCorner + route.points.Length - 1) % route.points.Length;
        Vector3 from = route.points[incomingSegment];
        Vector3 to = route.points[routeCorner];
        Vector3 direction = (to - from).normalized;
        Vector3 stopPosition = to - direction * TrafficStopLineDistance;
        GameObject signalObject = new GameObject("Trafik Isigi - " + (ring == 0 ? "Dis" : "Ic") + " " + corner + " " + group);
        signalObject.transform.SetParent(cityLifeRoot, false);
        IslandTrafficSignal signal = signalObject.AddComponent<IslandTrafficSignal>();
        signal.Initialize(ring, corner, group, stopPosition, direction, stopPosition, RoadSurfaceY);
        return signal;
    }

    private void UpdateTrafficSignals(float deltaTime)
    {
        if (trafficJunctions == null) return;
        for (int i = 0; i < trafficJunctions.Length; i++)
        {
            TrafficJunction junction = trafficJunctions[i];
            bool occupied = IsTrafficJunctionOccupied(i, junction);
            float remainingDelta = deltaTime;
            int transitions = 0;
            while (remainingDelta > .0001f && transitions < 8)
            {
                if (IsClearancePhase(junction.phase))
                {
                    if (junction.remaining > 0f)
                    {
                        float consumed = Mathf.Min(remainingDelta, junction.remaining);
                        junction.remaining -= consumed;
                        remainingDelta -= consumed;
                    }
                    if (junction.remaining <= .0001f && !occupied)
                    {
                        AdvanceTrafficPhase(junction);
                        RefreshTrafficSignalAspects(junction);
                        transitions++;
                        continue;
                    }
                    break;
                }

                float phaseTime = Mathf.Max(0f, junction.remaining);
                float phaseConsumed = Mathf.Min(remainingDelta, phaseTime);
                junction.remaining -= phaseConsumed;
                remainingDelta -= phaseConsumed;
                if (junction.remaining <= .0001f)
                {
                    AdvanceTrafficPhase(junction);
                    RefreshTrafficSignalAspects(junction);
                    transitions++;
                }
            }
        }
    }

    private bool IsTrafficJunctionOccupied(int junctionIndex, TrafficJunction junction)
    {
        float clearanceSqr = TrafficClearanceRadius * TrafficClearanceRadius;
        for (int i = 0; i < vehicles.Count; i++)
        {
            VehicleActor actor = vehicles[i];
            if (actor.root == null || (actor.displaced && !actor.returningToRoute)) continue;
            if (actor.committedJunction == junctionIndex ||
                HorizontalDistanceSquared(actor.root.position, junction.center) <= clearanceSqr) return true;
        }
        return false;
    }

    private static bool IsClearancePhase(TrafficPhase phase)
    {
        return phase == TrafficPhase.ClearA || phase == TrafficPhase.ClearB;
    }

    private static void AdvanceTrafficPhase(TrafficJunction junction)
    {
        switch (junction.phase)
        {
            case TrafficPhase.GreenA:
                junction.phase = TrafficPhase.AmberA;
                junction.remaining = TrafficAmberSeconds;
                break;
            case TrafficPhase.AmberA:
                junction.phase = TrafficPhase.ClearA;
                junction.remaining = TrafficAllRedSeconds;
                break;
            case TrafficPhase.ClearA:
                junction.phase = TrafficPhase.GreenB;
                junction.remaining = TrafficGreenSeconds;
                break;
            case TrafficPhase.GreenB:
                junction.phase = TrafficPhase.AmberB;
                junction.remaining = TrafficAmberSeconds;
                break;
            case TrafficPhase.AmberB:
                junction.phase = TrafficPhase.ClearB;
                junction.remaining = TrafficAllRedSeconds;
                break;
            default:
                junction.phase = TrafficPhase.GreenA;
                junction.remaining = TrafficGreenSeconds;
                break;
        }
    }

    private static void RefreshTrafficSignalAspects(TrafficJunction junction)
    {
        IslandTrafficSignal.Aspect aspectA;
        IslandTrafficSignal.Aspect aspectB;
        switch (junction.phase)
        {
            case TrafficPhase.GreenA:
                aspectA = IslandTrafficSignal.Aspect.Green;
                aspectB = IslandTrafficSignal.Aspect.Red;
                break;
            case TrafficPhase.AmberA:
                aspectA = IslandTrafficSignal.Aspect.Amber;
                aspectB = IslandTrafficSignal.Aspect.Red;
                break;
            case TrafficPhase.GreenB:
                aspectA = IslandTrafficSignal.Aspect.Red;
                aspectB = IslandTrafficSignal.Aspect.Green;
                break;
            case TrafficPhase.AmberB:
                aspectA = IslandTrafficSignal.Aspect.Red;
                aspectB = IslandTrafficSignal.Aspect.Amber;
                break;
            default:
                aspectA = aspectB = IslandTrafficSignal.Aspect.Red;
                break;
        }
        if (junction.signalA != null) junction.signalA.SetAspect(aspectA);
        if (junction.signalB != null) junction.signalB.SetAspect(aspectB);
    }

    public string GetTrafficSignalPhase(int ringIndex, int cornerIndex)
    {
        if (trafficJunctions == null || ringIndex < 0 || ringIndex >= 2 || cornerIndex < 0 || cornerIndex >= 4)
            return "YOK";
        return trafficJunctions[ringIndex * 4 + cornerIndex].phase.ToString();
    }

    public IslandTrafficSignal.Aspect GetTrafficSignalAspect(int ringIndex, int cornerIndex, int directionGroup)
    {
        if (trafficJunctions == null || ringIndex < 0 || ringIndex >= 2 || cornerIndex < 0 || cornerIndex >= 4)
            return IslandTrafficSignal.Aspect.Red;
        TrafficJunction junction = trafficJunctions[ringIndex * 4 + cornerIndex];
        IslandTrafficSignal signal = directionGroup == 0 ? junction.signalA : junction.signalB;
        return signal == null ? IslandTrafficSignal.Aspect.Red : signal.CurrentAspect;
    }

    private void CacheRoofShelters()
    {
        if (houses == null)
        {
            roofShelters = new RoofShelter[0];
            return;
        }

        List<RoofShelter> cached = new List<RoofShelter>(houses.Length);
        for (int i = 0; i < houses.Length; i++)
        {
            BuildingVisuals house = houses[i];
            if (house == null || house.binaTuru != BinaTuru.TemelBina) continue;
            Renderer[] renderers = house.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = default;
            bool found = false;
            for (int r = 0; r < renderers.Length; r++)
            {
                if (!found)
                {
                    bounds = renderers[r].bounds;
                    found = true;
                }
                else bounds.Encapsulate(renderers[r].bounds);
            }
            if (!found)
            {
                Collider collider = house.GetComponent<Collider>();
                if (collider == null) continue;
                bounds = collider.bounds;
            }
            cached.Add(new RoofShelter { bounds = bounds, roofY = bounds.max.y });
        }
        roofShelters = cached.ToArray();
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
        if (movingVehicleCount <= 0) return;
        int count = Mathf.Min(12, movingVehicleCount);
        for (int i = 0; i < count; i++)
        {
            int routeIndex = i % vehicleRoutes.Length;
            int onRoute = i / vehicleRoutes.Length;
            VehicleRoute route = vehicleRoutes[routeIndex];
            float desiredDistance = route.totalLength * onRoute / CitizensPerVehicleRoute;
            float distance;
            Vector3 position, direction;
            if (!FindSafeSpawnDistance(route, desiredDistance, out distance, out position, out direction)) continue;

            GameObject car = IslandActorArt.CreateVehicle(cityLifeRoot,i%6);
            car.name = "AdaAraci_" + (i + 1).ToString("00");
            car.transform.position = position;
            car.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            RemoveVehicleColliders(car);
            PlaceVehicleOnGround(car.transform);

            VehicleActor actor = new VehicleActor
            {
                root = car.transform,
                route = route,
                routeIndex = routeIndex,
                distance = distance,
                speed = Random.Range(4.2f, 5.6f)
            };
            vehicles.Add(actor);
        }
    }

    private bool FindSafeSpawnDistance(VehicleRoute route, float desiredDistance, out float safeDistance,
        out Vector3 safePosition, out Vector3 safeDirection)
    {
        const float searchStep = 2f;
        int steps = Mathf.CeilToInt(route.totalLength / searchStep);
        for (int i = 0; i < steps; i++)
        {
            float distance = Mathf.Repeat(desiredDistance + i * searchStep, route.totalLength);
            Vector3 position, direction;
            EvaluateRoute(route, distance, out position, out direction);
            bool clear = IsVehicleSpawnOutsideJunction(position);
            for (int other = 0; other < vehicles.Count; other++)
            {
                if (!clear) break;
                VehicleActor existing = vehicles[other];
                if (existing.root != null && VehicleOverlaps(position, direction, existing.root.position, existing.root.forward))
                {
                    clear = false;
                    break;
                }
            }
            if (!clear) continue;
            safeDistance = distance;
            safePosition = position;
            safeDirection = direction;
            return true;
        }

        safeDistance = 0f;
        safePosition = Vector3.zero;
        safeDirection = Vector3.forward;
        return false;
    }

    private bool IsVehicleSpawnOutsideJunction(Vector3 position)
    {
        if (trafficJunctions == null) return true;
        float minimumDistance = TrafficClearanceRadius + 1f;
        float minimumDistanceSqr = minimumDistance * minimumDistance;
        for (int i = 0; i < trafficJunctions.Length; i++)
        {
            if (HorizontalDistanceSquared(position, trafficJunctions[i].center) <= minimumDistanceSqr)
                return false;
        }
        return true;
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
                        gameManager.kullaniciDurdurdu));
        if (paused) return;

        float deltaTime = Time.deltaTime*MovementSpeedMultiplier;
        simulationElapsed+=deltaTime;
        UpdateTrafficSignals(deltaTime);
        bool stopVehicles = TrafficBlocked;
        UpdateVehicles(deltaTime, !stopVehicles, activeHazard);
        UpdateCitizens(deltaTime);
    }

    private void UpdateIncidentState()
    {
        bool disasterRunning = disasterController != null && disasterController.AfetDevamEdiyorMu;
        bool reportOpen = gameManager != null && gameManager.afetOlduMu;
        bool hasIncident = disasterRunning || reportOpen;
        bool shelterRequired=IslandDisasterCalendar.TsunamiShelterRequired(gameManager);
        if(shelterRequired&&!tsunamiShelterOrdered)
        {
            if(incidentActive)EndIncident();
            tsunamiShelterOrdered=true;
            OrderTsunamiShelter();
        }
        if(tsunamiShelterOrdered)
        {
            if(shelterRequired)
            {
                incidentActive=hasIncident;
                incidentType=AfetTuru.Tsunami;
                CurrentMode=CityLifeMode.Shelter;
                // Normally everyone arrives during the full warning day. Also handle a saved
                // scene/debug day jump into the tsunami day before the first water frame.
                if(gameManager.gun==IslandDisasterCalendar.TsunamiDay)
                    foreach(var actor in citizens)
                        if(actor.mode==CityLifeMode.Shelter&&!actor.reachedGoal&&actor.shelterHouse!=null)
                        {actor.root.position=actor.worldGoal;CompleteWorldGoal(actor);}
                return;
            }
            tsunamiShelterOrdered=false;
            EndIncident();
        }
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

    private void OrderTsunamiShelter()
    {
        CurrentMode=CityLifeMode.Shelter;
        WrongResponseCount=PanickedAssemblyCount=gatheredCount=shelteredCount=0;
        foreach(var actor in citizens)
        {
            actor.mode=CityLifeMode.Shelter;
            actor.panicAtAssembly=actor.unsafeResponse=false;
            actor.reachedGoal=false;actor.waitRemaining=0;
            actor.shelterHouse=NearestHouse(actor.root.position);
            if(actor.shelterHouse!=null)
            {
                PlanRouteToWorldPoint(actor,actor.shelterHouse.transform.position+new Vector3(0,0,-3));
                // Keep the existing street route, but finish the warning evacuation within
                // the 12-second economic day (speed buttons and pause still apply normally).
                Vector3 previous=actor.root.position;
                float distance=0;
                if(actor.hasInitialLeg){distance+=HorizontalDistance(previous,actor.initialLegTarget);previous=actor.initialLegTarget;}
                foreach(int node in actor.path){distance+=HorizontalDistance(previous,roadNodePositions[node]);previous=roadNodePositions[node];}
                distance+=HorizontalDistance(previous,actor.worldGoal);
                actor.tsunamiEvacuationSpeed=Mathf.Max(actor.emergencySpeed,distance/7f);
            }
        }
    }

    private void BeginIncident(AfetTuru type)
    {
        incidentActive = true;
        incidentType = type;
        float awareness=gameManager!=null?gameManager.farkindalikYuzdesi:100;
        // Closed upper endpoints: 0..60 low, >60..80 cautious, >80..100 calm.
        AwarenessBandAtIncident=awareness>80?2:awareness>60?1:0;
        CurrentMode=type==AfetTuru.Tsunami?CityLifeMode.Shelter
            : type==AfetTuru.Sel ? (AwarenessBandAtIncident==0?CityLifeMode.Panic:CityLifeMode.Shelter) : CityLifeMode.Assembly;
        WrongResponseCount=PanickedAssemblyCount=0;
        shelteredCount = 0;
        gatheredCount = 0;

        for (int i = 0; i < citizens.Count; i++)
        {
            CitizenActor actor = citizens[i];
            bool fireOrQuake=type==AfetTuru.Yangin||type==AfetTuru.Deprem;
            bool flood=type==AfetTuru.Sel;
            actor.unsafeResponse=type!=AfetTuru.Tsunami&&!actor.isAnimal&&AwarenessBandAtIncident==0&&(flood||i%10<7);
            actor.mode = type==AfetTuru.Tsunami?CityLifeMode.Shelter
                : flood ? (AwarenessBandAtIncident==0?CityLifeMode.Panic:CityLifeMode.Shelter)
                : actor.unsafeResponse ? (fireOrQuake?CityLifeMode.Shelter:CityLifeMode.Panic) : CityLifeMode.Assembly;
            actor.panicAtAssembly = !actor.isAnimal && actor.mode == CityLifeMode.Assembly &&
                (AwarenessBandAtIncident == 1 || (AwarenessBandAtIncident == 0 && fireOrQuake));
            if(actor.unsafeResponse)WrongResponseCount++;
            if(actor.panicAtAssembly)PanickedAssemblyCount++;
            actor.reachedGoal = false;
            actor.waitRemaining = 0f;
            actor.shelterHouse = null;
            Vector3 destination;
            if(actor.mode==CityLifeMode.Panic)
            {
                // A water emergency starts where the person stands, not at a snapped
                // graph node. Regain the nearest pavement before choosing street routes.
                PlanReturnToSidewalk(actor);continue;
            }
            if (actor.mode == CityLifeMode.Assembly)
            {
                destination = NearestGatheringPoint(actor.root.position, i);
                actor.assemblyAnchor=destination;actor.panicTimer=0;
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
            // A safe flood response stays indoors for the entire incident, including
            // debug/day jumps where there is no full warning day to walk home first.
            if((type==AfetTuru.Tsunami||(flood&&AwarenessBandAtIncident>0))&&actor.shelterHouse!=null)
            {actor.root.position=actor.worldGoal;CompleteWorldGoal(actor);}
        }
    }

    private void EndIncident()
    {
        incidentActive = false;
        CurrentMode = CityLifeMode.Normal;
        for (int i = 0; i < citizens.Count; i++)
        {
            CitizenActor actor = citizens[i];
            actor.panicAtAssembly=actor.unsafeResponse=false;
            if(actor.waterDriftBlend>0)
            {
                actor.gameObject.SetActive(true);actor.root.position=actor.sweptOrigin;
                actor.root.rotation=Quaternion.Euler(0,actor.root.eulerAngles.y,0);
                actor.waterDriftBlend=0;actor.lastWaterCurrent=Vector3.zero;
            }
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
            else if(actor.mode==CityLifeMode.Panic){actor.mode=CityLifeMode.Normal;PlanReturnToSidewalk(actor);}
        }
        gatheredCount = 0;
        shelteredCount = 0;
        WrongResponseCount=PanickedAssemblyCount=0;
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
            if (ApplyWaterCurrent(actor, deltaTime))
            {
                if(!actor.isAnimal)AnimateCitizen(actor,true,deltaTime);
                continue;
            }
            if(actor.mode==CityLifeMode.Assembly && actor.reachedGoal)
            {
                if(actor.panicAtAssembly)
                {
                    actor.panicTimer-=deltaTime;
                    if(actor.panicTimer<=0){actor.panicTimer=Random.Range(.4f,1.1f);Vector2 offset=Random.insideUnitCircle*1.8f;actor.worldGoal=actor.assemblyAnchor+new Vector3(offset.x,0,offset.y);}
                    bool walking=!MoveCitizen(actor,actor.worldGoal,3.8f,deltaTime);
                    AnimateCitizen(actor,walking,deltaTime);
                }
                else AnimateCitizen(actor,false,deltaTime);
                continue;
            }
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

    private bool ApplyWaterCurrent(CitizenActor actor, float deltaTime)
    {
        if (disasterController == null || !disasterController.AfetDevamEdiyorMu ||
            (incidentType != AfetTuru.Sel && incidentType != AfetTuru.Tsunami) ||
            (actor.waterDriftBlend<=0&&IsUnderRoof(actor.root.position)))
        {
            actor.waterDriftBlend = Mathf.MoveTowards(actor.waterDriftBlend, 0f, deltaTime * .8f);
            return false;
        }

        Vector3 currentPosition = actor.root.position;
        bool reached=disasterController.WaterReaches(currentPosition);
        if(!reached&&actor.waterDriftBlend<=0)return false;
        Vector3 current = reached?disasterController.WaterCurrentAt(currentPosition):actor.lastWaterCurrent;
        current.y = 0f;
        if (current.sqrMagnitude < .0001f) return false;

        if(actor.waterDriftBlend<=0)actor.sweptOrigin=currentPosition;
        actor.lastWaterCurrent=current;
        actor.waterDriftBlend = Mathf.MoveTowards(actor.waterDriftBlend, 1f, deltaTime * 1.8f);
        float incidentForce = Mathf.Lerp(.45f, 1.15f, Mathf.Clamp01(disasterController.IncidentProgress));
        Vector3 drift = current * (deltaTime * actor.waterDriftBlend * incidentForce);
        Vector3 next = currentPosition+drift;
        if(incidentType==AfetTuru.Sel)next=ClampToLand(next,.75f);
        next.y = Mathf.Max(RoadSurfaceY,disasterController.FloodSurfaceY+.04f)+Mathf.Sin(simulationElapsed*5+actor.id)*.04f;
        actor.root.position = next;
        Vector3 direction = Vector3.ProjectOnPlane(current, Vector3.up);
        if (direction.sqrMagnitude > .001f)
        {
            Quaternion facing = Quaternion.LookRotation(direction.normalized, Vector3.up)*Quaternion.Euler(58,0,Mathf.Sin(actor.id)*12);
            actor.root.rotation = Quaternion.Slerp(actor.root.rotation, facing, Mathf.Min(1f, deltaTime * 3f));
        }
        if(incidentType==AfetTuru.Tsunami&&next.z>landBounds.max.z+10)actor.gameObject.SetActive(false);
        return true;
    }

    private bool IsUnderRoof(Vector3 position)
    {
        if (disasterController == null || roofShelters == null) return false;
        float waterY = disasterController.FloodSurfaceY;
        for (int i = 0; i < roofShelters.Length; i++)
        {
            RoofShelter roof = roofShelters[i];
            if (waterY >= roof.roofY) continue;
            Bounds bounds = roof.bounds;
            if (position.x >= bounds.min.x - .65f && position.x <= bounds.max.x + .65f &&
                position.z >= bounds.min.z - .65f && position.z <= bounds.max.z + .65f)
                return true;
        }
        return false;
    }

    private Vector3 ClampToLand(Vector3 position, float inset)
    {
        if (landBounds.size.sqrMagnitude < .001f) return position;
        position.x = Mathf.Clamp(position.x, landBounds.min.x + inset, landBounds.max.x - inset);
        position.z = Mathf.Clamp(position.z, landBounds.min.z + inset, landBounds.max.z - inset);
        return position;
    }

    private bool AdvanceCitizen(CitizenActor actor, float deltaTime)
    {
        float speed = actor.mode == CityLifeMode.Normal ? actor.speed : actor.mode==CityLifeMode.Panic ? 7f : actor.emergencySpeed;
        if(tsunamiShelterOrdered&&actor.mode==CityLifeMode.Shelter)
            speed=Mathf.Max(speed,actor.tsunamiEvacuationSpeed);
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

        if(actor.mode==CityLifeMode.Panic)ChooseWanderRoute(actor);
        else if (actor.mode == CityLifeMode.Normal)
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

    private void AnimateCitizen(CitizenActor actor, bool walking, float deltaTime)
    {
        if(actor.isAnimal)
        {
            if(walking)actor.gaitPhase+=deltaTime*11;
            Vector3 p=actor.root.position;p.y=RoadSurfaceY+(walking?Mathf.Abs(Mathf.Sin(actor.gaitPhase))*.045f:0);actor.root.position=p;
            return;
        }
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

    private void UpdateVehicles(float deltaTime, bool canDrive, bool activeHazard)
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            VehicleActor actor = vehicles[i];
            if (actor.root == null) continue;
            actor.waitingAtSignal = false;
            ReleaseClearedJunction(actor);
            if (activeHazard && disasterController != null &&
                (incidentType == AfetTuru.Sel || incidentType == AfetTuru.Tsunami) &&
                !actor.displaced && disasterController.WaterReaches(actor.root.position))
            {
                BeginVehicleDisplacement(actor);
                actor.committedJunction = -1;
            }

            if (actor.displaced)
            {
                if (activeHazard && disasterController != null && disasterController.WaterReaches(actor.root.position))
                    PushVehicleToCurb(actor, deltaTime);
                if (canDrive && !activeHazard && !actor.returningToRoute)
                {
                    actor.returnDistance = FindNearestRouteDistance(actor.route, actor.root.position);
                    actor.returningToRoute = true;
                }
                if (actor.returningToRoute) ReturnVehicleToRoute(actor, deltaTime);
                continue;
            }
            if (!canDrive) continue;

            Vector3 previousPosition = actor.root.position;
            float movement = actor.speed * deltaTime;
            float distanceToCorner;
            int junctionIndex;
            GetNextJunction(actor, out distanceToCorner, out junctionIndex);
            bool greenForUpcomingJunction = false;
            if (junctionIndex >= 0 && actor.committedJunction != junctionIndex)
            {
                TrafficJunction junction = trafficJunctions[junctionIndex];
                int group = actor.routeIndex % 2;
                bool green = IsTrafficGroupGreen(junction, group);
                bool reachesStopLine = distanceToCorner <= TrafficStopLineDistance ||
                    movement >= distanceToCorner - TrafficStopLineDistance;
                greenForUpcomingJunction = green;
                bool alreadyInClearance = HorizontalDistanceSquared(previousPosition, junction.center) <=
                    TrafficClearanceRadius * TrafficClearanceRadius;
                if (green)
                {
                    // A car already in the physical zone must clear even if the phase just
                    // changed. New cars are committed only after a successful move enters it.
                    if (alreadyInClearance) actor.committedJunction = junctionIndex;
                }
                else if (alreadyInClearance)
                {
                    actor.committedJunction = junctionIndex;
                }
                else if (reachesStopLine)
                {
                    movement = Mathf.Min(movement, Mathf.Max(0f, distanceToCorner - TrafficStopLineDistance));
                    actor.waitingAtSignal = true;
                }
            }

            float proposedDistance = Mathf.Repeat(actor.distance + movement, actor.route.totalLength);
            Vector3 position, direction;
            EvaluateRoute(actor.route, proposedDistance, out position, out direction);
            position.y = actor.root.position.y;
            Quaternion proposedRotation=direction.sqrMagnitude>.001f?Quaternion.RotateTowards(actor.root.rotation,Quaternion.LookRotation(direction,Vector3.up),150f*deltaTime):actor.root.rotation;
            if (!HasRouteHeadway(actor, proposedDistance) || HasIntersectionReservation(actor, i, proposedDistance) ||
                !IsVehicleMovementSafe(actor, actor.root.position, position, proposedRotation*Vector3.forward))
            {
                RegisterVehicleStall(actor, deltaTime, previousPosition);
                continue;
            }

            actor.distance = proposedDistance;
            actor.root.position = position;
            if (greenForUpcomingJunction && junctionIndex >= 0 && actor.committedJunction < 0 &&
                SegmentEntersClearance(previousPosition, position, trafficJunctions[junctionIndex].center,
                    TrafficClearanceRadius))
            {
                actor.committedJunction = junctionIndex;
            }
            float travelled = HorizontalDistance(previousPosition, position);
            TrafficTotalTravelDistance += travelled;
            if (distanceToCorner > 0f && movement + .001f >= distanceToCorner && actor.committedJunction == junctionIndex)
                TrafficCrossingCount++;
            if (travelled > .02f) actor.stalledScaledSeconds = 0f;
            else if (actor.waitingAtSignal) actor.stalledScaledSeconds = 0f;
            else actor.stalledScaledSeconds += deltaTime;
            if (direction.sqrMagnitude > .001f)
            {
                actor.root.rotation = proposedRotation;
            }
        }
    }

    private void GetNextJunction(VehicleActor actor, out float distanceToCorner, out int junctionIndex)
    {
        distanceToCorner = float.PositiveInfinity;
        junctionIndex = -1;
        if (actor.route == null || trafficJunctions == null) return;
        float remaining = Mathf.Repeat(actor.distance, actor.route.totalLength);
        int segment = 0;
        while (segment < actor.route.segmentLengths.Length - 1 && remaining >= actor.route.segmentLengths[segment])
        {
            remaining -= actor.route.segmentLengths[segment];
            segment++;
        }
        int routeCorner = (segment + 1) % actor.route.points.Length;
        int physicalCorner = actor.routeIndex % 2 == 0 ? routeCorner : 3 - routeCorner;
        distanceToCorner = actor.route.segmentLengths[segment] - remaining;
        junctionIndex = (actor.routeIndex / 2) * 4 + physicalCorner;
    }

    private bool IsTrafficGroupGreen(TrafficJunction junction, int group)
    {
        return group == 0 ? junction.phase == TrafficPhase.GreenA : junction.phase == TrafficPhase.GreenB;
    }

    private void ReleaseClearedJunction(VehicleActor actor)
    {
        if (actor.committedJunction < 0 || trafficJunctions == null || actor.root == null) return;
        float distanceToNextCorner;
        int nextJunction;
        GetNextJunction(actor, out distanceToNextCorner, out nextJunction);
        // The route corner marks entry/exit ordering. Keep ownership until the car has
        // crossed that corner and its full footprint has also cleared the physical zone.
        if (nextJunction == actor.committedJunction) return;
        if (HorizontalDistanceSquared(actor.root.position, trafficJunctions[actor.committedJunction].center) >
            TrafficClearanceRadius * TrafficClearanceRadius)
            actor.committedJunction = -1;
    }

    private static void RegisterVehicleStall(VehicleActor actor, float deltaTime, Vector3 previousPosition)
    {
        if (actor.waitingAtSignal)
        {
            actor.stalledScaledSeconds = 0f;
            return;
        }
        if (HorizontalDistanceSquared(actor.root.position, previousPosition) > .01f)
            actor.stalledScaledSeconds = 0f;
        else
            actor.stalledScaledSeconds += deltaTime;
    }

    private void BeginVehicleDisplacement(VehicleActor actor)
    {
        actor.displaced = true;
        actor.returningToRoute = false;
        Vector3 direction = Vector3.ProjectOnPlane(actor.root.forward, Vector3.up).normalized;
        if (direction.sqrMagnitude < .001f) direction = Vector3.forward;
        Vector3 curb = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 current = disasterController.WaterCurrentAt(actor.root.position);
        current.y = 0f;
        if (current.sqrMagnitude > .001f) curb = (curb + current.normalized * .45f).normalized;
        actor.tiltAngle = -15f;
        actor.curbTarget = ClampToLand(actor.root.position + curb * 2.35f + current * .25f, 1.25f);
        actor.curbTarget.y = RoadSurfaceY;
    }

    private void PushVehicleToCurb(VehicleActor actor, float deltaTime)
    {
        Vector3 current = disasterController.WaterCurrentAt(actor.root.position);
        current.y = 0f;
        Vector3 next = Vector3.MoveTowards(actor.root.position, actor.curbTarget,
            (.85f + current.magnitude * .45f + Mathf.Clamp01(disasterController.IncidentProgress) * .45f) * deltaTime);
        next.y = actor.root.position.y;
        if (IsVehicleMovementSafe(actor, actor.root.position, next, actor.root.forward)) actor.root.position = next;
        Quaternion tilted = Quaternion.LookRotation(actor.root.forward, Vector3.up) *
            Quaternion.Euler(0f, 0f, actor.tiltAngle);
        actor.root.rotation = Quaternion.Slerp(actor.root.rotation, tilted, Mathf.Min(1f, deltaTime * 2f));
    }

    private void ReturnVehicleToRoute(VehicleActor actor, float deltaTime)
    {
        if (TrafficBlocked) return;
        Vector3 target, direction;
        EvaluateRoute(actor.route, actor.returnDistance, out target, out direction);
        target.y = actor.root.position.y;
        Vector3 next = Vector3.MoveTowards(actor.root.position, target, 2.5f * deltaTime);
        Quaternion proposedRotation=Quaternion.RotateTowards(actor.root.rotation,Quaternion.LookRotation(direction,Vector3.up),80f*deltaTime);
        if (!IsVehicleMovementSafe(actor, actor.root.position, next, proposedRotation*Vector3.forward)) return;
        actor.root.position = next;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        actor.root.rotation = proposedRotation;
        if (HorizontalDistanceSquared(actor.root.position, target) <= .04f)
        {
            if(!IsVehiclePositionSafe(actor,target,direction))return;
            actor.distance = actor.returnDistance;
            actor.displaced = false;
            actor.returningToRoute = false;
            actor.root.position = target;
            actor.root.rotation = rotation;
        }
    }

    private float FindNearestRouteDistance(VehicleRoute route, Vector3 position)
    {
        float bestDistance = 0f;
        float bestSqr = float.PositiveInfinity;
        float traversed = 0f;
        for (int segment = 0; segment < route.points.Length; segment++)
        {
            Vector3 from = route.points[segment];
            Vector3 to = route.points[(segment + 1) % route.points.Length];
            Vector3 closest = ClosestPointOnSegment(position, from, to);
            float sqr = HorizontalDistanceSquared(position, closest);
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestDistance = traversed + Vector3.Distance(from, closest);
            }
            traversed += route.segmentLengths[segment];
        }
        return Mathf.Repeat(bestDistance, route.totalLength);
    }

    private bool HasRouteHeadway(VehicleActor actor, float proposedDistance)
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            VehicleActor other = vehicles[i];
            if (other == actor || other.root == null || other.displaced || other.route != actor.route) continue;
            float gap = Mathf.Repeat(other.distance - proposedDistance, actor.route.totalLength);
            if (gap < VehicleRouteHeadway) return false;
        }
        return true;
    }

    private bool HasIntersectionReservation(VehicleActor actor, int actorIndex, float proposedDistance)
    {
        int segment = RouteSegmentAtDistance(actor.route, proposedDistance);
        int segmentIndex = actor.routeIndex * 4 + segment;
        if (segmentIndex < 0) return true;
        for (int i = 0; i < vehicles.Count; i++)
        {
            VehicleActor other = vehicles[i];
            if (other == actor || other.root == null || other.displaced || other.route == actor.route) continue;
            int otherSegment = RouteSegmentAtDistance(other.route, other.distance);
            int otherSegmentIndex = other.routeIndex * 4 + otherSegment;
            if (!vehicleRouteConflicts[segmentIndex, otherSegmentIndex]) continue;
            Vector3 conflict = vehicleRouteConflictPoints[segmentIndex, otherSegmentIndex];
            bool otherOwnsZone = HorizontalDistanceSquared(other.root.position, conflict) <= IntersectionReservationRadius * IntersectionReservationRadius;
            bool actorInZone = HorizontalDistanceSquared(actor.root.position, conflict) <= IntersectionReservationRadius * IntersectionReservationRadius;
            bool otherApproachingFirst = i < actorIndex &&
                HorizontalDistanceSquared(other.root.position, conflict) <= (IntersectionReservationRadius + 3f) * (IntersectionReservationRadius + 3f) &&
                !IsVehicleWaitingOnRed(other);
            if ((otherOwnsZone && !actorInZone) || (!actorInZone && otherApproachingFirst)) return true;
        }
        return false;
    }

    private bool IsVehicleWaitingOnRed(VehicleActor actor)
    {
        if (trafficJunctions == null || actor == null || actor.root == null) return false;
        float distanceToCorner;
        int junctionIndex;
        GetNextJunction(actor, out distanceToCorner, out junctionIndex);
        if (junctionIndex < 0 || distanceToCorner > TrafficStopLineDistance + .25f) return false;
        return !IsTrafficGroupGreen(trafficJunctions[junctionIndex], actor.routeIndex % 2);
    }

    private bool IsVehicleMovementSafe(VehicleActor actor, Vector3 from, Vector3 to, Vector3 direction)
    {
        float distance = HorizontalDistance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / .4f));
        for (int step = 1; step <= steps; step++)
        {
            Vector3 proposedPosition = Vector3.Lerp(from, to, step / (float)steps);
            if (!IsVehiclePositionSafe(actor, proposedPosition, direction)) return false;
        }
        return true;
    }

    private bool IsVehiclePositionSafe(VehicleActor actor, Vector3 proposedPosition, Vector3 proposedDirection)
    {
        if(fireResponse!=null&&!fireResponse.IsVehiclePositionSafe(proposedPosition,proposedDirection,2.6f,1.05f))return false;
        for (int i = 0; i < vehicles.Count; i++)
        {
            VehicleActor other = vehicles[i];
            if (other == actor || other.root == null) continue;
            if (VehicleOverlaps(proposedPosition, proposedDirection, other.root.position, other.root.forward)) return false;
        }
        return true;
    }

    private static bool VehicleOverlaps(Vector3 positionA, Vector3 directionA, Vector3 positionB, Vector3 directionB)
    {
        Vector3 forwardA = Vector3.ProjectOnPlane(directionA, Vector3.up).normalized;
        Vector3 forwardB = Vector3.ProjectOnPlane(directionB, Vector3.up).normalized;
        if (forwardA.sqrMagnitude < .001f) forwardA = Vector3.forward;
        if (forwardB.sqrMagnitude < .001f) forwardB = Vector3.forward;
        Vector3 rightA = Vector3.Cross(Vector3.up, forwardA).normalized;
        Vector3 rightB = Vector3.Cross(Vector3.up, forwardB).normalized;
        Vector3 delta = positionB - positionA;
        return !(SeparatesOnVehicleAxis(delta, forwardA, forwardA, rightA, forwardB, rightB) ||
               SeparatesOnVehicleAxis(delta, rightA, forwardA, rightA, forwardB, rightB) ||
               SeparatesOnVehicleAxis(delta, forwardB, forwardA, rightA, forwardB, rightB) ||
               SeparatesOnVehicleAxis(delta, rightB, forwardA, rightA, forwardB, rightB));
    }

    private static bool SeparatesOnVehicleAxis(Vector3 delta, Vector3 axis, Vector3 forwardA, Vector3 rightA,
        Vector3 forwardB, Vector3 rightB)
    {
        float distance = Mathf.Abs(Vector3.Dot(delta, axis));
        float radiusA = Mathf.Abs(Vector3.Dot(forwardA, axis)) * (VehicleHalfLength + VehicleSafetyMargin) +
            Mathf.Abs(Vector3.Dot(rightA, axis)) * (VehicleHalfWidth + VehicleSafetyMargin);
        float radiusB = Mathf.Abs(Vector3.Dot(forwardB, axis)) * (VehicleHalfLength + VehicleSafetyMargin) +
            Mathf.Abs(Vector3.Dot(rightB, axis)) * (VehicleHalfWidth + VehicleSafetyMargin);
        return distance >= radiusA + radiusB;
    }

    private static int RouteSegmentAtDistance(VehicleRoute route, float distance)
    {
        float remaining = Mathf.Repeat(distance, route.totalLength);
        int segment = 0;
        while (segment < route.segmentLengths.Length - 1 && remaining >= route.segmentLengths[segment])
        {
            remaining -= route.segmentLengths[segment];
            segment++;
        }
        return segment;
    }

    private static float SegmentDistance(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1,
        out Vector3 closestA, out Vector3 closestB)
    {
        if (SegmentsIntersectXZ(a0, a1, b0, b1))
        {
            float denominator = (a1.x - a0.x) * (b1.z - b0.z) - (a1.z - a0.z) * (b1.x - b0.x);
            float alongA = ((b0.x - a0.x) * (b1.z - b0.z) - (b0.z - a0.z) * (b1.x - b0.x)) / denominator;
            closestA = closestB = Vector3.Lerp(a0, a1, alongA);
            return 0f;
        }

        Vector3[] candidatesA = { a0, a1 };
        Vector3[] candidatesB = { b0, b1 };
        float best = float.PositiveInfinity;
        closestA = a0;
        closestB = b0;
        for (int i = 0; i < 2; i++)
        {
            Vector3 pointB = ClosestPointOnSegment(candidatesA[i], b0, b1);
            float d = HorizontalDistanceSquared(candidatesA[i], pointB);
            if (d < best) { best = d; closestA = candidatesA[i]; closestB = pointB; }
            Vector3 pointA = ClosestPointOnSegment(candidatesB[i], a0, a1);
            d = HorizontalDistanceSquared(pointA, candidatesB[i]);
            if (d < best) { best = d; closestA = pointA; closestB = candidatesB[i]; }
        }
        return Mathf.Sqrt(best);
    }

    private static bool SegmentsIntersectXZ(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
    {
        float denominator = (a1.x - a0.x) * (b1.z - b0.z) - (a1.z - a0.z) * (b1.x - b0.x);
        if (Mathf.Abs(denominator) < .0001f) return false;
        float u = ((b0.x - a0.x) * (b1.z - b0.z) - (b0.z - a0.z) * (b1.x - b0.x)) / denominator;
        float v = ((b0.x - a0.x) * (a1.z - a0.z) - (b0.z - a0.z) * (a1.x - a0.x)) / denominator;
        return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0f;
        float denominator = delta.sqrMagnitude;
        if (denominator < .0001f) return from;
        Vector3 offset = point - from;
        offset.y = 0f;
        return from + delta * Mathf.Clamp01(Vector3.Dot(offset, delta) / denominator);
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

    private static bool SegmentEntersClearance(Vector3 from, Vector3 to, Vector3 center, float radius)
    {
        float dx = to.x - from.x;
        float dz = to.z - from.z;
        float lengthSquared = dx * dx + dz * dz;
        float t = lengthSquared > .0001f
            ? Mathf.Clamp01(((center.x - from.x) * dx + (center.z - from.z) * dz) / lengthSquared)
            : 0f;
        Vector3 closest = new Vector3(from.x + dx * t, center.y, from.z + dz * t);
        return HorizontalDistanceSquared(closest, center) <= radius * radius;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying) ClearRuntimeActors();
    }
}
