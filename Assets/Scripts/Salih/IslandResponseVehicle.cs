using System.Collections.Generic;
using UnityEngine;

/// <summary>Cached road graph plus short, swept collision checks; moving cars never erase an entire road.</summary>
public sealed class IslandResponseVehicle : MonoBehaviour
{
    static readonly float[] RoadX={-60,-30,0,30,60},RoadZ={-42,-14,14,40};
    const float Shoulder=4.15f;
    const int LocalGridSize=21,LocalGridCenter=10,LocalStateCount=LocalGridSize*LocalGridSize*4;
    const float LocalGridStep=1f,LocalRoadMargin=10.75f,LocalGoalRadius=1.15f;
    readonly List<Vector3> centers=new List<Vector3>(26),route=new List<Vector3>(26);
    readonly List<Vector3> positiveRoute=new List<Vector3>(26);
    readonly List<Vector3> detourRoute=new List<Vector3>(36);
    readonly List<Quaternion> detourHeadings=new List<Quaternion>(36);
    readonly List<int> reverse=new List<int>(20);
    readonly float[] distances=new float[20];readonly int[] previous=new int[20];readonly bool[] visited=new bool[20];
    readonly float[] localCosts=new float[LocalStateCount],localPriorities=new float[LocalStateCount];
    readonly int[] localParents=new int[LocalStateCount],localHeap=new int[LocalStateCount],localHeapPositions=new int[LocalStateCount];
    readonly bool[] localClosed=new bool[LocalStateCount];
    readonly List<int> localReverse=new List<int>(LocalStateCount);
    readonly Vector3[] localGoals=new Vector3[24];
    IslandFireResponse owner;IslandCityLife city;
    Vector3 station,destination;Quaternion stationRotation;
    Light left,right;
    Camera view;
    int waypoint,id,detourWaypoint,localHeapCount,localGoalCount;
    bool returning;
    float flash,blockedTime,parkingOffset,departureDelay;
    Vector3 localGridOriginLocal,localDetourGoal;bool avoiding;
    public bool Dispatched {get;private set;}
    public bool Arrived {get;private set;}
    public bool RouteValid {get;private set;}
    public bool WaitingForDispatch => departureDelay>0;
    public bool Moving => Dispatched&&!Arrived&&RouteValid&&!WaitingForDispatch;
    public Vector3 PriorityPoint
    {get{return avoiding&&detourWaypoint<detourRoute.Count?Vector3.MoveTowards(transform.position,detourRoute[detourWaypoint],14):waypoint<route.Count?Vector3.MoveTowards(transform.position,route[waypoint],14):transform.position;}}
    public float DistanceRemaining
    {
        get
        {
            float distance=0;Vector3 p=transform.position;
            if(avoiding)
            {
                for(int i=detourWaypoint;i<detourRoute.Count;i++){distance+=Vector3.Distance(p,detourRoute[i]);p=detourRoute[i];}
                if(waypoint<route.Count&&detourRoute.Count>0)distance+=Vector3.Distance(p,route[waypoint]);
            }
            else for(int i=waypoint;i<route.Count;i++){distance+=Vector3.Distance(p,route[i]);p=route[i];}
            return distance;
        }
    }
    public void Initialize(IslandFireResponse manager,IslandCityLife life,int index,Vector3 home)
    {
        owner=manager;city=life;id=index;station=home;stationRotation=Quaternion.LookRotation(-city.transform.forward,Vector3.up);
        view=Camera.main;
        transform.SetPositionAndRotation(station,stationRotation);left=Beacon("AlarmSol");right=Beacon("AlarmSag");
    }
    Light Beacon(string name)
    {
        Transform anchor=transform.Find(name);if(anchor==null)return null;
        Light lamp=anchor.GetComponent<Light>();if(lamp==null)lamp=anchor.gameObject.AddComponent<Light>();
        lamp.type=LightType.Point;lamp.color=new Color(1,.04f,.025f);lamp.range=5;lamp.intensity=1.6f;lamp.shadows=LightShadows.None;lamp.enabled=false;return lamp;
    }
    public void Dispatch(Vector3 site,float parking,float delay=0)
    {destination=site;parkingOffset=parking;Dispatched=true;Arrived=returning=avoiding=false;blockedTime=0;departureDelay=delay;RouteValid=false;if(delay<=0)PlanRoute();}
    public void ReturnToStation(float delay=0){returning=true;Arrived=avoiding=false;destination=station;parkingOffset=0;departureDelay=delay;RouteValid=false;if(delay<=0)PlanRoute();}
    void Update()
    {
        if(owner==null||!Dispatched||owner.Paused)return;
        float dt=Time.deltaTime*(city!=null?city.MovementSpeedMultiplier:1);
        if(departureDelay>0){departureDelay=Mathf.Max(0,departureDelay-dt);if(departureDelay<=0)PlanRoute();return;}
        flash+=dt;bool blink=Mathf.FloorToInt(flash/.18f)%2==0;
        // In a city-wide shot these small pools occupy less than a pixel. Keep
        // the physical beacon geometry, and evaluate point lighting for close views.
        bool nearby=view==null||(view.transform.position-transform.position).sqrMagnitude<75*75;
        if(left!=null)left.enabled=nearby&&!returning&&blink;if(right!=null)right.enabled=nearby&&!returning&&!blink;
        if(Arrived)return;
        if(!RouteValid){blockedTime+=dt;if(blockedTime>1){blockedTime=0;PlanRoute();}return;}
        float budget=(returning?25:42)*dt;
        // Consume short waypoints in the same frame instead of losing a frame per grid corner.
        for(int iteration=0;iteration<16&&budget>.001f;iteration++)
        {
            if(!avoiding)while(waypoint<route.Count&&Vector3.Distance(transform.position,route[waypoint])<.12f)waypoint++;
            if(avoiding)
            {
                while(detourWaypoint<detourRoute.Count&&Vector3.Distance(transform.position,detourRoute[detourWaypoint])<.12f)detourWaypoint++;
                if(detourWaypoint>=detourRoute.Count){avoiding=false;blockedTime=0;continue;}
            }
            if(!avoiding&&waypoint>=route.Count){FinishRoute();break;}
            Vector3 goal=avoiding?detourRoute[detourWaypoint]:route[waypoint];Vector3 delta=goal-transform.position;delta.y=0;
            if(delta.sqrMagnitude<.015f){if(avoiding)detourWaypoint++;else waypoint++;continue;}
            Vector3 direction=delta.normalized;
            Quaternion wanted=avoiding&&detourWaypoint<detourHeadings.Count?detourHeadings[detourWaypoint]:Quaternion.LookRotation(direction,Vector3.up);
            Quaternion rotation=Quaternion.RotateTowards(transform.rotation,wanted,360*dt);
            float angle=Quaternion.Angle(transform.rotation,wanted);
            float step=Mathf.Min(delta.magnitude,budget*Mathf.Lerp(.25f,1,Mathf.Clamp01(1-angle/100)));
            Vector3 next=transform.position+direction*step;
            if(!SweepClear(transform.position,next,transform.rotation,rotation))
            {
                blockedTime+=dt;
                // Replan only a short street-corridor segment around the current
                // blocker. Civilian and responder OBBs remain live obstacles.
                if(blockedTime>.65f)
                {
                    Vector3 rejoin=avoiding?localDetourGoal:Vector3.MoveTowards(transform.position,route[waypoint],7f);
                    TryLocalDetour(rejoin);blockedTime=0;
                }
                break;
            }
            transform.SetPositionAndRotation(next,rotation);budget-=step;blockedTime=0;
        }
    }
    bool SweepClear(Vector3 a,Vector3 b,Quaternion fromRotation,Quaternion toRotation)
    {
        int count=Mathf.Max(1,Mathf.Max(Mathf.CeilToInt(Vector3.Distance(a,b)/.4f),Mathf.CeilToInt(Quaternion.Angle(fromRotation,toRotation)/12f)));
        for(int i=1;i<=count;i++)
        {
            float t=i/(float)count;
            if(!owner.IsResponseStepSafe(this,Vector3.Lerp(a,b,t),Quaternion.Slerp(fromRotation,toRotation,t)))return false;
        }
        return true;
    }
    bool TryLocalDetour(Vector3 requestedGoal)
    {
        if(owner==null||city==null)return false;
        Vector3 routeGoal=waypoint<route.Count?route[waypoint]:requestedGoal;
        routeGoal.y=transform.position.y;
        Vector3 localStart=city.transform.InverseTransformPoint(transform.position),localGoal=city.transform.InverseTransformPoint(routeGoal);
        float routeDistance=(new Vector2(localGoal.x-localStart.x,localGoal.z-localStart.z)).magnitude;
        if(routeDistance<.5f)return false;
        requestedGoal=Vector3.MoveTowards(transform.position,routeGoal,Mathf.Min(7f,routeDistance));
        BuildLocalGoalCandidates(requestedGoal);
        if(localGoalCount==0)return false;

        // A fixed, bounded 21 x 21 x 4 heading-state A* grid is centered at the
        // vehicle. It only searches the nearby road apron and checks the actual
        // oriented vehicle bounds on every transition; no road graph is removed.
        Vector3 gridOrigin=city.transform.InverseTransformPoint(transform.position);
        localGridOriginLocal=new Vector3(gridOrigin.x,0,gridOrigin.z);localReverse.Clear();
        for(int i=0;i<LocalStateCount;i++)
        {
            localCosts[i]=float.PositiveInfinity;localPriorities[i]=float.PositiveInfinity;
            localParents[i]=-1;localHeapPositions[i]=-1;localClosed[i]=false;
        }
        localHeapCount=0;int currentHeading=NearestLocalHeading(transform.rotation);
        for(int direction=0;direction<4;direction++)
        {
            int x=LocalGridCenter+(direction==0?1:direction==2?-1:0);
            int z=LocalGridCenter+(direction==1?1:direction==3?-1:0);
            if(!InLocalGrid(x,z))continue;
            Vector3 next=LocalGridPoint(x,z);if(!OnRoadApron(next))continue;
            int orientation=direction==currentHeading||direction==(currentHeading+2)%4?currentHeading:direction;
            Quaternion rotation=LocalHeading(orientation);
            if(!SweepClear(transform.position,next,transform.rotation,rotation))continue;
            int state=LocalState(x,z,orientation);float cost=LocalGridStep+TurnCost(transform.rotation,rotation)+(direction==(currentHeading+2)%4?.35f:0);
            localCosts[state]=cost;localParents[state]=-2;
            PushOrDecreaseLocalHeap(state,cost+LocalHeuristic(next));
        }

        int found=-1,expanded=0;Vector3 foundGoal=Vector3.zero;Quaternion foundGoalRotation=Quaternion.identity;
        while(localHeapCount>0&&expanded<900)
        {
            int state=PopLocalHeap();if(localClosed[state])continue;localClosed[state]=true;expanded++;
            int cell=state/4,direction=state%4,x=cell%LocalGridSize,z=cell/LocalGridSize;
            Vector3 from=LocalGridPoint(x,z);Quaternion fromRotation=LocalHeading(direction);
            for(int goalIndex=0;goalIndex<localGoalCount;goalIndex++)
            {
                Vector3 candidateGoal=localGoals[goalIndex];
                float goalDistance=(new Vector2(candidateGoal.x-from.x,candidateGoal.z-from.z)).magnitude;
                if(goalDistance>LocalGoalRadius)continue;
                Quaternion goalRotation=goalDistance>.05f?ConnectionHeading(from,candidateGoal,fromRotation):fromRotation;
                if(!SweepClear(from,candidateGoal,fromRotation,goalRotation))continue;
                if(waypoint<route.Count)
                {
                    Vector3 continuation=route[waypoint]-candidateGoal;continuation.y=0;
                    // The normal route controller drives forward after leaving the
                    // detour, so only accept a rejoin whose forward-facing sweep is
                    // safe. Reverse travel remains confined to the explicit detour.
                    if(continuation.sqrMagnitude>.0025f)
                    {
                        Quaternion forwardHeading=Quaternion.LookRotation(continuation.normalized,Vector3.up);
                        if(!SweepClear(candidateGoal,route[waypoint],goalRotation,forwardHeading))continue;
                    }
                }
                found=state;foundGoal=candidateGoal;foundGoalRotation=goalRotation;break;
            }
            if(found>=0)break;
            for(int nextDirection=0;nextDirection<4;nextDirection++)
            {
                int nx=x+(nextDirection==0?1:nextDirection==2?-1:0);
                int nz=z+(nextDirection==1?1:nextDirection==3?-1:0);
                if(!InLocalGrid(nx,nz))continue;
                Vector3 to=LocalGridPoint(nx,nz);if(!OnRoadApron(to))continue;
                int opposite=(direction+2)%4;
                int nextOrientation=nextDirection==direction||nextDirection==opposite?direction:nextDirection;
                Quaternion toRotation=LocalHeading(nextOrientation);
                if(!SweepClear(from,to,fromRotation,toRotation))continue;
                int nextState=LocalState(nx,nz,nextOrientation);if(localClosed[nextState])continue;
                float candidate=localCosts[state]+LocalGridStep+TurnCost(fromRotation,toRotation)+(nextDirection==opposite?.35f:0);
                if(candidate>=localCosts[nextState])continue;
                localCosts[nextState]=candidate;localParents[nextState]=state;
                PushOrDecreaseLocalHeap(nextState,candidate+LocalHeuristic(to));
            }
        }
        if(found<0)return false;

        detourRoute.Clear();detourHeadings.Clear();
        for(int state=found;state>=0;state=localParents[state])
        {
            localReverse.Add(state);
            if(localParents[state]==-2)break;
        }
        for(int i=localReverse.Count-1;i>=0;i--)
        {
            int cell=localReverse[i]/4;
            AddDetourWaypoint(LocalGridPoint(cell%LocalGridSize,cell/LocalGridSize),LocalHeading(localReverse[i]%4));
        }
        AddDetourWaypoint(foundGoal,foundGoalRotation);
        if(detourRoute.Count==0)return false;
        localDetourGoal=requestedGoal;detourWaypoint=0;avoiding=true;return true;
    }
    bool OnRoadApron(Vector3 worldPosition)
    {
        Vector3 local=city.transform.InverseTransformPoint(worldPosition);local.y=0;
        Project(local,out Vector3 road,out _,out _);
        float dx=local.x-road.x,dz=local.z-road.z;
        return dx*dx+dz*dz<=LocalRoadMargin*LocalRoadMargin;
    }
    Vector3 LocalGridPoint(int x,int z)
    {
        Vector3 local=localGridOriginLocal;
        local.x+=(x-LocalGridCenter)*LocalGridStep;local.z+=(z-LocalGridCenter)*LocalGridStep;
        return World(local);
    }
    Quaternion LocalHeading(int direction)
    {
        Vector3 local=direction==0?Vector3.right:direction==1?Vector3.forward:direction==2?Vector3.left:Vector3.back;
        Vector3 forward=city.transform.TransformDirection(local);forward.y=0;
        return Quaternion.LookRotation(forward.normalized,Vector3.up);
    }
    int NearestLocalHeading(Quaternion rotation)
    {
        Vector3 forward=city.transform.InverseTransformDirection(rotation*Vector3.forward);forward.y=0;forward.Normalize();
        int best=0;float bestDot=float.NegativeInfinity;
        for(int heading=0;heading<4;heading++)
        {
            Vector3 candidate=heading==0?Vector3.right:heading==1?Vector3.forward:heading==2?Vector3.left:Vector3.back;
            float dot=Vector3.Dot(forward,candidate);if(dot<=bestDot)continue;bestDot=dot;best=heading;
        }
        return best;
    }
    Quaternion ConnectionHeading(Vector3 from,Vector3 to,Quaternion current)
    {
        Vector3 delta=to-from;delta.y=0;
        if(delta.sqrMagnitude<.0025f)return current;
        delta.Normalize();return Vector3.Dot(current*Vector3.forward,delta)<-.7f?current:Quaternion.LookRotation(delta,Vector3.up);
    }
    void BuildLocalGoalCandidates(Vector3 baseGoal)
    {
        localGoalCount=0;Vector3 start=transform.position;Vector3 delta=baseGoal-start;delta.y=0;
        float distance=delta.magnitude;if(distance<.5f)return;
        Vector3 forward=delta/distance,side=Vector3.Cross(Vector3.up,forward).normalized;
        for(int progressIndex=0;progressIndex<2;progressIndex++)
        {
            float progress=progressIndex==0?.8f:1f;
            Vector3 center=start+forward*(distance*progress);
            for(int offsetIndex=0;offsetIndex<11;offsetIndex++)
            {
                float offset=offsetIndex==0?0:offsetIndex==1?-2:offsetIndex==2?2:offsetIndex==3?-4:offsetIndex==4?4:offsetIndex==5?-5:offsetIndex==6?5:offsetIndex==7?-6.5f:offsetIndex==8?6.5f:offsetIndex==9?-3:3;
                Vector3 candidate=center+side*offset;candidate.y=transform.position.y;
                if(!OnRoadApron(candidate))continue;
                bool duplicate=false;
                for(int i=0;i<localGoalCount;i++)if((localGoals[i]-candidate).sqrMagnitude<.25f){duplicate=true;break;}
                if(!duplicate&&localGoalCount<localGoals.Length)localGoals[localGoalCount++]=candidate;
            }
        }
    }
    static int LocalState(int x,int z,int direction)=>(z*LocalGridSize+x)*4+direction;
    static bool InLocalGrid(int x,int z)=>x>=0&&x<LocalGridSize&&z>=0&&z<LocalGridSize;
    static float TurnCost(Quaternion from,Quaternion to)=>Quaternion.Angle(from,to)*.0025f;
    float LocalHeuristic(Vector3 position)
    {
        Vector3 a=city.transform.InverseTransformPoint(position);float best=float.PositiveInfinity;
        for(int i=0;i<localGoalCount;i++)
        {
            Vector3 b=city.transform.InverseTransformPoint(localGoals[i]);
            float estimate=Mathf.Abs(a.x-b.x)+Mathf.Abs(a.z-b.z);if(estimate<best)best=estimate;
        }
        return best;
    }
    void AddDetourWaypoint(Vector3 position,Quaternion heading)
    {
        position.y=station.y;
        if(detourRoute.Count==0||(detourRoute[detourRoute.Count-1]-position).sqrMagnitude>.01f)
        {detourRoute.Add(position);detourHeadings.Add(heading);}
        else detourHeadings[detourHeadings.Count-1]=heading;
    }
    void PushOrDecreaseLocalHeap(int state,float priority)
    {
        localPriorities[state]=priority;int index=localHeapPositions[state];
        if(index<0){index=localHeapCount++;localHeap[index]=state;localHeapPositions[state]=index;}
        SiftLocalHeapUp(index);
    }
    void SiftLocalHeapUp(int index)
    {
        while(index>0)
        {
            int parent=(index-1)/2;if(!LocalHeapBefore(localHeap[index],localHeap[parent]))break;
            SwapLocalHeap(index,parent);index=parent;
        }
    }
    void SiftLocalHeapDown(int index)
    {
        while(true)
        {
            int left=index*2+1;if(left>=localHeapCount)return;
            int best=left,right=left+1;
            if(right<localHeapCount&&LocalHeapBefore(localHeap[right],localHeap[left]))best=right;
            if(!LocalHeapBefore(localHeap[best],localHeap[index]))return;
            SwapLocalHeap(index,best);index=best;
        }
    }
    bool LocalHeapBefore(int a,int b)
    {float difference=localPriorities[a]-localPriorities[b];return difference<-.0001f||(Mathf.Abs(difference)<=.0001f&&a<b);}
    void SwapLocalHeap(int a,int b)
    {
        int state=localHeap[a];localHeap[a]=localHeap[b];localHeap[b]=state;
        localHeapPositions[localHeap[a]]=a;localHeapPositions[localHeap[b]]=b;
    }
    int PopLocalHeap()
    {
        int first=localHeap[0],last=localHeap[--localHeapCount];localHeapPositions[first]=-1;
        if(localHeapCount>0){localHeap[0]=last;localHeapPositions[last]=0;SiftLocalHeapDown(0);}
        return first;
    }
    void FinishRoute()
    {
        if(returning)
        {
            if(Quaternion.Angle(transform.rotation,stationRotation)>.75f)
            {
                Quaternion rotation=Quaternion.RotateTowards(transform.rotation,stationRotation,360*Time.deltaTime*(city!=null?city.MovementSpeedMultiplier:1));
                if(SweepClear(transform.position,transform.position,transform.rotation,rotation))transform.rotation=rotation;
                return;
            }
            Dispatched=Arrived=false;returning=false;if(left!=null)left.enabled=false;if(right!=null)right.enabled=false;
        }
        else Arrived=true;
    }
    Vector3 World(Vector3 local){Vector3 p=city.transform.TransformPoint(local);p.y=station.y;return p;}
    void PlanRoute()
    {
        centers.Clear();route.Clear();detourRoute.Clear();detourHeadings.Clear();waypoint=0;detourWaypoint=0;RouteValid=false;avoiding=false;
        Vector3 localStart=city.transform.InverseTransformPoint(transform.position),localGoal=city.transform.InverseTransformPoint(destination);
        Project(localStart,out Vector3 start,out int sa,out int sb);Project(localGoal,out Vector3 goal,out int ga,out int gb);
        if(!returning&&parkingOffset!=0)
        {Vector3 edge=(Node(gb)-Node(ga)).normalized;goal=Closest(goal+edge*parkingOffset,Node(ga),Node(gb));}
        for(int i=0;i<20;i++){distances[i]=float.PositiveInfinity;previous[i]=-1;visited[i]=false;}
        distances[sa]=Vector3.Distance(start,Node(sa));distances[sb]=Vector3.Distance(start,Node(sb));
        for(int iteration=0;iteration<20;iteration++)
        {
            int node=-1;float best=float.PositiveInfinity;
            for(int i=0;i<20;i++)if(!visited[i]&&distances[i]<best){node=i;best=distances[i];}
            if(node<0)break;visited[node]=true;
            int x=node/4,z=node%4;
            if(x>0)Relax(node,node-4);if(x<4)Relax(node,node+4);
            if(z>0&&!(x==2&&z==2))Relax(node,node-1);
            if(z<3&&!(x==2&&z==1))Relax(node,node+1);
        }
        int end=distances[ga]+Vector3.Distance(Node(ga),goal)<distances[gb]+Vector3.Distance(Node(gb),goal)?ga:gb;
        if(float.IsInfinity(distances[end]))return;
        reverse.Clear();for(int node=end;node>=0;node=previous[node])reverse.Add(node);
        AddCenter(start);
        for(int i=reverse.Count-1;i>=0;i--)AddCenter(Node(reverse[i]));
        AddCenter(goal);
        // A single same-edge approach does not need to travel to its endpoint and back.
        if((sa==ga&&sb==gb)||(sa==gb&&sb==ga)){centers.Clear();AddCenter(start);AddCenter(goal);}
        BuildShoulderRoute(1);
        bool positiveClear=ParkedRouteClear();
        float positiveEntrance=route.Count>0?Vector3.Distance(transform.position,route[0]):0;
        positiveRoute.Clear();positiveRoute.AddRange(route);
        route.Clear();BuildShoulderRoute(-1);
        bool negativeClear=ParkedRouteClear();float negativeEntrance=route.Count>0?Vector3.Distance(transform.position,route[0]):0;
        if(positiveClear&&(!negativeClear||positiveEntrance<=negativeEntrance))
        {route.Clear();route.AddRange(positiveRoute);}
        if(returning)AddWaypoint(station);
        RouteValid=true;
    }
    void Relax(int from,int to)
    {float cost=distances[from]+Vector3.Distance(Node(from),Node(to));if(cost<distances[to]){distances[to]=cost;previous[to]=from;}}
    void AddCenter(Vector3 point){if(centers.Count==0||(centers[centers.Count-1]-point).sqrMagnitude>.01f)centers.Add(point);}
    void AddWaypoint(Vector3 point)
    {
        if(route.Count>0&&(route[route.Count-1]-point).sqrMagnitude<=.01f)return;
        if(route.Count>=2)
        {
            Vector3 a=route[route.Count-2],b=route[route.Count-1],horizontalPoint=point;
            a.y=0;b.y=0;horizontalPoint.y=0;
            Vector3 chord=horizontalPoint-a;float chordLengthSquared=chord.sqrMagnitude;
            if(chordLengthSquared>.01f)
            {
                float along=Vector3.Dot(b-a,chord)/chordLengthSquared;
                Vector3 offset=(b-a)-chord*along;
                if(along>0&&along<1&&offset.sqrMagnitude<=.0004f)
                    route.RemoveAt(route.Count-1);
            }
        }
        route.Add(point);
    }
    bool ParkedRouteClear()
    {
        Vector3 from=transform.position;
        foreach(Vector3 to in route)
        {
            Vector3 direction=to-from;if(direction.sqrMagnitude<.001f)continue;
            Quaternion rotation=Quaternion.LookRotation(direction,Vector3.up);
            int count=Mathf.Max(1,Mathf.CeilToInt(direction.magnitude/1.2f));
            for(int i=1;i<=count;i++)if(!owner.ParkedStepSafe(this,Vector3.Lerp(from,to,i/(float)count),rotation))return false;
            from=to;
        }
        return true;
    }
    void BuildShoulderRoute(float side)
    {
        if(centers.Count<2){if(centers.Count==1)AddWaypoint(World(centers[0]));return;}
        for(int i=0;i<centers.Count;i++)
        {
            Vector3 before=(centers[i]-centers[Mathf.Max(0,i-1)]).normalized;
            Vector3 after=(centers[Mathf.Min(centers.Count-1,i+1)]-centers[i]).normalized;
            if(before.sqrMagnitude<.1f)before=after;if(after.sqrMagnitude<.1f)after=before;
            Vector3 incoming=Vector3.Cross(Vector3.up,before),outgoing=Vector3.Cross(Vector3.up,after);
            Vector3 offset=(incoming+outgoing)*(Shoulder*side)/Mathf.Max(.6f,1+Vector3.Dot(before,after));
            if(offset.magnitude>Shoulder*1.5f)offset=outgoing*(Shoulder*side);
            AddWaypoint(World(centers[i]+offset));
        }
    }
    static Vector3 Node(int node)=>new Vector3(RoadX[node/4],0,RoadZ[node%4]);
    static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b){Vector3 d=b-a;return a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude));}
    static void Project(Vector3 p,out Vector3 point,out int a,out int b)
    {
        p.y=0;float best=float.PositiveInfinity;point=Vector3.zero;a=b=0;
        for(int node=0;node<20;node++)
        {
            int x=node/4,z=node%4;
            if(x<4)Edge(p,node,node+4,ref point,ref a,ref b,ref best);
            if(z<3&&!(x==2&&z==1))Edge(p,node,node+1,ref point,ref a,ref b,ref best);
        }
    }
    static void Edge(Vector3 p,int from,int to,ref Vector3 point,ref int a,ref int b,ref float best)
    {Vector3 q=Closest(p,Node(from),Node(to));float distance=(p-q).sqrMagnitude;if(distance>=best)return;best=distance;point=q;a=from;b=to;}
    public static bool Overlaps(Vector3 a,Quaternion ar,float al,float aw,Vector3 b,Quaternion br,float bl,float bw,float margin)
    {
        Vector3 af=ar*Vector3.forward,ax=ar*Vector3.right,bf=br*Vector3.forward,bx=br*Vector3.right,d=b-a;d.y=0;
        return !Separated(d,af,af,ax,bf,bx,al,aw,bl,bw,margin)&&!Separated(d,ax,af,ax,bf,bx,al,aw,bl,bw,margin)&&
            !Separated(d,bf,af,ax,bf,bx,al,aw,bl,bw,margin)&&!Separated(d,bx,af,ax,bf,bx,al,aw,bl,bw,margin);
    }
    static bool Separated(Vector3 d,Vector3 axis,Vector3 af,Vector3 ax,Vector3 bf,Vector3 bx,float al,float aw,float bl,float bw,float margin)
    {axis.y=0;axis.Normalize();return Mathf.Abs(Vector3.Dot(d,axis))>al*Mathf.Abs(Vector3.Dot(af,axis))+aw*Mathf.Abs(Vector3.Dot(ax,axis))+bl*Mathf.Abs(Vector3.Dot(bf,axis))+bw*Mathf.Abs(Vector3.Dot(bx,axis))+margin;}
}
