using System.Collections.Generic;
using UnityEngine;

public class DecalManager : MonoBehaviour
{
    public static DecalManager Instance { get; private set; }
    [SerializeField] private int _maxDecals = 100000;
    [SerializeField] private float _replacementDistance = 0.1f;
    [SerializeField] private LayerMask _layerMask;

    [HideInInspector] public List<DecalData> ActiveDecals;

    public struct DecalData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Size;
    }
    
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

    public void AddDecal(Vector3 position, Vector2 size, LayerMask wallLayerMask)
    {
        Collider2D hit = Physics2D.OverlapPoint(position, wallLayerMask);

        if (hit == null)
        {
            return;
        }

        for (int i = ActiveDecals.Count - 1; i >= 0; i--)
        {
            if ((ActiveDecals[i].Position - position).sqrMagnitude < _replacementDistance * _replacementDistance)
            {
                ActiveDecals.RemoveAt(i);
            }
        }

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