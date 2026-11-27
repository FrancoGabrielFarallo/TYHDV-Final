using UnityEngine;
using UnityEngine.UI;

namespace MGS2Tanker
{
    /// <summary>
    /// HUD construido por código con la distribución de MGS2:
    ///  - Arriba izquierda: nombre + barra LIFE.
    ///  - Arriba derecha: radar (con ALERT / EVASION y contador cuando está interferido).
    ///  - Abajo derecha: caja de arma (nombre, balas en el cargador, cargadores en reserva).
    ///  - Abajo izquierda: caja de ítem.
    ///  - Mensajes de pickup, pantalla de PAUSA y GAME OVER.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("Textos")]
        public string playerName = "SNAKE";

        static readonly Color Green = new Color(0.45f, 1f, 0.55f, 1f);
        static readonly Color FrameColor = new Color(0.55f, 0.85f, 0.65f, 0.9f);
        static readonly Color PanelBg = new Color(0f, 0.08f, 0.04f, 0.6f);
        static readonly Color AlertRed = new Color(1f, 0.25f, 0.2f, 1f);
        static readonly Color EvasionYellow = new Color(1f, 0.85f, 0.2f, 1f);

        Font font;
        PlayerHealth player;
        PlayerWeapon weapon;

        RectTransform lifeFill;
        Image lifeFillImage;
        Text weaponNameText, ammoText, magText, statusText;
        RadarGraphic radar;
        GameObject jamPanel;
        Text jamTitle, jamTimer;
        Text messageText;
        float messageTimer;
        GameObject pauseOverlay, gameOverOverlay;

        void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void ShowMessage(string msg, float duration = 2f)
        {
            if (!messageText) return;
            messageText.text = msg;
            messageTimer = duration;
        }

        void Update()
        {
            if (!player)
            {
                player = PlayerHealth.Current;
                if (player) weapon = player.GetComponent<PlayerWeapon>();
            }

            float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);

            if (player)
            {
                float ratio = Mathf.Clamp01(player.CurrentHealth / Mathf.Max(1f, player.maxHealth));
                lifeFill.anchorMax = new Vector2(ratio, 1f);
                lifeFillImage.color = ratio <= 0.25f ? Color.Lerp(AlertRed, Green, blink * 0.3f) : Green;
            }

            if (weapon)
            {
                weaponNameText.text = weapon.weaponName;
                ammoText.text = weapon.BulletsInMagazine.ToString();
                magText.text = $"MAG  x{weapon.MagazinesInPouch}";
                if (weapon.IsReloading) { statusText.text = "RELOADING"; statusText.color = Green; }
                else if (!weapon.HasAnyAmmo) { statusText.text = "NO AMMO"; statusText.color = AlertRed; }
                else if (weapon.BulletsInMagazine == 0) { statusText.text = "RELOAD"; statusText.color = new Color(1f, 1f, 1f, blink); }
                else statusText.text = "";
                ammoText.color = weapon.BulletsInMagazine == 0 ? AlertRed : Color.white;
            }

            var am = AlertManager.Instance;
            bool jammed = am && am.Phase != AlertPhase.None;
            radar.Jammed = jammed;
            jamPanel.SetActive(jammed);
            if (jammed)
            {
                bool alert = am.Phase == AlertPhase.Alert;
                jamTitle.text = alert ? "ALERT" : "EVASION";
                jamTitle.color = alert ? AlertRed : EvasionYellow;
                jamTimer.text = Mathf.Max(0f, am.Timer).ToString("00.00");
                jamTimer.color = jamTitle.color;
            }

            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                var c = messageText.color;
                c.a = Mathf.Clamp01(messageTimer * 2f);
                messageText.color = c;
            }
            else if (messageText.text.Length > 0) messageText.text = "";

            pauseOverlay.SetActive(PauseManager.IsPaused);
            gameOverOverlay.SetActive(player && !player.IsAlive);
        }

        // ------------------------------------------------------------------ Construcción

        void Build()
        {
            var canvasGO = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGO.transform;

            Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0), BC = new Vector2(0.5f, 0);

            // --- LIFE (arriba izquierda)
            var life = R("Life", root, TL, TL, new Vector2(60, -45), new Vector2(520, 70));
            Txt(R("Name", life, TL, TL, Vector2.zero, new Vector2(300, 28)), playerName, 24, Green, TextAnchor.UpperLeft);
            Txt(R("LifeLabel", life, TL, TL, new Vector2(0, -32), new Vector2(70, 24)), "LIFE", 20, Green, TextAnchor.MiddleLeft);
            var barFrame = R("LifeBar", life, TL, TL, new Vector2(70, -33), new Vector2(420, 22));
            Img(barFrame, FrameColor);
            var barBg = Stretch("Bg", barFrame, 2);
            Img(barBg, new Color(0f, 0f, 0f, 0.75f));
            lifeFill = Stretch("Fill", barBg, 2);
            lifeFillImage = Img(lifeFill, Green);

            // --- RADAR (arriba derecha)
            var radarFrame = R("Radar", root, TR, TR, new Vector2(-60, -45), new Vector2(340, 250));
            Img(radarFrame, FrameColor);
            var radarBg = Stretch("Bg", radarFrame, 3);
            Img(radarBg, new Color(0f, 0.1f, 0.06f, 0.65f));
            var maskArea = Stretch("Mask", radarBg, 0);
            maskArea.gameObject.AddComponent<RectMask2D>();
            var radarRT = Stretch("RadarGraphic", maskArea, 0);
            radarRT.gameObject.AddComponent<CanvasRenderer>();
            radar = radarRT.gameObject.AddComponent<RadarGraphic>();

            var jam = Stretch("Jammed", maskArea, 0);
            Img(jam, new Color(0.15f, 0f, 0f, 0.45f));
            jamPanel = jam.gameObject;
            jamTitle = Txt(R("Title", jam, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(320, 60)), "ALERT", 46, AlertRed, TextAnchor.MiddleCenter);
            jamTimer = Txt(R("Timer", jam, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -28), new Vector2(320, 50)), "00.00", 36, AlertRed, TextAnchor.MiddleCenter);
            jamPanel.SetActive(false);

            // --- ARMA (abajo derecha)
            var wFrame = R("WeaponBox", root, BR, BR, new Vector2(-60, 55), new Vector2(280, 130));
            Img(wFrame, FrameColor);
            var wIn = Stretch("Inner", wFrame, 2);
            Img(wIn, PanelBg);
            weaponNameText = Txt(R("Name", wIn, TL, TL, new Vector2(14, -8), new Vector2(200, 32)), "USP", 26, Green, TextAnchor.UpperLeft);
            ammoText = Txt(R("Ammo", wIn, BR, BR, new Vector2(-16, 8), new Vector2(150, 80)), "15", 64, Color.white, TextAnchor.LowerRight);
            magText = Txt(R("Mags", wIn, BL, BL, new Vector2(14, 12), new Vector2(150, 28)), "MAG  x1", 20, Green, TextAnchor.LowerLeft);
            statusText = Txt(R("Status", wIn, BL, BL, new Vector2(14, 44), new Vector2(160, 26)), "", 18, Color.white, TextAnchor.LowerLeft);

            // --- ITEM (abajo izquierda)
            var iFrame = R("ItemBox", root, BL, BL, new Vector2(60, 55), new Vector2(280, 130));
            Img(iFrame, FrameColor);
            var iIn = Stretch("Inner", iFrame, 2);
            Img(iIn, PanelBg);
            Txt(Stretch("Text", iIn, 10), "NO ITEM", 24, new Color(0.45f, 1f, 0.55f, 0.45f), TextAnchor.MiddleCenter);

            // --- Mensajes
            messageText = Txt(R("Message", root, BC, BC, new Vector2(0, 230), new Vector2(900, 50)), "", 30, Color.white, TextAnchor.MiddleCenter);

            // --- Pausa
            var pause = Stretch("PauseOverlay", root, 0);
            Img(pause, new Color(0f, 0f, 0f, 0.6f));
            Txt(R("Title", pause, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(800, 100)), "PAUSE", 80, Green, TextAnchor.MiddleCenter);
            Txt(R("Hint", pause, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(900, 40)), "ESC / START para continuar", 26, Color.white, TextAnchor.MiddleCenter);
            pauseOverlay = pause.gameObject;
            pauseOverlay.SetActive(false);

            // --- Game Over
            var go = Stretch("GameOverOverlay", root, 0);
            Img(go, new Color(0.25f, 0f, 0f, 0.55f));
            Txt(R("Title", go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1000, 110)), "GAME OVER", 90, AlertRed, TextAnchor.MiddleCenter);
            Txt(R("Hint", go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(1300, 40)),
                "F1 (A / Cruz): reaparecer        F2 (Select): reiniciar escena", 26, Color.white, TextAnchor.MiddleCenter);
            gameOverOverlay = go.gameObject;
            gameOverOverlay.SetActive(false);
        }

        static RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent, float inset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        static Image Img(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        Text Txt(RectTransform rt, string text, int size, Color color, TextAnchor anchor)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}