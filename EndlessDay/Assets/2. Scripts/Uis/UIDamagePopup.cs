using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 숫자 팝업. 맞은 위치에서 생성되어 위로 떠오르다 사라진다.
/// 치명타 여부, 피격 대상(플레이어인지 몬스터인지)에 따라 색상/크기가 달라진다.
/// </summary>
public class UIDamagePopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _text;
    [SerializeField] float _floatSpeed = 1.5f;
    [SerializeField] float _lifetime = 0.8f;

    static readonly Color _monsterHitColor = Color.white;              // 몬스터가 맞았을 때 (내가 준 데미지)
    static readonly Color _playerHitColor = new Color(1f, 0.3f, 0.3f); // 플레이어가 맞았을 때
    static readonly Color _critColor = new Color(1f, 0.65f, 0f);       // 치명타

    float _timer;

    public void Init(int damage, bool isCrit, bool isPlayerDamaged)
    {
        _text.text = damage.ToString();

        if (isCrit)
        {
            _text.color = _critColor;
            _text.fontSize *= 1.5f;
        }
        else
        {
            _text.color = isPlayerDamaged ? _playerHitColor : _monsterHitColor;
        }
    }

    void Update()
    {
        transform.position += Vector3.up * _floatSpeed * Time.deltaTime;
        _timer += Time.deltaTime;

        if (_timer >= _lifetime)
            Destroy(gameObject);
    }
}
