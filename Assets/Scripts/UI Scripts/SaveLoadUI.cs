using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class SaveLoadUI : MonoBehaviour
{
    public enum UIMode { Save, Load }
    public UIMode currentMode;

    [Header("Mode Specific UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI actionButtonText;
    public GameObject nameInputArea;

    [Header("List Settings")]
    public GameObject saveItemPrefab; 
    public Transform scrollContent;
    public ScrollRect scrollRect;

    [Header("Preview Area")]
    public Image previewImage;
    public TextMeshProUGUI descriptionText;
    public TMP_InputField nameInput;

    [Header("Confirmation UI")]
    public GameObject confirmationPanel; 
    public TextMeshProUGUI confirmationText; 
    private string pendingSaveName;

    [Header("References")]
    public SaveManager saveManager;
    private string selectedFileName;
    private GameObject selectedItemObj; 

    [Header("Character Limits")]
    public int maxFileNameLength = 33;

    private void Awake()
    {
        SetupInputFieldLimit();
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    private void SetupInputFieldLimit()
    {
        if (nameInput == null) return;
        nameInput.characterLimit = maxFileNameLength;
        nameInput.onValueChanged.AddListener(ValidateInput);
    }

    private void ValidateInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return;

        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            if (input.Contains(c.ToString()))
            {
                string cleanInput = input.Replace(c.ToString(), "");
                nameInput.text = cleanInput;
                break;
            }
        }
    }

    public void OpenPanel(UIMode mode)
    {
        currentMode = mode;

        titleText.text = (mode == UIMode.Save) ? "Játék Mentése" : "Játék Betöltése";
        actionButtonText.text = (mode == UIMode.Save) ? "Mentés" : "Betöltés";

        nameInputArea.SetActive(mode == UIMode.Save);

        RefreshList();
        ClearPreview();
    }

    public void RefreshList()
    {
        if (scrollContent == null) return;

        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        string path = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        string[] files = Directory.GetFiles(path, "*.json");

        System.Array.Sort(files, (a, b) =>
            File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            GameObject item = Instantiate(saveItemPrefab, scrollContent);

            var textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string lastModified = File.GetLastWriteTime(file).ToString("yyyy.MM.dd HH:mm");
                textComponent.text = $"{fileName}\n<size=80%>{lastModified}</size>";
            }

            var button = item.GetComponent<Button>();
            if (button != null)
            {
                string fileNameCopy = fileName;
                GameObject itemObjCopy = item; 
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectSave(fileNameCopy, itemObjCopy));
            }
        }
    }

    public void SelectSave(string fileName, GameObject clickedItem)
    {
        selectedFileName = fileName;
        nameInput.text = fileName;

        if (selectedItemObj != null)
        {
            var oldImage = selectedItemObj.GetComponent<Image>();
            if (oldImage != null) oldImage.color = Color.white;
        }

        selectedItemObj = clickedItem;
        if (selectedItemObj != null)
        {
            var newImage = selectedItemObj.GetComponent<Image>();
            if (newImage != null) newImage.color = new Color(0.7f, 0.8f, 1f); 
        }

        string imgPath = Path.Combine(Application.persistentDataPath, "Saves", fileName + ".png");
        previewImage.sprite = LoadSprite(imgPath);
        previewImage.color = previewImage.sprite != null ? Color.white : Color.gray;

        string jsonPath = Path.Combine(Application.persistentDataPath, "Saves", fileName + ".json");
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            descriptionText.text = $"Mentés Neve:\n {fileName}\n\n" +
                                   $"<color=#FFD700>Kör:</color> {data.turnNumber}\n" +
                                   $"<color=#FFD700>Egységek:</color> {data.units.Count}\n" +
                                   $"<color=#FFD700>Épületek:</color> {data.buildings.Count}\n" +
                                   $"<color=#FFD700>Mentés dátuma:\n</color> {File.GetCreationTime(jsonPath):g}";
        }
        else
        {
            descriptionText.text = "Mentés sérült vagy hiányzik!";
        }
    }

    public void OnSaveButtonClicked()
    {
        if (string.IsNullOrEmpty(nameInput.text))
        {
            descriptionText.text = "<color=red>Kérem adjon meg egy nevet!</color>";
            return;
        }

        string fileName = nameInput.text;
        string filePath = Path.Combine(Application.persistentDataPath, "Saves", fileName + ".json");

        if (File.Exists(filePath))
        {
            pendingSaveName = fileName;
            confirmationPanel.SetActive(true);

            if (confirmationText != null)
            {
                confirmationText.text = $"Biztosan felülírja a mentést?";
            }
        }
        else
        {
            ExecuteSave(fileName);
        }
    }

    public void OnActionButtonClicked()
    {
        if (currentMode == UIMode.Save)
        {
            string fileName = nameInput.text;
            if (string.IsNullOrEmpty(fileName)) return;

            string path = Path.Combine(Application.persistentDataPath, "Saves", fileName + ".json");

            if (File.Exists(path))
            {
                OpenConfirmation($"Biztosan felülírja a mentést?", () => {
                    ExecuteSave(fileName);
                });
            }
            else
            {
                ExecuteSave(fileName);
            }
        }
        else 
        {
            if (string.IsNullOrEmpty(selectedFileName)) return;

            OpenConfirmation($"Biztosan beolvassa a mentést? A nem mentett haladás elveszik.", () => {
                ExecuteLoad(selectedFileName);
            });
        }
    }

    private System.Action onConfirmAction;

    private void OpenConfirmation(string message, System.Action confirmAction)
    {
        if (confirmationPanel == null) return;

        confirmationText.text = message;
        onConfirmAction = confirmAction;
        confirmationPanel.SetActive(true);
    }

    public void OnConfirmYes()
    {
        onConfirmAction?.Invoke();
        confirmationPanel.SetActive(false);
    }

    public void OnConfirmNo()
    {
        confirmationPanel.SetActive(false);
    }

    private void ExecuteSave(string fileName)
    {
        saveManager.SaveGame(fileName);
        RefreshList();
        descriptionText.text = $"<color=green>Játék sikeresen mentve: {fileName}</color>";
    }

    private void ExecuteLoad(string fileName)
    {
        saveManager.LoadGame(fileName);
        Object.FindFirstObjectByType<GameUIController>().CloseAllMenus();
    }

    public void ConfirmOverwrite()
    {
        ExecuteSave(pendingSaveName);
        confirmationPanel.SetActive(false);
    }

    public void CancelOverwrite()
    {
        pendingSaveName = "";
        confirmationPanel.SetActive(false);
    }


    public void OnDeleteButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedFileName))
        {
            descriptionText.text = "<color=red>Válasszon ki egy mentést a törléshez!</color>";
            return;
        }

        OpenConfirmation($"Biztosan törli a mentést?", () => {
            ExecuteDelete(selectedFileName);
        });
    }

    private void ExecuteDelete(string fileName)
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "Saves", fileName + ".json");
        string imgPath = Path.Combine(Application.persistentDataPath, "Saves", fileName + ".png");

        if (File.Exists(jsonPath)) File.Delete(jsonPath);
        if (File.Exists(imgPath)) File.Delete(imgPath);

        ClearPreview();
        RefreshList();

        descriptionText.text = $"<color=green>Mentés törölve!</color>";
    }

    private void ClearPreview()
    {
        selectedFileName = "";
        selectedItemObj = null;
        nameInput.text = "";
        previewImage.sprite = null;
        previewImage.color = Color.gray;
        descriptionText.text = "Válasszon egy mentést...";
    }

    private Sprite LoadSprite(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load sprite from {path}: {e.Message}");
        }

        return null;
    }
}