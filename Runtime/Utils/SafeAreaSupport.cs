using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UuIiView
{
    [RequireComponent(typeof(CanvasScaler))]
    public class SafeAreaSupport : MonoBehaviour
    {
        RectTransform targetContent;

        Vector2 offsetUpperRight = new Vector2(-132f, 0f);
        Vector2 offsetLowerLeft = new Vector2(132f, 63f);

        //ScreenOrientation orientation = ScreenOrientation.AutoRotation;

        void Awake()
        {
            targetContent = transform.Find("Content").GetComponent<RectTransform>();
            StartCoroutine(SetSafeArea());
        }

        // Remove comments if allows change orientation in running app.
        //void Update()
        //{
        //    if (Screen.orientation != orientation)
        //    {
        //        StartCoroutine(SetSafeArea());
        //    }
        //}

        IEnumerator SetSafeArea()
        {
            // Wait to endframe for Screen.width and Screen.height updated.
            yield return new WaitForEndOfFrame();
            
            var canvasScaler = GetComponent<CanvasScaler>();
            if ( IsPortrait() )
            {
                // Portrait
                var max = Mathf.Max(canvasScaler.referenceResolution.x, canvasScaler.referenceResolution.y);
                var min = Mathf.Min(canvasScaler.referenceResolution.x, canvasScaler.referenceResolution.y);
                canvasScaler.referenceResolution = new Vector2(min, max);

                float scale = Screen.width / canvasScaler.referenceResolution.x;
                offsetUpperRight = new Vector2(0f, (Screen.safeArea.height + Screen.safeArea.y - Screen.height) / scale);
                offsetLowerLeft = new Vector2(0f, Screen.safeArea.y / scale);

                //Debug.Log($"[SetSafeArea] Portrait : max={max},min={min},scale={scale} | Screen({Screen.width},{Screen.height}) Canvas {canvasScaler.referenceResolution}");
            }
            else
            {
                // Landscape
                var max = Mathf.Max(canvasScaler.referenceResolution.x, canvasScaler.referenceResolution.y);
                var min = Mathf.Min(canvasScaler.referenceResolution.x, canvasScaler.referenceResolution.y);
                canvasScaler.referenceResolution = new Vector2(max, min);

                float scale = Screen.height / canvasScaler.referenceResolution.y;
                offsetUpperRight = new Vector2(-Screen.safeArea.x / scale, 0f);
                offsetLowerLeft = new Vector2(Screen.safeArea.x / scale, Screen.safeArea.y / scale);

                //Debug.Log($"[SetSafeArea] Landscape : max={max},min={min},scale={scale} | Screen({Screen.width},{Screen.height}) Canvas {canvasScaler.referenceResolution}");
            }

            targetContent.offsetMax = offsetUpperRight;
            targetContent.offsetMin = offsetLowerLeft;

            //orientation = Screen.orientation;
        }

        bool IsPortrait()
        {
#if UNITY_EDITOR
            return (Screen.height > Screen.width);
#else
            return (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown);
#endif
        }
    }
}