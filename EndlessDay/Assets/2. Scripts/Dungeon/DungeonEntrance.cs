using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 마을의 던전 입구. 플레이어가 범위 안에 들어오면 "E 입장" 프롬프트를 보여주고,
/// E를 누르면 이번 시도용 상태(레벨/경험치/강화)를 초기화하고 던전씬으로 전환한다.
/// 콜라이더(Is Trigger)로 범위 감지 - 상점 NPC와 같은 원리.
/// </summary>
public class DungeonEntrance : MonoBehaviour
{
    [SerializeField] string _dungeonSceneName = "DungeonScene";
    [SerializeField] GameObject _interactPrompt;   // "E 입장" 안내, 평소 꺼져있음

    bool _isPlayerInRange;

    void Awake()
    {
        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (!_isPlayerInRange)
            return;

        // 다른 모달(강화선택/인벤토리/상점)이 열려있으면 무시
        if (GameSession._instance.IsPerkSelectionOpen)
            return;
        if (GameSession._instance.IsInventoryOpen)
            return;
        if (GameSession._instance.IsShopOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            EnterDungeon();
    }

    void EnterDungeon()
    {
        GameSession._instance.StartNewDungeonRun();
        SceneManager.LoadScene(_dungeonSceneName);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = true;

        if (_interactPrompt != null)
            _interactPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isPlayerInRange = false;

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);
    }
}
