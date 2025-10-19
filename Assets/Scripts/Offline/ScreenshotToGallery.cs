using UnityEngine;
using System.IO;

public class ScreenshotToGallery : MonoBehaviour
{
    public GameObject saveBtn, homeBtn;
    public GameObject savedPanel;
    string fileName = "MyScreenshot.png";

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureAndSave());
    }

    private System.Collections.IEnumerator CaptureAndSave()
    {
        saveBtn.SetActive(false);
        homeBtn.SetActive(false);
        
        yield return new WaitForSeconds(0.2f);


        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        NativeGallery.SaveImageToGallery(tex, "My Game Screenshots", fileName);

        yield return new WaitForSeconds(0.2f);

        // Cleanup
        Destroy(tex);
        savedPanel.SetActive(true);
        homeBtn.SetActive(true);
    }
}
