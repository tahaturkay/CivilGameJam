using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared vector-sharp menu primitives. All dimensions are Canvas reference pixels.
public static class CivicUI
{
    public static readonly Color32 Navy = new Color32(24, 43, 55, 255);
    public static readonly Color32 Cream = new Color32(247, 242, 225, 255);
    public static readonly Color32 Teal = new Color32(43, 119, 116, 255);
    public static readonly Color32 Amber = new Color32(235, 179, 77, 255);
    public static readonly Color32 Muted = new Color32(91, 111, 117, 255);
    public static float ReadableScale(Component context)
    {
        Canvas canvas=context.GetComponentInParent<Canvas>();
        return Mathf.Clamp(Screen.height/(1080f*(canvas!=null?canvas.scaleFactor:1)),1,2);
    }
    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    public static void Fill(RectTransform rt, float inset = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one * inset; rt.offsetMax = Vector2.one * -inset;
    }
    public static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one;
    }
    public static Image Box(string name, Transform parent, Color color, bool raycast = false)
    {
        var rt = Rect(name, parent); var img = rt.gameObject.AddComponent<Image>();
        img.color = color; img.raycastTarget = raycast; return img;
    }
    public static TMP_Text Text(string name, Transform parent, string value, float size, Color color, TMP_FontAsset font = null)
    {
        var rt = Rect(name, parent); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = TurkishTMPFontSupport.EnsureFont(font != null ? font : TMP_Settings.defaultFontAsset);
        t.text = value; t.fontSize = size; t.color = color; t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal; t.alignment = TextAlignmentOptions.MidlineLeft;
        t.overflowMode = TextOverflowModes.Overflow; return t;
    }
    public static Button Button(string name, Transform parent, string label, Color fill, Color ink, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
    {
        var img = Box(name, parent, fill, true); var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors; colors.highlightedColor = new Color(.88f, .94f, .93f); colors.pressedColor = new Color(.76f, .85f, .83f);
        colors.disabledColor = new Color(.63f, .66f, .64f, .7f); b.colors = colors;
        var txt = Text("Label", b.transform, label, 24, ink, font); Fill(txt.rectTransform, 12);
        txt.fontStyle = FontStyles.Bold; txt.alignment = TextAlignmentOptions.Center;
        if (action != null) b.onClick.AddListener(action); return b;
    }
    public static void ReskinButton(Button button,Color fill,Color ink)
    {
        if(button==null)return;var image=button.GetComponent<Image>();
        if(image!=null){image.sprite=null;image.color=fill;button.targetGraphic=image;}
        var label=button.GetComponentInChildren<TMP_Text>(true);
        foreach(Transform child in button.transform)if(label==null||!label.transform.IsChildOf(child)&&label.transform!=child)child.gameObject.SetActive(false);
        if(label!=null){label.gameObject.SetActive(true);label.color=ink;label.fontStyle=FontStyles.Bold;label.fontSize=24;Fill(label.rectTransform,8);}
    }
    public static void FitCard(RectTransform card, Vector2 size, float margin = 36, float maxScale = 1)
    {
        var parent = card.parent as RectTransform; if (parent == null) return;
        Canvas canvas = card.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.min, cam, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.max, cam, out Vector2 max);
        float scale = Mathf.Min(maxScale, Mathf.Min((max.x-min.x-margin*2)/size.x, (max.y-min.y-margin*2)/size.y));
        card.anchorMin = card.anchorMax = card.pivot = Vector2.one * .5f;
        card.sizeDelta = size; card.localScale = Vector3.one * Mathf.Max(.1f, scale);
        card.anchoredPosition = (min+max)*.5f;
    }
}
