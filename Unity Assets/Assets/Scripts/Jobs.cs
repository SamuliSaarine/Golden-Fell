using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile(CompileSynchronously = false, Debug = false, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
public struct PopulateJob : IJob
{
    #region Variables

    public const float BEDROCK_SCALE = 1f;

    const int ROCK_OFFSET = 1000;
    const float ROCK_SCALE = 20f;
    const float ROCK_TRESHOLD = 0.5f;

    public const float DIRT_SCALE = 1f;

    const int GOLD_OFFSET = 3000;
    const float GOLD_SCALE = 40f;
    const float GOLD_TRESHOLD = 0.75f;

    [ReadOnly] public NativeArray<NativePixelBehaviour> behaviours;
    [ReadOnly] public int posX;
    [ReadOnly] public int posY;
    [ReadOnly] public int seed;

    [WriteOnly] public NativeArray<byte> data;
    [WriteOnly] public NativeArray<Color32> texture;
    [WriteOnly] public NativeArray<byte> chunkState;

    #endregion

    public void Execute()
    {
        byte state = 0;

        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            //Getting height of terrain in x coordinate
            int gx = posX * Chunk.CHUNK_SIZE + x;
            int h1 = GetRockHeight(gx);
            int h2 = GetDirtHeight(gx);

            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                //What type of pixel will we place here
                int gy = posY * Chunk.CHUNK_SIZE + y;
                byte id = GetID(gx,gy, h1, h2);
                data[Chunk.FlatCoord(x, y)] = id;

                //Updating state
                if (x == 0 && y == 0)
                {
                    state = id;
                }
                else if(state<3)
                {
                    if(id!=state)
                    {
                        state = 3;
                    }
                }

                //Getting color of the pixel
                texture[Chunk.FlatCoord(x,y)] = GetColor(gx, gy, id);
            }
        }

        chunkState[0] = state;
    }

    #region Utility

    public int GetRockHeight(float x)
    {
        //We get height by modifying perlin noise
        float noise = Mathf.PerlinNoise1D((x + seed) / World.WORLD_HEIGHT_PIXELS * BEDROCK_SCALE);
        int height = (int)(noise * World.WORLD_HEIGHT_PIXELS)-32;
        return height;
    }
    public int GetDirtHeight(float x)
    {
        float noise = Mathf.PerlinNoise1D((x + seed) / World.WORLD_HEIGHT_PIXELS*DIRT_SCALE);
        int height = (int)(noise * World.WORLD_HEIGHT_PIXELS);
        return height;
    }

    //Generation rules
    byte GetID(int x,int y, int rockHeight, int dirtHeight)
    {
        byte id = 0;

        #region Height based rules

        if (rockHeight > dirtHeight)
        {
            if (y < rockHeight) id = 1;
        }
        else if(y <= dirtHeight)
        {
            if (y >= dirtHeight - 2)
            {
                id = 3;
            }
            else
            {
                if (y < rockHeight)
                {
                    id = 1;
                }
                else
                {
                    id = 2;
                }
            }
        }

        #endregion

        #region Node based rules

        //Rock node
        if (id!=1 && y<dirtHeight+8 && Get2DNoise(x, y, ROCK_SCALE, ROCK_OFFSET, ROCK_TRESHOLD))
        {
            id = 1;
        }

        //Gold node
        if(id==1&&Get2DNoise(x,y,GOLD_SCALE,GOLD_OFFSET,GOLD_TRESHOLD))
        {
            id = 4;
        }

        #endregion

        return id;
    }

    bool Get2DNoise(float x, float y, float scale, int offset, float tresold)
    {

        return Mathf.PerlinNoise((x + offset + seed) / World.WORLD_HEIGHT_PIXELS * scale, (y + offset + 200 + seed) / World.WORLD_HEIGHT_PIXELS * scale) >= tresold;
    }

    Color32 GetColor(float x, float y, byte id)
    {
        //Generating different colors in texture using perlinnoise
        float t = Mathf.PerlinNoise(x / World.WORLD_HEIGHT_PIXELS * 60, y / World.WORLD_HEIGHT_PIXELS * 60);
        Color32 c = new();
        for (int i = 0; i < 4; i++)
        {
            c[i] = LerpByte(behaviours[id].minColor[i], behaviours[id].maxColor[i], t);
        }
        return c;
    }

    byte LerpByte(byte a, byte b, float t)
    {
        return (byte)(a + (b - a) * t);
    }

    #endregion
}

[BurstCompile(CompileSynchronously = false, Debug = false, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
public struct UpdateJob : IJob
{
    [ReadOnly] public NativeArray<NativePixelBehaviour> behaviours;
    [ReadOnly] public int posX;
    [ReadOnly] public int posY;
    [ReadOnly] public NativeArray<byte> data;
    [WriteOnly] public NativeArray<Color32> texture;

    public void Execute()
    {

        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            int gx = posX * Chunk.CHUNK_SIZE + x;

            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                int gy = posY * Chunk.CHUNK_SIZE + y;
                byte id = data[Chunk.FlatCoord(x, y)];
                texture[Chunk.FlatCoord(x, y)] = GetColor(gx, gy, id);
            }
        }
    }

    Color32 GetColor(float x, float y, byte id)
    {
        float t = Mathf.PerlinNoise(x / World.WORLD_HEIGHT_PIXELS * 60, y / World.WORLD_HEIGHT_PIXELS * 60);
        Color32 c = new();
        for (int i = 0; i < 4; i++)
        {
            c[i] = LerpByte(behaviours[id].minColor[i], behaviours[id].maxColor[i], t);
        }
        return c;
    }

    byte LerpByte(byte a, byte b, float t)
    {
        return (byte)(a + (b - a) * t);
    }
}
