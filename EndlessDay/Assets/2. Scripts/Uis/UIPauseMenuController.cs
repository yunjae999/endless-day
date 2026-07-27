using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ESC로 열리는 일시정지 메뉴. 종료/취소만 있는 단순한 형태. HUD 프리팹에 하나 배치.
/// 다른 모달(강화선택/상점/인벤토리/결과창)이 떠있으면 열리지 않음 - 한 번에 하나씩만.
/// </summary>
public class UIPauseMenuController : MonoBehaviour
{
    [SerializeField] GameObject _menuRoot;   // 평소 꺼져있음
    [SerializeField] Button _quitButton;
    [SerializeField] Button _cancelButton;

    void Awake()
    {
        _quitButton.onClick.AddListener(OnClickQuit);
        _cancelButton.onClick.AddListener(OnClickCancel);

        if (_menuRoot != null)
            _menuRoot.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (GameSession._instance.IsPauseMenuOpen)
        {
            CloseMenu();
            return;
        }

        // 다른 모달이 떠있으면 열지 않음
        if (GameSession._instance.IsPerkSelectionOpen) return;
        if (GameSession._instance.IsShopOpen) return;
        if (GameSession._instance.IsInventoryOpen) return;
        if (GameSession._instance.IsResultShown) return;

        OpenMenu();
    }

    void OpenMenu()
    {
        if (_menuRoot != null)
            _menuRoot.SetActive(true);

        GameSession._instance.SetPauseMenuOpen(true);
    }

    void CloseMenu()
    {
        if (_menuRoot != null)
            _menuRoot.SetActive(false);

        GameSession._instance.SetPauseMenuOpen(false);
    }

    void OnClickCancel()
    {
        CloseMenu();
    }

    void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
