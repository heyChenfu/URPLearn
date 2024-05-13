using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class ColorPicker : MonoBehaviour
{

	public BoxCollider pickerCollider;

	private bool m_grab;
	private Camera m_camera;
    [SerializeField]
	private Texture2D m_screenRenderTexture;
	private static Texture2D m_staticRectTexture;
	private static GUIStyle m_staticRectStyle;

	private static Vector3 m_pixelPosition = Vector3.zero;
	private Color m_pickedColor = Color.white;

	void Awake()
	{
		// Get the Camera component
		m_camera = GetComponent<Camera>();
		if (m_camera == null)
		{
			Debug.LogError("You need to dray this script to a camera!");
			return;
		}

		// Attach a BoxCollider to this camera
		// In order to receive mouse events
		if (pickerCollider == null)
		{
			pickerCollider = gameObject.AddComponent<BoxCollider>();
			// Make sure the collider is in the camera's frustum
			pickerCollider.center = Vector3.zero;
			pickerCollider.center += m_camera.transform.worldToLocalMatrix.MultiplyVector(m_camera.transform.forward) * (m_camera.nearClipPlane + 0.2f);
			pickerCollider.size = new Vector3(Screen.width, Screen.height, 0.1f);
		}

		RenderPipelineManager.endCameraRendering += EndCameraRendering;

	}

    private void OnDestroy()
    {
		RenderPipelineManager.endCameraRendering -= EndCameraRendering;

	}

    // Draw the color we picked
    public static void GUIDrawRect(Rect position, Color color)
	{
		if (m_staticRectTexture == null)
		{
			m_staticRectTexture = new Texture2D(1, 1);
		}

		if (m_staticRectStyle == null)
		{
			m_staticRectStyle = new GUIStyle();
		}

		m_staticRectTexture.SetPixel(0, 0, color);
		m_staticRectTexture.Apply();

		m_staticRectStyle.normal.background = m_staticRectTexture;

		GUI.Box(position, GUIContent.none, m_staticRectStyle);
	}

	void EndCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		if (m_grab)
		{
			if (RenderTexture.active == null)
            {
				m_grab = false;
				return;
			}
			m_screenRenderTexture = new Texture2D(RenderTexture.active.width, RenderTexture.active.height);
			//不正确 ReadPixels 读取的是Scene窗口的内容
			m_screenRenderTexture.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0, false);
			m_screenRenderTexture.Apply();
			m_pickedColor = m_screenRenderTexture.GetPixel(Mathf.FloorToInt(m_pixelPosition.x), Mathf.FloorToInt(m_pixelPosition.y));
			m_grab = false;
		}
	}

	void OnMouseDown()
	{
		m_grab = true;
		// Record the mouse position to pick pixel
		m_pixelPosition = Input.mousePosition;
	}

	void OnGUI()
	{
		GUI.Box(new Rect(0, 0, 120, 200), "Color Picker");
		GUIDrawRect(new Rect(20, 30, 80, 80), m_pickedColor);
		GUI.Label(new Rect(10, 120, 100, 20), "R: " + System.Math.Round((double)m_pickedColor.r, 4) + "\t(" + Mathf.FloorToInt(m_pickedColor.r * 255) + ")");
		GUI.Label(new Rect(10, 140, 100, 20), "G: " + System.Math.Round((double)m_pickedColor.g, 4) + "\t(" + Mathf.FloorToInt(m_pickedColor.g * 255) + ")");
		GUI.Label(new Rect(10, 160, 100, 20), "B: " + System.Math.Round((double)m_pickedColor.b, 4) + "\t(" + Mathf.FloorToInt(m_pickedColor.b * 255) + ")");
		GUI.Label(new Rect(10, 180, 100, 20), "A: " + System.Math.Round((double)m_pickedColor.a, 4) + "\t(" + Mathf.FloorToInt(m_pickedColor.a * 255) + ")");
	}
}