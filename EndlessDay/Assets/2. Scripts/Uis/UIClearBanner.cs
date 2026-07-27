using TMPro;
using UnityEngine;

/// <summary>
/// 화면 중앙에 잠깐 뜨는 텍스트 배너 ("Room 2 Clear" 등). HUD 프리팹에 하나 배치.
/// 스크립트가 붙은 오브젝트 자신은 항상 켜둔 채로, _bannerRoot(자식)만 켰다 끈다
/// (자기 자신을 끄면 Update가 멈춰서 다시 못 끄니 주의).
/// </summary>
public class UIClearBanner : MonoBehaviour
{
    public static UIClearBanner _instance { get; private set; }

    [SerializeField] GameObject _bannerRoot;   // 실제 보이는 배너 (평소 꺼져있음)
    [SerializeField] TextMeshProUGUI _bannerText;
    [SerializeField] float _showDuration = 2f;

    float _hideTimer;

    void Awake()
    {
        _instance = this;

        if (_bannerRoot != null)
            _bannerRoot.SetActive(false);
    }

    void Update()
    {
        if (_hideTimer <= 0f)
            return;

        _hideTimer -= Time.deltaTime;
        if (_hideTimer <= 0f && _bannerRoot != null)
            _bannerRoot.SetActive(false);
    }

    public void Show(string text)
    {
        if (_bannerRoot == null || _bannerText == null)
            return;

        _bannerText.text = text;
        _bannerRoot.SetActive(true);
        _hideTimer = _showDuration;
    }
}
