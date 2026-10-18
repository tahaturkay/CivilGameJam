using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shared, procedurally textured runtime effects; no imported VFX prefab is required.</summary>
public static class IslandEffectsFactory
{
    static readonly List<Object> resources = new List<Object>();
    static Material flameMaterial, glowMaterial, smokeMaterial, rainMaterial, splashMaterial, rubbleMaterial, markerMaterial, markerStripeMaterial;
    static Mesh rubbleMesh, quadMesh;
    static Mesh citizenCubeMesh;
    static readonly Dictionary<Color,Material> citizenMaterials = new Dictionary<Color,Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetResources()
    {
        Application.quitting -= ReleaseResources;
        ReleaseResources();
        Application.quitting += ReleaseResources;
    }

    static void ReleaseResources()
    {
        foreach (Object resource in resources)
        {
            if (resource == null) continue;
            if (Application.isPlaying) Object.Destroy(resource); else Object.DestroyImmediate(resource);
        }
        resources.Clear(); citizenMaterials.Clear();
        flameMaterial = glowMaterial = smokeMaterial = rainMaterial = splashMaterial = rubbleMaterial = markerMaterial = markerStripeMaterial = null;
        rubbleMesh = quadMesh = citizenCubeMesh = null;
    }

    static T Keep<T>(T resource) where T : Object
    {
        resource.hideFlags = HideFlags.HideAndDontSave; resources.Add(resource); return resource;
    }

    static float Noise(float x, float y)
    {
        return Mathf.PerlinNoise(x * 3.1f + 13.7f, y * 3.1f + 8.2f) * .55f
            + Mathf.PerlinNoise(x * 8.3f + 42, y * 8.3f + 7) * .3f
            + Mathf.PerlinNoise(x * 19.7f + 2, y * 19.7f + 31) * .15f;
    }

    static Texture2D SpriteTexture(string name, int kind)
    {
        const int size = 64;
        var texture = Keep(new Texture2D(size, size, TextureFormat.RGBA32, false, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear });
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float u = x / (float)(size - 1), v = y / (float)(size - 1), px = (u - .5f) * 2, py = (v - .5f) * 2;
            float radius = Mathf.Sqrt(px * px + py * py), alpha;
            if (kind == 1) // Wispy flame: broad base, turbulent narrow tip.
            {
                float width = Mathf.Lerp(.75f, .10f, v) + (Noise(u, v) - .5f) * .20f;
                float bend = Mathf.Sin(v * 8.5f) * v * .14f;
                alpha = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(px + bend) / Mathf.Max(.03f, width)), 1.4f)
                    * Mathf.Clamp01(v / .15f) * Mathf.Pow(1 - v, .45f);
            }
            else if (kind == 2) // Soft, elongated rain streak.
                alpha = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(px) * 5.0f), 2) * Mathf.Sin(v * Mathf.PI);
            else if (kind == 3) // Ground splash ring.
                alpha = Mathf.Exp(-Mathf.Pow((radius - .62f) * 14, 2)) * .75f;
            else if (kind == 4) // Ember/core glow.
                alpha = Mathf.Pow(Mathf.Clamp01(1 - radius), 2.6f);
            else // Billowing smoke/soot with feathered, nonrectangular edges.
                alpha = Mathf.Clamp01((1 - radius) / .6f) * Mathf.Clamp01((Noise(u, v) - .18f) * 1.65f);
            pixels[y * size + x] = new Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels); texture.Apply(false, true); return texture;
    }

    static Material ParticleMaterial(string name, Texture texture, bool additive)
    {
        Material template = Resources.Load<Material>("IslandEffects/ParticleShader");
        Shader shader = template != null ? template.shader : Shader.Find("CivilIsland/Soft Particle");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var material = Keep(new Material(shader) { name = name, renderQueue = 3100 });
        material.SetTexture("_BaseMap", texture); material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", additive ? 2 : 0);
        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", additive ? 4 : 2);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        material.SetInt("_SrcBlendAlpha", (int)BlendMode.One); material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0); material.SetInt("_Cull", (int)CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetShaderPassEnabled("ShadowCaster", false);
        return material;
    }

    static void EnsureMaterials()
    {
        if (smokeMaterial != null) return;
        smokeMaterial = ParticleMaterial("Ada FX / Yumusak Duman", SpriteTexture("Ada Duman Alfa", 0), false);
        flameMaterial = ParticleMaterial("Ada FX / Alev", SpriteTexture("Ada Alev Alfa", 1), true);
        glowMaterial = ParticleMaterial("Ada FX / Kor", SpriteTexture("Ada Kor Alfa", 4), true);
        rainMaterial = ParticleMaterial("Ada FX / Yagmur", SpriteTexture("Ada Yagmur Alfa", 2), false);
        splashMaterial = ParticleMaterial("Ada FX / Sicrama", SpriteTexture("Ada Sicrama Alfa", 3), false);
    }

    public static Material SmokeMaterial { get { EnsureMaterials(); return smokeMaterial; } }

    static GameObject Root(Transform parent, string name, Vector3 worldPosition)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = worldPosition; return go;
    }

    static ParticleSystem Particles(Transform parent, string name, Material material, int maxParticles)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = true; main.loop = true; main.duration = 6; main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.cullingMode = ParticleSystemCullingMode.Automatic;
        var emission = ps.emission; emission.rateOverTime = 0;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.maxParticleSize = .3f;
        return ps;
    }

    static Gradient Fade(Color start, Color middle, Color end, float peak = .15f)
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(start, 0), new GradientColorKey(middle, .45f), new GradientColorKey(end, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(start.a, peak), new GradientAlphaKey(middle.a, .55f), new GradientAlphaKey(0, 1) });
        return gradient;
    }

    static void UpwardCone(ParticleSystem ps, float radius, float angle)
    {
        var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = radius; shape.angle = angle; shape.length = .1f; shape.rotation = new Vector3(-90, 0, 0);
    }

    public static GameObject CreateFire(Transform parent, Vector3 worldPosition, float size)
    {
        EnsureMaterials(); size = Mathf.Clamp(size, .35f, 5);
        GameObject root = Root(parent, "KatmanliYangin", worldPosition);
        var flames = Particles(root.transform, "TuruncuAlevler", flameMaterial, 100);
        var main = flames.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(size * 1.5f, size * 3.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * .65f, size * 1.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-.25f, .25f);
        main.startColor = new Color(1, .55f, .10f, .85f);
        var emission = flames.emission; emission.rateOverTime = 24;
        UpwardCone(flames, size * .5f, 12);
        var color = flames.colorOverLifetime; color.enabled = true;
        color.color = Fade(new Color(1,.93f,.44f,.9f), new Color(1,.30f,.025f,.7f), new Color(.4f,.05f,.01f,0));
        var scale = flames.sizeOverLifetime; scale.enabled = true; scale.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,.7f),new Keyframe(.3f,1),new Keyframe(1,.05f)));
        var noise = flames.noise; noise.enabled = true; noise.strength = size * .22f; noise.frequency = .8f; noise.scrollSpeed = .6f;
        flames.Play();

        var core = Particles(root.transform, "SariBeyazSicakMerkez", glowMaterial, 36);
        main = core.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.3f,.65f); main.startSpeed = size * 1.2f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * .6f,size * 1.0f); main.startColor = new Color(1,.85f,.35f,.8f);
        emission = core.emission; emission.rateOverTime = 15; UpwardCone(core, size*.28f, 8);
        color = core.colorOverLifetime; color.enabled = true; color.color = Fade(new Color(1,.98f,.75f,.85f),new Color(1,.60f,.11f,.7f),Color.clear);
        core.Play();

        var embers = Particles(root.transform, "YukselenKivilcimlar", glowMaterial, 65);
        main = embers.main; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f,2.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(size * 2,size * 4);
        main.startSize = new ParticleSystem.MinMaxCurve(.045f,.12f); main.gravityModifier = .12f; main.startColor = new Color(1,.63f,.18f,.95f);
        emission = embers.emission; emission.rateOverTime = 10; UpwardCone(embers,size*.45f,26);
        color = embers.colorOverLifetime; color.enabled = true; color.color = Fade(new Color(1,.9f,.5f,1),new Color(1,.28f,.05f,.7f),Color.clear,.05f);
        noise = embers.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .35f;
        embers.Play();
        CreateSmoke(root.transform,worldPosition + Vector3.up*size,size,true);
        var lightObject = Root(root.transform,"AlevIsigi",worldPosition+Vector3.up*size);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1,.39f,.09f);
        light.range = size*4; light.intensity = 1.7f; light.shadows = LightShadows.None;
        lightObject.AddComponent<IslandFireLightPulse>();
        return root;
    }

    public static GameObject CreateSeaImpact(Transform parent,Vector3 worldPosition,float strength)
    {
        EnsureMaterials();var root=Root(parent,"TsunamiSetCarpmasi",worldPosition);
        root.AddComponent<IslandSeaImpactSplash>().Initialize(smokeMaterial,strength);
        var spray=Particles(root.transform,"SettenYukselenSu",smokeMaterial,180);
        var main=spray.main;main.loop=false;main.duration=.3f;
        main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.35f);
        main.startSize=new ParticleSystem.MinMaxCurve(.35f,1.2f);main.startSpeed=0;
        main.gravityModifier=1.3f;main.startColor=new Color(.80f,.94f,.96f,.85f);
        var shape=spray.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(90,1,2);
        var velocity=spray.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=new ParticleSystem.MinMaxCurve(-8,8);velocity.y=new ParticleSystem.MinMaxCurve(5*strength,10*strength);velocity.z=new ParticleSystem.MinMaxCurve(-3,4);
        var emission=spray.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,160)});
        var colors=spray.colorOverLifetime;colors.enabled=true;colors.color=Fade(new Color(.9f,.98f,1,.8f),new Color(.7f,.9f,.94f,.55f),Color.clear,.06f);
        spray.Play();
        var source=root.AddComponent<AudioSource>();source.spatialBlend=0;source.volume=.30f*Ayarlar.EfektSesi;
        const int frequency=16000;var samples=new float[frequency];var random=new System.Random(7391);
        float low=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)frequency,env=Mathf.Min(1,t*25)*Mathf.Exp(-t*5);
            low=Mathf.Lerp(low,(float)random.NextDouble()*2-1,.12f);
            samples[i]=(low*.7f+Mathf.Sin(t*47*Mathf.PI*2)*.22f)*env;
        }
        var clip=AudioClip.Create("SeteCarpanDalga",frequency,1,frequency,false);clip.SetData(samples,0);source.clip=clip;source.Play();Object.Destroy(clip,4);
        return root;
    }

    public static GameObject CreateSmoke(Transform parent, Vector3 worldPosition, float size, bool heavy = false)
    {
        EnsureMaterials(); size = Mathf.Clamp(size,.3f,4);
        GameObject root = Root(parent,heavy?"YanginDumanSutunu":"KaliciHafifHasarDumani",worldPosition);
        var smoke = Particles(root.transform,"YumusakDuman",smokeMaterial,heavy?90:42);
        var main = smoke.main; main.startLifetime = new ParticleSystem.MinMaxCurve(4,7); main.startSpeed = new ParticleSystem.MinMaxCurve(size*.7f,size*1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(size*.7f,size*1.25f); main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
        main.startColor = Color.white; var emission = smoke.emission; emission.rateOverTime = heavy?9:5.2f;
        UpwardCone(smoke,size*.32f,17);
        var color = smoke.colorOverLifetime; color.enabled = true;
        color.color = Fade(new Color(.22f,.22f,.20f,heavy?.42f:.33f),new Color(.37f,.36f,.32f,heavy?.28f:.21f),new Color(.51f,.52f,.51f,0));
        var scale = smoke.sizeOverLifetime; scale.enabled = true; scale.size = new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.7f),new Keyframe(1,3.7f)));
        var velocity = smoke.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = size*.27f; velocity.y = .25f; velocity.z = size*.08f;
        var noise = smoke.noise; noise.enabled = true; noise.strength = size*.3f; noise.frequency = .23f; noise.scrollSpeed = .17f; noise.quality = ParticleSystemNoiseQuality.Medium;
        var rotation = smoke.rotationOverLifetime; rotation.enabled = true; rotation.z = .18f;
        smoke.Play(); smoke.Emit(3); return root;
    }

    public static GameObject CreateDust(Transform parent, Vector3 worldPosition, float size)
    {
        EnsureMaterials(); size = Mathf.Clamp(size,.5f,6);
        GameObject root = Root(parent,"DepremTozu",worldPosition);
        var dust = Particles(root.transform,"YayilanToz",smokeMaterial,70);
        var main = dust.main; main.loop = false; main.duration = .3f; main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f,4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(size*.4f,size*.9f); main.startSpeed = new ParticleSystem.MinMaxCurve(size*.4f,size*1.2f);
        main.gravityModifier = -.02f; main.startColor = Color.white;
        var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = size*.35f; shape.rotation = new Vector3(-90,0,0);
        var emission = dust.emission; emission.SetBursts(new[] { new ParticleSystem.Burst(0,35) });
        var color = dust.colorOverLifetime; color.enabled = true; color.color = Fade(new Color(.55f,.47f,.33f,.6f),new Color(.64f,.59f,.47f,.32f),Color.clear,.08f);
        var scale = dust.sizeOverLifetime; scale.enabled = true; scale.size = new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,3));
        dust.Play(); root.AddComponent<IslandEffectLifetime>().seconds = 7;
        return root;
    }

    /// <summary>Returns an inactive rain rig; activate it during a flood and disable it afterwards.</summary>
    public static GameObject CreateRain(Transform parent, Bounds land)
    {
        EnsureMaterials();
        float height = Mathf.Clamp(land.size.y+22,24,45);
        GameObject root = Root(parent,"AdaYagmuru",new Vector3(land.center.x,land.min.y+height,land.center.z));
        var rain = Particles(root.transform,"EgikYagmurTaneleri",rainMaterial,4200);
        var main = rain.main; main.startLifetime = height/30f; main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.07f,.13f);
        main.startColor = new Color(.71f,.83f,.88f,.48f);
        var shape = rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(land.size.x+10,.1f,land.size.z+10);
        var velocity = rain.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World; velocity.x = -4.5f; velocity.y = -30; velocity.z = 1.5f;
        var emission = rain.emission; emission.rateOverTime = Mathf.Clamp(land.size.x*land.size.z*.055f,350,1900);
        var renderer = rain.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.lengthScale = 5; renderer.velocityScale = .06f; renderer.cameraVelocityScale = 0;
        var splash = Particles(root.transform,"ZeminSicramalari",splashMaterial,250);
        splash.transform.position = new Vector3(land.center.x,land.min.y+.09f,land.center.z);
        main = splash.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.16f,.35f); main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.16f,.38f); main.startColor = new Color(.72f,.85f,.85f,.40f);
        shape = splash.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(land.size.x,.01f,land.size.z);
        emission = splash.emission; emission.rateOverTime = 200;
        var color = splash.colorOverLifetime; color.enabled = true; color.color = Fade(Color.white,new Color(1,1,1,.5f),Color.clear,.05f);
        var scale = splash.sizeOverLifetime; scale.enabled = true; scale.size = new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,1.5f));
        splash.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        root.SetActive(false); return root;
    }

    static Material SolidMaterial(ref Material material, string name, Color color)
    {
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard");
        material = Keep(new Material(shader) { name=name, color=color }); return material;
    }

    static Mesh RubbleMesh()
    {
        if (rubbleMesh != null) return rubbleMesh;
        Vector3[] points={new Vector3(-.6f,0,-.5f),new Vector3(.5f,0,-.4f),new Vector3(.6f,0,.5f),new Vector3(-.4f,0,.6f),new Vector3(-.2f,.8f,0)};
        int[] faces={0,4,1,1,4,2,2,4,3,3,4,0,0,1,2,0,2,3}; var vertices=new Vector3[faces.Length]; var triangles=new int[faces.Length];
        for(int i=0;i<faces.Length;i++){vertices[i]=points[faces[i]];triangles[i]=i;}
        rubbleMesh=Keep(new Mesh { name="Ada Ortak Moloz",vertices=vertices,triangles=triangles });rubbleMesh.RecalculateNormals();rubbleMesh.RecalculateBounds();return rubbleMesh;
    }

    public static GameObject CreateRubble(Transform parent, Bounds localBounds)
    {
        GameObject root=Root(parent,"KaliciDepremMolozu",parent.position);root.transform.localPosition=Vector3.zero;
        Material material=SolidMaterial(ref rubbleMaterial,"Ada Kirik Tas",new Color(.43f,.40f,.33f));
        float width=Mathf.Min(localBounds.size.x,12),depth=Mathf.Min(localBounds.size.z,10);
        for(int i=0;i<15;i++)
        {
            float a=i*2.399963f,r=.25f+Mathf.Sqrt(i/15f)*width*.18f;
            var piece=new GameObject("KirikDuvarParcasi",typeof(MeshFilter),typeof(MeshRenderer));piece.transform.SetParent(root.transform,false);
            piece.transform.localPosition=new Vector3(localBounds.center.x+width*.22f+Mathf.Cos(a)*r,localBounds.min.y+.03f,localBounds.center.z-depth*.30f+Mathf.Sin(a)*r*.65f);
            piece.transform.localRotation=Quaternion.Euler(0,i*47,0);piece.transform.localScale=new Vector3(.3f+(i%4)*.19f,.3f+(i%3)*.22f,.4f+(i%5)*.11f);
            piece.GetComponent<MeshFilter>().sharedMesh=RubbleMesh();piece.GetComponent<Renderer>().sharedMaterial=material;
        }
        return root;
    }

    public static Mesh SoftQuadMesh
    {
        get
        {
            if(quadMesh!=null)return quadMesh;
            quadMesh=Keep(new Mesh { name="Ada Alfa Dortgen",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},triangles=new[]{0,2,1,0,3,2},colors=new[]{Color.white,Color.white,Color.white,Color.white} });
            quadMesh.RecalculateNormals();quadMesh.RecalculateBounds();return quadMesh;
        }
    }

    public static GameObject CreateSoot(Transform parent, Bounds bounds)
    {
        EnsureMaterials(); var root=Root(parent,"KaliciIsIzleri",parent.position);root.transform.localPosition=Vector3.zero;
        for(int i=0;i<3;i++)
        {
            var patch=new GameObject("IsLekesi",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(root.transform,false);
            patch.transform.localPosition=new Vector3(bounds.center.x+(i-1)*bounds.size.x*.19f,bounds.min.y+bounds.size.y*(.40f+i*.12f),bounds.min.z-.04f);
            patch.transform.localScale=new Vector3(Mathf.Min(bounds.size.x*.45f,4),bounds.size.y*.38f,1);
            patch.GetComponent<MeshFilter>().sharedMesh=SoftQuadMesh;patch.GetComponent<Renderer>().sharedMaterial=smokeMaterial;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.10f,.085f,.065f,.42f));block.SetColor("_Color",new Color(.10f,.085f,.065f,.42f));patch.GetComponent<Renderer>().SetPropertyBlock(block);
        }
        return root;
    }

    // The Resources/CitizenInstancing material preserves Lit instancing variants in player builds.
    static Material CitizenMaterial(Color color)
    {
        if(citizenMaterials.TryGetValue(color,out Material material)&&material!=null)return material;
        Material created=null; material=SolidMaterial(ref created,"Ada Vatandas Rengi",color);material.enableInstancing=true;citizenMaterials[color]=material;return material;
    }
    static Mesh CitizenCubeMesh()
    {
        if (citizenCubeMesh != null) return citizenCubeMesh;
        citizenCubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if (citizenCubeMesh != null) return citizenCubeMesh;

        // Fallback for Unity installations where the built-in mesh resource is unavailable.
        Vector3[] vertices =
        {
            new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f),
            new Vector3(.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,.5f), new Vector3(-.5f,.5f,.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(.5f,-.5f,.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(.5f,.5f,.5f),
            new Vector3(-.5f,.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
            new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,-.5f,.5f), new Vector3(-.5f,-.5f,.5f)
        };
        int[] triangles =
        {
            0,1,2, 0,2,3, 4,5,6, 4,6,7, 8,9,10, 8,10,11,
            12,13,14, 12,14,15, 16,17,18, 16,18,19, 20,21,22, 20,22,23
        };
        citizenCubeMesh = Keep(new Mesh { name = "Ada Vatandasi Ortak Kupu" });
        citizenCubeMesh.vertices = vertices;
        citizenCubeMesh.triangles = triangles;
        citizenCubeMesh.RecalculateNormals();
        citizenCubeMesh.RecalculateBounds();
        return citizenCubeMesh;
    }

    /// <summary>Clears runtime-only citizen materials and mesh references when the city rig is rebuilt.</summary>
    public static void ClearCitizenRuntimeCache()
    {
        foreach (Material material in citizenMaterials.Values)
        {
            if (material == null) continue;
            resources.Remove(material);
            if (Application.isPlaying) Object.Destroy(material); else Object.DestroyImmediate(material);
        }
        citizenMaterials.Clear();
        if (citizenCubeMesh != null && resources.Remove(citizenCubeMesh))
        {
            if (Application.isPlaying) Object.Destroy(citizenCubeMesh); else Object.DestroyImmediate(citizenCubeMesh);
        }
        citizenCubeMesh = null;
    }

    static void CitizenPart(Transform parent,string name,Vector3 pos,Vector3 scale,Material material)
    {
        var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(parent,false);
        part.transform.localPosition=pos;part.transform.localScale=scale;
        part.GetComponent<MeshFilter>().sharedMesh=CitizenCubeMesh();
        part.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    /// <summary>1.31m character with foot origin and named shoulder/hip pivots for walking animation.</summary>
    public static GameObject CreateCitizen(Transform parent,Color shirt,Color skin)
    {
        var root=new GameObject("AdaVatandasi");root.transform.SetParent(parent,false);
        Material top=CitizenMaterial(shirt),face=CitizenMaterial(skin),pants=CitizenMaterial(new Color(.18f,.24f,.27f)),shoe=CitizenMaterial(new Color(.16f,.14f,.12f));
        CitizenPart(root.transform,"Govde",new Vector3(0,.77f,0),new Vector3(.42f,.52f,.26f),top);
        CitizenPart(root.transform,"Bas",new Vector3(0,1.17f,0),new Vector3(.27f,.28f,.26f),face);
        CitizenPart(root.transform,"Sac",new Vector3(0,1.30f,.015f),new Vector3(.29f,.09f,.28f),shoe);
        for(int side=-1;side<=1;side+=2)
        {
            var arm=new GameObject(side<0?"SolKol":"SagKol");arm.transform.SetParent(root.transform,false);arm.transform.localPosition=new Vector3(side*.275f,.98f,0);
            CitizenPart(arm.transform,"Kol",new Vector3(0,-.17f,0),new Vector3(.13f,.34f,.15f),top);
            CitizenPart(arm.transform,"El",new Vector3(0,-.39f,0),new Vector3(.115f,.13f,.13f),face);
            var leg=new GameObject(side<0?"SolBacak":"SagBacak");leg.transform.SetParent(root.transform,false);leg.transform.localPosition=new Vector3(side*.115f,.51f,0);
            CitizenPart(leg.transform,"Bacak",new Vector3(0,-.24f,0),new Vector3(.16f,.48f,.19f),pants);
            CitizenPart(leg.transform,"Ayakkabi",new Vector3(0,-.46f,-.04f),new Vector3(.18f,.10f,.27f),shoe);
        }
        return root;
    }

    /// <summary>Local, compact markers for a damaged road section. Caller owns damage lifetime.</summary>
    public static GameObject CreateConstructionMarkers(Transform parent, Vector3 worldPosition, float width,int variant=0)
    {
        var root=Root(parent,"OnarimIsaretleri",worldPosition);Material material=SolidMaterial(ref markerMaterial,"Ada Onarim Turuncusu",new Color(.94f,.43f,.07f));
        for(int i=0;i<2;i++)
        {
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="OnarimDubasi";marker.transform.SetParent(root.transform,false);
            marker.transform.localPosition=new Vector3((i==0?-1:1)*(width*.5f+.45f),.45f,0);marker.transform.localScale=new Vector3(.3f,.9f,.3f);marker.GetComponent<Renderer>().sharedMaterial=material;Object.Destroy(marker.GetComponent<Collider>());
        }
        var sign=GameObject.CreatePrimitive(PrimitiveType.Cube);sign.name=variant==0?"YolCalismasi":variant==1?"DikkatBozukYol":"Yavasla";sign.transform.SetParent(root.transform,false);
        sign.transform.localPosition=new Vector3(width*.5f+.45f,1.25f,0);sign.transform.localScale=new Vector3(.8f,.65f,.08f);sign.GetComponent<Renderer>().sharedMaterial=material;Object.Destroy(sign.GetComponent<Collider>());
        // White bars, chevrons and a broken-road stripe keep the road-work vocabulary readable without text.
        for(int i=0;i<3;i++)
        {
            var stripe=GameObject.CreatePrimitive(PrimitiveType.Cube);stripe.name="UyariSeridi";stripe.transform.SetParent(root.transform,false);
            stripe.transform.localPosition=new Vector3(width*.5f+.18f+i*.26f,1.25f,-.06f);stripe.transform.localScale=new Vector3(.12f,.53f,.04f);
            stripe.transform.localRotation=Quaternion.Euler(0,0,variant==0?25:variant==1?(i%2==0?30:-30):0);
            stripe.GetComponent<Renderer>().sharedMaterial=SolidMaterial(ref markerStripeMaterial,"Ada Isaret Kremi",new Color(.95f,.90f,.72f));Object.Destroy(stripe.GetComponent<Collider>());
        }
        return root;
    }
}

sealed class IslandEffectLifetime : MonoBehaviour
{
    public float seconds=7;
    void Start(){Destroy(gameObject,seconds);}
}
sealed class IslandFireLightPulse : MonoBehaviour
{
    Light flameLight;
    void Awake(){flameLight=GetComponent<Light>();}
    void Update(){if(flameLight!=null)flameLight.intensity=1.5f+Mathf.PerlinNoise(Time.time*8.1f,3)*.7f;}
}
