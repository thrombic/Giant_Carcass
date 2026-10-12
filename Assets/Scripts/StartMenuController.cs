using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class StartMenuController : MonoBehaviour
{
    [Header("Menu Setup")]
    public GameObject startMenuCanvas;

    [Header("Menu Elements")]
    public TextMeshProUGUI[] menuTexts;
    public GameObject[] underlineMarkers;

    private int selectedIndex = 0;
    private bool isMenuVisualActive = true;

    void Start()
    {
        startMenuCanvas.SetActive(true);
        Time.timeScale = 0f;
        // Initialize the underline graphic
        UpdateMenuVisuals();
    }

    void Update()
    {
        if (!isMenuVisualActive) return;
        Keyboard keyboard = Keyboard.current;
        // Safeguard if no keyboard is plugged in
        if (keyboard == null) return; 

        // event lsiteners for up and down
        // check it up or down and iterate through the array of options
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = menuTexts.Length - 1;
            UpdateMenuVisuals();
        }
        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            selectedIndex++;
            if (selectedIndex >= menuTexts.Length) selectedIndex = 0;
            UpdateMenuVisuals();
        }

        // Enterkey listener for choice.
        // This can be changed to whatever we need it to be
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
        {
            ChooseOption();
        }
    }

    // checks the length of the options and creates underlines.
    void UpdateMenuVisuals()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (underlineMarkers != null && i < underlineMarkers.Length)
            {
                underlineMarkers[i].SetActive(i == selectedIndex);
            }
        }
    }

    void ChooseOption()
    {
        switch (selectedIndex)
        {
            case 0: // New Game
                Debug.Log("Starting Carcass playground/gym!");
                StartGame();
                break;
            case 1: // Options
                Debug.Log("Options placeholder selected.");
                break;
            case 2: // Cool Stuff
                Debug.Log("Cool Stuff placeholder selected.");
                break;
            case 3: // Credits
                Debug.Log("Credits placeholder selected.");
                break;
        }
    }

    void StartGame()
    {
        isMenuVisualActive = false;
        startMenuCanvas.SetActive(false); // Make the menu disappear
        Time.timeScale = 1f;              // Unfreeze time so your sprites can move!
    }
}