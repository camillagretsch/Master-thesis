using UnityEngine;

/// <summary>
/// Struct to define a lighting unit which has a position, surface plane type, prefab game object, range, number of triangle hits, unit type, lux value, durability value, and a watt value. 
/// </summary>
public struct LightingUnit
{
    public Vector3 position;
    public PlaneTypes planeType;
    public GameObject bulb;
    public float maxRange;
    public int hits;
    public string unitType;
    public int lux;
    public int durability;
    public int watt;

    public LightingUnit(Vector3 p, PlaneTypes t, GameObject b, float r, int h, string n)
    {
        position = p;
        planeType = t;
        bulb = b;
        maxRange = r;
        hits = h;
        unitType = n;

        if (unitType == "Type A")
        {
            lux = 4;
            durability = 3;
            watt = 100;
        }
        else if (unitType == "Type B")
        {
            lux = 3;
            durability = 4;
            watt = 70;
        } else
        {
            lux = 1;
            durability = 6;
            watt = 10;
        }
    }

    public static int[] GetLightingUnitTypeValues(string type)
    {
        if (type == "Type A")
        {
            return new int[3] { 4, 3, 100 };
        } else if (type == "Type B")
        {
            return new int[3] { 3, 4, 70 };
        } else
        {
            return new int[3] { 1, 6, 10 };
        }
    }
}
