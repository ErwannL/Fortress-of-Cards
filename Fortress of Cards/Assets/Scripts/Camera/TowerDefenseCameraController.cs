using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FortressOfCards.Cameras
{
    public sealed class TowerDefenseCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 75f;
        [SerializeField] private Vector2 xBounds = new Vector2(-150f, 150f);
        [SerializeField] private Vector2 zBounds = new Vector2(-150f, 150f);

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 65f;
        [SerializeField] private float minHeight = 40f;
        [SerializeField] private float maxHeight = 170f;
        [SerializeField] private float minOrthoSize = 28f;
        [SerializeField] private float maxOrthoSize = 92f;

        [Header("Isometric")]
        [SerializeField] private bool useIsometric2D = true;
        [SerializeField] private float isoPitchAngle = 35.264f;
        [SerializeField] private float isoYawAngle = 45f;
        [SerializeField] private float defaultOrthoSize = 56f;

        [Header("UI")]
        [SerializeField] private bool showControlsOverlay = true;

        private GUIStyle controlsBoxStyle;
        private GUIStyle controlsLabelStyle;
        private Camera cachedCamera;

        private void Start()
        {
            cachedCamera = GetComponent<Camera>();

            if (useIsometric2D)
            {
                transform.position = new Vector3(0f, 95f, -95f);
                transform.rotation = Quaternion.Euler(isoPitchAngle, isoYawAngle, 0f);
            }
            else
            {
                transform.position = new Vector3(0f, 95f, -65f);
                transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            }

            if (cachedCamera != null)
            {
                cachedCamera.orthographic = useIsometric2D;
                cachedCamera.nearClipPlane = 0.3f;
                cachedCamera.farClipPlane = 1600f;

                if (useIsometric2D)
                {
                    cachedCamera.orthographicSize = defaultOrthoSize;
                }
                else
                {
                    cachedCamera.fieldOfView = 55f;
                }
            }
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            Vector2 panInput = ReadPanInput();
            float horizontal = panInput.x;
            float vertical = panInput.y;

            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 movement = ((right * horizontal) + (forward * vertical)) * (moveSpeed * Time.deltaTime);
            Vector3 targetPosition = transform.position + movement;

            targetPosition.x = Mathf.Clamp(targetPosition.x, xBounds.x, xBounds.y);
            targetPosition.z = Mathf.Clamp(targetPosition.z, zBounds.x, zBounds.y);

            transform.position = targetPosition;
        }

        private void HandleZoom()
        {
            float scroll = ReadScrollInput();
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            if (cachedCamera != null && cachedCamera.orthographic)
            {
                float nextSize = cachedCamera.orthographicSize - (scroll * zoomSpeed * Time.deltaTime * 2.2f);
                cachedCamera.orthographicSize = Mathf.Clamp(nextSize, minOrthoSize, maxOrthoSize);
                return;
            }

            Vector3 position = transform.position;
            position += transform.forward * (scroll * zoomSpeed * Time.deltaTime * 10f);
            position.y = Mathf.Clamp(position.y, minHeight, maxHeight);
            position.x = Mathf.Clamp(position.x, xBounds.x, xBounds.y);
            position.z = Mathf.Clamp(position.z, zBounds.x, zBounds.y);

            transform.position = position;
        }

        private static Vector2 ReadPanInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }

        private static float ReadScrollInput()
        {
            float keyboardZoom = 0f;
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.iKey.isPressed || keyboard.numpadPlusKey.isPressed || keyboard.equalsKey.isPressed || keyboard.pageUpKey.isPressed)
                {
                    keyboardZoom += 1f;
                }

                if (keyboard.oKey.isPressed || keyboard.numpadMinusKey.isPressed || keyboard.minusKey.isPressed || keyboard.pageDownKey.isPressed)
                {
                    keyboardZoom -= 1f;
                }
            }

            if (mouse == null)
            {
                return keyboardZoom;
            }

            // Input System reports larger wheel deltas on some platforms, normalize to legacy-like feel.
            return (mouse.scroll.ReadValue().y * 0.01f) + keyboardZoom;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.I) || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.PageUp))
            {
                keyboardZoom += 1f;
            }

            if (Input.GetKey(KeyCode.O) || Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.PageDown))
            {
                keyboardZoom -= 1f;
            }

            return Input.mouseScrollDelta.y + keyboardZoom;
#else
            return keyboardZoom;
#endif
        }

        private void OnGUI()
        {
            if (!showControlsOverlay)
            {
                return;
            }

            EnsureStyles();

            Rect panel = new Rect(14f, 14f, 350f, 118f);
            Color previousGuiColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.Box(panel, GUIContent.none, controlsBoxStyle);
            GUI.color = previousGuiColor;

            GUI.Label(new Rect(24f, 22f, 330f, 22f), "Camera Controls", controlsLabelStyle);
            GUI.Label(new Rect(24f, 46f, 330f, 20f), "Move: ZQSD / WASD / Arrow Keys", controlsLabelStyle);
            GUI.Label(new Rect(24f, 66f, 330f, 20f), "Zoom: Mouse Wheel / I,O / +,-", controlsLabelStyle);
            GUI.Label(new Rect(24f, 86f, 330f, 20f), "View: Isometric 2D", controlsLabelStyle);
        }

        private void EnsureStyles()
        {
            if (controlsBoxStyle != null && controlsLabelStyle != null)
            {
                return;
            }

            controlsBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal =
                {
                    background = Texture2D.whiteTexture,
                    textColor = Color.white
                }
            };

            controlsLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
        }
    }
}
