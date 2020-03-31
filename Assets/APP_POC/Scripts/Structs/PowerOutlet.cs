using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///  Struct to define a power outlet which has a prefab game object, surface plane, lamp position, list of raycast hits, and a range. 
/// </summary>
public struct PowerOutlet
{
    public GameObject outlet;
    public SurfacePlane surfacePlane;
    public Vector3 lampPosition;
    public List<RaycastHit> hits;
    public float range;

    public PowerOutlet(GameObject g, SurfacePlane p, Vector3 v, List<RaycastHit> h)
    {
        outlet = g;
        surfacePlane = p;
        lampPosition = v;
        hits = h;
        range = hits.Sum(d => d.distance);
    }
}
