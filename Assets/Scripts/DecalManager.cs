using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 贴花管理器，用于管理和渲染场景中的贴花效果
/// </summary>
public class DecalManager : MonoBehaviour
{
    public static DecalManager Instance { get; private set; }
    [SerializeField] private int _maxDecals = 100000;
    [SerializeField] private float _replacementDistance = 0.1f;
    [SerializeField] private LayerMask _layerMask;

    [HideInInspector] public List<DecalData> ActiveDecals;

    /// <summary>
    /// 贴花数据结构，存储贴花的位置、旋转和大小信息
    /// </summary>
    public struct DecalData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Size;
    }
    
    /// <summary>
    /// 初始化贴花管理器，确保单例模式并初始化活跃贴花列表
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        ActiveDecals = new List<DecalData>(_maxDecals);
    }

    /// <summary>
    /// 添加一个新的贴花到指定位置
    /// </summary>
    /// <param name="position">贴花的世界坐标位置</param>
    /// <param name="size">贴花的尺寸</param>
    /// <param name="wallLayerMask">用于检测碰撞的层遮罩</param>
    public void AddDecal(Vector3 position, Vector2 size, LayerMask wallLayerMask)
    {
        // 检测指定位置是否存在碰撞体
        Collider2D hit = Physics2D.OverlapPoint(position, wallLayerMask);

        if (hit == null)
        {
            return;
        }

        // 移除与新贴花位置过于接近的现有贴花（避免重叠）
        for (int i = ActiveDecals.Count - 1; i >= 0; i--)
        {
            if ((ActiveDecals[i].Position - position).sqrMagnitude < _replacementDistance * _replacementDistance)
            {
                ActiveDecals.RemoveAt(i);
            }
        }

        // 如果达到最大贴花数量限制，移除最旧的贴花
        if (ActiveDecals.Count >= _maxDecals)
        {
            ActiveDecals.RemoveAt(0);
        }
        
        DecalData newDecal = new DecalData
        {
            Position = position,
            Rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f)),
            Size = size
        };
        
        ActiveDecals.Add(newDecal);
    }
}
