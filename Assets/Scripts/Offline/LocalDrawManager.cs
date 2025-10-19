using TMPro;
using UnityEngine;
using Gley.Localization;
using UnityEngine.UI;
using System.Collections;

public class LocalDrawManager : MonoBehaviour
{
    public static LocalDrawManager instance;
    public Camera cam;
    public int totalParts = 5;
    private int currentPart = 0;

    [Header("Line Renderer")]
    public localLineRenderer lineRenderer;

    public TMP_Text bodyPartTxt;
    public GameObject shareBtn;
    public GameObject homeBtn;
    public int totalPlayers = 2;

    [Header("Objects To TurnOff In Portrait")]
    public GameObject[] offObjs;

    private float moveStep = 4.5f;

    [Header("Color Pallet")]
    public ColorPaletteController colorPallet;
    public GameObject colorPalletPanel;


    [Header("Ui Panels")]
    public GameObject localPlayPanel;
    public GameObject modeSelectionPanel;
    public GameObject hostPanel;
    public GameObject premiumPanel;
    public GameObject premiumBtn;
    public GameObject localPlayGetPremiumPanel;

    [Header("Turn System")]
    public GameObject playerSelectionPopup;
    public AdvancedDropdown playerDropdown;
    public Button playerConfirmButton;

    public GameObject passDevicePopup;
    public TMP_Text passDeviceText;
    public Button passDeviceConfirmButton;

    private int currentPlayer = 1;

    [Header("Mystery Creatures")]
    public TMP_Text titleTxt;
    public GameObject creatureSelPanel;
    public GameObject creaturePanelStartGame;
    public GameObject lvlCompleteBgCanvas;
    public Image lvlCompleteBgImg;
    public AdvancedDropdown creatureDropdown;

    public Sprite[] mysteryCreatures;
    private int creatureIndex = -1;


    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        playerSelectionPopup.SetActive(true);
        playerConfirmButton.onClick.AddListener(OnPlayerSelectionConfirmed);
        passDeviceConfirmButton.onClick.AddListener(OnPassConfirmed);
    }

    private void OnPlayerSelectionConfirmed()
    {
        if (playerDropdown.value != -1)
        {
            totalPlayers = playerDropdown.value + 2;
            currentPlayer = 1;
            playerSelectionPopup.SetActive(false);

            if (PlayerPrefs.GetInt("BuyPremium", 0) == 1)
                creatureSelPanel.SetActive(true);
            else
            {
                UpdateBodyPartTxt();
                lineRenderer.ClearAllLines();
                lineRenderer.canDraw = true;
            }
        }
    }

    public void OnCreatureSelected(int selection)
    {
        switch (selection)
        {
            case 0:
                creatureIndex = -1;
                break;
            case 1:
                creatureIndex = Random.Range(0, mysteryCreatures.Length);
                break;
            case 2:
                creatureIndex = creatureDropdown.value;
                break;

        }
        DisableCreaturePanel();
    }

    public void DisableCreaturePanel()
    {
        creatureSelPanel.SetActive(false);
        SelectTitle(creatureIndex);
        UpdateBodyPartTxt();
        lineRenderer.ClearAllLines();
        lineRenderer.canDraw = true;
    }

    void SelectTitle(int index)
    {
        switch (index)
        {
            case 0:
                titleTxt.text = API.GetText(WordIDs.Creature_0_Id);
                break;
            case 1:
                titleTxt.text = API.GetText(WordIDs.Creature_1_Id);
                break;
            case 2:
                titleTxt.text = API.GetText(WordIDs.Creature_2_Id);
                break;
            case 3:
                titleTxt.text = API.GetText(WordIDs.Creature_3_Id);
                break;
            case 4:
                titleTxt.text = API.GetText(WordIDs.Creature_4_Id);
                break;
            case 5:
                titleTxt.text = API.GetText(WordIDs.Creature_5_Id);
                break;
            case 6:
                titleTxt.text = API.GetText(WordIDs.Creature_6_Id);
                break;
            case 7:
                titleTxt.text = API.GetText(WordIDs.Creature_7_Id);
                break;
            case 8:
                titleTxt.text = API.GetText(WordIDs.Creature_8_Id);
                break;
            case 9:
                titleTxt.text = API.GetText(WordIDs.Creature_9_Id);
                break;
            case 10:
                titleTxt.text = API.GetText(WordIDs.Creature_10_Id);
                break;
            case 11:
                titleTxt.text = API.GetText(WordIDs.Creature_11_Id);
                break;
            case 12:
                titleTxt.text = API.GetText(WordIDs.Creature_12_Id);
                break;
            case 13:
                titleTxt.text = API.GetText(WordIDs.Creature_13_Id);
                break;
            case 14:
                titleTxt.text = API.GetText(WordIDs.Creature_14_Id);
                break;
            case 15:
                titleTxt.text = API.GetText(WordIDs.Creature_15_Id);
                break;
            case 16:
                titleTxt.text = API.GetText(WordIDs.Creature_16_Id);
                break;
            case 17:
                titleTxt.text = API.GetText(WordIDs.Creature_17_Id);
                break;
            case 18:
                titleTxt.text = API.GetText(WordIDs.Creature_18_Id);
                break;
            case 19:
                titleTxt.text = API.GetText(WordIDs.Creature_19_Id);
                break;
        }
    }

    public void OnCompletePart()
    {
        if (isEnabled)
        {
            EnableColorPallet();
            return;
        }

        if (currentPart < totalParts)
        {
            currentPart++;
            UpdateBodyPartTxt();
            lineRenderer.RemoveSaves();


            if (currentPart < totalParts)
            {
                lineRenderer.lineColor = Color.black;
                cam.transform.position -= new Vector3(0, moveStep, 0);

                currentPlayer++;
                if (currentPlayer > totalPlayers)
                    currentPlayer = 1;

                ShowPassDevicePopup();
            }
            else
            {
                FinishDrawing();
            }
        }
    }

    string heading;
    private void ShowPassDevicePopup()
    {
        switch (currentPlayer)
        {
            case 1:
                heading = API.GetText(WordIDs.PassDevicePlayer1_Id);
                break;
            case 2:
                heading = API.GetText(WordIDs.PassDevicePlayer2_Id);
                break;
            case 3:
                heading = API.GetText(WordIDs.PassDevicePlayer3_Id);
                break;
            case 4:
                heading = API.GetText(WordIDs.PassDevicePlayer4_Id);
                break;
            case 5:
                heading = API.GetText(WordIDs.PassDevicePlayer5_Id);
                break;
        }
        passDeviceText.text = heading;
        passDevicePopup.SetActive(true);
    }
    bool isEnabled = false;
    public void EnableColorPallet()
    {
        if (PlayerPrefs.GetInt("BuyPremium", 0) == 1)
        {
            if (!isEnabled)
            {
                colorPalletPanel.SetActive(true);
                lineRenderer.canDraw = false;
                isEnabled = true;
            }
            else
            {
                colorPalletPanel.SetActive(false);
                lineRenderer.canDraw = true;
                isEnabled = false;
            }
        }
        else
        {
            localPlayGetPremiumPanel.SetActive(true);
        }
    }

    public void UpdateToBlack()
    {
        colorPalletPanel.SetActive(false);
        lineRenderer.canDraw = true;
        lineRenderer.lineColor = Color.black;
        isEnabled = false;
    }

    public void UpdateDrawColor()
    {
        lineRenderer.lineColor = colorPallet.SelectedColor;
    }

    private void OnPassConfirmed()
    {
        passDevicePopup.SetActive(false);
    }

    private void FinishDrawing()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        lineRenderer.canDraw = false;
        Invoke(nameof(DoRemaining), 0.2f);
    }

    void DoRemaining()
    {
        if (creatureIndex != -1)
        {
            lvlCompleteBgImg.sprite = mysteryCreatures[creatureIndex];
            lvlCompleteBgCanvas.SetActive(true);
        }
        Vector3 pos = cam.transform.position;
        cam.transform.position = new Vector3(pos.x, pos.y / 2f, pos.z);
        shareBtn.SetActive(true);
        homeBtn.SetActive(true);
        cam.orthographicSize = 12f;
        foreach (GameObject obj in offObjs)
        {
            obj.SetActive(false);
        }
    }

    public void HomeBtn()
    {
        lineRenderer.ClearAllLines();
        Screen.orientation = ScreenOrientation.LandscapeRight;

        localPlayPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
        homeBtn.SetActive(false);
        shareBtn.SetActive(false);
        cam.orthographicSize = 3f;

        foreach (GameObject obj in offObjs)
        {
            obj.SetActive(true);
        }

        currentPart = 0;
        UpdateBodyPartTxt();
    }

    void UpdateBodyPartTxt()
    {
        switch (currentPart)
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

    public void QuitApplication()
    {
        Application.Quit();
    }
}
