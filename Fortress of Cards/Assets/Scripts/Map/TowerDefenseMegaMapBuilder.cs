using System.Collections.Generic;
using UnityEngine;

namespace FortressOfCards.Map
{
    public sealed class TowerDefenseMegaMapBuilder : MonoBehaviour
    {
        [Header("Map Size")]
        [SerializeField] private float pathWidth = 8f;
        [SerializeField] private float pathThickness = 1.2f;
        [SerializeField] private float towerSlotRadius = 2.4f;
        [SerializeField] private float towerSlotSpacing = 12f;

        [Header("Path Quality")]
        [SerializeField] private int samplesPerSegment = 8;
        [SerializeField] private int crossingRampSamples = 10;

        [Header("Crossings")]
        [SerializeField] private float bridgeHeight = 8f;
        [SerializeField] private float tunnelDepth = 5f;
        [SerializeField] private float crossingMinVerticalSeparation = 3f;

        [Header("Scenic Layout")]
        [SerializeField] private int scenicBridgeCount = 2;
        [SerializeField] private int scenicBridgeWindow = 9;
        [SerializeField] private int scenicTunnelWindow = 11;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = true;

        private const float GroundLevel = -2f;

        private readonly List<Vector3> pathControlPoints = new List<Vector3>
        {
            new Vector3(-110f, 0f, -80f),
            new Vector3(-88f, 0f, -73f),
            new Vector3(-66f, 0f, -48f),
            new Vector3(-56f, 0f, -18f),
            new Vector3(-66f, 0f, 12f),
            new Vector3(-86f, 0f, 35f),
            new Vector3(-52f, 0f, 56f),
            new Vector3(-14f, 0f, 62f),
            new Vector3(22f, 0f, 48f),
            new Vector3(45f, 0f, 21f),
            new Vector3(36f, 0f, -8f),
            new Vector3(5f, 0f, -23f),
            new Vector3(-24f, 0f, -14f),
            new Vector3(-34f, 0f, 7f),
            new Vector3(-14f, 0f, 22f),
            new Vector3(16f, 0f, 14f),
            new Vector3(52f, 0f, -9f),
            new Vector3(84f, 0f, -28f),
            new Vector3(95f, 0f, -6f),
            new Vector3(74f, 0f, 20f),
            new Vector3(43f, 0f, 34f),
            new Vector3(20f, 0f, 56f),
            new Vector3(36f, 0f, 79f),
            new Vector3(70f, 0f, 93f),
            new Vector3(100f, 0f, 90f)
        };

        private readonly List<Vector3> generatedPathPoints = new List<Vector3>();

        public IReadOnlyList<Vector3> PathPoints => generatedPathPoints.Count > 1 ? generatedPathPoints : pathControlPoints;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateMap();
            }
        }

        [ContextMenu("Generate Mega Map")]
        public void GenerateMap()
        {
            ClearChildren();

            generatedPathPoints.Clear();
            generatedPathPoints.AddRange(GeneratePlayablePath());

            Shader surfaceShader = Shader.Find("Universal Render Pipeline/Lit");
            if (surfaceShader == null)
            {
                surfaceShader = Shader.Find("Standard");
            }

            Material groundMaterial = CreateMaterial(surfaceShader, new Color(0.23f, 0.28f, 0.22f), "Ground");
            Material pathMaterial = CreateMaterial(surfaceShader, new Color(0.40f, 0.40f, 0.37f), "Path");
            Material slotMaterial = CreateMaterial(surfaceShader, new Color(0.15f, 0.44f, 0.70f), "TowerSlot");
            Material startMaterial = CreateMaterial(surfaceShader, new Color(0.26f, 0.74f, 0.33f), "Start");
            Material endMaterial = CreateMaterial(surfaceShader, new Color(0.86f, 0.20f, 0.17f), "End");
            Material bridgeMaterial = CreateMaterial(surfaceShader, new Color(0.56f, 0.52f, 0.47f), "Bridge");
            Material tunnelMaterial = CreateMaterial(surfaceShader, new Color(0.27f, 0.24f, 0.21f), "Tunnel");

            Transform environmentRoot = new GameObject("Environment").transform;
            environmentRoot.SetParent(transform, false);

            Transform pathRoot = new GameObject("EnemyPath").transform;
            pathRoot.SetParent(transform, false);

            Transform slotsRoot = new GameObject("TowerSlots").transform;
            slotsRoot.SetParent(transform, false);

            Transform landmarksRoot = new GameObject("Landmarks").transform;
            landmarksRoot.SetParent(transform, false);

            CreateGround(environmentRoot, groundMaterial);
            CreatePath(pathRoot, pathMaterial, generatedPathPoints);
            CreateTurnCaps(pathRoot, pathMaterial, generatedPathPoints);
            CreateBridgeSupports(pathRoot, bridgeMaterial, generatedPathPoints);
            CreateTunnelPieces(pathRoot, tunnelMaterial, generatedPathPoints);
            CreateTowerSlots(slotsRoot, slotMaterial, generatedPathPoints);
            CreateSpawnAndGoal(landmarksRoot, startMaterial, endMaterial, generatedPathPoints);
        }

        private static Material CreateMaterial(Shader shader, Color color, string materialName)
        {
            Material material = new Material(shader)
            {
                name = materialName,
                color = color
            };

            return material;
        }

        private void CreateGround(Transform root, Material groundMaterial)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, GroundLevel - 0.5f, 0f);
            ground.transform.localScale = new Vector3(26f, 1f, 26f);
            ground.GetComponent<Renderer>().material = groundMaterial;
        }

        private void CreatePath(Transform root, Material pathMaterial, IReadOnlyList<Vector3> points)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];
                Vector3 direction = end - start;
                float length = direction.magnitude;

                if (length < 0.001f)
                {
                    continue;
                }

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"PathSegment_{i:00}";
                segment.transform.SetParent(root, false);
                segment.transform.position = (start + end) * 0.5f;
                segment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                segment.transform.localScale = new Vector3(pathWidth, pathThickness, length);
                segment.GetComponent<Renderer>().material = pathMaterial;
            }
        }

        private void CreateTurnCaps(Transform root, Material pathMaterial, IReadOnlyList<Vector3> points)
        {
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 prev = (points[i] - points[i - 1]).normalized;
                Vector3 next = (points[i + 1] - points[i]).normalized;
                float cornerStrength = Vector3.Dot(prev, next);

                if (cornerStrength > 0.995f)
                {
                    continue;
                }

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = $"PathTurnCap_{i:000}";
                cap.transform.SetParent(root, false);
                cap.transform.position = points[i];
                cap.transform.localScale = new Vector3(pathWidth * 0.52f, pathThickness * 0.5f, pathWidth * 0.52f);
                cap.GetComponent<Renderer>().material = pathMaterial;
            }
        }

        private void CreateBridgeSupports(Transform root, Material bridgeMaterial, IReadOnlyList<Vector3> points)
        {
            for (int i = 0; i < points.Count - 1; i += 2)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                Vector3 midpoint = (a + b) * 0.5f;

                if (midpoint.y < 1.6f)
                {
                    continue;
                }

                float supportHeight = Mathf.Max(1f, midpoint.y - GroundLevel);
                GameObject support = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                support.name = $"BridgeSupport_{i:000}";
                support.transform.SetParent(root, false);
                support.transform.position = new Vector3(midpoint.x, GroundLevel + supportHeight * 0.5f, midpoint.z);
                support.transform.localScale = new Vector3(1.2f, supportHeight * 0.5f, 1.2f);
                support.GetComponent<Renderer>().material = bridgeMaterial;
            }
        }

        private void CreateTunnelPieces(Transform root, Material tunnelMaterial, IReadOnlyList<Vector3> points)
        {
            int i = 0;
            while (i < points.Count - 1)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                float segmentY = (a.y + b.y) * 0.5f;
                if (segmentY > -1.2f)
                {
                    i++;
                    continue;
                }

                int start = i;
                int end = i + 1;

                while (end < points.Count - 1)
                {
                    Vector3 c = points[end];
                    Vector3 d = points[end + 1];
                    float y = (c.y + d.y) * 0.5f;
                    if (y > -1.2f)
                    {
                        break;
                    }

                    end++;
                }

                CreateTunnelSection(root, tunnelMaterial, points[start], points[end]);
                i = end + 1;
            }
        }

        private void CreateTunnelSection(Transform root, Material tunnelMaterial, Vector3 tunnelStart, Vector3 tunnelEnd)
        {
            Vector3 tunnelDirection = tunnelEnd - tunnelStart;
            float tunnelLength = tunnelDirection.magnitude;

            if (tunnelLength < 1f)
            {
                return;
            }

            Vector3 tunnelCenter = (tunnelStart + tunnelEnd) * 0.5f;
            Vector3 direction = tunnelDirection.normalized;

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "TunnelRoof";
            roof.transform.SetParent(root, false);
            roof.transform.position = tunnelCenter + new Vector3(0f, 4.5f, 0f);
            roof.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            roof.transform.localScale = new Vector3(pathWidth + 2f, 2f, tunnelLength + 4f);
            roof.GetComponent<Renderer>().material = tunnelMaterial;

            float halfWidth = (pathWidth + 2f) * 0.5f;
            Vector3 lateral = Vector3.Cross(Vector3.up, direction) * halfWidth;

            GameObject wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallLeft.name = "TunnelWall_Left";
            wallLeft.transform.SetParent(root, false);
            wallLeft.transform.position = tunnelCenter + lateral + new Vector3(0f, 1.5f, 0f);
            wallLeft.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            wallLeft.transform.localScale = new Vector3(1.6f, 5f, tunnelLength + 4f);
            wallLeft.GetComponent<Renderer>().material = tunnelMaterial;

            GameObject wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRight.name = "TunnelWall_Right";
            wallRight.transform.SetParent(root, false);
            wallRight.transform.position = tunnelCenter - lateral + new Vector3(0f, 1.5f, 0f);
            wallRight.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            wallRight.transform.localScale = new Vector3(1.6f, 5f, tunnelLength + 4f);
            wallRight.GetComponent<Renderer>().material = tunnelMaterial;
        }

        private void CreateTowerSlots(Transform root, Material slotMaterial, IReadOnlyList<Vector3> points)
        {
            List<Vector3> slots = new List<Vector3>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];
                Vector3 direction = (end - start);
                float segmentLength = direction.magnitude;

                if (segmentLength < 1f)
                {
                    continue;
                }

                Vector3 segmentDirection = direction / segmentLength;
                Vector3 side = Vector3.Cross(Vector3.up, segmentDirection).normalized;

                int placements = Mathf.FloorToInt(segmentLength / towerSlotSpacing);
                for (int j = 1; j <= placements; j++)
                {
                    float along = (j * towerSlotSpacing) / segmentLength;
                    Vector3 center = Vector3.Lerp(start, end, Mathf.Clamp01(along));
                    float sideSign = ((i + j) % 2 == 0) ? -1f : 1f;
                    Vector3 candidate = center + side * sideSign * (pathWidth * 1.2f);
                    candidate.y = Mathf.Max(0f, center.y) + 0.4f;

                    bool tooClose = false;
                    for (int s = 0; s < slots.Count; s++)
                    {
                        if (Vector3.Distance(slots[s], candidate) < 7f)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                    {
                        continue;
                    }

                    slots.Add(candidate);
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                GameObject slot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                slot.name = $"TowerSlot_{i:000}";
                slot.transform.SetParent(root, false);
                slot.transform.position = slots[i];
                slot.transform.localScale = new Vector3(towerSlotRadius, 0.35f, towerSlotRadius);
                slot.GetComponent<Renderer>().material = slotMaterial;
            }
        }

        private static void CreateSpawnAndGoal(Transform root, Material startMaterial, Material endMaterial, IReadOnlyList<Vector3> points)
        {
            Vector3 spawnPoint = points[0] + new Vector3(0f, 1.2f, 0f);
            Vector3 endPoint = points[points.Count - 1] + new Vector3(0f, 1.2f, 0f);

            GameObject spawnMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spawnMarker.name = "EnemySpawn";
            spawnMarker.transform.SetParent(root, false);
            spawnMarker.transform.position = spawnPoint;
            spawnMarker.transform.localScale = Vector3.one * 5f;
            spawnMarker.GetComponent<Renderer>().material = startMaterial;

            GameObject goalMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            goalMarker.name = "PlayerBase";
            goalMarker.transform.SetParent(root, false);
            goalMarker.transform.position = endPoint;
            goalMarker.transform.localScale = Vector3.one * 5f;
            goalMarker.GetComponent<Renderer>().material = endMaterial;
        }

        private List<Vector3> GeneratePlayablePath()
        {
            List<Vector3> sampledPoints = BuildSmoothedPath(pathControlPoints, Mathf.Max(3, samplesPerSegment));
            AddScenicBridges(sampledPoints);
            AddScenicTunnel(sampledPoints);
            ResolveSelfCrossings(sampledPoints);
            SmoothHeights(sampledPoints, 2);
            EnforceCrossingClearance(sampledPoints);
            return sampledPoints;
        }

        private void AddScenicBridges(List<Vector3> points)
        {
            if (points.Count < 6 || scenicBridgeCount <= 0)
            {
                return;
            }

            for (int bridgeIndex = 0; bridgeIndex < scenicBridgeCount; bridgeIndex++)
            {
                float t = (bridgeIndex + 1f) / (scenicBridgeCount + 1f);
                int pivot = Mathf.RoundToInt(t * (points.Count - 1));
                ApplyAbsoluteHeightRamp(points, pivot, scenicBridgeWindow, bridgeHeight);
            }
        }

        private void AddScenicTunnel(List<Vector3> points)
        {
            if (points.Count < 6)
            {
                return;
            }

            int pivot = Mathf.RoundToInt((points.Count - 1) * 0.68f);
            ApplyAbsoluteHeightRamp(points, pivot, scenicTunnelWindow, -Mathf.Abs(tunnelDepth));
        }

        private static List<Vector3> BuildSmoothedPath(IReadOnlyList<Vector3> controlPoints, int samples)
        {
            List<Vector3> points = new List<Vector3>(controlPoints.Count * samples);

            if (controlPoints.Count < 2)
            {
                points.AddRange(controlPoints);
                return points;
            }

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector3 p0 = controlPoints[Mathf.Max(0, i - 1)];
                Vector3 p1 = controlPoints[i];
                Vector3 p2 = controlPoints[i + 1];
                Vector3 p3 = controlPoints[Mathf.Min(controlPoints.Count - 1, i + 2)];

                for (int s = 0; s < samples; s++)
                {
                    float t = s / (float)samples;
                    points.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            points.Add(controlPoints[controlPoints.Count - 1]);
            return points;
        }

        private void ResolveSelfCrossings(List<Vector3> points)
        {
            if (points.Count < 8)
            {
                return;
            }

            int crossingCount = 0;
            int segmentCount = points.Count - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];

                for (int j = i + 6; j < segmentCount; j++)
                {
                    Vector3 c = points[j];
                    Vector3 d = points[j + 1];

                    if (!TryGetSegmentIntersectionXZ(a, b, c, d, out _, out _, out _))
                    {
                        continue;
                    }

                    float currentSeparation = Mathf.Abs(((a.y + b.y) * 0.5f) - ((c.y + d.y) * 0.5f));
                    if (currentSeparation >= crossingMinVerticalSeparation)
                    {
                        continue;
                    }

                    bool firstSegmentIsBridge = crossingCount % 2 == 0;
                    int window = Mathf.Max(4, crossingRampSamples);

                    if (firstSegmentIsBridge)
                    {
                        ApplyAbsoluteHeightRamp(points, i, window, bridgeHeight);
                        ApplyAbsoluteHeightRamp(points, j, window, 0f);
                    }
                    else
                    {
                        ApplyAbsoluteHeightRamp(points, j, window, bridgeHeight);
                        ApplyAbsoluteHeightRamp(points, i, window, 0f);
                    }

                    crossingCount++;
                    if (crossingCount > 10)
                    {
                        return;
                    }
                }
            }
        }

        private static void ApplyElevationRamp(List<Vector3> points, int pivotIndex, int halfWindow, float targetOffset)
        {
            int start = Mathf.Max(0, pivotIndex - halfWindow);
            int end = Mathf.Min(points.Count - 1, pivotIndex + halfWindow);

            for (int i = start; i <= end; i++)
            {
                float normalizedDistance = Mathf.Abs(i - pivotIndex) / (halfWindow + 0.01f);
                float blend = Mathf.Clamp01(1f - normalizedDistance);

                Vector3 p = points[i];
                p.y = Mathf.Clamp(p.y + targetOffset * blend, -12f, 16f);
                points[i] = p;
            }
        }

        private static void ApplyAbsoluteHeightRamp(List<Vector3> points, int pivotIndex, int halfWindow, float targetHeight)
        {
            int start = Mathf.Max(0, pivotIndex - halfWindow);
            int end = Mathf.Min(points.Count - 1, pivotIndex + halfWindow);

            for (int i = start; i <= end; i++)
            {
                float normalizedDistance = Mathf.Abs(i - pivotIndex) / (halfWindow + 0.01f);
                float blend = Mathf.Clamp01(1f - normalizedDistance);

                Vector3 p = points[i];
                p.y = Mathf.Lerp(p.y, targetHeight, blend);
                points[i] = p;
            }
        }

        private static void SmoothHeights(List<Vector3> points, int iterations)
        {
            if (points.Count < 3 || iterations <= 0)
            {
                return;
            }

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                List<float> yCache = new List<float>(points.Count);
                for (int i = 0; i < points.Count; i++)
                {
                    yCache.Add(points[i].y);
                }

                for (int i = 1; i < points.Count - 1; i++)
                {
                    float neighborAverage = (yCache[i - 1] + yCache[i] + yCache[i + 1]) / 3f;
                    Vector3 p = points[i];
                    p.y = Mathf.Lerp(yCache[i], neighborAverage, 0.45f);
                    points[i] = p;
                }
            }
        }

        private void EnforceCrossingClearance(List<Vector3> points)
        {
            if (points.Count < 8)
            {
                return;
            }

            int segmentCount = points.Count - 1;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < segmentCount; i++)
                {
                    Vector3 a = points[i];
                    Vector3 b = points[i + 1];

                    for (int j = i + 6; j < segmentCount; j++)
                    {
                        Vector3 c = points[j];
                        Vector3 d = points[j + 1];
                        if (!TryGetSegmentIntersectionXZ(a, b, c, d, out _, out _, out _))
                        {
                            continue;
                        }

                        float yA = (points[i].y + points[i + 1].y) * 0.5f;
                        float yB = (points[j].y + points[j + 1].y) * 0.5f;
                        float separation = Mathf.Abs(yA - yB);

                        if (separation >= crossingMinVerticalSeparation)
                        {
                            continue;
                        }

                        bool iIsHigher = yA >= yB;
                        int window = Mathf.Max(3, crossingRampSamples / 2);
                        if (iIsHigher)
                        {
                            ApplyAbsoluteHeightRamp(points, i, window, bridgeHeight);
                            ApplyAbsoluteHeightRamp(points, j, window, 0f);
                        }
                        else
                        {
                            ApplyAbsoluteHeightRamp(points, j, window, bridgeHeight);
                            ApplyAbsoluteHeightRamp(points, i, window, 0f);
                        }
                    }
                }
            }
        }

        private static bool TryGetSegmentIntersectionXZ(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 intersection, out float tA, out float tB)
        {
            Vector2 p = new Vector2(a.x, a.z);
            Vector2 r = new Vector2(b.x - a.x, b.z - a.z);
            Vector2 q = new Vector2(c.x, c.z);
            Vector2 s = new Vector2(d.x - c.x, d.z - c.z);

            float denominator = (r.x * s.y) - (r.y * s.x);
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                intersection = Vector3.zero;
                tA = 0f;
                tB = 0f;
                return false;
            }

            Vector2 qMinusP = q - p;
            tA = ((qMinusP.x * s.y) - (qMinusP.y * s.x)) / denominator;
            tB = ((qMinusP.x * r.y) - (qMinusP.y * r.x)) / denominator;

            if (tA <= 0.01f || tA >= 0.99f || tB <= 0.01f || tB >= 0.99f)
            {
                intersection = Vector3.zero;
                return false;
            }

            Vector2 hit = p + r * tA;
            intersection = new Vector3(hit.x, 0f, hit.y);
            return true;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private void ClearChildren()
        {
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < children.Count; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(children[i]);
                    continue;
                }
#endif
                Destroy(children[i]);
            }
        }
    }
}
