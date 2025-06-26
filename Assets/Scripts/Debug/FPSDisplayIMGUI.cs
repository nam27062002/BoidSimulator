using UnityEngine;

public class FPSDisplayIMGUI : MonoBehaviour
{
    private float _smoothDeltaTime = 0f;
    public float smoothingFactor = 0.1f;
    private GUIStyle _fpsStyle;

    private void Update()
    {
        _smoothDeltaTime = Mathf.Lerp(_smoothDeltaTime, Time.unscaledDeltaTime, smoothingFactor);
    }

    private void OnGUI()
    {
        _fpsStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            normal =
            {
                textColor = Color.white
            }
        };

        var fps = 1.0f / _smoothDeltaTime;
        var fpsText = "FPS: " + fps.ToString("F0");
        var x = Screen.width - 140f;
        const float y = 10f;
        var rect = new Rect(x, y, 100f, 30f);
        _fpsStyle.normal.textColor = fps < 60f ? Color.red : Color.green;
        GUI.Label(rect, fpsText, _fpsStyle);
    }
}