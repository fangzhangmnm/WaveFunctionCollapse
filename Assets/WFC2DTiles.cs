using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFC2DTiles : MonoBehaviour
{
    public int sizeX = 10;
    public int sizeY = 10;
    public float tileSizeX = 1;
    public float tileSizeY = 1;
    public WFC2DTile[] tiles;

    WFC2DAdjacent.State[] states;
    WFC2DAdjacent wfc;
    [Multiline(11)] public string debug_string;


    [Button]
    public void Init()
    {
        states = WFC2DTile.GenerateStates(tiles);
        wfc = new WFC2DAdjacent(states);
        wfc.Init(sizeX, sizeY);
        ClearChild();
        debug_string = GetBanListText(states);
    }
    [Button]
    public void Step()
    {
        if (wfc == null) Init();
        if (!wfc.Step())
        {
            Debug.LogError("Generation Failed");
        }
        debug_string = GetResultText(wfc.result);
        ClearChild();
        Spawn(wfc.result, states);
    }
    [Button]
    public void GenerateAll()
    {
        if (wfc == null) Init();
        int n = wfc.nNotGenerated;
        for (int i = 0; i < n; ++i)
        {
            if (!wfc.Step())
            {
                Debug.LogError("Generation Failed");
                //break;
            }
        }
        debug_string = GetResultText(wfc.result);
        ClearChild();
        Spawn(wfc.result, states);
    }
    public void ClearChild()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
    string GetResultText(int[,] result)
    {
        string s = "";
        for (int y = result.GetUpperBound(1); y >= result.GetLowerBound(1); --y)
        {
            for (int x = result.GetLowerBound(0); x <= result.GetUpperBound(0); ++x)
                s+=$"{result[x, y]}\t";
            s += "\n";
        }
        return s;
    }
    string GetBanListText(WFC2DAdjacent.State[] states)
    {
        string s = "";
        for(int i=0;i<states.Length;++i)
        {
            var state = states[i];
            s += $"state {i}\tscaleRot {state.scaleRot}\n";
            s += $"banXPlus {string.Join(" ", state.banXPlus)}\n";
            s += $"banXMinus {string.Join(" ", state.banXMinus)}\n";
            s += $"banYPlus {string.Join(" ", state.banYPlus)}\n";
            s += $"banYMinus {string.Join(" ", state.banYMinus)}\n";
        }
        return s;
    }
    void Spawn(int[,] result,WFC2DAdjacent.State[] states)
    {
        for (int x = result.GetLowerBound(0); x <= result.GetUpperBound(0); ++x)
            for (int y = result.GetLowerBound(1); y <= result.GetUpperBound(1); ++y)
                if (result[x, y] != WFC2DAdjacent.NotCollapsed)
                {
                    var state = states[result[x, y]];
                    var tile = (WFC2DTile)state.payload;
                    var go = Instantiate(tile.gameObject, transform);
                    go.transform.localPosition = new Vector3(tileSizeX * x, tileSizeY * y);
                    go.transform.localScale = new Vector3(state.scaleRot.x, state.scaleRot.y, 1);
                    go.transform.localRotation = Quaternion.Euler(0, 0, state.scaleRot.z * 90f) * tile.gameObject.transform.localRotation;//left handed
                }
    }
}
