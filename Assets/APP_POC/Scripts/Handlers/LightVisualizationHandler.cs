using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the light visualization for one light in a room.
/// </summary>
public class LightVisualizationHandler : MonoBehaviour
{
    private SpatialObserverHandler observer;
    private SpatialMeshConversionHandler converter;

    private float maxDistance = 0;
    private int countMeshHit = 0;
    private int countOtherHit = 0;

#if UNITY_EDITOR || UNITY_STANDALONE
    // Time in sec the converter can run in Unity Editor before returning control to the main program 
    private const float FRAME_TIME = .016f;
#else
    // Time in sec the converter can run before returning control to the main program
    private const float FRAME_TIME = .008f;
#endif

    [Tooltip("Light object to place in room")]
    [SerializeField]
    private new GameObject light = null;

    [Tooltip("Mesh Container for visualize the light.")]
    [SerializeField]
    private GameObject meshContainer = null;

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        GameObject spatialProcessing = GameObject.Find("SpatialProcessing");
        observer = spatialProcessing.GetComponent<SpatialObserverHandler>();
        converter = spatialProcessing.GetComponent<SpatialMeshConversionHandler>();
        Debug.Log(Time.realtimeSinceStartup);
        PlaceLight();
        StartCoroutine(CreateSphereRayCastToMesh());
    }


    /// <summary>
    /// Place the light below the center point of the ceiling.
    /// </summary>
    private void PlaceLight()
    {
        Vector3 ceilingCenter = converter.GetFloorOrCeiling(PlaneTypes.Ceiling).Bounds.Center;
        light.transform.position = new Vector3(ceilingCenter.x, ceilingCenter.y - 0.4f, ceilingCenter.z);
    }

    /// <summary>
    /// Makes a spherecast from the light position to the mesh triangles.
    /// </summary>
    private IEnumerator CreateSphereRayCastToMesh()
    {
        // List of all triangles in the mesh
        List<int> allTriangleApices = observer.CombinedMesh.triangles.Where((x, i) => i % 3 == 0).ToList();

        // Subset of all traingles
        List<int> selectedTriangles = allTriangleApices.Where((x, i) => i % 10 == 0).ToList();

        // Pause work, and continue on the next frame
        yield return null;
        float start = Time.realtimeSinceStartup;

        RaycastHit hit;
        List<RaycastHit> hits = new List<RaycastHit>();

        // Iterate over each triangle to see if it is hit by the spherecast
        foreach (int i in selectedTriangles)
        {
            Vector3 direction = observer.CombinedMesh.vertices[i] - light.transform.position;

            if (Physics.SphereCast(light.transform.position, 0, direction, out hit, Mathf.Infinity, 1 << 31))
            {
                if (hit.transform != null)
                {
                    if (hit.transform.gameObject.layer == 31)
                    {
                        countMeshHit++;

                        //Debug.DrawLine(light.transform.position, hit.point, Color.blue, 100);
                        hits.Add(hit);
                    }
                    else if (hit.transform.gameObject.layer != 31)
                    {
                        //Debug.DrawLine(light.transform.position, hit.point, Color.black, 100);
                        countOtherHit++;
                    }
                }
            }
            else
            {
                //Debug.DrawLine(light.transform.position, observer.CombinedMesh.vertices[i], Color.green, 100);
                countOtherHit++;
            }

            // If too much time has passed, we need to return control to the main game loop
            if ((Time.realtimeSinceStartup - start) > FRAME_TIME)
            {
                // Pause our work, and continue on the next frame
                yield return null;
                start = Time.realtimeSinceStartup;
            }
        }

        // Calulate the maximal distance from light to the hit triangles
        maxDistance = hits.Select(h => h.distance).Max();
        Debug.Log("total triangles: " + allTriangleApices.Count + ", selected for raycast triangles: " + selectedTriangles.Count + ", mesh hits: " + countMeshHit + ", other hits: " + countOtherHit);

        SetupMesh();
        DisplayLighting();
    }

    /// <summary>
    /// Setup the mesh where the light is displayed on. 
    /// </summary>
    private void SetupMesh()
    {
        GameObject meshC = GameObject.Instantiate(meshContainer, light.transform, true);

        // Needs to have the same position and orientation as the spatial mesh from the spatial awareness system
        Transform spatialAwarenessSystemContainer = GameObject.Find("Spatial Awareness System").transform.GetChild(0);
        meshC.transform.position = spatialAwarenessSystemContainer.position;
        meshC.transform.rotation = spatialAwarenessSystemContainer.rotation;

        meshC.GetComponent<MeshFilter>().mesh = observer.CombinedMesh;
        meshC.GetComponent<MeshCollider>().sharedMesh = observer.CombinedMesh;

        meshC.SetActive(true);
    }

    /// <summary>
    /// Change the properties of the light shader.
    /// </summary>
    private void DisplayLighting()
    {
        observer.SetMeshDisplayOption(SpatialAwarenessMeshDisplayOptions.None);

        light.transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetVector("_CenterPoint", light.transform.position);
        light.transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetFloat("_MaxDistance", maxDistance);
        light.transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetFloat("_ChangePoint", maxDistance / 2);

        Debug.Log(Time.realtimeSinceStartup);
    }
}
