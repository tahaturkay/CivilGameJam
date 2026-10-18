using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Small runtime low-poly traffic light with an approach stop line.</summary>
[DisallowMultipleComponent]
public sealed class IslandTrafficSignal : MonoBehaviour
{
    public enum Aspect { Red, Amber, Green }

    private static Material housingMaterial;
    private static Material poleMaterial;
    private static Material redLitMaterial;
    private static Material amberLitMaterial;
    private static Material greenLitMaterial;
    private static Material lampOffMaterial;
    private static Material stopLineMaterial;

    private MeshRenderer redLamp;
    private MeshRenderer amberLamp;
    private MeshRenderer greenLamp;
    private Aspect aspect;
    private bool hasAspect;

    public int RingIndex { get; private set; }
    public int CornerIndex { get; private set; }
    public int DirectionGroup { get; private set; }
    public Aspect CurrentAspect { get { return aspect; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticMaterials()
    {
        DestroyMaterial(housingMaterial);
        DestroyMaterial(poleMaterial);
        DestroyMaterial(redLitMaterial);
        DestroyMaterial(amberLitMaterial);
        DestroyMaterial(greenLitMaterial);
        DestroyMaterial(lampOffMaterial);
        DestroyMaterial(stopLineMaterial);
        housingMaterial = poleMaterial = redLitMaterial = amberLitMaterial = greenLitMaterial = null;
        lampOffMaterial = stopLineMaterial = null;
    }

    private static void DestroyMaterial(Material material)
    {
        if (material == null) return;
        if (Application.isPlaying) Destroy(material);
        else DestroyImmediate(material);
    }

    public void Initialize(int ringIndex, int cornerIndex, int directionGroup,
        Vector3 signalPosition, Vector3 travelDirection, Vector3 stopLinePosition, float groundY)
    {
        RingIndex = ringIndex;
        CornerIndex = cornerIndex;
        DirectionGroup = directionGroup;
        Vector3 forward = Vector3.ProjectOnPlane(travelDirection, Vector3.up).normalized;
        if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 curbOffset = right * 2.65f;
        transform.position = signalPosition + curbOffset;
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

        CreatePrimitive("Direk", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f),
            new Vector3(.12f, 1.5f, .12f), GetPoleMaterial());
        CreatePrimitive("Taban", PrimitiveType.Cube, new Vector3(0f, .12f, 0f),
            new Vector3(.62f, .24f, .62f), GetHousingMaterial());
        CreatePrimitive("Gövde", PrimitiveType.Cube, new Vector3(0f, 2.82f, 0f),
            new Vector3(.5f, 1.12f, .42f), GetHousingMaterial());
        redLamp = CreatePrimitive("Kırmızı", PrimitiveType.Sphere, new Vector3(0f, 3.17f, .225f),
            new Vector3(.27f, .27f, .16f), GetLampOffMaterial());
        amberLamp = CreatePrimitive("Sarı", PrimitiveType.Sphere, new Vector3(0f, 2.82f, .225f),
            new Vector3(.27f, .27f, .16f), GetLampOffMaterial());
        greenLamp = CreatePrimitive("Yeşil", PrimitiveType.Sphere, new Vector3(0f, 2.47f, .225f),
            new Vector3(.27f, .27f, .16f), GetLampOffMaterial());

        Vector3 lineCenter = stopLinePosition + Vector3.up * .055f;
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "DurCizgisi";
        line.transform.SetParent(transform, true);
        line.transform.position = lineCenter;
        line.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        line.transform.localScale = new Vector3(3.1f, .08f, .24f);
        RemoveCollider(line);
        ApplyRenderer(line.GetComponent<MeshRenderer>(), GetStopLineMaterial());
        line.transform.position = lineCenter;
        line.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        SetAspect(Aspect.Red);
    }

    public void SetAspect(Aspect next)
    {
        if (hasAspect && aspect == next && redLamp != null) return;
        aspect = next;
        hasAspect = true;
        if (redLamp == null) return;
        redLamp.sharedMaterial = next == Aspect.Red ? GetRedLitMaterial() : GetLampOffMaterial();
        amberLamp.sharedMaterial = next == Aspect.Amber ? GetAmberLitMaterial() : GetLampOffMaterial();
        greenLamp.sharedMaterial = next == Aspect.Green ? GetGreenLitMaterial() : GetLampOffMaterial();
    }

    private MeshRenderer CreatePrimitive(string partName, PrimitiveType type, Vector3 localPosition,
        Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        RemoveCollider(part);
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        ApplyRenderer(renderer, material);
        return renderer;
    }

    private static void RemoveCollider(GameObject part)
    {
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private static void ApplyRenderer(MeshRenderer renderer, Material material)
    {
        if (renderer == null) return;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material CreateMaterial(string name, Color color, float emission = 0f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name, color = color, enableInstancing = true };
        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        return material;
    }

    private static Material GetHousingMaterial()
    {
        if (housingMaterial == null) housingMaterial = CreateMaterial("Trafik Sinyali - Lacivert", new Color(.12f, .18f, .24f));
        return housingMaterial;
    }

    private static Material GetPoleMaterial()
    {
        if (poleMaterial == null) poleMaterial = CreateMaterial("Trafik Sinyali - Direk", new Color(.34f, .38f, .39f));
        return poleMaterial;
    }

    private static Material GetRedLitMaterial()
    {
        if (redLitMaterial == null) redLitMaterial = CreateMaterial("Trafik Sinyali - Kırmızı", new Color(.96f, .07f, .07f), 1.1f);
        return redLitMaterial;
    }

    private static Material GetAmberLitMaterial()
    {
        if (amberLitMaterial == null) amberLitMaterial = CreateMaterial("Trafik Sinyali - Sarı", new Color(1f, .55f, .04f), 1.1f);
        return amberLitMaterial;
    }

    private static Material GetGreenLitMaterial()
    {
        if (greenLitMaterial == null) greenLitMaterial = CreateMaterial("Trafik Sinyali - Yeşil", new Color(.12f, .86f, .23f), 1.1f);
        return greenLitMaterial;
    }

    private static Material GetLampOffMaterial()
    {
        if (lampOffMaterial == null) lampOffMaterial = CreateMaterial("Trafik Sinyali - Kapalı", new Color(.19f, .2f, .19f));
        return lampOffMaterial;
    }

    private static Material GetStopLineMaterial()
    {
        if (stopLineMaterial == null) stopLineMaterial = CreateMaterial("Trafik Sinyali - Dur Çizgisi", new Color(.92f, .88f, .71f));
        return stopLineMaterial;
    }
}
