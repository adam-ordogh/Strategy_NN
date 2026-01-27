using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    public float panSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 20f;

    public Vector2 mapMinBounds = new Vector2(-10, -10);
    public Vector2 mapMaxBounds = new Vector2(50, 50);

    private Camera cam;
    private Vector3 dragOrigin;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
        ClampCameraPosition();
    }

    void HandlePan()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        if (mouse.middleButton.wasPressedThisFrame)
        {
            dragOrigin = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        }

        if (mouse.middleButton.isPressed)
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(mouse.position.ReadValue());
            cam.transform.position += difference;
        }

        Vector2 move = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1;

        cam.transform.position += (Vector3)(move * panSpeed * Time.deltaTime);
    }

    void HandleZoom()
    {
        var scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void ClampCameraPosition()
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float minX = mapMinBounds.x + camWidth;
        float maxX = mapMaxBounds.x - camWidth;
        float minY = mapMinBounds.y + camHeight;
        float maxY = mapMaxBounds.y - camHeight;

        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        cam.transform.position = pos;
    }

}
