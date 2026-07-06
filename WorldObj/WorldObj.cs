using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 게임 월드에서 동작하는 네트워크 오브젝트의 공통 부모 클래스입니다.
///
/// 오브젝트 생성 시 부착된 컴포넌트와 사용자 정의 부모 타입을 캐싱하여,
/// 이후 반복적인 GetComponent 호출 없이 타입을 기준으로 조회할 수 있습니다.
/// </summary>
public class WorldObj : NetworkBehaviour
{
    // 컴포넌트 타입을 키로 사용하여 캐싱된 컴포넌트를 즉시 조회합니다.
    private readonly Dictionary<Type, Component> _componentCache = new();

    protected virtual void Awake()
    {
        CacheComponents();
    }

    /// <summary>
    /// 현재 오브젝트에 부착된 컴포넌트와 사용자 정의 상속 타입을 캐싱합니다.
    ///
    /// 예: Miner : Production : Structure
    /// Miner 컴포넌트 하나를 Miner, Production, Structure 타입으로 조회할 수 있습니다.
    /// </summary>
    private void CacheComponents()
    {
        foreach (Component component in GetComponents<Component>())
        {
            Type currentType = component.GetType();

            // Unity 기본 타입을 제외하고 프로젝트에서 정의한 상속 타입을 등록합니다.
            while (currentType != null &&
                   currentType != typeof(MonoBehaviour) &&
                   currentType != typeof(Behaviour) &&
                   currentType != typeof(Component))
            {
                // 프로젝트 구조상 같은 타입 계층의 컴포넌트는
                // 하나의 오브젝트에 중복으로 존재하지 않습니다.
                if (!_componentCache.ContainsKey(currentType))
                {
                    _componentCache.Add(currentType, component);
                }

                currentType = currentType.BaseType;
            }
        }
    }

    /// <summary>
    /// 지정한 타입의 컴포넌트가 캐싱되어 있는지 확인합니다.
    /// </summary>
    public bool Has<T>() where T : Component
    {
        return _componentCache.ContainsKey(typeof(T));
    }

    /// <summary>
    /// 캐싱된 컴포넌트를 반환하며, 존재하지 않으면 null을 반환합니다.
    /// </summary>
    public T Get<T>() where T : Component
    {
        return _componentCache.TryGetValue(typeof(T), out Component component)
            ? component as T
            : null;
    }

    /// <summary>
    /// 캐싱된 컴포넌트 조회를 시도합니다.
    /// </summary>
    public bool TryGet<T>(out T result) where T : Component
    {
        if (_componentCache.TryGetValue(typeof(T), out Component component) &&
            component is T typedComponent)
        {
            result = typedComponent;
            return true;
        }

        result = null;
        return false;
    }
}
