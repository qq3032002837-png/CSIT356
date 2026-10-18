using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple "+" crosshair in the centre of the screen, where the shotgun's
/// pellets are aimed.
///
/// It builds its own overlay canvas when the game starts, so no scene or
/// prefab has to change. It hides while the mouse cursor is free, which is
/// when the death screen is showing.
/// </summary>
public class Crosshair : MonoBehaviour
{
    private const float ArmLength = 10f;
    private const float ArmThickness = 2f;
    private const float CentreGap = 4f;

    // Drawn above the HUD, which uses the default sort order.
    private const int SortOrder = 100;

    private static readonly Color ArmColour = new Color(1f, 1f, 1f, 0.85f);

    private GameObject arms;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject host = new GameObject("Crosshair");
        DontDestroyOnLoad(host);
        host.AddComponent<Crosshair>();
    }

    private void Awake()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortOrder;

        arms = new GameObject("Arms", typeof(RectTransform));
        arms.transform.SetParent(transform, false);

        float offset = CentreGap + ArmLength / 2f;
        AddArm(new Vector2(0f, offset), new Vector2(ArmThickness, ArmLength));
        AddArm(new Vector2(0f, -offset), new Vector2(ArmThickness, ArmLength));
        AddArm(new Vector2(offset, 0f), new Vector2(ArmLength, ArmThickness));
        AddArm(new Vector2(-offset, 0f), new Vector2(ArmLength, ArmThickness));
    }

    private void Update()
    {
        arms.SetActive(Cursor.lockState == CursorLockMode.Locked);
    }

    private void AddArm(Vector2 position, Vector2 size)
    {
        GameObject arm = new GameObject("Arm", typeof(RectTransform), typeof(Image));
        arm.transform.SetParent(arms.transform, false);

        RectTransform rect = arm.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = arm.GetComponent<Image>();
        image.color = ArmColour;
        image.raycastTarget = false;
    }
}
