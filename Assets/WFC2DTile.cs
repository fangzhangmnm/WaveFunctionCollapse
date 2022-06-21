using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFC2DTile : MonoBehaviour
{
    public float weight=1;
    public bool canRotate = false;
    public bool canFlipX = false;
    public bool canFlipY = false;
    public string XPlus;
    public string XMinus;
    public string YPlus;
    public string YMinus;
    public bool requiresConnectivity = false;


    IEnumerable<Vector3Int> Symmetries()
    {
        int nRot = canRotate ? 4 : 1;
        for (int rot=0;rot< nRot; ++rot)
        {
            yield return new Vector3Int(1,1,rot);
            if (canFlipX) yield return new Vector3Int(-1, 1, rot);
            if (!canFlipX && canFlipY) yield return new Vector3Int(1, -1, rot); //D4 group only has 8 elements
        }
    }
    int nSymmetries { get => (canRotate ? 4 : 1) * (canFlipX || canFlipY ? 2 : 1); }

    static TileSides TransformSide(Vector3Int scaleRot, TileSides input)
    {
        TileSides r = input;
        if (scaleRot.x == -1) r.FlipX();
        if (scaleRot.y == -1) r.FlipY();
        int rot = scaleRot.z;rot = (rot % 4 + 4) % 4;
        for (int i = 0; i < rot; ++i)
            r.Rotate();
        return r;
    }

    struct TileSides
    {
        //  yp
        //xm  xp
        //  ym
        public int XPlus,XMinus,YPlus,YMinus;
        public bool cXPlus,cXMinus,cYPlus,cYMinus;
        public void Rotate()
        {
            { int t = YPlus; YPlus = XPlus; XPlus = YMinus; YMinus = XMinus; XMinus = t; }
            { bool t = cYPlus; cYPlus = cXPlus; cXPlus = cYMinus; cYMinus = cXMinus; cXMinus = t; }
        }
        void FlipSides()
        {
            XPlus = sideFlip[XPlus];
            XMinus = sideFlip[XMinus];
            YPlus = sideFlip[YPlus];
            YMinus = sideFlip[YMinus];
        }
        public void FlipX()
        {
            { int t = XPlus; XPlus = XMinus; XMinus = t; }
            { bool t = cXPlus; cXPlus = cXMinus; cXMinus = t; }
            FlipSides();
        }
        public void FlipY()
        {
            { int t = YPlus; YPlus = YMinus; YMinus = t; }
            { bool t = cYPlus; cYPlus = cYMinus; cYMinus = t; }
            FlipSides();
        }
    }
    static Dictionary<string, (int, int)> sideName2Id = new Dictionary<string, (int, int)>();
    static Dictionary<int, int> sideFlip = new Dictionary<int, int>();
    static int nSideId;
    static int GetSideId(string name)
    {
        int flip = 0;
        if (name.Length >= 2 && name.Substring(name.Length - 2) == "_L") { flip = 1;  }
        else if (name.Length >= 2 && name.Substring(name.Length - 2) == "_R") { flip = 2; name = name.Substring(0, name.Length - 1)+"L"; }
        int l, r;
        if (!sideName2Id.ContainsKey(name))
        {
            if (flip == 0)
                l = r = nSideId++;
            else
            {
                l = nSideId++;
                r = nSideId++;
            }
            sideName2Id[name] = (l, r);
            sideFlip[l] = r;
            sideFlip[r] = l;
        }
        else
        {
            (l, r) = sideName2Id[name];
        }
        return flip == 2 ? r : l;
    }
    static bool GetSideConnectivity(string name)
    {
        if (name.Length > 0 && name[0] == '*')
            return false;
        else
            return true;
    }

    public static WFC2DAdjacent.State[] GenerateStates(WFC2DTile[] tiles)
    {
        List<WFC2DAdjacent.State> states = new List<WFC2DAdjacent.State>();
        List<TileSides> sides = new List<TileSides>();
        nSideId = 0;
        sideName2Id.Clear();
        sideFlip.Clear();
        foreach(var tile in tiles)
        {
            var side0 = new TileSides
            {
                XPlus = GetSideId(tile.XPlus),
                XMinus = GetSideId(tile.XMinus),
                YPlus = GetSideId(tile.YPlus),
                YMinus = GetSideId(tile.YMinus),
                cXPlus = GetSideConnectivity(tile.XPlus),
                cXMinus = GetSideConnectivity(tile.XMinus),
                cYPlus = GetSideConnectivity(tile.YPlus),
                cYMinus = GetSideConnectivity(tile.YMinus)
            };
            foreach (var scaleRot in tile.Symmetries())
            {
                var state = new WFC2DAdjacent.State();
                var side = TransformSide(scaleRot,side0);
                state.payload = tile;
                state.scaleRot = scaleRot;
                state.weight = tile.weight / tile.nSymmetries;
                state.connectXMinus = side.cXMinus;
                state.connectXPlus = side.cXPlus;
                state.connectYMinus = side.cYMinus;
                state.connectYPlus = side.cYPlus;
                state.requiresConnectivity = tile.requiresConnectivity;
                states.Add(state);
                sides.Add(side);
            }
        }
        int nState = states.Count;
        int[][][] banLists= new int[nSideId][][];
        for(int it = 0; it < nSideId; ++it)
        {
            List<int> xp = new List<int>();
            List<int> xm = new List<int>();
            List<int> yp = new List<int>();
            List<int> ym = new List<int>();
            for (int s = 0; s < nState; ++s)
            {
                int fit = sideFlip[it];
                if (sides[s].XMinus != fit) xp.Add(s);
                if (sides[s].XPlus != fit) xm.Add(s);
                if (sides[s].YMinus != fit) yp.Add(s);
                if (sides[s].YPlus != fit) ym.Add(s);
            }
            banLists[it] = new int[4][];
            banLists[it][0] = xp.ToArray();
            banLists[it][1] = xm.ToArray();
            banLists[it][2] = yp.ToArray();
            banLists[it][3] = ym.ToArray();
        }
        for(int s = 0; s < nState; ++s)
        {
            states[s].banXPlus = banLists[sides[s].XPlus][0];
            states[s].banXMinus = banLists[sides[s].XMinus][1];
            states[s].banYPlus = banLists[sides[s].YPlus][2];
            states[s].banYMinus = banLists[sides[s].YMinus][3];
        }
        return states.ToArray();
    }

}
