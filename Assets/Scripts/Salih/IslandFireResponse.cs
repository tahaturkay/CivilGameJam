using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent station engine that travels over the island's road grid for fire calls.</summary>
[DisallowMultipleComponent]
public sealed class IslandFireResponse : MonoBehaviour
{
    private const float RoadSurfaceClearance = .34f;
    private const float EngineBottomOffset = 0f;
    private const float RoadLaneOffset = 1.45f;
    private const float EngineHalfWidth = 1.05f;
    private const float EngineHalfLength = 2.57f;
    private const float VehicleClearance = .20f;
    private const float ResponseSpeed = 42f;
    private const float ReturnSpeed = 25f;
    private const float WaypointTolerance = .12f;
    private const float ObstacleRefreshSeconds = .5f;
    private const float MaxFireApproachDistance = 20f;

    private static readonly float[] RoadX = { -60f, -30f, 0f, 30f, 60f };
    private static readonly float[] RoadZ = { -42f, -14f, 14f, 40f };

    private enum EngineState { AtStation, Responding, OnScene, Returning }

    private sealed class VehicleObstacle
    {
        public Transform root;
        public float halfWidth, halfLength;
    }

    private readonly List<Vector3> route = new List<Vector3>(24);
    private readonly List<VehicleObstacle> obstacles = new List<VehicleObstacle>(16);
    private readonly List<int> nodePath = new List<int>(20);
    private Transform engine;
    private Transform beaconLeft, beaconRight;
    private Light beaconLightLeft, beaconLightRight;
    private IslandCityLife city;
    private Transform actorsRoot;
    private DisasterController disaster;
    private GameManager gameManager;
    private EngineState state;
    private int waypointIndex;
    private float beaconTimer, obstacleRefreshTimer, retryRouteTimer;
    private bool dispatched, arrived, routeValid;
    private Vector3 stationLocal, stationWorld, stationAccessWorld;
    private Vector3 callDestination;

    /// <summary>The persistent vehicle transform used by city traffic collision checks.</summary>
    public Transform Engine => engine;
    /// <summary>True from dispatch until the truck has returned to the station.</summary>
    public bool Dispatched => dispatched;
    /// <summary>True while the truck is parked at the fire-side roadside approach.</summary>
    public bool Arrived => arrived;
    public bool RouteValid => routeValid;
    public float DistanceRemaining
    {
        get
        {
            if (!routeValid || engine == null || waypointIndex >= route.Count) return 0f;
            float distance = Vector3.Distance(engine.position, route[waypointIndex]);
            for (int i = waypointIndex; i + 1 < route.Count; i++)
                distance += Vector3.Distance(route[i], route[i + 1]);
            return distance;
        }
    }

    private void Awake()
    {
        city = GetComponent<IslandCityLife>();
        if (city == null) city = GetComponentInParent<IslandCityLife>();
        gameManager = GameManager.Instance;
        ResolveDisaster();
        EnsureEngine();
    }

    private void Start()
    {
        RefreshStationPosition();
        PlaceAtStation();
    }

    private void OnDestroy()
    {
        if (engine != null) Destroy(engine.gameObject);
        engine = null;
    }

    private void Update()
    {
        if (city == null) city = GetComponent<IslandCityLife>();
        if (city == null) return;
        if (gameManager == null) gameManager = GameManager.Instance;
        ResolveDisaster();
        EnsureEngine();
        if (engine == null || disaster == null) return;

        bool fireCallOpen = IsFireCallOpen();
        if (fireCallOpen && !dispatched)
            Dispatch();
        else if (!fireCallOpen && dispatched && state != EngineState.Returning)
            ReturnToStation();

        if (!dispatched || state == EngineState.OnScene || state == EngineState.AtStation || IsSimulationPaused())
            return;

        float deltaTime = Time.deltaTime * city.MovementSpeedMultiplier;
        if (deltaTime <= 0f) return;

        // Vehicles stop during an incident, so the dispatch snapshot stays valid on scene.
        // Refresh while returning, when normal traffic has resumed.
        if (state == EngineState.Returning)
        {
            obstacleRefreshTimer -= deltaTime;
            if (obstacleRefreshTimer <= 0f)
            {
                RefreshVehicleObstacles();
                obstacleRefreshTimer = ObstacleRefreshSeconds;
            }
        }

        if (!routeValid)
        {
            retryRouteTimer -= deltaTime;
            if (retryRouteTimer <= 0f)
            {
                Vector3 destination = state == EngineState.Returning ? stationWorld : callDestination;
                PlanRoute(destination, state == EngineState.Returning);
                retryRouteTimer = .65f;
            }
            return;
        }

        float travelSpeed = state == EngineState.Returning ? ReturnSpeed : ResponseSpeed;
        MoveAlongRoute(travelSpeed * deltaTime, deltaTime);
    }

    private bool IsSimulationPaused()
    {
        return IslandSimulationPause.IsPaused ||
               (gameManager != null && (gameManager.oyunDurduMu || gameManager.oyunBittiMi || gameManager.afetOlduMu));
    }

    private bool IsFireCallOpen()
    {
        if (disaster == null) return false;
        bool fireRunning = disaster.AfetDevamEdiyorMu && disaster.AktifAfetTuru == AfetTuru.Yangin;
        bool reportOpen = gameManager != null && gameManager.afetOlduMu && gameManager.sonAfetTuru == AfetTuru.Yangin;
        return fireRunning || disaster.FireAwaitingReport || reportOpen;
    }

    private void ResolveDisaster()
    {
        if (city != null && city.disasterController != null)
            disaster = city.disasterController;
        else if (disaster == null)
            disaster = FindAnyObjectByType<DisasterController>();
    }

    private void EnsureEngine()
    {
        if (engine != null)
        {
            MeshFilter filter = engine.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) return;
            Destroy(engine.gameObject);
            engine = null;
        }

        if (city == null) return;
        Transform actorParent = city.transform.Find("SehirYasami");
        GameObject truck = IslandActorArt.CreateFireEngine(actorParent != null ? actorParent : city.transform);
        truck.name = "AdaAraci_Itfaiye";
        engine = truck.transform;
        beaconLeft = engine.Find("AlarmSol");
        beaconRight = engine.Find("AlarmSag");
        beaconLightLeft = AddBeaconLight(beaconLeft);
        beaconLightRight = AddBeaconLight(beaconRight);
        RefreshStationPosition();
        PlaceAtStation();
        if (dispatched)
        {
            state = EngineState.Responding;
            arrived = false;
            callDestination = disaster != null ? disaster.FireOrigin : engine.position;
            PlanRoute(callDestination, false);
        }
    }

    private static Light AddBeaconLight(Transform anchor)
    {
        if (anchor == null) return null;
        Light beacon = anchor.GetComponent<Light>();
        if (beacon == null) beacon = anchor.gameObject.AddComponent<Light>();
        beacon.type = LightType.Point;
        beacon.color = new Color(1f, .055f, .035f);
        beacon.intensity = 2.1f;
        beacon.range = 7f;
        beacon.shadows = LightShadows.None;
        beacon.enabled = false;
        return beacon;
    }

    private void RefreshStationPosition()
    {
        if (city == null) return;
        Vector3 local = disaster != null && disaster.itfaiyeObj != null
            ? city.transform.InverseTransformPoint(disaster.itfaiyeObj.position)
            : new Vector3(15f, 0f, 27f);
        // Use the second garage bay; the authored station display truck occupies the first.
        stationLocal = new Vector3(local.x - 1.6f, 0f, local.z - 4.5f);
        stationWorld = ToWorld(stationLocal);
        stationAccessWorld = ToWorld(new Vector3(stationLocal.x, 0f, 14f));
    }

    private Vector3 ToWorld(Vector3 local)
    {
        Vector3 world = city.transform.TransformPoint(new Vector3(local.x, 0f, local.z));
        world.y = city.landBounds.max.y + RoadSurfaceClearance - EngineBottomOffset;
        return world;
    }

    private void PlaceAtStation()
    {
        if (engine == null || city == null) return;
        RefreshStationPosition();
        engine.position = stationWorld;
        engine.rotation = Quaternion.LookRotation(-city.transform.forward, Vector3.up);
        SetBeacons(false);
    }

    private void Dispatch()
    {
        if (engine == null || disaster == null) return;
        dispatched = true;
        arrived = false;
        state = EngineState.Responding;
        callDestination = disaster.FireOrigin;
        RefreshVehicleObstacles();
        PlanRoute(callDestination, false);
        SetBeacons(true);
    }

    private void ReturnToStation()
    {
        if (engine == null) return;
        if (HorizontalDistance(engine.position, stationWorld) < .5f)
        {
            PlaceAtStation();
            dispatched = false;
            arrived = false;
            state = EngineState.AtStation;
            route.Clear();
            waypointIndex = 0;
            routeValid = true;
            SetBeacons(false);
            return;
        }
        state = EngineState.Returning;
        arrived = false;
        RefreshStationPosition();
        RefreshVehicleObstacles();
        PlanRoute(stationWorld, true);
        SetBeacons(false);
    }

    private void SetBeacons(bool enabled)
    {
        beaconTimer = 0f;
        if (beaconLightLeft != null) beaconLightLeft.enabled = enabled;
        if (beaconLightRight != null) beaconLightRight.enabled = enabled;
        if (enabled) UpdateBeaconFlash();
    }

    private void UpdateBeaconFlash()
    {
        if (beaconLightLeft == null || beaconLightRight == null) return;
        beaconTimer += Time.unscaledDeltaTime;
        bool left = Mathf.FloorToInt(beaconTimer / .18f) % 2 == 0;
        beaconLightLeft.enabled = left;
        beaconLightRight.enabled = !left;
    }

    private void MoveAlongRoute(float maxDistance, float deltaTime)
    {
        if (state == EngineState.Responding) UpdateBeaconFlash();
        while (waypointIndex < route.Count && Vector3.Distance(engine.position, route[waypointIndex]) <= WaypointTolerance)
            waypointIndex++;
        if (waypointIndex >= route.Count)
        {
            if (state == EngineState.Responding)
            {
                if (HorizontalDistance(engine.position, callDestination) > MaxFireApproachDistance)
                {
                    routeValid = false;
                    retryRouteTimer = 0f;
                    return;
                }
                arrived = true;
                state = EngineState.OnScene;
                SetBeacons(false);
            }
            else if (state == EngineState.Returning)
            {
                PlaceAtStation();
                dispatched = false;
                arrived = false;
                state = EngineState.AtStation;
                route.Clear();
                routeValid = true;
            }
            return;
        }

        Vector3 target = route[waypointIndex];
        Vector3 toTarget = target - engine.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < .0001f) return;
        Vector3 direction = toTarget.normalized;
        Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(engine.rotation, facing, 180f * deltaTime);
        Vector3 nextPosition = Vector3.MoveTowards(engine.position, target, maxDistance);
        nextPosition.y = engine.position.y;
        if (!IsPositionClear(nextPosition, nextRotation, EngineHalfLength, EngineHalfWidth, null))
        {
            routeValid = false;
            retryRouteTimer = 0f;
            return;
        }
        engine.rotation = nextRotation;
        engine.position = nextPosition;
    }

    private void PlanRoute(Vector3 destination, bool destinationIsStation)
    {
        route.Clear();
        waypointIndex = 0;
        routeValid = false;
        if (engine == null || city == null) return;

        Vector3 startWorld = engine.position;
        Vector3 startLocal = city.transform.InverseTransformPoint(startWorld);
        Vector3 targetLocal = destinationIsStation
            ? stationLocal
            : city.transform.InverseTransformPoint(destination);
        bool startingAtStation = state == EngineState.Responding && HorizontalDistance(startWorld, stationWorld) < 1f;
        Vector3 startAccessLocal = startingAtStation ? new Vector3(stationLocal.x, 0f, 14f) : NearestRoadPoint(startLocal);
        Vector3 targetAccessLocal = new Vector3(stationLocal.x, 0f, 14f);
        Vector3 startAccess = ToWorld(startAccessLocal);
        if (!AddSafeDirectLeg(startWorld, startAccess)) return;

        FindNearestRoadSegment(startAccessLocal, out _, out int accessNodeA, out int accessNodeB);
        float[] distances = new float[20];
        int[] previous = new int[20];
        bool[] visited = new bool[20];
        for (int i = 0; i < distances.Length; i++) { distances[i] = float.PositiveInfinity; previous[i] = -1; }
        Vector3 routeStart = route.Count > 0 ? route[route.Count - 1] : startWorld;
        SeedRoadAccess(accessNodeA, routeStart, distances);
        SeedRoadAccess(accessNodeB, routeStart, distances);
        if (float.IsInfinity(distances[accessNodeA]) && float.IsInfinity(distances[accessNodeB]))
        { route.Clear(); return; }
        for (int iteration = 0; iteration < distances.Length; iteration++)
        {
            int node = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < distances.Length; i++)
                if (!visited[i] && distances[i] < best) { best = distances[i]; node = i; }
            if (node < 0) break;
            visited[node] = true;
            foreach (int next in Neighbors(node))
            {
                Vector3 a = NodeWorld(node), b = NodeWorld(next);
                if (!TryChooseLane(a, b, out _)) continue;
                float cost = Vector3.Distance(a, b);
                if (distances[next] > distances[node] + cost)
                {
                    distances[next] = distances[node] + cost;
                    previous[next] = node;
                }
            }
        }

        int targetNode = -1;
        Vector3 roadTarget = ToWorld(targetAccessLocal);
        float targetCost = float.PositiveInfinity;
        if (destinationIsStation)
            FindStationEndpoint(distances, targetAccessLocal, ref targetNode, ref targetCost);
        else
            FindTargetEndpoint(distances, targetLocal, ref targetNode, ref roadTarget, ref targetCost);
        if (targetNode < 0) { route.Clear(); return; }

        nodePath.Clear();
        for (int node = targetNode; node >= 0; node = previous[node])
        {
            nodePath.Add(node);
            if (previous[node] < 0) break;
        }
        float sourceDistance = nodePath.Count > 0 ? distances[nodePath[nodePath.Count - 1]] : float.PositiveInfinity;
        if (nodePath.Count == 0 || float.IsInfinity(sourceDistance) || float.IsNaN(sourceDistance)) { route.Clear(); return; }
        nodePath.Reverse();
        int startNode = nodePath[0];

        Vector3 last = route.Count > 0 ? route[route.Count - 1] : startAccess;
        if (Vector3.Distance(last, NodeWorld(startNode)) > .15f)
        {
            Vector3 startCenter = NodeWorld(startNode);
            if (!AddSafeRoadLeg(last, startCenter)) { route.Clear(); return; }
            last = startCenter;
        }

        for (int i = 0; i + 1 < nodePath.Count; i++)
        {
            Vector3 from = NodeWorld(nodePath[i]);
            Vector3 to = NodeWorld(nodePath[i + 1]);
            if (!AddSafeRoadLeg(from, to)) { route.Clear(); return; }
        }

        Vector3 endCenter = NodeWorld(targetNode);
        if (Vector3.Distance(endCenter, roadTarget) > .15f && !AddSafeRoadLeg(endCenter, roadTarget))
        { route.Clear(); return; }
        if (destinationIsStation && Vector3.Distance(route[route.Count - 1], stationWorld) > .15f &&
            !AddSafeDirectLeg(route[route.Count - 1], stationWorld))
        { route.Clear(); return; }
        if (!destinationIsStation && route.Count > 0 &&
            HorizontalDistance(route[route.Count - 1], destination) > MaxFireApproachDistance)
        { route.Clear(); return; }

        // Leave the exact current position as the movement origin, then remove any zero-length duplicates.
        route.RemoveAll(point => Vector3.Distance(point, engine.position) < .02f);
        if (route.Count == 0)
        {
            routeValid = true;
            return;
        }
        routeValid = true;
        retryRouteTimer = .65f;
    }

    private void SeedRoadAccess(int node, Vector3 routeStart, float[] distances)
    {
        if (node < 0 || node >= distances.Length) return;
        if (!TryGetSafeRoadLegCost(routeStart, NodeWorld(node), out float cost)) return;
        if (cost >= distances[node]) return;
        distances[node] = cost;
    }

    private bool AddSafeDirectLeg(Vector3 from, Vector3 to)
    {
        if (Vector3.Distance(from, to) < .15f) return true;
        Vector3 direction = to - from;
        direction.y = 0f;
        if (!TryChooseLane(from, to, out float lane)) return false;
        Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);
        Vector3 laneFrom = from + right * lane * RoadLaneOffset;
        Vector3 laneTo = to + right * lane * RoadLaneOffset;
        if (!AddPoint(laneFrom) || !AddPoint(laneTo)) return false;
        return true;
    }

    private bool AddSafeRoadLeg(Vector3 from, Vector3 to)
    {
        if (Vector3.Distance(from, to) < .15f) return true;
        Vector3 direction = to - from;
        direction.y = 0f;
        if (!TryChooseLane(from, to, out float lane)) return false;
        Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);
        Vector3 laneFrom = from + right * lane * RoadLaneOffset;
        Vector3 laneTo = to + right * lane * RoadLaneOffset;
        Vector3 current = route.Count > 0 ? route[route.Count - 1] : from;
        if (Vector3.Distance(current, laneFrom) > .15f)
        {
            if (Vector3.Distance(current, from) > .15f && !AddPoint(from)) return false;
            if (!AddPoint(laneFrom)) return false;
        }
        return AddPoint(laneTo);
    }

    private bool TryGetSafeRoadLegCost(Vector3 from, Vector3 to, out float cost)
    {
        cost = 0f;
        if (Vector3.Distance(from, to) < .15f) return true;
        Vector3 direction = to - from;
        direction.y = 0f;
        if (!TryChooseLane(from, to, out float lane)) return false;
        Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);
        Vector3 laneFrom = from + right * lane * RoadLaneOffset;
        Vector3 laneTo = to + right * lane * RoadLaneOffset;
        Vector3 current = route.Count > 0 ? route[route.Count - 1] : from;
        if (Vector3.Distance(current, laneFrom) > .15f)
        {
            if (Vector3.Distance(current, from) > .15f)
            {
                if (!SegmentClear(current, from, direction)) return false;
                cost += HorizontalDistance(current, from);
            }
            if (!SegmentClear(from, laneFrom, direction)) return false;
            cost += HorizontalDistance(from, laneFrom);
        }
        if (!SegmentClear(laneFrom, laneTo, direction)) return false;
        cost += HorizontalDistance(laneFrom, laneTo);
        return true;
    }

    private bool AddPoint(Vector3 point)
    {
        if (route.Count > 0 && Vector3.Distance(route[route.Count - 1], point) < .12f) return true;
        Vector3 from = route.Count > 0 ? route[route.Count - 1] : engine.position;
        if (!SegmentClear(from, point, Vector3.forward)) return false;
        route.Add(point);
        return true;
    }

    private void FindTargetEndpoint(float[] distances, Vector3 targetLocal,
        ref int selectedNode, ref Vector3 selectedRoadPoint, ref float bestCost)
    {
        float bestIncidentDistance = float.PositiveInfinity;
        for (int x = 0; x < RoadX.Length; x++)
        for (int z = 0; z < RoadZ.Length; z++)
        {
            int node = Index(x, z);
            if (x + 1 < RoadX.Length)
                ConsiderTargetEdge(node, Index(x + 1, z), targetLocal, distances,
                    ref selectedNode, ref selectedRoadPoint, ref bestIncidentDistance, ref bestCost);
            if (z + 1 < RoadZ.Length && !(x == 2 && z == 1))
                ConsiderTargetEdge(node, Index(x, z + 1), targetLocal, distances,
                    ref selectedNode, ref selectedRoadPoint, ref bestIncidentDistance, ref bestCost);
        }
    }

    private void ConsiderTargetEdge(int a, int b, Vector3 targetLocal, float[] distances,
        ref int selectedNode, ref Vector3 selectedRoadPoint, ref float bestIncidentDistance, ref float bestCost)
    {
        Vector3 aLocal = NodeLocal(a), bLocal = NodeLocal(b);
        Vector3 projectedLocal = ClosestPointOnSegment(new Vector3(targetLocal.x, 0f, targetLocal.z), aLocal, bLocal);
        Vector3 projectedWorld = ToWorld(projectedLocal);
        for (int end = 0; end < 2; end++)
        {
            int endpoint = end == 0 ? a : b;
            if (float.IsInfinity(distances[endpoint])) continue;
            Vector3 nodeWorld = NodeWorld(endpoint);
            if (!TryChooseLane(nodeWorld, projectedWorld, out float lane)) continue;
            Vector3 direction = projectedWorld - nodeWorld;
            direction.y = 0f;
            if (direction.sqrMagnitude < .001f) continue;
            Vector3 lanePoint = projectedWorld + Vector3.Cross(Vector3.up, direction.normalized) * lane * RoadLaneOffset;
            float incidentDistance = HorizontalDistance(lanePoint, ToWorld(targetLocal));
            if (incidentDistance > MaxFireApproachDistance) continue;
            // Incident proximity decides the road; route distance only chooses its direction/entry.
            float cost = distances[endpoint] + HorizontalDistance(nodeWorld, projectedWorld);
            if (incidentDistance > bestIncidentDistance + .05f ||
                (Mathf.Abs(incidentDistance - bestIncidentDistance) <= .05f && cost >= bestCost)) continue;
            bestIncidentDistance = incidentDistance;
            bestCost = cost;
            selectedNode = endpoint;
            selectedRoadPoint = projectedWorld;
        }
    }

    private void FindStationEndpoint(float[] distances, Vector3 stationAccessLocal,
        ref int selectedNode, ref float bestCost)
    {
        Vector3 access = ToWorld(stationAccessLocal);
        for (int node = 0; node < distances.Length; node++)
        {
            if (float.IsInfinity(distances[node])) continue;
            Vector3 point = NodeWorld(node);
            if (!TryChooseLane(point, access, out _)) continue;
            float cost = distances[node] + HorizontalDistance(point, access);
            if (cost >= bestCost) continue;
            selectedNode = node;
            bestCost = cost;
        }
    }

    private Vector3 NearestRoadPoint(Vector3 local)
    {
        FindNearestRoadSegment(local, out Vector3 best, out _, out _);
        return best;
    }

    private void FindNearestRoadSegment(Vector3 local, out Vector3 closestPoint, out int endpointA, out int endpointB)
    {
        Vector3 point = new Vector3(local.x, 0f, local.z);
        closestPoint = NodeLocal(NearestNode(point));
        endpointA = endpointB = NearestNode(point);
        float distance = (closestPoint - point).sqrMagnitude;
        for (int x = 0; x < RoadX.Length; x++)
        for (int z = 0; z < RoadZ.Length; z++)
        {
            int node = Index(x, z);
            if (x + 1 < RoadX.Length)
                ConsiderNearestRoadSegment(point, node, Index(x + 1, z), ref closestPoint, ref endpointA, ref endpointB, ref distance);
            if (z + 1 < RoadZ.Length && !(x == 2 && z == 1))
                ConsiderNearestRoadSegment(point, node, Index(x, z + 1), ref closestPoint, ref endpointA, ref endpointB, ref distance);
        }
    }

    private void ConsiderNearestRoadSegment(Vector3 point, int a, int b, ref Vector3 best, ref int endpointA, ref int endpointB, ref float distance)
    {
        Vector3 projected = ClosestPointOnSegment(point, NodeLocal(a), NodeLocal(b));
        float candidate = (projected - point).sqrMagnitude;
        if (candidate >= distance) return;
        best = projected;
        endpointA = a;
        endpointB = b;
        distance = candidate;
    }

    private bool TryChooseLane(Vector3 from, Vector3 to, out float lane)
    {
        Vector3 direction = to - from;
        direction.y = 0f;
        if (direction.sqrMagnitude < .001f) { lane = 0f; return true; }
        direction.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        bool positive = SegmentClear(from + right * RoadLaneOffset, to + right * RoadLaneOffset, direction);
        bool negative = SegmentClear(from - right * RoadLaneOffset, to - right * RoadLaneOffset, direction);
        if (positive) { lane = 1f; return true; }
        if (negative) { lane = -1f; return true; }
        lane = 0f;
        return false;
    }

    private bool SegmentClear(Vector3 from, Vector3 to, Vector3 direction)
    {
        Vector3 horizontal = to - from;
        horizontal.y = 0f;
        float length = horizontal.magnitude;
        if (length < .01f) return true;
        direction = horizontal / length;
        int samples = Mathf.Max(1, Mathf.CeilToInt(length / 1.25f));
        for (int i = 0; i <= samples; i++)
        {
            Vector3 point = Vector3.Lerp(from, to, i / (float)samples);
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (!IsPositionClear(point, rotation, EngineHalfLength, EngineHalfWidth, null)) return false;
        }
        return true;
    }

    private bool IsPositionClear(Vector3 position, Quaternion rotation, float halfLength, float halfWidth,
        Transform ignored)
    {
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            VehicleObstacle obstacle = obstacles[i];
            if (obstacle.root == null) { obstacles.RemoveAt(i); continue; }
            if (obstacle.root == ignored || obstacle.root == engine) continue;
            if (ObbOverlap(position, rotation, halfLength, halfWidth,
                    obstacle.root.position, obstacle.root.rotation,
                    obstacle.halfLength, obstacle.halfWidth, VehicleClearance)) return false;
        }
        return true;
    }

    private static bool ObbOverlap(Vector3 a, Quaternion aRotation, float aLength, float aWidth,
        Vector3 b, Quaternion bRotation, float bLength, float bWidth, float margin)
    {
        Vector3 aForward = aRotation * Vector3.forward;
        Vector3 aRight = aRotation * Vector3.right;
        Vector3 bForward = bRotation * Vector3.forward;
        Vector3 bRight = bRotation * Vector3.right;
        Vector3 delta = b - a;
        delta.y = 0f;
        return !SeparatedOnAxis(delta, aForward, aForward, aRight, bForward, bRight,
                   aLength, aWidth, bLength, bWidth, margin) &&
               !SeparatedOnAxis(delta, aRight, aForward, aRight, bForward, bRight,
                   aLength, aWidth, bLength, bWidth, margin) &&
               !SeparatedOnAxis(delta, bForward, aForward, aRight, bForward, bRight,
                   aLength, aWidth, bLength, bWidth, margin) &&
               !SeparatedOnAxis(delta, bRight, aForward, aRight, bForward, bRight,
                   aLength, aWidth, bLength, bWidth, margin);
    }

    private static bool SeparatedOnAxis(Vector3 delta, Vector3 axis, Vector3 aForward, Vector3 aRight,
        Vector3 bForward, Vector3 bRight, float aLength, float aWidth, float bLength, float bWidth, float margin)
    {
        axis.y = 0f;
        float axisSqrMagnitude = axis.sqrMagnitude;
        if (axisSqrMagnitude < .0001f) return false;
        axis /= Mathf.Sqrt(axisSqrMagnitude);
        float distance = Mathf.Abs(Vector3.Dot(delta, axis));
        float aRadius = aLength * Mathf.Abs(Vector3.Dot(aForward, axis)) +
                        aWidth * Mathf.Abs(Vector3.Dot(aRight, axis));
        float bRadius = bLength * Mathf.Abs(Vector3.Dot(bForward, axis)) +
                        bWidth * Mathf.Abs(Vector3.Dot(bRight, axis));
        return distance > aRadius + bRadius + margin;
    }

    private void RefreshVehicleObstacles()
    {
        obstacles.Clear();
        if (city == null) return;
        if (actorsRoot == null || actorsRoot.parent != city.transform)
            actorsRoot = city.transform.Find("SehirYasami");
        if (actorsRoot == null) return;
        for (int i = 0; i < actorsRoot.childCount; i++)
        {
            Transform child = actorsRoot.GetChild(i);
            if (child == null || child == engine || !child.name.StartsWith("AdaAraci_")) continue;
            MeshFilter filter = child.GetComponent<MeshFilter>();
            Vector3 extents = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.extents : new Vector3(.95f, 1f, 2f);
            Vector3 scale = child.lossyScale;
            obstacles.Add(new VehicleObstacle
            {
                root = child,
                halfWidth = Mathf.Max(.95f, extents.x * Mathf.Abs(scale.x)) + .1f,
                halfLength = Mathf.Max(1.75f, extents.z * Mathf.Abs(scale.z)) + .1f
            });
        }
        obstacleRefreshTimer = ObstacleRefreshSeconds;
    }

    /// <summary>Allows city traffic to yield while this response engine is returning through town.</summary>
    public bool IsVehiclePositionSafe(Vector3 position, Vector3 forward, float halfLength, float halfWidth)
    {
        if (engine == null || !dispatched || !engine.gameObject.activeInHierarchy) return true;
        Quaternion rotation = forward.sqrMagnitude > .001f
            ? Quaternion.LookRotation(forward, Vector3.up) : engine.rotation;
        return !ObbOverlap(position, rotation, halfLength, halfWidth,
            engine.position, engine.rotation, EngineHalfLength, EngineHalfWidth, VehicleClearance);
    }

    private static IEnumerable<int> Neighbors(int node)
    {
        int x = node / RoadZ.Length, z = node % RoadZ.Length;
        if (x > 0) yield return Index(x - 1, z);
        if (x + 1 < RoadX.Length) yield return Index(x + 1, z);
        if (z > 0 && !(x == 2 && z == 2)) yield return Index(x, z - 1);
        if (z + 1 < RoadZ.Length && !(x == 2 && z == 1)) yield return Index(x, z + 1);
    }

    private static int Index(int x, int z) => x * RoadZ.Length + z;
    private static Vector3 NodeLocal(int node) => new Vector3(RoadX[node / RoadZ.Length], 0f, RoadZ[node % RoadZ.Length]);
    private Vector3 NodeWorld(int node) => ToWorld(NodeLocal(node));

    private int NearestNode(Vector3 local)
    {
        int closest = 0;
        float best = float.PositiveInfinity;
        for (int node = 0; node < RoadX.Length * RoadZ.Length; node++)
        {
            float distance = (NodeLocal(node) - new Vector3(local.x, 0f, local.z)).sqrMagnitude;
            if (distance < best) { closest = node; best = distance; }
        }
        return closest;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float denominator = ab.sqrMagnitude;
        if (denominator < .0001f) return a;
        return a + ab * Mathf.Clamp01(Vector3.Dot(point - a, ab) / denominator);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
