using System.Collections;
using UnityEngine;

// Otomatik test aracının "foto:" modu: sahne açıldıktan sonra ekran görüntüsü alıp Play'den çıkar.
// Sonuç: Logs/Ekran_<sahne adı>.png
public class EkranGoruntusu : MonoBehaviour
{
    IEnumerator Start()
    {
        Application.runInBackground = true;
        yield return new WaitForSecondsRealtime(3f);
        yield return new WaitForEndOfFrame();
        string ad = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        System.IO.File.WriteAllBytes("Logs/Ekran_" + ad + ".png", tex.EncodeToPNG());
        Destroy(tex);
        yield return null;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
