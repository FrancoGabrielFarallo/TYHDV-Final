using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MGS2Tanker
{
    /// <summary>
    /// Radar estilo MGS2 dibujado con un mesh de UI:
    ///  - contorno del nivel alrededor del jugador (líneas verdes),
    ///  - jugador en el centro (punto blanco + dirección),
    ///  - soldados (puntos rojos) y cámaras (cuadrados amarillos) con su cono de visión,
    ///  - en ALERT / EVASION queda interferido (estática) y el HUD muestra el contador.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class RadarGraphic : MaskableGraphic
    {
        [Tooltip("Metros desde el centro hasta el borde corto del radar.")]
        public float worldRadius = 12f;
        [Tooltip("False = orientación fija al mundo (+Z arriba), como el radar de MGS2.")]
        public bool rotateWithCamera = false;
        public float lineWidth = 2f;
        [Tooltip("Sólo se dibuja la geometría a esta altura (±) del jugador.")]
        public float heightTolerance = 2.5f;

        public Color mapColor = new Color(0.45f, 1f, 0.6f, 0.95f);
        public Color enemyColor = new Color(1f, 0.25f, 0.2f, 1f);
        public Color coneColor = new Color(0.35f, 0.75f, 1f, 0.35f);
        public Color coneChaseColor = new Color(1f, 0.3f, 0.2f, 0.4f);
        public Color cameraColor = new Color(1f, 0.85f, 0.2f, 1f);
        public Color playerColor = Color.white;
        public Color noiseColor = new Color(0.6f, 1f, 0.7f, 0.35f);

        public bool Jammed { get; set; }

        struct Segment { public Vector3 a, b; public float minY, maxY; }
        readonly List<Segment> segments = new List<Segment>();
        bool mapBuilt;

        Vector2 center;
        float scale;
        Vector3 origin;
        Quaternion invRot = Quaternion.identity;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            if (!mapBuilt) BuildMap();
            SetVerticesDirty();
        }

        /// <summary>Lee todas las piezas con RadarOutline y genera los segmentos del mapa.</summary>
        public void BuildMap()
        {
            segments.Clear();
            foreach (var o in FindObjectsByType<RadarOutline>(FindObjectsSortMode.None))
            {
                var box = o.GetComponent<BoxCollider>();
                if (!box) continue;
                var t = o.transform;
                Vector3 c = box.center;
                Vector3 h = box.size * 0.5f;
                Bounds b = box.bounds;
                float sx = Mathf.Abs(t.lossyScale.x * box.size.x);
                float sz = Mathf.Abs(t.lossyScale.z * box.size.z);

                if (sx < 0.5f && sz >= sx)
                    Add(t.TransformPoint(c + new Vector3(0, 0, -h.z)), t.TransformPoint(c + new Vector3(0, 0, h.z)), b);
                else if (sz < 0.5f)
                    Add(t.TransformPoint(c + new Vector3(-h.x, 0, 0)), t.TransformPoint(c + new Vector3(h.x, 0, 0)), b);
                else
                {
                    Vector3 p0 = t.TransformPoint(c + new Vector3(-h.x, 0, -h.z));
                    Vector3 p1 = t.TransformPoint(c + new Vector3(h.x, 0, -h.z));
                    Vector3 p2 = t.TransformPoint(c + new Vector3(h.x, 0, h.z));
                    Vector3 p3 = t.TransformPoint(c + new Vector3(-h.x, 0, h.z));
                    Add(p0, p1, b); Add(p1, p2, b); Add(p2, p3, b); Add(p3, p0, b);
                }
            }
            mapBuilt = true;
        }

        void Add(Vector3 a, Vector3 b, Bounds bounds) =>
            segments.Add(new Segment { a = a, b = b, minY = bounds.min.y, maxY = bounds.max.y });

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!Application.isPlaying) return;

            Rect rect = GetPixelAdjustedRect();
            if (Jammed) { DrawNoise(vh, rect); return; }

            var player = PlayerHealth.Current;
            if (player == null) return;

            center = rect.center;
            scale = Mathf.Min(rect.width, rect.height) * 0.5f / Mathf.Max(1f, worldRadius);
            origin = player.transform.position;
            var cam = Camera.main;
            float yaw = rotateWithCamera && cam ? cam.transform.eulerAngles.y : 0f;
            invRot = Quaternion.Euler(0f, -yaw, 0f);

            // Mapa
            foreach (var s in segments)
            {
                if (origin.y < s.minY - heightTolerance || origin.y > s.maxY + heightTolerance) continue;
                Line(vh, ToRadar(s.a), ToRadar(s.b), lineWidth, mapColor);
            }

            // Conos primero (debajo), después los puntos
            foreach (var s in SoldierController.All)
            {
                if (!s.IsAlive || !s.Vision) continue;
                var v = s.Vision;
                bool hostile = s.IsAlerted || s.CurrentState == SoldierController.State.Chase;
                Cone(vh, v.EyePosition, v.Forward, v.viewAngle, v.viewRange, hostile ? coneChaseColor : coneColor);
            }
            foreach (var c in SurveillanceCameraController.All)
            {
                if (!c.IsAlive || !c.Vision) continue;
                var v = c.Vision;
                Vector3 f = v.Forward;
                float horizontal = new Vector2(f.x, f.z).magnitude;
                Cone(vh, v.EyePosition, f, v.viewAngle, v.viewRange * horizontal, coneColor);
            }
            foreach (var s in SoldierController.All)
                if (s.IsAlive) Square(vh, ToRadar(s.transform.position), 8f, enemyColor);
            foreach (var c in SurveillanceCameraController.All)
                if (c.IsAlive) Square(vh, ToRadar(c.transform.position), 6f, cameraColor);

            // Jugador
            Vector2 p = ToRadar(origin);
            Square(vh, p, 8f, playerColor);
            Line(vh, p, ToRadar(origin + player.transform.forward * 1.3f), 2f, playerColor);
        }

        Vector2 ToRadar(Vector3 world)
        {
            Vector3 d = invRot * (world - origin);
            return center + new Vector2(d.x, d.z) * scale;
        }

        static void V(VertexHelper vh, Vector2 p, Color32 c)
        {
            var v = UIVertex.simpleVert;
            v.position = p;
            v.color = c;
            vh.AddVert(v);
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (width * 0.5f);
            int i = vh.currentVertCount;
            V(vh, a - n, c); V(vh, a + n, c); V(vh, b + n, c); V(vh, b - n, c);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        static void RectQuad(VertexHelper vh, Rect r, Color c)
        {
            int i = vh.currentVertCount;
            V(vh, new Vector2(r.xMin, r.yMin), c);
            V(vh, new Vector2(r.xMin, r.yMax), c);
            V(vh, new Vector2(r.xMax, r.yMax), c);
            V(vh, new Vector2(r.xMax, r.yMin), c);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        static void Square(VertexHelper vh, Vector2 c, float size, Color col) =>
            RectQuad(vh, new Rect(c - Vector2.one * size * 0.5f, Vector2.one * size), col);

        void Cone(VertexHelper vh, Vector3 apex, Vector3 forward, float angle, float range, Color col)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f || range <= 0.01f) return;
            forward.Normalize();
            const int segs = 12;
            int start = vh.currentVertCount;
            V(vh, ToRadar(apex), col);
            for (int i = 0; i <= segs; i++)
            {
                Vector3 d = Quaternion.AngleAxis(-angle * 0.5f + angle * i / segs, Vector3.up) * forward;
                V(vh, ToRadar(apex + d * range), col);
            }
            for (int i = 0; i < segs; i++) vh.AddTriangle(start, start + 1 + i, start + 2 + i);
        }

        void DrawNoise(VertexHelper vh, Rect rect)
        {
            for (int i = 0; i < 45; i++)
            {
                float y = Random.Range(rect.yMin, rect.yMax);
                float h = Random.Range(1f, 4f);
                float w = Random.Range(15f, rect.width * 0.7f);
                float x = Random.Range(rect.xMin, rect.xMax - w);
                Color c = noiseColor;
                c.a *= Random.Range(0.3f, 1f);
                RectQuad(vh, new Rect(x, y, w, h), c);
            }
        }
    }
}