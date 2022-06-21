using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFC2DAdjacent
{
    public class State
    {
        public bool requiresConnectivity;
        public bool connectXMinus, connectXPlus, connectYMinus, connectYPlus;
        public int[] banXMinus, banXPlus, banYPlus, banYMinus;
        public float weight, wLogW;
        public object payload;
        public Vector3Int scaleRot;
    }

    public State[] states;
    public int[,] result;
    public int sizeX, sizeY;
    public int nState => states.Length;
    public int nNotGenerated = 0;

    bool[,,] wave;
    float[,] sW,sWLogW;
    int[,] sN;
    public const int NotCollapsed = -1;
    bool[,] connected;

    List<(int, int)> minList;

    public WFC2DAdjacent(State[] states)
    {
        this.states = states;
    }
    
    public void Init(int sizeX, int sizeY)
    {
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        nNotGenerated = this.sizeX * this.sizeY;
        float totalW = 0, totalWLogW = 0;
        for(int i=0;i<nState;++i)
        {
            states[i].wLogW = states[i].weight * Mathf.Log(states[i].weight);
            if (states[i].weight == 0) states[i].wLogW = 0;
            totalW += states[i].weight;
            totalWLogW += states[i].wLogW;
        }

        wave = new bool[sizeX, sizeY, nState];
        sW = new float[sizeX, sizeY];
        sWLogW = new float[sizeX, sizeY];
        sN = new int[sizeX, sizeY];
        result = new int[sizeX, sizeY];
        minList = new List<(int, int)>();
        minList.Capacity = sizeX * sizeY;

        for (int x = 0; x < sizeX; ++x)
            for (int y = 0; y < sizeY; ++y)
                for (int i = 0; i < nState; ++i)
                    wave[x, y, i] = true;
        for (int x = 0; x < sizeX; ++x)

            for (int y = 0; y < sizeY; ++y)
            {
                sW[x, y] = totalW;
                sWLogW[x, y] = totalWLogW;
                sN[x, y] = nState;
                result[x, y] = NotCollapsed;
            }
    }
    float GetEntropy(int x, int y) 
    {
        if (sW[x, y] <= 0) return 0;
        else return -sWLogW[x, y] / sW[x, y] + Mathf.Log(sW[x, y]);
    }

    (int,int) FindMinEntropyCoord()
    {
        float minEntropy = Mathf.Infinity;
        minList.Clear();

        for (int x = 0; x < sizeX; ++x)
            for (int y = 0; y < sizeY; ++y)
                if(result[x,y]==NotCollapsed)
                {
                    float entropy = GetEntropy(x, y);
                    if (entropy < minEntropy)
                    {
                        minList.Clear();
                        minEntropy = entropy;
                    }
                    if (entropy == minEntropy)
                        minList.Add((x, y));
                }
        return minList[Random.Range(0, minList.Count)];
    }
    bool Ban(int x,int y, int[] banList)
    {
        foreach(var b in banList)
            if (wave[x, y, b])
            {
                wave[x, y, b] = false;
                sW[x, y] -= states[b].weight;
                sWLogW[x, y] -= states[b].wLogW;
                sN[x, y] -= 1;
            }
        return sN[x, y] > 0;
    }
    int Collapse(int x,int y)
    {
        float rnd = Random.value * sW[x, y];
        int collapsedStateId = 0;
        for(int i = 0; i < nState; ++i)
            if (wave[x, y, i])
            {
                rnd -= states[i].weight;
                collapsedStateId = i;
                if (rnd < 0) break;
            }
        result[x, y] = collapsedStateId;
        return collapsedStateId;
    }
    bool InRange(int x,int y)
    {
        return 0 <= x && x < sizeX && 0 <= y && y < sizeY;
    }
    public bool Step()
    {
        Debug.Assert(result != null);
        if (nNotGenerated <= 0) return false;
        (int x, int y) = FindMinEntropyCoord();
        int s = Collapse(x, y);
        nNotGenerated -= 1;
        if (InRange(x - 1, y))
            if (!Ban(x - 1, y, states[s].banXMinus)) return false;
        if (InRange(x + 1, y))
            if (!Ban(x + 1, y, states[s].banXPlus)) return false;
        if (InRange(x, y-1))
            if (!Ban(x, y-1, states[s].banYMinus)) return false;
        if (InRange(x, y + 1))
            if (!Ban(x, y + 1, states[s].banYPlus)) return false;
        return true;
    }
}
