using UnityEngine;

/// <summary>
/// Handles the the creation of surface plane game objects. 
/// </summary>
public class SurfacePlaneExtension : MonoBehaviour
{
    private const float PLANE_THICKNESS = 0.01f;

    // Returns the surface plane of this game object
    public SurfacePlane Plane { get; private set; }

    [Tooltip("Material to use when rendering wall planes.")]
    [SerializeField]
    private Material wallMaterial;

    [Tooltip("Material to use when rendering floor planes.")]
    [SerializeField]
    private Material floorMaterial;

    [Tooltip("Material to use when rendering ceiling planes.")]
    [SerializeField]
    private Material ceilingMaterial;

    [Tooltip("Material to use when rendering table planes.")]
    [SerializeField]
    private Material tableMaterial;

    [Tooltip("Material to use when rendering transparent planes.")]
    [SerializeField]
    private Material transparentMaterial;
    

    /// <summary>
    /// Initializes a cube game object which represents a surface plane in the scene. 
    /// </summary>
    /// <param name="p">Plane data from the surface plane which contains the relevant coordinates.</param>
    /// <param name="display">Specify if the surface plane objects should be displayed in the scene.</param>
    public void Init(SurfacePlane p, bool display)
    {
        Plane = p;
        gameObject.layer = 30; 
        SetFigureGeometry();

        if (display)
        {
            SetFigureMaterialByType();
        }
        else
        {
            gameObject.GetComponent<Renderer>().material = transparentMaterial;
        }
    }

    /// <summary>
    /// Sets the game object to have the same geometry as the surface plane. 
    /// </summary>
    private void SetFigureGeometry()
    {
        gameObject.transform.position = Plane.Bounds.Center;
        gameObject.transform.rotation = Plane.Bounds.Rotation;
        gameObject.transform.localScale = new Vector3((Plane.Bounds.Extents.x * 2), (Plane.Bounds.Extents.y * 2), PLANE_THICKNESS);
    }


    /// <summary>
    /// Sets the game object render material if it should be displayed in the scene. 
    /// Each plane type has its own color. 
    /// </summary>
    private void SetFigureMaterialByType()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        switch (Plane.Type)
        {
            case PlaneTypes.Floor: // blue
                renderer.material = floorMaterial;
                break;
            case PlaneTypes.Table: // green
                renderer.material = tableMaterial;
                break;
            case PlaneTypes.Ceiling: // yellow
                renderer.material = ceilingMaterial;
                break;
            case PlaneTypes.Wall: // red
                renderer.material = wallMaterial;
                break;
            default:
                break;
        }
    }
}
