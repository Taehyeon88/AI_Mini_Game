using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;

public class FloorChunkStreamer : MonoBehaviour
{
    [SerializeField] private FloorChunk _chunkPrefab;
    [SerializeField] private Transform _chunkParent;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Sprite _tile1Sprite;
    [SerializeField] private Sprite _tile2Sprite;
    [SerializeField] private Sprite _tile3Sprite;
    [SerializeField] private int _chunkSize = 8;
    [SerializeField] private int _viewRadiusChunks = 3;
    [SerializeField] private float _accentChance = 0.12f;
    [SerializeField] private int _worldSeed = 12345;

    private Tile _tile1;
    private Tile _tile2;
    private Tile _tile3;
    private TileBase[] _paintBuffer;
    private ObjectPool<FloorChunk> _pool;
    private readonly Dictionary<Vector2Int, FloorChunk> _activeChunks = new Dictionary<Vector2Int, FloorChunk>();
    private readonly List<Vector2Int> _chunksToRelease = new List<Vector2Int>();
    private Vector2Int _lastCameraChunk = new Vector2Int(int.MinValue, int.MinValue);

    private void Awake()
    {
        _tile1 = CreateTile(_tile1Sprite);
        _tile2 = CreateTile(_tile2Sprite);
        _tile3 = CreateTile(_tile3Sprite);
        _paintBuffer = new TileBase[_chunkSize * _chunkSize];

        int viewDiameter = _viewRadiusChunks * 2 + 1;
        _pool = new ObjectPool<FloorChunk>(
            createFunc: () => Instantiate(_chunkPrefab, _chunkParent),
            actionOnGet: chunk => chunk.gameObject.SetActive(true),
            actionOnRelease: chunk => chunk.gameObject.SetActive(false),
            actionOnDestroy: chunk => Destroy(chunk.gameObject),
            collectionCheck: true,
            defaultCapacity: viewDiameter * viewDiameter,
            maxSize: viewDiameter * viewDiameter * 2);
    }

    private void LateUpdate()
    {
        Vector2Int cameraChunk = WorldToChunkCoord(_cameraTransform.position);
        if (cameraChunk == _lastCameraChunk) return;

        _lastCameraChunk = cameraChunk;
        RefreshChunks(cameraChunk);
    }

    private void RefreshChunks(Vector2Int center)
    {
        for (int dy = -_viewRadiusChunks; dy <= _viewRadiusChunks; dy++)
        {
            for (int dx = -_viewRadiusChunks; dx <= _viewRadiusChunks; dx++)
            {
                var coord = new Vector2Int(center.x + dx, center.y + dy);
                if (!_activeChunks.ContainsKey(coord))
                {
                    SpawnChunk(coord);
                }
            }
        }

        _chunksToRelease.Clear();
        foreach (Vector2Int coord in _activeChunks.Keys)
        {
            bool outOfRange = Mathf.Abs(coord.x - center.x) > _viewRadiusChunks
                || Mathf.Abs(coord.y - center.y) > _viewRadiusChunks;
            if (outOfRange)
            {
                _chunksToRelease.Add(coord);
            }
        }

        foreach (Vector2Int coord in _chunksToRelease)
        {
            DespawnChunk(coord);
        }
    }

    private void SpawnChunk(Vector2Int coord)
    {
        FloorChunk chunk = _pool.Get();
        chunk.transform.position = new Vector3(coord.x * _chunkSize, coord.y * _chunkSize, 0f);
        chunk.Paint(_chunkSize, _tile1, _tile2, _tile3, _accentChance, ComputeSeed(coord), _paintBuffer);
        _activeChunks[coord] = chunk;
    }

    private void DespawnChunk(Vector2Int coord)
    {
        _pool.Release(_activeChunks[coord]);
        _activeChunks.Remove(coord);
    }

    private int ComputeSeed(Vector2Int coord)
    {
        // 공간 해시 — 같은 청크 좌표는 재방문해도 항상 같은 시드(=같은 타일 배치)를 내야 무늬가 안 바뀐다.
        return _worldSeed ^ (coord.x * 73856093) ^ (coord.y * 19349663);
    }

    private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
    {
        // 무한 평원이라 음수 좌표가 기본 시나리오 — (int) 캐스팅(0으로 truncate)이 아닌 FloorToInt여야 경계에서 안 틀어짐.
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / _chunkSize),
            Mathf.FloorToInt(worldPosition.y / _chunkSize));
    }

    private static Tile CreateTile(Sprite sprite)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        return tile;
    }
}
