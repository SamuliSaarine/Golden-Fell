using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    #region Variables

    /* CONST */
    public const int WORLD_WIDTH_CHUNKS = 64;
    public const int WORLD_HEIGHT_CHUNKS = 24;

    public const int WORLD_HEIGHT_PIXELS = WORLD_HEIGHT_CHUNKS * Chunk.CHUNK_SIZE;
    public const float WORLD_HEIGHT = WORLD_HEIGHT_CHUNKS * Chunk.CHUNK_REAL_SIZE;

    public const int WORLD_WIDTH_PIXELS = WORLD_WIDTH_CHUNKS * Chunk.CHUNK_SIZE;
    public static readonly float WORLD_WIDTH = WORLD_WIDTH_CHUNKS * Chunk.CHUNK_REAL_SIZE;

    /* SERIALIZED */

    [SerializeField] Transform house;
    [SerializeField] PixelBehaviour[] blocks;


    /* PRIVATE */

    readonly Chunk[,] chunks = new Chunk[WORLD_WIDTH_CHUNKS,WORLD_HEIGHT_CHUNKS];

    readonly Queue<Chunk> chunksToUpdate = new();

    /* STATIC */

    public static NativeArray<NativePixelBehaviour> nativeBlocks;
    public static int seed;

    public static World Instance;

    #endregion

    #region Generate

    private void Awake()
    {
        if(Instance==null)
        {
            Debug.Log("Instance was null");
            Instance=this;
        }
        else
        {
            Debug.Log("Destroying instance");
            Destroy(gameObject);
        }

        nativeBlocks = new NativeArray<NativePixelBehaviour>(blocks.Length,Allocator.Persistent);
        for (int i = 0; i < blocks.Length; i++)
        {
            nativeBlocks[i] = new NativePixelBehaviour(blocks[i]);
        }      
    }

    void Start()
    {
        seed = Random.Range(0, 10000)*10;
        Debug.Log($"Seed: {seed}");
        Random.InitState(seed);

        for (int x = 0; x < WORLD_WIDTH_CHUNKS; x++)
        {
            for (int y = 0; y < WORLD_HEIGHT_CHUNKS; y++)
            {
                chunks[x, y] = new Chunk(x, y);
            }
        }

        house.position = new Vector3(WORLD_WIDTH/2,GetTerrainHeight(WORLD_WIDTH / 2));
    }

    private void Update()
    {
        if(chunksToUpdate.Count>0)
        {
            Chunk c = chunksToUpdate.Dequeue();
            c.UpdateChunk();
        }
    }

    public void OnDestroy()
    {
        foreach (var c in chunks)
        {
            c.data.Dispose();
        }
        nativeBlocks.Dispose();
        Destroy(Instance);
        Instance = null;
    }

    #endregion

    #region Interact

    public PixelBehaviour CheckForPixel(float x, float y)
    {
        if(x<0||x>WORLD_WIDTH||y<0||y>WORLD_HEIGHT)
        {
            return new();
        }

        int cx = (int)(x / Chunk.CHUNK_REAL_SIZE);
        int cy = (int)(y / Chunk.CHUNK_REAL_SIZE);

        try
        {
            Chunk c = chunks[cx, cy];
            return blocks[c.GetData(x, y)];
        }
        catch(IndexOutOfRangeException e)
        {
            Debug.Log($"( {x} > {cx} ), ( {y}  > {cy} ): {e}");

            return new();
        }        
    }

    public bool DigPixel (float x, float y)
    {
        if (x < 0 || x > WORLD_WIDTH || y < 0 || y > WORLD_HEIGHT)
        {
            Debug.Log("Out of world");
            return false;
        }

        int cx = (int)(x / Chunk.CHUNK_REAL_SIZE);
        int cy = (int)(y / Chunk.CHUNK_REAL_SIZE);

        try
        {
            Chunk c = chunks[cx, cy];
            byte old = c.DigPixel(x, y);

            if (!chunksToUpdate.Contains(c))
            {
                chunksToUpdate.Enqueue(c);
            }

            //Did it drop gold?
            return old == 4;
        }
        catch (IndexOutOfRangeException e)
        {
            Debug.Log($"( {x} > {cx} ), ( {y}  > {cy} ): {e}");
            return false;
        }
    }

    #endregion

    #region Utility

    public static Vector3 GetSpawnPos { get { return new Vector3(WORLD_WIDTH / 2 - 1, WORLD_HEIGHT - 1, 0); }}

    public static Vector3 GetOrcSpawnPos(out float direction)
    {
        direction = Random.Range(0, 2);
        if(direction==0)
        {
            direction = -1;
        }
        return new Vector3(direction == 1 ? 0 : WORLD_WIDTH - 1, WORLD_HEIGHT - 1, 0);
    }

    public float GetTerrainHeight(float x)
    {
        x *= 100;
        float rockNoise = Mathf.PerlinNoise1D((x + seed) / WORLD_HEIGHT_PIXELS * PopulateJob.BEDROCK_SCALE);
        float dirtNoise = Mathf.PerlinNoise1D((x + seed) / WORLD_HEIGHT_PIXELS * PopulateJob.DIRT_SCALE);
        int height = (int)(rockNoise>dirtNoise ? rockNoise * WORLD_HEIGHT_PIXELS - 32 : dirtNoise * WORLD_HEIGHT_PIXELS);
        return height/100f;
    }

    #endregion
}

#region Structs

[Serializable]
public struct PixelBehaviour
{
    [SerializeField] string name;
    public int solidity;
    public Color32 minColor;
    public Color32 maxColor;

    public bool IsNull { get { return name==null || name.Length==0; } }
    public string GetName { get { return name; } }
}

public readonly struct NativePixelBehaviour
{
    public readonly int solidity;
    public readonly Color32 minColor;
    public readonly Color32 maxColor;

    public NativePixelBehaviour(PixelBehaviour p)
    {
        solidity = p.solidity;
        minColor = p.minColor;
        maxColor = p.maxColor;
    }
}

#endregion