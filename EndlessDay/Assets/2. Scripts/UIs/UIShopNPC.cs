using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 상점 NPC. 항상 "상점 주인" 네임플레이트를 띄우고, 플레이어가 범위 안에 들어오면
/// "E 상점" 상호작용 프롬프트를 추가로 보여준다. 그 상태에서 E를 누르면 상점을 연다.
/// NPC의 콜라이더(Is Trigger)로 범위를 감지 - 몬스터 감지 존과 같은 원리.
/// </summary>
public class UIShopNPC : MonoBehaviour
{
    [SerializeField] UIShopController _shopController;
    [SerializeField] GameObject _interactPrompt;      // "E 상점" 안내, 평소 꺼져있음
    [SerializeField] TextMeshProUGUI _nameplateText;  // "상점 주인", 항상 표시

    bool _isPlayerInRange;

    void Awake()
    {
        if (_nameplateText != null)
            _nameplateText.text = "상점 주인";

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (!_isPlayerInRange)
            return;

        if (GameSession._instance.IsPerkSelectionOpen)
            return;

        if (GameSession._instance.IsInventoryOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            _shopController.ToggleShopPanel();
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