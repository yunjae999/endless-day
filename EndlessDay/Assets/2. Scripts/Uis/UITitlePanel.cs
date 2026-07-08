using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITitlePanel 자체의 UI만 담당 (게임 시작/게임 종료 버튼 참조 공개, 종료 처리).
/// 다른 패널로의 전환 로직은 모른다 ? TitleFrameManager가 담당.
/// </summary>
public class UITitlePanel : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _startGameButton;
    [SerializeField] private Button _quitGameButton;

    public Button StartGameButton
    {
        get { return _startGameButton; }
    }

    private void Awake()
    {
        _quitGameButton.onClick.AddListener(OnClickQuitGame);
    }

    private void OnClickQuitGame()
    {
        Application.Quit();
    }
}