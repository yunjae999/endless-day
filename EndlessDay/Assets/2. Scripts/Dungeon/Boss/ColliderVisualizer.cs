using UnityEngine;

/// <summary>
/// 지정된 콜라이더의 실제 모양/크기를 그대로 반투명 메시로 보여준다.
/// Box/Sphere/Capsule 콜라이더를 자동으로 인식해서 알맞은 프리미티브를 만들어 겹쳐 보여줌 -
/// 판정용 콜라이더 자체는 그대로 두고, 순수하게 "보여주기용" 메시만 추가하는 방식.
/// </summary>
public class ColliderVisualizer : MonoBehaviour
{
    [SerializeField] Collider _targetCollider;
    [SerializeField] Material _visualMaterial;   // 반투명 머티리얼 (Surface Type: Transparent)

    GameObject _visualObject;

    void Awake()
    {
        CreateVisual();
        Hide();
    }

    void CreateVisual()
    {
        if (_targetCollider == null)
            return;

        PrimitiveType type;
        if (_targetCollider is BoxCollider)
            type = PrimitiveType.Cube;
        else if (_targetCollider is SphereCollider)
            type = PrimitiveType.Sphere;
        else if (_targetCollider is CapsuleCollider)
            type = PrimitiveType.Capsule;
        else
            return;

        _visualObject = GameObject.CreatePrimitive(type);
        Destroy(_visualObject.GetComponent<Collider>());   // 진짜 판정은 원본이 하니, 프리미티브 자체 콜라이더는 필요 없음
        _visualObject.transform.SetParent(_targetCollider.transform, false);

        if (_visualMaterial != null)
            _visualObject.GetComponent<Renderer>().material = _visualMaterial;

        ApplyShapeToVisual();
    }

    void ApplyShapeToVisual()
    {
        if (_targetCollider is BoxCollider box)
        {
            _visualObject.transform.localPosition = box.center;
            _visualObject.transform.localScale = box.size;
        }
        else if (_targetCollider is SphereCollider sphere)
        {
            _visualObject.transform.localPosition = sphere.center;
            _visualObject.transform.localScale = Vector3.one * sphere.radius * 2f;
        }
        else if (_targetCollider is CapsuleCollider capsule)
        {
            _visualObject.transform.localPosition = capsule.center;
            float diameter = capsule.radius * 2f;
            _visualObject.transform.localScale = new Vector3(diameter, capsule.height / 2f, diameter);
        }
    }

    public void Show()
    {
        if (_visualObject != null)
            _visualObject.SetActive(true);
    }

    public void Hide()
    {
        if (_visualObject != null)
            _visualObject.SetActive(false);
    }
}
