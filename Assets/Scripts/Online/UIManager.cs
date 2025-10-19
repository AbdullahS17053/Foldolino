using Gley.Localization;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameplayManager gamePlayManager;
    public RoomManager roomManager;

    [Header("Color Pallet")]
    public ColorPaletteController colorPallet;
    public GameObject colorPalletPanel;
    public GameObject getPremiumPanel;
    [HideInInspector] public bool isColorPalletEnabled = false;

    [Header("Draw Message")]
    public GameObject drawMessage;

    public TMP_Text waitingText;
    public TMP_Text bodyPartTxt;
    public GameObject waitingPanel;
    public RectTransform drawingArea;

    [Header("Drawing Completed")]
    public GameObject shareBtn;
    public GameObject homeBtn;
    public GameObject savedPanel;
    [HideInInspector] string fileName = "MyScreenshot.png";
    public GameObject[] navigationBtns;
    public GameObject[] offObjs;

    [Header("Mystery Creatures")]
    public TMP_Text titleText;
    public GameObject mysteryCreaturePanel;
    public Image mysteryCreatureBg;
    public List<Sprite> mysteryCreatureBgs = new();

    [Header("Player Left UI")]
    public GameObject playerLeftPanel;
    public TMP_Text playerLeftText;
    public GameObject quitPanel;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowWaitingMessage(string msg)
    {
        if (waitingText != null && waitingPanel != null)
        {
            waitingPanel.SetActive(true);
            waitingText.text = msg;
        }
    }

    public void HideWaitingMessage()
    {
        if (waitingPanel != null)
            waitingPanel.gameObject.SetActive(false);
    }

    public void ShowGameOver()
    {

    }

    public void Undo()
    {
        for (int i = 0; i < gamePlayManager.allSurfaces.Count; i++)
        {
            if (gamePlayManager.allSurfaces[i].lineDrawer.canDraw)
                gamePlayManager.allSurfaces[i].lineDrawer.UndoLine();
        }
    }

    public void Redo()
    {
        for (int i = 0; i < gamePlayManager.allSurfaces.Count; i++)
        {
            if (gamePlayManager.allSurfaces[i].lineDrawer.canDraw)
                gamePlayManager.allSurfaces[i].lineDrawer.RedoLine();
        }
    }

    public void UpdateBodyPartTxt(int index)
    {
        switch (index - 1)
        {
            case 0:
                bodyPartTxt.text = API.GetText(WordIDs.Hat_Id);
                break;
            case 1:
                bodyPartTxt.text = API.GetText(WordIDs.Head_Id);
                break;
            case 2:
                bodyPartTxt.text = API.GetText(WordIDs.UpperBody_Id);
                break;
            case 3:
                bodyPartTxt.text = API.GetText(WordIDs.Legs_Id);
                break;
            case 4:
                bodyPartTxt.text = API.GetText(WordIDs.Feet_Id);
                break;
        }
    }

    public void UpdateTitleText(int index)
    {
        switch (index)
        {
            case 0:
                titleText.text = API.GetText(WordIDs.Creature_0_Id);
                break;
            case 1:
                titleText.text = API.GetText(WordIDs.Creature_1_Id);
                break;
            case 2:
                titleText.text = API.GetText(WordIDs.Creature_2_Id);
                break;
            case 3:
                titleText.text = API.GetText(WordIDs.Creature_3_Id);
                break;
            case 4:
                titleText.text = API.GetText(WordIDs.Creature_4_Id);
                break;
            case 5:
                titleText.text = API.GetText(WordIDs.Creature_5_Id);
                break;
            case 6:
                titleText.text = API.GetText(WordIDs.Creature_6_Id);
                break;
            case 7:
                titleText.text = API.GetText(WordIDs.Creature_7_Id);
                break;
            case 8:
                titleText.text = API.GetText(WordIDs.Creature_8_Id);
                break;
            case 9:
                titleText.text = API.GetText(WordIDs.Creature_9_Id);
                break;
            case 10:
                titleText.text = API.GetText(WordIDs.Creature_10_Id);
                break;
            case 11:
                titleText.text = API.GetText(WordIDs.Creature_11_Id);
                break;
            case 12:
                titleText.text = API.GetText(WordIDs.Creature_12_Id);
                break;
            case 13:
                titleText.text = API.GetText(WordIDs.Creature_13_Id);
                break;
            case 14:
                titleText.text = API.GetText(WordIDs.Creature_14_Id);
                break;
            case 15:
                titleText.text = API.GetText(WordIDs.Creature_15_Id);
                break;
            case 16:
                titleText.text = API.GetText(WordIDs.Creature_16_Id);
                break;
            case 17:
                titleText.text = API.GetText(WordIDs.Creature_17_Id);
                break;
            case 18:
                titleText.text = API.GetText(WordIDs.Creature_18_Id);
                break;
            case 19:
                titleText.text = API.GetText(WordIDs.Creature_19_Id);
                break;
        }
    }

    public void UpdateBgOnComplete(int index)
    {
        mysteryCreaturePanel.SetActive(true);
        mysteryCreatureBg.sprite = mysteryCreatureBgs[PlayerDataStore.Instance.assignedCreatureIndices[index]];
    }
    public void EnableColorPallet()
    {
        if (PlayerPrefs.GetInt("BuyPremium", 0) == 1)
        {
            if (!isColorPalletEnabled)
            {
                colorPalletPanel.SetActive(true);
                gamePlayManager.CurrentDrawingSurface().lineDrawer.canDraw = false;
                isColorPalletEnabled = true;
            }
            else
            {
                colorPalletPanel.SetActive(false);
                gamePlayManager.CurrentDrawingSurface().lineDrawer.canDraw = true;
                isColorPalletEnabled = false;
            }
        }
        else
            getPremiumPanel.SetActive(true);
    }

    public void UpdateToBlack()
    {
        colorPalletPanel.SetActive(false);
        gamePlayManager.CurrentDrawingSurface().lineDrawer.canDraw = true;
        gamePlayManager.CurrentDrawingSurface().lineDrawer.lineColor = Color.black;
        isColorPalletEnabled = false;
    }

    public void ShowDrawMessage()
    {
        StartCoroutine(ShowMessage());
    }

    IEnumerator ShowMessage()
    {
        drawMessage.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        drawMessage.SetActive(false);
    }

    public void UpdateDrawColor()
    {
        gamePlayManager.CurrentDrawingSurface().lineDrawer.lineColor = colorPallet.SelectedColor;
    }

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureAndSave());
    }

    private IEnumerator CaptureAndSave()
    {
        shareBtn.SetActive(false);
        homeBtn.SetActive(false);

        foreach (GameObject btn in navigationBtns)
            btn.SetActive(false);

        yield return new WaitForSeconds(0.2f);


        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        NativeGallery.SaveImageToGallery(tex, "My Game Screenshots", fileName);

        yield return new WaitForSeconds(0.2f);

        // Cleanup
        Destroy(tex);
        savedPanel.SetActive(true);
        shareBtn.SetActive(true);
        homeBtn.SetActive(true);
        foreach (GameObject btn in navigationBtns)
            btn.SetActive(true);
    }

    public void ShowPlayerLeftMessage(string message)
    {
        playerLeftPanel.SetActive(true);
        playerLeftText.text = message;
    }
}
