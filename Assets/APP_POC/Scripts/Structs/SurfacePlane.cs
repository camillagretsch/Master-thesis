using TMPro;
using UnityEngine;

/// <summary>
/// Struct to define a surface plane which has a type, plane, bounds, area and a name tag. 
/// </summary>
public struct SurfacePlane
{
    public PlaneTypes Type;
    public Plane Plane;
    public OrientedBoundingBox Bounds;
    public float Area;
    public TextMeshPro Tag;

    public SurfacePlane(PlaneTypes type, Plane plane, OrientedBoundingBox bounds, TextMeshPro tag, Transform parent)
     {
        Type = type;
        Plane = plane;
        Bounds = bounds;
        Area = ((bounds.Extents.x * 2) * (bounds.Extents.y * 2));
        Tag = GameObject.Instantiate(tag, parent);
        Tag.text = Type.ToString();
        Tag.transform.localPosition = bounds.Center;
        Tag.transform.eulerAngles = new Vector3(bounds.Rotation.eulerAngles.x, bounds.Rotation.eulerAngles.y, 0);
    }

}

