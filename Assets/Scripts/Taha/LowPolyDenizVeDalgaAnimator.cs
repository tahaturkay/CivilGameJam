using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class LowPolyDenizVeDalgaAnimator : MonoBehaviour
{
    public enum AnimasyonModu
    {
        Deniz = 0,
        TsunamiDalgasi = 1
    }

    [Header("Animasyon Türü")]
    public AnimasyonModu mod = AnimasyonModu.Deniz;

    [Header("Dalga / Deniz Ayarları")]
    [Tooltip("Dalgalanma hızı")]
    public float dalgaHizi = 2.2f;

    [Tooltip("Dalga yüksekliği çarpanı")]
    public float dalgaYuksekligi = 0.22f;

    [Tooltip("Dalga sıklığı (frekansı)")]
    public float dalgaFrekansi = 1.15f;

    [Tooltip("Normal vektörlerini her karede yeniden hesapla (Low-Poly ışık parlaması için)")]
    public bool normalleriGuncelle = true;

    private MeshFilter meshFilter;
    private Mesh calismaMeshi;
    private Vector3[] baslangicNoktalari;
    private Vector3[] guncelNoktalar;
    bool oceanShader;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        MeshHazirla();
    }

    void OnEnable()
    {
        if (calismaMeshi == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            MeshHazirla();
        }
    }

    void MeshHazirla()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        var surface=GetComponent<Renderer>();
        oceanShader=surface!=null&&surface.sharedMaterial!=null&&surface.sharedMaterial.shader.name=="CivilIsland/Stylized Ocean";
        if (!meshFilter.sharedMesh.isReadable)
        {
            Debug.LogWarning(name + ": Dalga animasyonu için modelin Read/Write ayarını açın.", this);
            enabled = false;
            return;
        }

        // Orijinal FBX meshini bozmamak için çalışma zamanı kopyası oluşturuyoruz
        calismaMeshi = Instantiate(meshFilter.sharedMesh);
        calismaMeshi.name = meshFilter.sharedMesh.name + "_Animated";
        calismaMeshi.MarkDynamic();
        meshFilter.mesh = calismaMeshi;

        baslangicNoktalari = calismaMeshi.vertices;
        guncelNoktalar = new Vector3[baslangicNoktalari.Length];
        System.Array.Copy(baslangicNoktalari, guncelNoktalar, baslangicNoktalari.Length);
    }

    void Update()
    {
        if (calismaMeshi == null || baslangicNoktalari == null) return;

        float t = Time.time * dalgaHizi;
        int n = baslangicNoktalari.Length;

        if (mod == AnimasyonModu.Deniz)
        {
            float f = dalgaFrekansi;
            float amp = dalgaYuksekligi;

            for (int i = 0; i < n; i++)
            {
                Vector3 v = baslangicNoktalari[i];
                float dalga1 = Mathf.Sin(v.x * f + v.z * (f * 0.85f) + t) * amp;
                float dalga2 = Mathf.Cos(v.x * (f * 1.45f) - v.z * (f * 1.25f) - t * 1.3f) * (amp * 0.55f);
                float dalga3 = Mathf.Sin((v.x - v.z) * (f * 2.3f) + t * 1.85f) * (amp * 0.25f);

                v.y = baslangicNoktalari[i].y * 0.35f + dalga1 + dalga2 + dalga3;
                guncelNoktalar[i] = v;
            }
        }
        else // TsunamiDalgasi
        {
            float amp = dalgaYuksekligi;
            float f = dalgaFrekansi * 0.35f;

            for (int i = 0; i < n; i++)
            {
                Vector3 v = baslangicNoktalari[i];

                // Dalga genişliği (X) boyunca ilerleyen kırılma ve köpürme salınımı
                float yanDalga = Mathf.Sin(v.x * f + t * 1.6f) * amp;
                float kopukTitreme = Mathf.Cos(v.x * (f * 2.4f) - t * 2.8f + v.y * 0.5f) * (amp * 0.65f);

                // Üst kırılan tepe (Y > 1.0) ve ön köpük silindiri (Z > 1.5) daha canlı hareket eder
                float tepeEtki = Mathf.Clamp01((v.y + 3.5f) / 8.0f);
                float onKisimEtki = Mathf.Clamp01((v.z + 2.0f) / 6.0f);

                v.y += (yanDalga + kopukTitreme * onKisimEtki) * tepeEtki;
                v.z += kopukTitreme * 0.85f * onKisimEtki * tepeEtki;

                guncelNoktalar[i] = v;
            }
        }

        calismaMeshi.vertices = guncelNoktalar;
        calismaMeshi.RecalculateBounds();
        // The stylized ocean shader does not read normals; other scene shaders retain them.
        if (normalleriGuncelle&&!oceanShader)
        {
            calismaMeshi.RecalculateNormals();
        }
    }

    void OnDestroy()
    {
        if (calismaMeshi != null)
        {
            Destroy(calismaMeshi);
        }
    }
}
