using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Gley.Localization;

[Serializable]
public class AdvancedOption
{
    public string nameText; //Text for option

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="nameText">Text for option</param>
    public AdvancedOption(string nameText)
    {
        this.nameText = nameText;
    }
}

/// <summary>
/// Main script for dropdown
/// </summary>
public class AdvancedDropdown : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI targetText; //Text component

    [SerializeField] GameObject blockerPrefab; //Blocker object
    [SerializeField] RectTransform optionsObject; //Options object
    [SerializeField] RectTransform optionsContent; //Options content
    [SerializeField] bool isLocalPlay;
    [SerializeField] bool isCreatureSelection;
    [SerializeField] bool isLobbySelection;
    [SerializeField] bool langSelection;

    enum AnimationDropdownTypes
    { //Allowed animation types
        None,
        Shrinking,
        Fading,
        ShrinkingAndFading
    }

    [SerializeField] AnimationDropdownTypes dropdownAnimationType = AnimationDropdownTypes.Shrinking; //Currently selected animation

    GameObject firstObj;

    public List<AdvancedOption> optionsList = new List<AdvancedOption>(); //List of all options

    List<OptionScript> spawnedList = new List<OptionScript>(); //List of all spawned options

    public int value = -1; //Current value

    [SerializeField] float speedOfShrinking = 70; //Speed of shrinking
    [SerializeField] float speedOfFading = 4; //Speed of fading

    float targetPos; //Target position for shrinking
    float targetFade; //Target fade value
    float startPos; //Start position for shrinking
    float startFade; //Start fade value

    float ratioShrinking = 1; //Ratio of shrinking
    float ratioFading = 1; //Ratio of fading

    [SerializeField] float maximumDropdownHeight = 350; //Maximum dropdown height
    [SerializeField] AutoSizeLayoutDropdown optionsDropdown; //Options dropdown resizer

    [SerializeField] AnimationCurve curveShrinking; //Curve for shrinking trajectory

    RectTransform targetCanvas; //Target canvas

    GameObject currentBlocker; //Current blocker

    bool isOpened; //Is opened options

    public string defaultText; //Default text

    [SerializeField] Canvas backCanvasSorting; //Back canvas sorting object (Setted by default)
    [SerializeField] Canvas optionsCanvasSorting; //Options canvas sorting object (Setted by default)

    public Action<int> onChangedValue; //Action that executes when user select an option

    CanvasGroup optionCanvasGroup; //Option canvas group component

    RectTransform FindCanvas(RectTransform currentParent)
    {
        if (currentParent.GetComponent<Canvas>())
        {
            return currentParent;
        }

        return FindCanvas(currentParent.parent.GetComponent<RectTransform>());
    }

    void Start()
    {
        firstObj = optionsContent.GetChild(0).gameObject;
        firstObj.SetActive(false);
        optionsObject.gameObject.SetActive(false);
        targetCanvas = FindCanvas(GetComponent<RectTransform>());
        optionsCanvasSorting.overrideSorting = false;
        backCanvasSorting.overrideSorting = false;
        optionsCanvasSorting.sortingOrder = 100;
        backCanvasSorting.sortingOrder = 100;
        optionsObject.sizeDelta = new Vector2(optionsObject.sizeDelta.x, 0);
        if (value != -1)
        {
            SelectOption(value);
        }
        else
        {
            SetDefaultText();
        }
        optionCanvasGroup = optionsObject.GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Set default text "defaultText" to a dropdown and unselect the last selected option
    /// </summary>
    public void SetDefaultText()
    {
        value = -1;
        //targetText.text = defaultText;
    }

    /// <summary>
    /// Update for animations
    /// </summary>
    void Update()
    {
        optionsDropdown.maxSize = maximumDropdownHeight;
        if (!optionsObject.gameObject.activeSelf)
        {
            return;
        }
        switch (dropdownAnimationType)
        {
            case AnimationDropdownTypes.Shrinking:
                ratioShrinking = Mathf.Clamp(ratioShrinking + (speedOfShrinking * Time.deltaTime) * curveShrinking.Evaluate(ratioShrinking), 0, 1);
                optionsObject.sizeDelta = new Vector2(optionsObject.sizeDelta.x, Mathf.Lerp(startPos, targetPos, ratioShrinking));
                if (ratioShrinking > 0.99f && targetPos == 0)
                {
                    Closed();
                }
                break;
            case AnimationDropdownTypes.Fading:
                ratioFading = Mathf.Clamp(ratioFading + (speedOfFading * Time.deltaTime), 0, 1);
                optionCanvasGroup.alpha = Mathf.Lerp(startFade, targetFade, ratioFading);
                if (ratioFading > 0.99f && targetPos == 0)
                {
                    Closed();
                }
                break;
            case AnimationDropdownTypes.ShrinkingAndFading:
                ratioShrinking = Mathf.Clamp(ratioShrinking + (speedOfShrinking * Time.deltaTime) * curveShrinking.Evaluate(ratioShrinking), 0, 1);
                optionsObject.sizeDelta = new Vector2(optionsObject.sizeDelta.x, Mathf.Lerp(startPos, targetPos, ratioShrinking));

                ratioFading = Mathf.Clamp(ratioFading + (speedOfFading * Time.deltaTime), 0, 1);
                optionCanvasGroup.alpha = Mathf.Lerp(startFade, targetFade, ratioFading);
                if (ratioFading > 0.99f && ratioShrinking > 0.99f && targetPos == 0)
                {
                    Closed();
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Add one option
    /// </summary>
    /// <param name="targetOption">Target option text</param>
    public void AddOptions(string targetOption)
    {
        optionsList.Add(new AdvancedOption(targetOption));
    }

    /// <summary>
    /// Add an array of options
    /// </summary>
    /// <param name="targetOptions">Array of options</param>
    public void AddOptions(string[] targetOptions)
    {
        for (int i = 0; i < targetOptions.Length; i++)
        {
            optionsList.Add(new AdvancedOption(targetOptions[i]));
        }
    }

    /// <summary>
    /// Delete of options
    /// </summary>
    public void DeleteAllOptions()
    {
        optionsList.Clear();
    }

    /// <summary>
    /// Change state of the dropdown Open/Close
    /// </summary>
    public void ChangeState()
    {
        if (!isOpened)
        {
            OpenOptions();
        }
        else
        {
            CloseOptions();
        }
    }

    /// <summary>
    /// Open options method
    /// </summary>
    string tempName;
    public void OpenOptions()
    {
        if (optionsList.Count == 0)
        {
            return;
        }

        switch (dropdownAnimationType)
        {
            case AnimationDropdownTypes.None:

                break;
            case AnimationDropdownTypes.Shrinking:
                if (ratioShrinking < 0.99f && ratioShrinking > 0.01f)
                {
                    return;
                }
                break;
            case AnimationDropdownTypes.Fading:
                if (ratioFading < 0.99f && ratioFading > 0.01f)
                {
                    return;
                }
                break;
            case AnimationDropdownTypes.ShrinkingAndFading:
                if (ratioFading < 0.99f && ratioFading > 0.01f && ratioShrinking < 0.99f && ratioShrinking > 0.01f)
                {
                    return;
                }
                break;
        }

        isOpened = true;
        spawnedList.Clear();
        for (int i = 0; i < optionsList.Count; i++)
        {
            var newOption = Instantiate(firstObj, optionsContent).GetComponent<OptionScript>();
            newOption.gameObject.SetActive(true);

            if (isLobbySelection)
            {
                switch (i)
                {
                    case 0:
                        tempName = API.GetText(WordIDs.Plain_Bg_Id);
                        break;
                    case 1:
                        tempName = API.GetText(WordIDs.Random_Creature_Id);
                        break;
                }

                newOption.InitOption(i, tempName.ToString());

            }
            else if (isLocalPlay && isCreatureSelection)
            {
                switch (i)
                {
                    case 0:
                        tempName = API.GetText(WordIDs.Creature_0_Id);
                        break;
                    case 1:
                        tempName = API.GetText(WordIDs.Creature_1_Id);
                        break;
                    case 2:
                        tempName = API.GetText(WordIDs.Creature_2_Id);
                        break;
                    case 3:
                        tempName = API.GetText(WordIDs.Creature_3_Id);
                        break;
                    case 4:
                        tempName = API.GetText(WordIDs.Creature_4_Id);
                        break;
                    case 5:
                        tempName = API.GetText(WordIDs.Creature_5_Id);
                        break;
                    case 6:
                        tempName = API.GetText(WordIDs.Creature_6_Id);
                        break;
                    case 7:
                        tempName = API.GetText(WordIDs.Creature_7_Id);
                        break;
                    case 8:
                        tempName = API.GetText(WordIDs.Creature_8_Id);
                        break;
                    case 9:
                        tempName = API.GetText(WordIDs.Creature_9_Id);
                        break;
                    case 10:
                        tempName = API.GetText(WordIDs.Creature_10_Id);
                        break;
                    case 11:
                        tempName = API.GetText(WordIDs.Creature_11_Id);
                        break;
                    case 12:
                        tempName = API.GetText(WordIDs.Creature_12_Id);
                        break;
                    case 13:
                        tempName = API.GetText(WordIDs.Creature_13_Id);
                        break;
                    case 14:
                        tempName = API.GetText(WordIDs.Creature_14_Id);
                        break;
                    case 15:
                        tempName = API.GetText(WordIDs.Creature_15_Id);
                        break;
                    case 16:
                        tempName = API.GetText(WordIDs.Creature_16_Id);
                        break;
                    case 17:
                        tempName = API.GetText(WordIDs.Creature_17_Id);
                        break;
                    case 18:
                        tempName = API.GetText(WordIDs.Creature_18_Id);
                        break;
                    case 19:
                        tempName = API.GetText(WordIDs.Creature_19_Id);
                        break;
                }
                newOption.InitOption(i, tempName.ToString());

            }
            else if (isLocalPlay)
            {
                switch (i)
                {
                    case 0:
                        tempName = API.GetText(WordIDs.Players2_Id);
                        break;
                    case 1:
                        tempName = API.GetText(WordIDs.Players3_Id);
                        break;
                    case 2:
                        tempName = API.GetText(WordIDs.Players4_Id);
                        break;
                    case 3:
                        tempName = API.GetText(WordIDs.Players5_Id);
                        break;
                }
                newOption.InitOption(i, tempName.ToString());
            }
            else if (langSelection)
            {
                switch (i)
                {
                    case 0:
                        tempName = "English";
                        break;
                    case 1:
                        tempName = "Deutsch";
                        break;
                    case 2:
                        tempName = "Italiano";
                        break;
                    case 3:
                        tempName = "Français";
                        break;
                    case 4:
                        tempName = "Español";
                        break;
                }
                newOption.InitOption(i, tempName.ToString());
            }
            else
                newOption.InitOption(i, optionsList[i].nameText);
            spawnedList.Add(newOption);
        }
        optionsObject.GetChild(0).GetComponent<AutoSizeLayoutDropdown>().UpdateAllRect();
        StartCoroutine(WaitForSeveralFrames());
        /*   currentBlocker = Instantiate(blockerPrefab, targetCanvas);
           currentBlocker.GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(CloseOptions));
           currentBlocker.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
           currentBlocker.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);*/
    }

    IEnumerator WaitForSeveralFrames()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        optionsObject.gameObject.SetActive(true);
        optionsCanvasSorting.overrideSorting = true;
        backCanvasSorting.overrideSorting = true;
        optionsCanvasSorting.sortingOrder = 30000;
        backCanvasSorting.sortingOrder = 30000;
        ratioShrinking = 0;
        ratioFading = 0;
        startPos = optionsObject.sizeDelta.y;
        startFade = 0;
        targetPos = optionsObject.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
        targetFade = 1;
        if (dropdownAnimationType == AnimationDropdownTypes.None || dropdownAnimationType == AnimationDropdownTypes.Fading)
        {
            optionsObject.sizeDelta = new Vector2(optionsObject.sizeDelta.x, optionsObject.GetChild(0).GetComponent<RectTransform>().sizeDelta.y);
        }
        if (dropdownAnimationType == AnimationDropdownTypes.Shrinking || dropdownAnimationType == AnimationDropdownTypes.None)
        {
            optionCanvasGroup.alpha = 1;
        }
    }

    /// <summary>
    /// Close options method
    /// </summary>
    public void CloseOptions()
    {
        switch (dropdownAnimationType)
        {
            case AnimationDropdownTypes.None:

                break;
            case AnimationDropdownTypes.Shrinking:
                if (ratioShrinking < 0.99f && ratioShrinking > 0.01f)
                {
                    return;
                }
                break;
            case AnimationDropdownTypes.Fading:
                if (ratioFading < 0.99f && ratioFading > 0.01f)
                {
                    return;
                }
                break;
            case AnimationDropdownTypes.ShrinkingAndFading:
                if (ratioFading < 0.99f && ratioFading > 0.01f && ratioShrinking < 0.99f && ratioShrinking > 0.01f)
                {
                    return;
                }
                break;
        }

        isOpened = false;
        ratioShrinking = 0;
        ratioFading = 0;
        startPos = optionsObject.sizeDelta.y;
        startFade = 1;
        targetPos = 0;
        targetFade = 0;
        //Destroy(currentBlocker);
        if (dropdownAnimationType == AnimationDropdownTypes.None)
        {
            optionsObject.sizeDelta = new Vector2(optionsObject.sizeDelta.x, 0);
            Closed();
        }
    }

    void Closed()
    {
        optionsCanvasSorting.overrideSorting = false;
        backCanvasSorting.overrideSorting = false;
        optionsCanvasSorting.sortingOrder = 100;
        backCanvasSorting.sortingOrder = 100;
        optionsObject.gameObject.SetActive(false);
        for (int i = 0; i < spawnedList.Count; i++)
        {
            Destroy(spawnedList[i].gameObject);
        }
        spawnedList.Clear();
        if (dropdownAnimationType == AnimationDropdownTypes.Fading)
        {
            optionsObject.sizeDelta = new Vector2(optionsObject.sizeDelta.x, 0);
        }
    }

    /// <summary>
    /// Select option
    /// </summary>
    /// <param name="id">ID of the option</param>
    string selectedNum;
    public void SelectOption(int id)
    {
        switch (dropdownAnimationType)
        {
            case AnimationDropdownTypes.None:

                break;
            case AnimationDropdownTypes.Shrinking:
                if (ratioShrinking < 0.99f)
                {
                    return;
                }
                break;
            case AnimationDropdownTypes.Fading:
                if (ratioFading < 0.99f)
                {
                    return;
                }
                break;
            case AnimationDropdownTypes.ShrinkingAndFading:
                if (ratioFading < 0.99f && ratioShrinking < 0.99f)
                {
                    return;
                }
                break;
        }
        value = id;

        if (isLobbySelection)
        {
            switch (id)
            {
                case 0:
                    selectedNum = API.GetText(WordIDs.Plain_Bg_Id);
                    break;
                case 1:
                    selectedNum = API.GetText(WordIDs.Random_Creature_Id);
                    break;
            }

            targetText.text = selectedNum;
        }
        else if (isLocalPlay && isCreatureSelection)
        {
            switch (id)
            {
                case 0:
                    selectedNum = API.GetText(WordIDs.Creature_0_Id);
                    break;
                case 1:
                    selectedNum = API.GetText(WordIDs.Creature_1_Id);
                    break;
                case 2:
                    selectedNum = API.GetText(WordIDs.Creature_2_Id);
                    break;
                case 3:
                    selectedNum = API.GetText(WordIDs.Creature_3_Id);
                    break;
                case 4:
                    selectedNum = API.GetText(WordIDs.Creature_4_Id);
                    break;
                case 5:
                    selectedNum = API.GetText(WordIDs.Creature_5_Id);
                    break;
                case 6:
                    selectedNum = API.GetText(WordIDs.Creature_6_Id);
                    break;
                case 7:
                    selectedNum = API.GetText(WordIDs.Creature_7_Id);
                    break;
                case 8:
                    selectedNum = API.GetText(WordIDs.Creature_8_Id);
                    break;
                case 9:
                    selectedNum = API.GetText(WordIDs.Creature_9_Id);
                    break;
                case 10:
                    selectedNum = API.GetText(WordIDs.Creature_10_Id);
                    break;
                case 11:
                    selectedNum = API.GetText(WordIDs.Creature_11_Id);
                    break;
                case 12:
                    selectedNum = API.GetText(WordIDs.Creature_12_Id);
                    break;
                case 13:
                    selectedNum = API.GetText(WordIDs.Creature_13_Id);
                    break;
                case 14:
                    selectedNum = API.GetText(WordIDs.Creature_14_Id);
                    break;
                case 15:
                    selectedNum = API.GetText(WordIDs.Creature_15_Id);
                    break;
                case 16:
                    selectedNum = API.GetText(WordIDs.Creature_16_Id);
                    break;
                case 17:
                    selectedNum = API.GetText(WordIDs.Creature_17_Id);
                    break;
                case 18:
                    selectedNum = API.GetText(WordIDs.Creature_18_Id);
                    break;
                case 19:
                    selectedNum = API.GetText(WordIDs.Creature_19_Id);
                    break;
            }

            targetText.text = selectedNum;
            LocalDrawManager.instance.creaturePanelStartGame.SetActive(true);

        }
        else if (isLocalPlay)
        {
            switch (id)
            {
                case 0:
                    selectedNum = API.GetText(WordIDs.Players2_Id);
                    break;
                case 1:
                    selectedNum = API.GetText(WordIDs.Players3_Id);
                    break;
                case 2:
                    selectedNum = API.GetText(WordIDs.Players4_Id);
                    break;
                case 3:
                    selectedNum = API.GetText(WordIDs.Players5_Id);
                    break;
            }

            targetText.text = selectedNum;
        }
        else if (langSelection)
        {
            switch (id)
            {
                case 0:
                    selectedNum = "English";
                    break;
                case 1:
                    selectedNum = "Deutsch";
                    break;
                case 2:
                    selectedNum = "Italiano";
                    break;
                case 3:
                    selectedNum = "Français";
                    break;
                case 4:
                    selectedNum = "Español";
                    break;
            }
            targetText.text = selectedNum;
        }
        else
        {
            targetText.text = optionsList[id].nameText;

        }
        for (int i = 0; i < spawnedList.Count; i++)
        {
            spawnedList[i].SetSelectState(i == value);
        }
        CloseOptions();
        if (onChangedValue != null)
        {
            onChangedValue(id);
        }
    }
}
