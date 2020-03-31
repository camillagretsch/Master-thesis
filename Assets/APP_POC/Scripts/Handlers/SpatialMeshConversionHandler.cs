using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles the converstion of the spatial mesh data to real-world surface planes. 
/// </summary>
public class SpatialMeshConversionHandler : MonoBehaviour
{
    private SpatialObserverHandler observer;

    private float floorYPosition = 0.0f;
    private float ceilingYPosition = 0.0f;
    private GameObject roomTagParent = null;
    private List<SurfacePlane> detectedSurfacePlanes;

    // Indicates if the converter is currently creating planes
    private bool isCreatingPlanes = false;

    // Aligns surface planes with gravity so that they appear more level
    private const float SNAP_TO_GRAVITY_THRESHOLD = 5.0f;

#if UNITY_EDITOR || UNITY_STANDALONE
    // Time in sec the converter can run in Unity Editor before returning control to the main program 
    private const float FRAME_TIME = .016f;
#else
    // Time in sec the converter can run before returning control to the main program
    private const float FRAME_TIME = .008f;
#endif

    // Delegate is called when the create surface planes completed event is triggered
    public delegate void EventHandler(object source, EventArgs args);
    // Event handler which is triggered when the create surface planes is completed
    public event EventHandler CreateSurfacePlanesCompletedEvent;

    // Parent of the surface plane collection game objects
    public GameObject SurfacePlanesParent { get; private set; }

    [Tooltip("Text to tag the objects found in room.")]
    [SerializeField]
    private TextMeshPro roomTagPrefab;

    [Tooltip("Minimum area required for a plane to be created.")]
    [SerializeField]
    private float minArea = 0.025f;

    [Tooltip("Threshold for acceptable normals (the closer to 1, the stricter the standard). Used when determining plane type.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float upNormalThreshold = 0.9f;

    [Tooltip("Display surface planes in the scene.")]
    [SerializeField]
    private bool showSurfacePlanes = false;

    [Tooltip("Cube to display walls, floor, ceiling, tables.")]
    [SerializeField]
    private GameObject surfacePlanePrefab;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        observer = GetComponent<SpatialObserverHandler>();

        isCreatingPlanes = false;
        detectedSurfacePlanes = new List<SurfacePlane>();
        SurfacePlanesParent = CreateCollectionParent("SurfacePlaneCollection");
        roomTagParent = CreateCollectionParent("RoomTagCollection");
    }

    /// <summary>
    /// Starts creating surface planes based on the spatial mesh data.
    /// </summary>
    public void CreateSurfacePlanes()
    {
        if (!isCreatingPlanes)
        {
            isCreatingPlanes = true;
            Debug.Log("Start creating surface planes");
            // Use coroutine to split the work across multiple frames and avoid impacting the frame rate too much
            StartCoroutine(CreateSurfacePlanesRoutine());
        }
    }

    /// <summary>
    /// Returns all detected surface planes of a specific type.
    /// </summary>
    /// <param name="planeTypes">Specified surface type.</param>
    /// <returns>List of surface planes of the specified type.</returns>
    public List<SurfacePlane> GetDetectedSurfacePlanesByType(PlaneTypes planeTypes)
    {
        return detectedSurfacePlanes.FindAll(p => p.Type == planeTypes);
    }

    /// <summary>
    /// Returns either the largest floor or ceiling based on its type.
    /// </summary>
    /// <param name="planeTypes">Specified surface type.</param>
    /// <returns>The largest surfaceplane if several exist.</returns>
    public SurfacePlane GetFloorOrCeiling(PlaneTypes planeTypes)
    {
        List<SurfacePlane> detectedFloorsOrCeilings = detectedSurfacePlanes.FindAll(p => p.Type == planeTypes);

        // Return first if only one exist
        if (detectedFloorsOrCeilings.Count == 1)
        {
            return detectedFloorsOrCeilings[0];

        } // Return empty if none exist
        else if (detectedFloorsOrCeilings.Count == 0)
        {
            return new SurfacePlane();

        } // Return surface plane with largest area if several exist
        else
        {
            return detectedFloorsOrCeilings.OrderByDescending(c => c.Area).First();
        }
    }

    /// <summary>
    /// Create an empty game object which will be a collection parent.
    /// </summary>
    /// <param name="name">Name of the collection parent.</param>
    /// <returns>Gameobject parent collection.</returns>
    private GameObject CreateCollectionParent(string name)
    {
        GameObject tmp = new GameObject(name);
        tmp.transform.parent = gameObject.transform;
        tmp.layer = 30;
        return tmp;
    }

    /// <summary>
    /// Iterator block which analyzes the spatial mesh to find understandable surface planes. 
    /// </summary>
    /// <returns>A completion event</returns>
    private IEnumerator CreateSurfacePlanesRoutine()
    {
        ResetSurfacePlaneContent();

        // Pause work, and continue on the next frame
        yield return null;
        float start = Time.realtimeSinceStartup;

        // Get the mesh filters from the spatial observer
        List<MeshFilter> filters = observer.GetMeshFilters();
        List<PlaneFinder.MeshData> meshData = new List<PlaneFinder.MeshData>();

        foreach (MeshFilter filter in filters)
        {
            // Fix spatial mesh normals to get correct plane orientation
            filter.mesh.RecalculateNormals();
            // Extract the relevant meshdata from the mesh filter
            meshData.Add(new PlaneFinder.MeshData(filter));

            // If too much time has passed, we need to return control to the main game loop.
            if ((Time.realtimeSinceStartup - start) > FRAME_TIME)
            {
                // Pause our work, and continue on the next frame
                yield return null;
                start = Time.realtimeSinceStartup;
            }
        }

        // Start the find plane task and wait till it returns the bounded planes
        Task<BoundedPlane[]> boundedPlaneFinding = Task.Run(() => PlaneFinder.FindBoundedPlanes(meshData, SNAP_TO_GRAVITY_THRESHOLD, minArea));
        while (boundedPlaneFinding.IsCompleted == false)
        {
            yield return false;
        }
        BoundedPlane[] boundedPlanes = boundedPlaneFinding.Result;

        // Pause work here, and continue on the next frame.
        yield return null;
        start = Time.realtimeSinceStartup;

        FindFloorAndCeilingPosition(boundedPlanes);

        // Create a surface plane for each bounded plane and add them to the detected surface planes
        foreach (BoundedPlane boundedPlane in boundedPlanes)
        {
            SurfacePlane surfacePlane = new SurfacePlane(ClassifySurfacePlane(boundedPlane), boundedPlane.Plane, boundedPlane.Bounds, roomTagPrefab, roomTagParent.transform);
            detectedSurfacePlanes.Add(surfacePlane);

            // If too much time has passed, we need to return control to the main game loop
            if ((Time.realtimeSinceStartup - start) > FRAME_TIME)
            {
                // Pause our work here, and continue making additional planes on the next frame
                yield return null;
                start = Time.realtimeSinceStartup;
            }
        }

        CreateSurfacePlaneObjects();

        Debug.Log("Finished creating surface planes.");
        isCreatingPlanes = false;

        // Creating surface planes is done and triggers the completion event
        EventHandler handler = CreateSurfacePlanesCompletedEvent;
        if (handler != null)
        {
            handler(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resets the detected surface planes. 
    /// </summary>
    private void ResetSurfacePlaneContent()
    {
        detectedSurfacePlanes.Clear();

        // Remove all room tags from the scene
        foreach (Transform tag in roomTagParent.transform)
        {
            Destroy(tag.gameObject);
        }

        // Removes all suface plane objects from the scene
        foreach (Transform plane in SurfacePlanesParent.transform)
        {
            Destroy(plane.gameObject);
        }
    }

    /// <summary>
    /// Finds the floor and ceiling plane by comparing the y values and areas of the planes. 
    /// Using the up normal threshold to check if the plane is horizontal.
    /// </summary>
    /// <param name="boundedPlanes">List of bounded planes detected by the plane finder api.</param>
    private void FindFloorAndCeilingPosition(BoundedPlane[] boundedPlanes)
    {
        floorYPosition = 0.0f;
        ceilingYPosition = 0.0f;
        float maxFloorArea = 0.0f;
        float maxCeilingArea = 0.0f;

        // Iterate through each bounded plane and check if it is either the floor or the ceiling
        foreach (BoundedPlane boundedPlane in boundedPlanes)
        {
            // Classify the floor as the maximum horizontal surface below the user's head
            if (boundedPlane.Bounds.Center.y < 0 && boundedPlane.Plane.normal.y >= upNormalThreshold)
            {
                maxFloorArea = Mathf.Max(maxFloorArea, boundedPlane.Area);
                if (maxFloorArea == boundedPlane.Area)
                {
                    floorYPosition = boundedPlane.Bounds.Center.y;
                }
            }
            // Classify the ceiling as the maximum horizontal surface above the user's head
            else if (boundedPlane.Bounds.Center.y > 0 && boundedPlane.Plane.normal.y <= -(upNormalThreshold))
            {
                maxCeilingArea = Mathf.Max(maxCeilingArea, boundedPlane.Area);
                if (maxCeilingArea == boundedPlane.Area)
                {
                    ceilingYPosition = boundedPlane.Bounds.Center.y;
                }
            }
        }
    }

    /// <summary>
    /// Returns the type to which the plane belongs. 
    /// </summary>
    /// <param name="boundedPlane">Bounded plane which was detected by the plane finder api.</param>
    /// <returns></returns>
    private PlaneTypes ClassifySurfacePlane(BoundedPlane boundedPlane)
    {
        // If the plane if horizontal and pointing up, the plane is either the floor or a table 
        if (boundedPlane.Plane.normal.y >= upNormalThreshold)
        {
            // Check if the plane is close enough to the previously detected floor position
            if (boundedPlane.Bounds.Center.y <= (floorYPosition + 0.1f))
            {
                return PlaneTypes.Floor;
            } // If the surface plane is too high to be considered part of the floor, classify it as a table
            else
            {
                return PlaneTypes.Table;
            }
        } // If the plane if horizontal and pointing down, the plane is either the ceiling or a table 
        else if (boundedPlane.Plane.normal.y <= -(upNormalThreshold))
        {
            // Check if the plane is close enought to the previously detected ceiling position
            if (boundedPlane.Bounds.Center.y >= (ceilingYPosition - 0.1f))
            {
                return PlaneTypes.Ceiling;
            } // If the surface plane is not high enough to be considered part of the ceiling, classify it as a table 
            else
            {
                return PlaneTypes.Table;
            }
        } // If the plane is vertical, classify it as a wall
        else if (Mathf.Abs(boundedPlane.Plane.normal.y) <= (1 - 0.9f))
        {
            return PlaneTypes.Wall;
        } // If the surface plane has an unusal angle, classify it as unkown
        else
        {
            return PlaneTypes.Unknown;
        }
    }

    /// <summary>
    /// Creates cubes for each surface planes and adds them to the scene. 
    /// </summary>
    private void CreateSurfacePlaneObjects()
    {
        // Display the surface planes in the scene if desired
        if (showSurfacePlanes)
        {
            SurfacePlanesParent.SetActive(true);
        }
        else
        {
            SurfacePlanesParent.SetActive(false);
        }


        foreach (SurfacePlane surfacePlane in detectedSurfacePlanes)
        {
            GameObject figure = Instantiate(surfacePlanePrefab, SurfacePlanesParent.transform);
            figure.GetComponent<SurfacePlaneExtension>().Init(surfacePlane, showSurfacePlanes);
        }
    }
}
