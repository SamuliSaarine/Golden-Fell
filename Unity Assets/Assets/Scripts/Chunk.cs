using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

public class Chunk
{
    #region Variables

    public const int CHUNK_SIZE = 64;
    public const float CHUNK_REAL_SIZE = CHUNK_SIZE / 100f;

    SpriteRenderer renderer;
    readonly int posX;
    readonly int posY;

    public NativeArray<byte> data;
    public ChunkState state;

    #endregion

    #region Create and Update

    public Chunk(int posX, int posY)
    {
        this.posX = posX;
        this.posY = posY;       
        state = ChunkState.Default;

        PopulateChunk();
    }

    void PopulateChunk()
    {
        //We use job system to multithread populating and generate the world faster.

        //Creating and completing job
        var job = new PopulateJob()
        {
            behaviours = World.nativeBlocks,
            posX = posX,
            posY = posY,
            data = new NativeArray<byte>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent),
            texture = new NativeArray<Color32>(CHUNK_SIZE * CHUNK_SIZE, Allocator.TempJob),
            chunkState = new NativeArray<byte>(1,Allocator.TempJob),
            seed = World.seed
        };
        
        job.Schedule().Complete();

        //Assigning generated pixelgrid
        data = job.data;

        //Getting information if chunk is full
        state = (ChunkState)job.chunkState[0];

        if(state!=ChunkState.Air)
        {
            //Rendering chunk if it's not full air
            RenderChunk(job.texture);
        }

        //Disposing data not used anymore from job
        job.texture.Dispose();
        job.chunkState.Dispose();
    }

    public void UpdateChunk()
    {
        //Creating and completing job to get updated texture for chunk
        var job = new UpdateJob()
        {
            behaviours = World.nativeBlocks,
            posX = posX,
            posY = posY,
            data = data,
            texture = new NativeArray<Color32>(CHUNK_SIZE * CHUNK_SIZE, Allocator.TempJob),
        };

        job.Schedule().Complete();

        RenderChunk(job.texture);

        job.texture.Dispose();
    }

    void RenderChunk(NativeArray<Color32> colors)
    {     
        if (renderer == null)
        {
            //Creating components to render the chunk
            GameObject newObject = new();
            renderer = newObject.AddComponent<SpriteRenderer>();
            newObject.transform.position = new Vector3(posX * CHUNK_SIZE * 0.01f, posY * CHUNK_SIZE * 0.01f, 0);
            newObject.name = $"Chunk({posX},{posY})";
            newObject.transform.parent = World.Instance.transform;
        }

        //Creating the texture
        Texture2D tex = new(CHUNK_SIZE, CHUNK_SIZE, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        tex.LoadRawTextureData(colors);
        tex.Apply();

        //Creating sprite from texture
        renderer.sprite = Sprite.Create(tex, new Rect(0, 0, CHUNK_SIZE, CHUNK_SIZE), new(0, 0));
    }

    #endregion

    #region Interact

    public byte GetData(float globalX, float globalY)
    {
        if (state != ChunkState.Default)
        {
            return (byte)state;
        }

        //Global coordinates to chunk coordinates
        int x = (int)(globalX * 100 - posX * CHUNK_SIZE);
        int y = (int)(globalY * 100 - posY * CHUNK_SIZE);

        return data[FlatCoord(x, y)];
    }

    public byte DigPixel(float globalX, float globalY)
    {
        //Global coordinates to chunk coordinates
        int x = (int)(globalX * 100 - posX * CHUNK_SIZE);
        int y = (int)(globalY * 100 - posY * CHUNK_SIZE);

        int coord = FlatCoord(x, y);
        byte old = data[coord];

        //Rock and gold is transformed to dirt, dirt and moss to air
        bool isHard = World.nativeBlocks[old].solidity > 1;
        data[coord] = (byte)(isHard ? 2 : 0);
  
        state = ChunkState.Default;

        return old;
    }

    #endregion

    public static int FlatCoord(int x, int y)
    {
        //2D-coordinates to data index
        return x + CHUNK_SIZE * y;
    }
}

// To get data faster from chunks that contain only one type of pixel, we store information if it's full something.
// Default means that there are different types of pixels in the chunk 
public enum ChunkState { Air, Rock, Dirt, Default }