using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class FloorChunk : MonoBehaviour
{
    private Tilemap _tilemap;

    private void Awake()
    {
        if (!TryGetComponent(out _tilemap))
        {
            Debug.LogError($"{nameof(FloorChunk)} requires a Tilemap.", this);
        }
    }

    public void Paint(int chunkSize, Tile tile1, Tile tile2, Tile tile3, float accentChance, int seed, TileBase[] buffer)
    {
        var random = new System.Random(seed);

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = random.NextDouble() < accentChance
                ? (random.Next(2) == 0 ? tile2 : tile3)
                : tile1;
        }

        _tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, chunkSize, chunkSize, 1), buffer);
    }
}
