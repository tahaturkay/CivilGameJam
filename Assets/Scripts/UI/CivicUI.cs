using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Polished pixel-art arcade UI system and primitives.
/// Unified aesthetic matching Screenshot 2: stepped rounded cards, ribbon banners,
/// chunky two-tone arcade buttons, 3D star plaques, capsule badges, and crisp vector icons.
/// </summary>
public static class CivicUI
{
    public enum IconType
    {
        Person,
        HexCoin,
        Book,
        Calendar,
        Star,
        UpArrow,
        Play,
        CloseX,
        Gear,
        Guide,
        Shield,
        Clock,
        Heart,
        Refresh,
        Home,
        MenuGrid,
        Volume,
        Checkmark,
        Exit
    }

    // --- COHESIVE ARCADE PALETTE (SCREENSHOT 2 STYLE) ---
    public static readonly Color32 DarkInk         = new Color32(18, 27, 40, 255);    // #121B28
    public static readonly Color32 Navy            = DarkInk;
    public static readonly Color32 Cream           = new Color32(255, 248, 234, 255); // #FFF8EA
    public static readonly Color32 CreamShadow     = new Color32(226, 210, 176, 255); // #E2D2B0
    public static readonly Color32 Mint            = new Color32(232, 247, 245, 255); // #E8F7F5 (Image 2 style soft cyan/mint)
    public static readonly Color32 MintShadow     = new Color32(184, 218, 214, 255); // #B8DAD6
    public static readonly Color32 DarkSlotBg      = new Color32(24, 38, 53, 255);    // #182635
    public static readonly Color32 DarkSlotBorder  = new Color32(14, 23, 34, 255);    // #0E1722

    // Ribbon Banners
    public static readonly Color32 RibbonBlue      = new Color32(43, 148, 229, 255);  // #2B94E5
    public static readonly Color32 RibbonBlueLip   = new Color32(24, 95, 150, 255);   // #185F96
    public static readonly Color32 RibbonBlueTail  = new Color32(18, 75, 120, 255);   // #124B78

    public static readonly Color32 RibbonGreen     = new Color32(63, 212, 127, 255);  // #3FD47F
    public static readonly Color32 RibbonGreenLip  = new Color32(28, 138, 74, 255);   // #1C8A4A
    public static readonly Color32 RibbonGreenTail = new Color32(20, 105, 56, 255);   // #146938

    public static readonly Color32 RibbonGold      = new Color32(255, 184, 46, 255);  // #FFB82E
    public static readonly Color32 RibbonGoldLip   = new Color32(186, 120, 20, 255);  // #BA7814
    public static readonly Color32 RibbonGoldTail  = new Color32(145, 92, 14, 255);   // #915C0E

    public static readonly Color32 RibbonRed       = new Color32(232, 74, 85, 255);   // #E84A55
    public static readonly Color32 RibbonRedLip    = new Color32(154, 36, 46, 255);   // #9A242E
    public static readonly Color32 RibbonRedTail   = new Color32(118, 26, 34, 255);   // #761A22

    // Buttons
    public static readonly Color32 GoldBtn         = new Color32(255, 184, 46, 255);  // #FFB82E
    public static readonly Color32 GoldBtnLeft     = new Color32(226, 152, 22, 255);  // #E29816
    public static readonly Color32 GoldBtnDark     = new Color32(179, 111, 10, 255);  // #B36F0A
    public static readonly Color32 Amber           = GoldBtn;

    public static readonly Color32 GreenBtn        = new Color32(63, 212, 127, 255);  // #3FD47F
    public static readonly Color32 GreenBtnLeft    = new Color32(39, 176, 97, 255);   // #27B061
    public static readonly Color32 GreenBtnDark    = new Color32(22, 120, 63, 255);   // #16783F
    public static readonly Color32 Teal            = GreenBtn;

    public static readonly Color32 CyanBtn         = new Color32(56, 180, 242, 255);  // #38B4F2
    public static readonly Color32 CyanBtnLeft     = new Color32(30, 147, 209, 255);  // #1E93D1
    public static readonly Color32 CyanBtnDark     = new Color32(19, 104, 150, 255);  // #136896

    public static readonly Color32 PurpleBtn       = new Color32(168, 108, 228, 255); // #A86CE4
    public static readonly Color32 PurpleBtnLeft   = new Color32(136, 72, 201, 255);  // #8848C9
    public static readonly Color32 PurpleBtnDark   = new Color32(89, 39, 138, 255);   // #59278A

    public static readonly Color32 CoralBtn        = new Color32(240, 77, 90, 255);   // #F04D5A
    public static readonly Color32 CoralBtnLeft    = new Color32(207, 47, 60, 255);   // #CF2F3C
    public static readonly Color32 CoralBtnDark    = new Color32(140, 24, 34, 255);   // #8C1822

    // Compatibility names used by current report, menu, and settings callers.
    // Keep the original palette constants above as the source of truth.
    public static readonly Color32 Green           = GreenBtn;
    public static readonly Color32 GreenLeft       = GreenBtnLeft;
    public static readonly Color32 GreenShadow     = GreenBtnDark;
    public static readonly Color32 Coral           = CoralBtn;
    public static readonly Color32 CoralDark       = CoralBtnDark;
    public static readonly Color32 Cyan            = CyanBtn;
    public static readonly Color32 CyanLeft        = CyanBtnLeft;
    public static readonly Color32 CyanShadow      = CyanBtnDark;
    public static readonly Color32 NavyLeft        = new Color32(34, 49, 66, 255);
    public static readonly Color32 NavyShadow      = new Color32(8, 14, 22, 255);
    public static readonly Color32 RibbonCyan      = CyanBtn;
    public static readonly Color32 RibbonCyanLip   = CyanBtnLeft;
    public static readonly Color32 RibbonCyanTail  = CyanBtnDark;
    public static readonly Color32 TealShadow      = GreenBtnDark;
    public static readonly Color32 AmberShadow     = GoldBtnDark;

    public static readonly Color32 SlateBtn        = new Color32(82, 104, 122, 255);  // #52687A
    public static readonly Color32 SlateBtnLeft    = new Color32(60, 78, 92, 255);    // #3C4E5C
    public static readonly Color32 SlateBtnDark    = new Color32(42, 55, 66, 255);    // #2A3742
    public static readonly Color32 Muted           = new Color32(110, 124, 135, 255); // #6E7C87

    public static readonly Color32 GoldStar        = new Color32(255, 214, 64, 255);
    public static readonly Color32 GoldStarDark    = new Color32(196, 135, 18, 255);

    // =========================================================================
    // PROCEDURAL PIXEL-ART SPRITE GENERATORS
    // =========================================================================
    private static readonly Dictionary<ulong, Sprite> cardCache = new Dictionary<ulong, Sprite>();
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static Sprite solidWhiteSprite;
    private static Sprite darkSlotSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCache()
    {
        cardCache.Clear();
        spriteCache.Clear();
        solidWhiteSprite = null;
        darkSlotSprite = null;
    }

    private static ulong ColorKey(Color32 a, Color32 b)
    {
        return ((ulong)a.r << 56) | ((ulong)a.g << 48) | ((ulong)a.b << 40) | ((ulong)a.a << 32)
             | ((ulong)b.r << 24) | ((ulong)b.g << 16) | ((ulong)b.b << 8) | b.a;
    }

    public static Sprite GetSolidWhiteSprite()
    {
        if (solidWhiteSprite != null) return solidWhiteSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
        tex.Apply();
        solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return solidWhiteSprite;
    }

    // 1) STEPPED ROUNDED ARCADE CARD (Image 2 Card Style)
    // 64x64 with stepped corners, outer dark ink border, inner top highlight, and bottom shadow bevel lip.
    public static Sprite GetBeveledCardSprite(Color32 fillColor, Color32 shadowColor)
    {
        ulong key = ColorKey(fillColor, shadowColor);
        if (cardCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int size = 64;
        int border = 3;
        int shadowHeight = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);

                // Cut rounded corner steps
                bool cornerCut = (dx == 0 && dy < 6) || (dy == 0 && dx < 6) ||
                                 (dx == 1 && dy < 3) || (dy == 1 && dx < 3) ||
                                 (dx < 3 && dy < 3 && dx + dy < 4);
                if (cornerCut)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Outer dark border
                bool isBorder = (dx < border) || (dy < border) ||
                                (dx < border + 2 && dy < border + 2 && dx + dy < border + 4);
                if (isBorder)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (y < border + shadowHeight)
                {
                    // Bottom 3D shadow bevel lip
                    tex.SetPixel(x, y, shadowColor);
                }
                else if (y >= size - border - 2 && dx >= border + 2)
                {
                    // Subtle top highlight line for juicy arcade emboss
                    tex.SetPixel(x, y, Color.Lerp(fillColor, Color.white, 0.55f));
                }
                else
                {
                    // Main card body
                    tex.SetPixel(x, y, fillColor);
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        cardCache[key] = sp;
        return sp;
    }

    // 2) RECESSED DARK SLOT SPRITE (#182635)
    public static Sprite GetDarkSlotSprite()
    {
        if (darkSlotSprite != null) return darkSlotSprite;

        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);

                if ((dx < 3 && dy < 3 && dx + dy < 3))
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dx < 3 || dy < 3 || (dx < 5 && dy < 5 && dx + dy < 6))
                {
                    tex.SetPixel(x, y, DarkSlotBorder);
                }
                else if (y >= size - 5 && dx >= 4)
                {
                    // Subtle top inner highlight
                    tex.SetPixel(x, y, new Color32(37, 56, 77, 255));
                }
                else
                {
                    tex.SetPixel(x, y, DarkSlotBg);
                }
            }
        }

        tex.Apply();
        darkSlotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        return darkSlotSprite;
    }

    // 3) OVERLAPPING RIBBON BANNER SPRITES (MAIN & NOTCHED TAILS)
    public static Sprite GetRibbonMainSprite(Color32 fill, Color32 bottomLip)
    {
        string key = "ribbon_main_" + ColorKey(fill, bottomLip);
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int w = 48, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 topHighlight = Color.Lerp(fill, Color.white, 0.35f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = y < 2 || y >= h - 2 || x < 2 || x >= w - 2;
                if (isBorder)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (y < 9)
                {
                    tex.SetPixel(x, y, bottomLip);
                }
                else if (y >= h - 5)
                {
                    tex.SetPixel(x, y, topHighlight);
                }
                else
                {
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 10, 8, 8));
        spriteCache[key] = sp;
        return sp;
    }

    public static Sprite GetRibbonTailSprite(bool leftSide, Color32 mainCol, Color32 lipCol, Color32 undersideShadow)
    {
        string key = "ribbon_tail_" + (leftSide ? "L_" : "R_") +
            ColorKey(mainCol, lipCol) + "_" + ColorKey(undersideShadow, undersideShadow);
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int w = 36, h = 46;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / h;
            // V-notch cut on the outer edge
            float notchDepth = (0.5f - Mathf.Abs(ny - 0.5f)) * 0.72f;

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                bool outside = leftSide ? (nx < notchDepth) : (nx > 1f - notchDepth);

                if (outside)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool innerFold = leftSide ? (x >= w - 6) : (x < 6);
                if (innerFold)
                {
                    tex.SetPixel(x, y, undersideShadow);
                }
                else if (y < 4 || y >= h - 4 || (leftSide && Mathf.Abs(nx - notchDepth) < 0.08f) || (!leftSide && Mathf.Abs((1f - nx) - notchDepth) < 0.08f))
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (y < 10)
                {
                    tex.SetPixel(x, y, lipCol);
                }
                else
                {
                    tex.SetPixel(x, y, mainCol);
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        spriteCache[key] = sp;
        return sp;
    }

    public static Sprite GetRibbonTailSprite(bool leftSide, Color32 ribbonColor)
    {
        Color32 main, lip, tail;
        ResolveRibbonColors(ribbonColor, out main, out lip, out tail);
        return GetRibbonTailSprite(leftSide, main, lip, tail);
    }

    // Older result-card code passes the color before the side flag.
    public static Sprite GetRibbonTailSprite(Color32 ribbonColor, bool leftSide)
    {
        return GetRibbonTailSprite(leftSide, ribbonColor);
    }

    private static void ResolveRibbonColors(Color32 color, out Color32 main, out Color32 lip, out Color32 tail)
    {
        if (color.Equals(RibbonGoldTail) || color.Equals(RibbonGold))
        {
            main = RibbonGold;
            lip = RibbonGoldLip;
            tail = RibbonGoldTail;
        }
        else if (color.Equals(RibbonRedTail) || color.Equals(RibbonRed))
        {
            main = RibbonRed;
            lip = RibbonRedLip;
            tail = RibbonRedTail;
        }
        else if (color.Equals(RibbonGreenTail) || color.Equals(RibbonGreen))
        {
            main = RibbonGreen;
            lip = RibbonGreenLip;
            tail = RibbonGreenTail;
        }
        else if (color.Equals(RibbonCyanTail) || color.Equals(RibbonCyan))
        {
            main = RibbonCyan;
            lip = RibbonCyanLip;
            tail = RibbonCyanTail;
        }
        else if (color.Equals(RibbonBlueTail) || color.Equals(RibbonBlue))
        {
            main = RibbonBlue;
            lip = RibbonBlueLip;
            tail = RibbonBlueTail;
        }
        else
        {
            main = color;
            lip = Color.Lerp(color, DarkInk, .24f);
            tail = Color.Lerp(color, DarkInk, .42f);
        }
    }

    // 4) TWO-TONE ARCADE BUTTON SPRITE (Image 2 style PLAY / MULTIPLAYER buttons)
    // Left side has recessed icon container with divider, right side has vibrant fill, bottom has 3D shadow lip.
    public static Sprite GetTwoToneButtonSprite(Color32 mainFill, Color32 leftIconFill, Color32 shadowColor)
    {
        string key = "two_tone_btn_" + ColorKey(mainFill, leftIconFill) + "_" + ColorKey(shadowColor, shadowColor);
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int w = 128, h = 54;
        int leftWidth = 46;
        int border = 3;
        int shadowHeight = 7;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);

                bool cornerCut = (dx < 3 && dy < 3 && dx + dy < 4);
                if (cornerCut)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isOuterBorder = dx < border || dy < border;
                bool isDivider = (x >= leftWidth - 2 && x <= leftWidth);

                if (y < shadowHeight)
                {
                    // Bottom 3D shadow bevel lip
                    tex.SetPixel(x, y, isOuterBorder ? DarkInk : shadowColor);
                }
                else if (isOuterBorder || isDivider)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (x < leftWidth)
                {
                    // Left recessed icon compartment with subtle top highlight
                    if (y >= h - border - 2)
                        tex.SetPixel(x, y, Color.Lerp(leftIconFill, Color.white, 0.35f));
                    else
                        tex.SetPixel(x, y, leftIconFill);
                }
                else
                {
                    // Right text compartment with subtle top highlight
                    if (y >= h - border - 2)
                        tex.SetPixel(x, y, Color.Lerp(mainFill, Color.white, 0.40f));
                    else
                        tex.SetPixel(x, y, mainFill);
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(50, 14, 12, 10));
        spriteCache[key] = sp;
        return sp;
    }

    // 5) ROUNDED SQUARE ARCADE BUTTON (Image 2 corner buttons & speed buttons)
    public static Sprite GetSquareButtonSprite(Color32 fill, Color32 shadowColor)
    {
        string key = "sq_btn_" + ColorKey(fill, shadowColor);
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int size = 52;
        int border = 3;
        int shadowHeight = 6;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);

                if (dx < 3 && dy < 3 && dx + dy < 4)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isBorder = dx < border || dy < border;
                if (y < shadowHeight)
                {
                    tex.SetPixel(x, y, isBorder ? DarkInk : shadowColor);
                }
                else if (isBorder)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (y >= size - border - 2)
                {
                    tex.SetPixel(x, y, Color.Lerp(fill, Color.white, 0.45f));
                }
                else
                {
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        spriteCache[key] = sp;
        return sp;
    }

    // 6) CAPSULE PILL BADGE (Image 2 "BEST SCORE" pill tag)
    public static Sprite GetPillBadgeSprite(Color32 fill, Color32 shadowColor)
    {
        string key = "pill_badge_" + ColorKey(fill, shadowColor);
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int w = 48, h = 32;
        int border = 2;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Min(x, w - 1 - x);
                int dy = Mathf.Min(y, h - 1 - y);

                if (dx < 4 && dy < 4 && dx + dy < 5)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isBorder = dx < border || dy < border;
                if (y < 5)
                {
                    tex.SetPixel(x, y, isBorder ? DarkInk : shadowColor);
                }
                else if (isBorder)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (y >= h - border - 2)
                {
                    tex.SetPixel(x, y, Color.Lerp(fill, Color.white, 0.40f));
                }
                else
                {
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 8, 10, 8));
        spriteCache[key] = sp;
        return sp;
    }

    // 7) 3D GOLDEN STAR PLAQUE (Image 2 Right Panel Star Badges!)
    public static Sprite GetStarPlaqueSprite(bool achieved)
    {
        string key = "star_plaque_" + achieved;
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int size = 56;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 frameCol   = achieved ? GoldBtn : new Color32(115, 130, 142, 255);
        Color32 frameHigh  = achieved ? new Color32(255, 230, 120, 255) : new Color32(165, 180, 192, 255);
        Color32 frameDark  = achieved ? GoldBtnDark : new Color32(75, 88, 98, 255);
        Color32 slotCol    = achieved ? new Color32(196, 130, 14, 255) : new Color32(50, 60, 68, 255);
        Color32 starCol    = achieved ? new Color32(255, 235, 90, 255) : new Color32(95, 108, 118, 255);
        Color32 starDark   = achieved ? new Color32(180, 105, 10, 255) : new Color32(40, 50, 58, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);

                if (dx < 4 && dy < 4 && dx + dy < 5)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isOuterBorder = dx < 3 || dy < 3;
                bool isInnerBorder = dx >= 7 && dx <= 8 && dy >= 7 && dy <= size - 8 ||
                                     dy >= 7 && dy <= 8 && dx >= 7 && dx <= size - 8;

                if (isOuterBorder || isInnerBorder)
                {
                    tex.SetPixel(x, y, DarkInk);
                }
                else if (dx < 7 || dy < 7)
                {
                    // Plaque frame bevel
                    if (y < 6) tex.SetPixel(x, y, frameDark);
                    else if (y >= size - 5) tex.SetPixel(x, y, frameHigh);
                    else tex.SetPixel(x, y, frameCol);
                }
                else
                {
                    // Center recessed area with 5-point star
                    float nx = (x + 0.5f) / size;
                    float ny = (y + 0.5f) / size;
                    bool inStarOuter = PointInStar(nx, ny, 0.50f, 0.49f, 0.28f, 0.12f);
                    bool inStarInner = PointInStar(nx, ny, 0.50f, 0.49f, 0.23f, 0.09f);

                    if (inStarInner)
                    {
                        // Star highlight on top-left
                        bool highlight = nx <= 0.50f && ny >= 0.45f;
                        tex.SetPixel(x, y, highlight ? Color.Lerp(starCol, Color.white, 0.45f) : starCol);
                    }
                    else if (inStarOuter)
                    {
                        tex.SetPixel(x, y, starDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, slotCol);
                    }
                }
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        spriteCache[key] = sp;
        return sp;
    }

    private static bool PointInStar(float px, float py, float cx, float cy, float rOuter, float rInner)
    {
        const int points = 10;
        bool inside = false;
        float prevX = 0f, prevY = 0f;
        for (int i = 0; i <= points; i++)
        {
            float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0) ? rOuter : rInner;
            float x = cx + Mathf.Cos(angle) * r;
            float y = cy + Mathf.Sin(angle) * r;
            if (i > 0 && ((y > py) != (prevY > py)) && (px < (prevX - x) * (py - y) / (prevY - y) + x))
                inside = !inside;
            prevX = x; prevY = y;
        }
        return inside;
    }

    // 8) VECTOR / PIXEL ICONS GENERATOR
    public static Sprite GetColoredIconSprite(IconType type, Color32 color)
    {
        string key = "icon_" + (int)type + "_" + ColorKey(color, color);
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                tex.SetPixel(x, y, RenderIconPixel(type, nx, ny, color));
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        spriteCache[key] = sp;
        return sp;
    }

    private static Color RenderIconPixel(IconType type, float x, float y, Color32 col)
    {
        switch (type)
        {
            case IconType.Person:
                float dHead = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.70f));
                float dBody = Vector2.Distance(new Vector2(x, (y - 0.14f) * 1.15f + 0.14f), new Vector2(0.5f, 0.14f));
                if (dHead <= 0.14f || (y >= 0.20f && y <= 0.45f && dBody <= 0.29f)) return col;
                if (dHead <= 0.21f || (y >= 0.14f && y <= 0.46f && dBody <= 0.36f)) return DarkInk;
                return Color.clear;

            case IconType.HexCoin:
                float dx = Mathf.Abs(x - 0.5f);
                float dy = Mathf.Abs(y - 0.5f);
                float hexDist = Mathf.Max(dx * 0.866f + dy * 0.5f, dy);
                if (hexDist > 0.42f) return Color.clear;
                if (hexDist > 0.34f) return DarkInk;
                if (hexDist > 0.26f) return Color.Lerp(col, DarkInk, 0.25f);
                return col;

            case IconType.Book:
                if (x < 0.10f || x > 0.90f || y < 0.18f || y > 0.82f) return Color.clear;
                bool bookBorder = x < 0.16f || x > 0.84f || y < 0.24f || y > 0.76f || Mathf.Abs(x - 0.5f) < 0.05f;
                if (bookBorder) return DarkInk;
                bool lines = (Mathf.Abs(y - 0.60f) < 0.035f || Mathf.Abs(y - 0.40f) < 0.035f) &&
                             ((x >= 0.23f && x <= 0.43f) || (x >= 0.57f && x <= 0.77f));
                return lines ? (Color)col : Color.white;

            case IconType.Calendar:
                if (x < 0.12f || x > 0.88f || y < 0.12f || y > 0.86f) return Color.clear;
                if (x < 0.18f || x > 0.82f || y < 0.18f || y > 0.80f || (y > 0.58f && y < 0.65f)) return DarkInk;
                if (y >= 0.65f) return col;
                return Color.white;

            case IconType.Star:
                if (PointInStar(x, y, 0.5f, 0.48f, 0.48f, 0.20f))
                    return PointInStar(x, y, 0.5f, 0.48f, 0.38f, 0.16f) ? (Color)col : (Color)DarkInk;
                return Color.clear;

            case IconType.Play:
                bool triOuter = x >= 0.22f && x <= 0.82f && Mathf.Abs(y - 0.5f) <= (0.82f - x) * 0.72f;
                bool triInner = x >= 0.28f && x <= 0.76f && Mathf.Abs(y - 0.5f) <= (0.76f - x) * 0.62f;
                if (triInner) return col;
                if (triOuter) return DarkInk;
                return Color.clear;

            case IconType.Gear:
                float dCenter = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
                if (dCenter <= 0.12f) return Color.clear;
                if (dCenter <= 0.17f) return DarkInk;
                float angle = Mathf.Atan2(y - 0.5f, x - 0.5f);
                float tooth = Mathf.Sin(angle * 8f);
                float rOuter = 0.38f + tooth * 0.08f;
                if (dCenter <= rOuter) return col;
                if (dCenter <= rOuter + 0.06f) return DarkInk;
                return Color.clear;

            case IconType.Guide:
                float dBulb = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.62f));
                bool stemBulb = y >= 0.20f && y <= 0.45f && Mathf.Abs(x - 0.5f) <= 0.16f;
                bool baseBulb = y >= 0.14f && y <= 0.24f && Mathf.Abs(x - 0.5f) <= 0.10f;
                if (dBulb <= 0.24f || stemBulb || baseBulb) return col;
                if (dBulb <= 0.30f || (y >= 0.16f && y <= 0.48f && Mathf.Abs(x - 0.5f) <= 0.22f)) return DarkInk;
                return Color.clear;

            case IconType.Shield:
                bool inShieldOuter = y >= 0.14f && y <= 0.84f && Mathf.Abs(x - 0.5f) <= 0.38f * (1f - Mathf.Pow(Mathf.Clamp01((0.55f - y) / 0.45f), 2f));
                bool inShieldInner = y >= 0.20f && y <= 0.80f && Mathf.Abs(x - 0.5f) <= 0.32f * (1f - Mathf.Pow(Mathf.Clamp01((0.55f - y) / 0.45f), 2f));
                if (inShieldInner) return col;
                if (inShieldOuter) return DarkInk;
                return Color.clear;

            case IconType.Clock:
                float dClock = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.46f));
                bool pin = y >= 0.80f && y <= 0.88f && Mathf.Abs(x - 0.5f) <= 0.10f;
                if (pin || (dClock <= 0.38f && dClock >= 0.28f)) return DarkInk;
                if (dClock < 0.28f)
                {
                    bool hand1 = x >= 0.47f && x <= 0.53f && y >= 0.46f && y <= 0.68f;
                    bool hand2 = y >= 0.44f && y <= 0.50f && x >= 0.50f && x <= 0.68f;
                    return (hand1 || hand2) ? (Color)DarkInk : Color.white;
                }
                return Color.clear;

            case IconType.Heart:
                float hx = (x - 0.5f) * 1.8f;
                float hy = (y - 0.55f) * 1.8f;
                float heartEq = Mathf.Pow(hx * hx + hy * hy - 1f, 3f) - hx * hx * hy * hy * hy;
                if (heartEq <= 0f) return col;
                if (heartEq <= 0.45f) return DarkInk;
                return Color.clear;

            case IconType.Refresh:
                float dRing = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
                if (dRing <= 0.40f && dRing >= 0.24f)
                {
                    bool cutGap = x >= 0.40f && y >= 0.50f;
                    if (!cutGap) return col;
                }
                bool arr = x >= 0.48f && x <= 0.84f && y >= 0.58f && y <= 0.88f && (x - 0.48f) + (y - 0.58f) <= 0.35f;
                if (arr) return col;
                return Color.clear;

            case IconType.Home:
                bool roof = y >= 0.48f && y <= 0.85f && Mathf.Abs(x - 0.5f) <= (0.85f - y) * 1.15f;
                bool body = y >= 0.16f && y <= 0.52f && Mathf.Abs(x - 0.5f) <= 0.28f;
                bool door = y >= 0.16f && y <= 0.36f && Mathf.Abs(x - 0.5f) <= 0.10f;
                if (door) return DarkInk;
                if (roof || body) return col;
                return Color.clear;

            case IconType.MenuGrid:
                int gx = Mathf.FloorToInt(x * 3f);
                int gy = Mathf.FloorToInt(y * 3f);
                float lox = x * 3f - gx;
                float loy = y * 3f - gy;
                bool inDot = lox >= 0.20f && lox <= 0.80f && loy >= 0.20f && loy <= 0.80f;
                return inDot ? (Color)col : Color.clear;

            case IconType.CloseX:
                float d1 = Mathf.Abs((x - 0.5f) - (y - 0.5f));
                float d2 = Mathf.Abs((x - 0.5f) + (y - 0.5f));
                if (x >= 0.18f && x <= 0.82f && y >= 0.18f && y <= 0.82f && (d1 <= 0.12f || d2 <= 0.12f))
                    return col;
                return Color.clear;

            case IconType.UpArrow:
                bool head = y >= 0.50f && y <= 0.86f && Mathf.Abs(x - 0.5f) <= (0.86f - y) * 0.95f;
                bool stem = y >= 0.16f && y <= 0.52f && Mathf.Abs(x - 0.5f) <= 0.13f;
                return (head || stem) ? (Color)col : Color.clear;

            case IconType.Volume:
                bool speakerBody = x >= 0.15f && x <= 0.35f && y >= 0.32f && y <= 0.68f;
                bool speakerCone = x >= 0.30f && x <= 0.55f && Mathf.Abs(y - 0.5f) <= (x - 0.25f) * 0.70f;
                float dWave = Vector2.Distance(new Vector2(x, y), new Vector2(0.40f, 0.5f));
                bool wave1 = x >= 0.58f && Mathf.Abs(dWave - 0.26f) < 0.05f && Mathf.Abs(y - 0.5f) <= 0.25f;
                bool wave2 = x >= 0.68f && Mathf.Abs(dWave - 0.40f) < 0.05f && Mathf.Abs(y - 0.5f) <= 0.38f;
                if (speakerBody || speakerCone || wave1 || wave2) return col;
                return Color.clear;

            case IconType.Exit:
                bool doorFrame = x >= 0.14f && x <= 0.62f && (y >= 0.78f || y <= 0.22f || x <= 0.24f);
                bool arrowHead = x >= 0.55f && x <= 0.86f && Mathf.Abs(y - 0.5f) <= (0.86f - x) * 0.75f;
                bool arrowStem = x >= 0.35f && x <= 0.75f && Mathf.Abs(y - 0.5f) <= 0.10f;
                if (doorFrame || arrowHead || arrowStem) return col;
                return Color.clear;

            case IconType.Checkmark:
                bool line1 = x >= 0.20f && x <= 0.45f && Mathf.Abs((x - 0.20f) - (0.50f - y)) < 0.09f;
                bool line2 = x >= 0.42f && x <= 0.85f && Mathf.Abs((x - 0.42f) - (y - 0.25f)) < 0.09f;
                if (line1 || line2) return col;
                return Color.clear;
        }

        return col;
    }

    // =========================================================================
    // UI COMPONENT FACTORY HELPERS
    // =========================================================================

    public static float ReadableScale(Component context)
    {
        Canvas canvas = context.GetComponentInParent<Canvas>();
        return Mathf.Clamp(Screen.height / (1080f * (canvas != null ? canvas.scaleFactor : 1f)), 1f, 2f);
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Fill(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one * inset; rt.offsetMax = Vector2.one * -inset;
    }

    public static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one;
    }

    public static Image Box(string name, Transform parent, Color color, bool raycast = false)
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = GetBeveledCardSprite(color, Color.Lerp(color, DarkInk, 0.25f));
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.raycastTarget = raycast;
        return img;
    }

    public static Image RawBox(string name, Transform parent, Color color, bool raycast = false)
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static TMP_Text Text(string name, Transform parent, string value, float size, Color color, TMP_FontAsset font = null)
    {
        var rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = TurkishTMPFontSupport.EnsureFont(font != null ? font : TMP_Settings.defaultFontAsset);
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    // Standard / Reskinned Button (Fully upgraded to arcade look)
    public static Button Button(string name, Transform parent, string label, Color fill, Color ink, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
    {
        Color32 shadow = Color.Lerp(fill, DarkInk, 0.35f);
        var img = Box(name, parent, fill, true);
        img.sprite = GetSquareButtonSprite(fill, shadow);
        img.type = Image.Type.Sliced;

        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;

        var colors = b.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        b.colors = colors;

        var txt = Text("Label", b.transform, label, 22, ink, font);
        Fill(txt.rectTransform, 8);
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;

        if (action != null) b.onClick.AddListener(action);
        return b;
    }

    public static void ReskinButton(Button button, Color fill, Color ink)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            Color32 shadow = Color.Lerp(fill, DarkInk, 0.35f);
            image.sprite = GetSquareButtonSprite(fill, shadow);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            button.targetGraphic = image;
        }
        var label = button.GetComponentInChildren<TMP_Text>(true);
        foreach (Transform child in button.transform)
            if (label == null || (!label.transform.IsChildOf(child) && label.transform != child))
                child.gameObject.SetActive(false);
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.color = ink;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 22;
            Fill(label.rectTransform, 6);
        }
    }

    // --- SCREENSHOT 2 SIGNATURE COMPONENTS ---

    // 1) Overlapping Ribbon Banner
    public static RectTransform CreateRibbon(
        RectTransform card, string title,
        Color32 mainColor, Color32 lipColor, Color32 tailColor,
        TMP_FontAsset font = null, float height = 66f, float extraWidth = 48f)
    {
        GameObject ribbonObj = new GameObject("RibbonBanner", typeof(RectTransform), typeof(Image));
        ribbonObj.transform.SetParent(card, false);
        RectTransform ribbonRt = ribbonObj.GetComponent<RectTransform>();
        ribbonRt.anchorMin = new Vector2(0f, 1f);
        ribbonRt.anchorMax = new Vector2(1f, 1f);
        ribbonRt.pivot = new Vector2(0.5f, 1f);
        ribbonRt.anchoredPosition = new Vector2(0f, 16f); // Hang slightly above top edge
        ribbonRt.sizeDelta = new Vector2(extraWidth, height);

        // Left Tail
        GameObject tailL = new GameObject("TailLeft", typeof(RectTransform), typeof(Image));
        tailL.transform.SetParent(ribbonRt, false);
        RectTransform tlRt = tailL.GetComponent<RectTransform>();
        tlRt.anchorMin = new Vector2(0f, 0f);
        tlRt.anchorMax = new Vector2(0f, 0f);
        tlRt.pivot = new Vector2(0f, 0f);
        tlRt.anchoredPosition = new Vector2(-22f, -14f);
        tlRt.sizeDelta = new Vector2(36f, height - 12f);
        Image imgL = tailL.GetComponent<Image>();
        imgL.sprite = GetRibbonTailSprite(true, mainColor, lipColor, tailColor);
        imgL.raycastTarget = false;

        // Right Tail
        GameObject tailR = new GameObject("TailRight", typeof(RectTransform), typeof(Image));
        tailR.transform.SetParent(ribbonRt, false);
        RectTransform trRt = tailR.GetComponent<RectTransform>();
        trRt.anchorMin = new Vector2(1f, 0f);
        trRt.anchorMax = new Vector2(1f, 0f);
        trRt.pivot = new Vector2(1f, 0f);
        trRt.anchoredPosition = new Vector2(22f, -14f);
        trRt.sizeDelta = new Vector2(36f, height - 12f);
        Image imgR = tailR.GetComponent<Image>();
        imgR.sprite = GetRibbonTailSprite(false, mainColor, lipColor, tailColor);
        imgR.raycastTarget = false;

        // Main Ribbon Center
        Image mainImg = ribbonObj.GetComponent<Image>();
        mainImg.sprite = GetRibbonMainSprite(mainColor, lipColor);
        mainImg.type = Image.Type.Sliced;
        mainImg.color = Color.white;
        mainImg.raycastTarget = false;

        // Ribbon Text
        TMP_Text txt = Text("RibbonTitle", ribbonRt, title, 28f, Color.white, TurkishTMPFontSupport.DisplayFont);
        txt.fontStyle = FontStyles.Normal;
        txt.enableAutoSizing=true;txt.fontSizeMin=20;txt.fontSizeMax=28;
        txt.alignment = TextAlignmentOptions.Center;
        Fill(txt.rectTransform, 8f);
        txt.rectTransform.offsetMin = new Vector2(16f, 6f);
        txt.rectTransform.offsetMax = new Vector2(-16f, 0f);

        // Add soft drop shadow effect to title
        var shadow = txt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(14, 23, 34, 180);
        shadow.effectDistance = new Vector2(2f, -2f);

        return ribbonRt;
    }

    public static RectTransform CreateRibbon(
        RectTransform card, string title,
        Color32 mainColor, Color32 lipColor, Color32 tailColor,
        TMP_FontAsset font,
        out Image main, out Image left, out Image right, out TMP_Text titleText)
    {
        RectTransform ribbon = CreateRibbon(card, title, mainColor, lipColor, tailColor, font);
        main = ribbon.GetComponent<Image>();
        left = ribbon.Find("TailLeft").GetComponent<Image>();
        right = ribbon.Find("TailRight").GetComponent<Image>();
        titleText = ribbon.Find("RibbonTitle").GetComponent<TMP_Text>();
        return ribbon;
    }

    // 2) Two-Tone Arcade Button (Screenshot 2 Main Menu Style)
    public static Button CreateTwoToneButton(
        Transform parent, string name, string label,
        Color32 mainFill, Color32 leftFill, Color32 shadowCol,
        IconType icon, UnityEngine.Events.UnityAction action,
        TMP_FontAsset font = null, float height = 68f)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x > 0 ? rt.sizeDelta.x : 400f, height);

        Image img = btnObj.GetComponent<Image>();
        img.sprite = GetTwoToneButtonSprite(mainFill, leftFill, shadowCol);
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button b = btnObj.GetComponent<Button>();
        b.targetGraphic = img;

        // Left Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(26f, 2f);
        iconRt.sizeDelta = new Vector2(34f, 34f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = GetColoredIconSprite(icon, Color.white);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Right Text
        TMP_Text txt = Text("Label", btnObj.transform, label, 24f, Color.white, TurkishTMPFontSupport.DisplayFont);
        txt.fontStyle = FontStyles.Normal;
        txt.enableAutoSizing=true;txt.fontSizeMin=17;txt.fontSizeMax=24;
        txt.alignment = TextAlignmentOptions.Center;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = new Vector2(58f, 6f);
        txt.rectTransform.offsetMax = new Vector2(-12f, 0f);

        var shadow = txt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(14, 23, 34, 180);
        shadow.effectDistance = new Vector2(2f, -2f);

        if (action != null) b.onClick.AddListener(action);
        return b;
    }

    public static Button CreateTwoToneButton(
        Transform parent, string name, TMP_FontAsset font, string label,
        Color32 mainFill, Color32 leftFill, Color32 shadowCol,
        Color iconTint, IconType icon, UnityEngine.Events.UnityAction action,
        float height = 68f)
    {
        Button button = CreateTwoToneButton(parent, name, label, mainFill, leftFill, shadowCol,
            icon, action, font, height);
        Transform iconTransform = button.transform.Find("Icon");
        Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (iconImage != null) iconImage.color = iconTint;
        return button;
    }

    // 3) Square Floating Button (Image 2 corner buttons)
    public static Button CreateSquareButton(
        Transform parent, string name, IconType icon,
        Color32 fill, Color32 shadow,
        UnityEngine.Events.UnityAction action, float size = 52f)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        Image img = btnObj.GetComponent<Image>();
        img.sprite = GetSquareButtonSprite(fill, shadow);
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button b = btnObj.GetComponent<Button>();
        b.targetGraphic = img;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        Fill(iconRt, 10f);
        iconRt.anchoredPosition = new Vector2(0f, 2f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = GetColoredIconSprite(icon, Color.white);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        if (action != null) b.onClick.AddListener(action);
        return b;
    }

    // 4) Capsule Pill Badge (like Image 2 "BEST SCORE" pill tag)
    public static RectTransform CreatePillBadge(
        Transform parent, string name, string text,
        Color32 fill, Color32 shadow, Color32 textColor,
        TMP_FontAsset font = null, float height = 28f)
    {
        GameObject pill = new GameObject(name, typeof(RectTransform), typeof(Image));
        pill.transform.SetParent(parent, false);
        RectTransform rt = pill.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(130f, height);

        Image img = pill.GetComponent<Image>();
        img.sprite = GetPillBadgeSprite(fill, shadow);
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.raycastTarget = false;

        TMP_Text txt = Text("BadgeLabel", pill.transform, text, 14f, textColor, font);
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        Fill(txt.rectTransform, 3f);
        txt.rectTransform.offsetMin = new Vector2(8f, 2f);
        txt.rectTransform.offsetMax = new Vector2(-8f, 0f);

        return rt;
    }

    // 5) 3D Star Rating Row (Screenshot 2 Right Panel Star Badges!)
    public static RectTransform CreateStarRatingRow(
        Transform parent, string name, int earnedStars,
        int maxStars = 3, float plaqueSize = 64f, float spacing = 14f)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2((plaqueSize * maxStars) + (spacing * (maxStars - 1)), plaqueSize + 10f);

        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < maxStars; i++)
        {
            bool achieved = i < earnedStars;
            GameObject star = new GameObject("StarPlaque_" + (i + 1), typeof(RectTransform), typeof(Image));
            star.transform.SetParent(row.transform, false);
            RectTransform sRt = star.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(plaqueSize, plaqueSize);

            Image sImg = star.GetComponent<Image>();
            sImg.sprite = GetStarPlaqueSprite(achieved);
            sImg.preserveAspect = true;
            sImg.raycastTarget = false;

            // Middle star is slightly elevated / larger like in Screenshot 2!
            if (i == 1)
            {
                sRt.sizeDelta = new Vector2(plaqueSize * 1.15f, plaqueSize * 1.15f);
                sRt.anchoredPosition = new Vector2(0f, 6f);
            }
        }

        return rowRt;
    }

    // Responsive Fit
    public static void FitCard(RectTransform card, Vector2 size, float margin = 36f, float maxScale = 1f)
    {
        var parent = card.parent as RectTransform;
        if (parent == null) return;
        Canvas canvas = card.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.min, cam, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.max, cam, out Vector2 max);
        float scale = Mathf.Min(maxScale, Mathf.Min((max.x - min.x - margin * 2f) / size.x, (max.y - min.y - margin * 2f) / size.y));
        card.anchorMin = card.anchorMax = card.pivot = Vector2.one * 0.5f;
        card.sizeDelta = size;
        card.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        card.anchoredPosition = (min + max) * 0.5f;
    }
}
