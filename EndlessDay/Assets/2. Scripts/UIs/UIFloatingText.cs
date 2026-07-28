using TMPro;
using UnityEngine;

/// <summary>
/// 텍스트가 위로 떠오르며 사라지는 연출. 데미지 팝업과 같은 원리지만, 숫자가 아니라
/// 자유로운 문구(레벨업 등)를 캐릭터 머리 위에 짧게 띄울 때 사용.
/// </summary>
public class UIFloatingText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _text;
    [SerializeField] float _floatSpeed = 1f;
    [SerializeField] float _lifetime = 1.2f;

    float _timer;

    public void Init(string text, Color color)
    {
        _text.text = text;
        _text.color = color;
    }

    void Update()
    {
        transform.position += Vector3.up * _floatSpeed * Time.deltaTime;
        _timer += Time.deltaTime;

        if (_timer >= _lifetime)
            Destroy(gameObject);
    }
}
