using UnityEngine;

/// <summary>
/// Manages multi-layer parallax scrolling background for the Mad Doctor game.
/// Attach this to a parent "BackgroundManager" GameObject.
/// Each child represents a background layer with configurable parallax speed.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public string layerName;
        public Transform transform;
        [Range(0f, 1f)]
        [Tooltip("0 = moves with camera (foreground), 1 = static (far background)")]
        public float parallaxFactor = 0.5f;
        [Tooltip("Enable horizontal infinite scrolling for this layer")]
        public bool infiniteScrollX = false;
        [HideInInspector] public float spriteWidth;
        [HideInInspector] public Vector3 startPos;
    }

    [Header("References")]
    [Tooltip("The camera to track. If null, will use Camera.main")]
    public Camera targetCamera;

    [Header("Parallax Layers")]
    public ParallaxLayer[] layers;

    private Vector3 lastCameraPos;

void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        lastCameraPos = targetCamera.transform.position;

        // Auto-populate layers from active children if not manually set
        if (layers == null || layers.Length == 0)
        {
            AutoPopulateLayers();
        }

        // Initialize each layer
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].transform == null) continue;

            layers[i].startPos = layers[i].transform.position;

            // Calculate sprite width for infinite scrolling
            SpriteRenderer sr = layers[i].transform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                layers[i].spriteWidth = sr.sprite.bounds.size.x * sr.transform.lossyScale.x;
            }
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 deltaMovement = targetCamera.transform.position - lastCameraPos;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].transform == null) continue;

            float parallaxX = deltaMovement.x * layers[i].parallaxFactor;
            float parallaxY = deltaMovement.y * layers[i].parallaxFactor;

            Vector3 newPos = layers[i].transform.position;
            newPos.x -= parallaxX;
            newPos.y -= parallaxY;
            layers[i].transform.position = newPos;

            // Infinite scroll logic
            if (layers[i].infiniteScrollX && layers[i].spriteWidth > 0)
            {
                float camPosX = targetCamera.transform.position.x;
                float distFromStart = camPosX * (1 - layers[i].parallaxFactor);
                float clampDist = Mathf.Abs(distFromStart);

                if (clampDist > layers[i].spriteWidth)
                {
                    layers[i].startPos.x += layers[i].spriteWidth * Mathf.Sign(distFromStart);
                    layers[i].transform.position = new Vector3(
                        layers[i].startPos.x,
                        layers[i].transform.position.y,
                        layers[i].transform.position.z
                    );
                }
            }
        }

        lastCameraPos = targetCamera.transform.position;
    }

/// <summary>
    /// Auto-populates parallax layers from active child GameObjects.
    /// Layers further back (first child) have higher parallax factor (move slower).
    /// </summary>
    private void AutoPopulateLayers()
    {
        var activeChildren = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                activeChildren.Add(child);
            }
        }

        layers = new ParallaxLayer[activeChildren.Count];
        for (int i = 0; i < activeChildren.Count; i++)
        {
            layers[i] = new ParallaxLayer();
            layers[i].layerName = activeChildren[i].name;
            layers[i].transform = activeChildren[i];

            // Assign parallax factors based on layer name
            layers[i].parallaxFactor = GetParallaxFactorByName(activeChildren[i].name);
            layers[i].infiniteScrollX = true;
        }

        Debug.Log($"[ParallaxBackground] Auto-populated {layers.Length} layers");
    }

    /// <summary>
    /// Returns parallex factor based on layer name.
    /// Higher factor = moves slower (stays more static).
    /// BlackBorders shares the same factor as Layer3_Floor.
    /// </summary>
    private float GetParallaxFactorByName(string name)
    {
        if (name.Contains("Layer0") || name.Contains("FarWall"))          return 0.95f;
        if (name.Contains("Layer1") || name.Contains("LabWall"))          return 0.90f;
        if (name.Contains("Layer2") || name.Contains("LabStructure"))     return 0.85f;
        if (name.Contains("Layer3") || name.Contains("Floor") 
            || name.Contains("BlackBorders"))                             return 0.80f;
        if (name.Contains("Layer4") || name.Contains("Ceiling"))          return 0.60f;
        if (name.Contains("Layer5") || name.Contains("FloorSilhouette")) return 0.50f;
        if (name.Contains("Layer6") || name.Contains("TopDecor"))         return 0.40f;
        if (name.Contains("Layer7") || name.Contains("Foreground"))       return 0.30f;

        // Default: match floor
        return 0.80f;
    }
}
