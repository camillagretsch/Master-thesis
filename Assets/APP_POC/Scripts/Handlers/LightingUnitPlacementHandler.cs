using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the lighting setup recommendation. 
/// </summary>
public class LightingUnitPlacementHandler : MonoBehaviour
{
    private SpatialObserverHandler observer;
    private RoomVolumeCalculationHandler calculator;
    private PowerOutletPlacementHandler powerOutletPlacing;

    private List<PowerOutlet> outlets = new List<PowerOutlet>();
    private Mesh combinedMesh;
    private List<int> remainingTriangles = new List<int>();

    // Different lighting unit types 
    private const string TYPE_A = "Type A";
    private const string TYPE_B = "Type B";
    private const string Type_C = "Type C";

    // Return the used light bulbs
    public List<LightingUnit> LightBulbs = new List<LightingUnit>();

    // Returns the light covering percentage
    public float Covering { get; private set; } = 0;

    // Delegate is called when the lighting recommendation event is triggered
    public delegate void EventHandler(object source, EventArgs args);
    // Event handler which is triggered when the lighting recommendation is completed
    public event EventHandler LightingRecommendationCompleted;

#if UNITY_EDITOR || UNITY_STANDALONE
    // Time in sec the converter can run in Unity Editor before returning control to the main program 
    private const float FRAME_TIME = .016f;
#else
    // Time in sec the converter can run before returning control to the main program
    private const float FRAME_TIME = .008f;
#endif

    [Tooltip("Light bulb object to place in room.")]
    [SerializeField]
    private GameObject bulbPrefab = null;

    [Tooltip("Mesh Container for visualize the light.")]
    [SerializeField]
    private GameObject meshContainerPrefab = null;

    [Tooltip("Defines brightness preference.")]
    [SerializeField]
    public BrightnessPreference Preference = BrightnessPreference.High;

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        // Acess the set power outlets
        powerOutletPlacing = GameObject.Find("PowerOutletCollection").GetComponent<PowerOutletPlacementHandler>();
        // The power outlets are ordered by range so that first a lighting unit is placed at the power outlet with the largest range
        outlets = powerOutletPlacing.PowerOutletCollection.OrderByDescending(d => d.range).ToList();

        // Access and update the combined mesh
        GameObject spatialProcessing = GameObject.Find("SpatialProcessing_and_RoomVolumeCalculation");
        observer = spatialProcessing.GetComponent<SpatialObserverHandler>();
        calculator = spatialProcessing.GetComponent<RoomVolumeCalculationHandler>();

        observer.SetMeshDisplayOption(SpatialAwarenessMeshDisplayOptions.None);
        combinedMesh = observer.CombinedMesh;
        combinedMesh.subMeshCount = outlets.Count;
        remainingTriangles = combinedMesh.triangles.ToList();

        CreateRecommendation();
    }

    /// <summary>
    /// Creates recommendation and divides into first and following recommendations.
    /// </summary>
    private void CreateRecommendation()
    {
        if (LightBulbs.Count == 0)
        {
            FirstRecommendation();
        }
        else
        {
            FollowingRecommendation();
        }
    }

    /// <summary>
    /// For the frist recommendation the lighting unit type is chosen depending on the set brighntess preference and the calulated room volume.
    /// </summary>
    private void FirstRecommendation()
    {
        if (Preference == BrightnessPreference.High)
        {
            if (calculator.RoomVolume > 60)
            {
                PlaceLightingUnit(outlets[0], TYPE_A);
            }
            else
            {
                PlaceLightingUnit(outlets[0], TYPE_B);
            }
        }
        else if (Preference == BrightnessPreference.Medium)
        {
            if (calculator.RoomVolume > 70)
            {
                PlaceLightingUnit(outlets[0], TYPE_A);
            }
            else if (calculator.RoomVolume > 35 && calculator.RoomVolume <= 70)
            {
                PlaceLightingUnit(outlets[0], TYPE_B);
            }
            else
            {
                PlaceLightingUnit(outlets[0], Type_C);
            }
        }
        else if (Preference == BrightnessPreference.Low)
        {
            if (calculator.RoomVolume > 120)
            {
                PlaceLightingUnit(outlets[0], TYPE_A);
            }
            else if (calculator.RoomVolume > 65 && calculator.RoomVolume <= 120)
            {
                PlaceLightingUnit(outlets[0], TYPE_B);
            }
            else
            {
                PlaceLightingUnit(outlets[0], Type_C);
            }
        }
    }

    /// <summary>
    /// The following recommendation depends on how much is already covered by light.
    /// </summary>
    private void FollowingRecommendation()
    {
        // Coverage is calulated by the number of remaining triangles divided by the total number of triangles
        Covering = 100 - (((float)remainingTriangles.Count / (float)observer.CombinedMesh.triangles.Length) * 100);

        // If the brightness preference is high und the coverage is smaller than it should be
        if (Preference == BrightnessPreference.High && Covering < BrightnessPreference.High.GetHashCode())
        {
            if (Covering < 50)
            {
                PlaceLightingUnit(outlets[LightBulbs.Count], TYPE_A);
            }
            else
            {
                PlaceLightingUnit(outlets[LightBulbs.Count], TYPE_B);
            }
        } // If the brightness preference is medium und the coverage is smaller than it should be
        else if (Preference == BrightnessPreference.Medium && Covering < BrightnessPreference.Medium.GetHashCode())
        {
            if (Covering < 40)
            {
                PlaceLightingUnit(outlets[LightBulbs.Count], TYPE_A);
            }
            else
            {
                PlaceLightingUnit(outlets[LightBulbs.Count], TYPE_B);
            }
        } // If the brightness preference is low und the coverage is smaller than it should be
        else if (Preference == BrightnessPreference.Low && Covering < BrightnessPreference.Low.GetHashCode())
        {
            if (Covering < 5)
            {
                PlaceLightingUnit(outlets[LightBulbs.Count], TYPE_A);
            }
            else if (Covering < 15)
            {
                PlaceLightingUnit(outlets[LightBulbs.Count], TYPE_B);
            }
            else
            {
                PlaceLightingUnit(outlets[LightBulbs.Count], Type_C);
            }
        } // If the coverage is equal or higher than it should be the lighting recommendation is finished
        else
        {
            EventHandler handler = LightingRecommendationCompleted;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Creates and places a lighting unit at the power outlet position with the largest range.
    /// </summary>
    /// <param name="p">Current power outlet with the largest range.</param>
    /// <param name="type">Lighting unit type which should be created.</param>
    private void PlaceLightingUnit(PowerOutlet p, string type)
    {
        // Creating a lighting unit and set its properties
        GameObject bulb = Instantiate(bulbPrefab, gameObject.transform);
        bulb.transform.position = p.lampPosition;
        LightingUnit unit = new LightingUnit(p.lampPosition, p.surfacePlane.Type, bulb, p.hits.Select(d => d.distance).Max(), 0, type);

        LightingUnitConfigurationHandler confgurator = bulb.GetComponent<LightingUnitConfigurationHandler>();
        if (confgurator != null)
        {
            confgurator.Init(unit, LightBulbs.Count);
        }

        LightBulbs.Add(unit);

        SetupMesh(unit);
        StartCoroutine(CreateSphereRayCasts(unit));
    }

    /// <summary>
    /// Setup the mesh where the light is displayed on.
    /// </summary>
    /// <param name="unit">Current lighting unit.</param>
    private void SetupMesh(LightingUnit unit)
    {
        Mesh mesh = new Mesh();

        // Set the mesh depending on the number of lighting units already exist
        if (LightBulbs.Count == 1)
        {
            mesh = combinedMesh;
        } else
        {
            mesh = combinedMesh.GetSubMesh(LightBulbs.Count - 1);
        }

        // Create the mesh and add it to the lighting unit object
        GameObject meshC = GameObject.Instantiate(meshContainerPrefab, unit.bulb.transform, true);
        Transform spatialAwarenessSystemContainer = GameObject.Find("Spatial Awareness System").transform.GetChild(0);
        meshC.transform.position = spatialAwarenessSystemContainer.position;
        meshC.transform.rotation = spatialAwarenessSystemContainer.rotation;
        meshC.GetComponent<MeshFilter>().mesh = mesh;
        meshC.GetComponent<MeshCollider>().sharedMesh = mesh;
        meshC.SetActive(true);

        DisplayLighting(unit);
    }

    /// <summary>
    /// Visualize the light on the mesh by setting the properties of the light shader accorind to the lighting unit.
    /// </summary>
    /// <param name="unit">Current lighting unit.</param>
    private void DisplayLighting(LightingUnit unit)
    {
        LightingUnitConfigurationHandler configurator = unit.bulb.GetComponent<LightingUnitConfigurationHandler>();
        if (configurator == null)
        {
            unit.bulb.transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetVector("_CenterPoint", unit.position);
            unit.bulb.transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetFloat("_MaxDistance", unit.maxRange);
            unit.bulb.transform.GetChild(1).gameObject.GetComponent<Renderer>().material.SetFloat("_ChangePoint", unit.lux);
        } else
        {
            unit.bulb.transform.GetChild(2).gameObject.GetComponent<Renderer>().material.SetVector("_CenterPoint", unit.position);
            unit.bulb.transform.GetChild(2).gameObject.GetComponent<Renderer>().material.SetFloat("_MaxDistance", unit.maxRange);
            unit.bulb.transform.GetChild(2).gameObject.GetComponent<Renderer>().material.SetFloat("_ChangePoint", unit.lux);
        }
    }

    /// <summary>
    /// Makes a spherecast from the light position to the mesh triangles.
    /// </summary>
    /// <param name="unit">Current lighting unit</param>
    /// <returns></returns>
    private IEnumerator CreateSphereRayCasts(LightingUnit unit)
    {
        // List of all triangles in the mesh
        List<int> allTriangles = remainingTriangles.Where((x, i) => i % 3 == 0).ToList();

        // Pause work, and continue on the next frame
        yield return null;
        float start = Time.realtimeSinceStartup;

        // Reset the remaining traingles list
        remainingTriangles = new List<int>();
        RaycastHit hit;
        int hitsCount = 0;

        // Iterate over each triangle to see if it is hit by the spherecast
        foreach (int i in allTriangles)
        {
            Vector3 direction = combinedMesh.vertices[i] - unit.position;

            if (Physics.SphereCast(unit.position, 0, direction, out hit, Mathf.Infinity, 1 << 31))
            {
                if (hit.transform != null && hit.transform.gameObject.layer == 31)
                {
                    // Add triangles to remaining if it is not within the lighting unit range
                    if (hit.distance > unit.lux)
                    {
                        remainingTriangles.Add(combinedMesh.triangles[hit.triangleIndex * 3 + 0]);
                        remainingTriangles.Add(combinedMesh.triangles[hit.triangleIndex * 3 + 1]);
                        remainingTriangles.Add(combinedMesh.triangles[hit.triangleIndex * 3 + 2]);
                    } // Add triangles to hits if it is within the lighting unit range
                    else
                    {
                        hitsCount += 3;
                        //Debug.DrawLine(unit.position, hit.point, Color.blue, 100);
                    }
                }
            }

            // If too much time has passed, we need to return control to the main game loop
            if ((Time.realtimeSinceStartup - start) > FRAME_TIME)
            {
                // Pause our work, and continue on the next frame
                yield return null;
                start = Time.realtimeSinceStartup;
            }
        }

        LightingUnit tmp = LightBulbs.Last();
        tmp.hits = hitsCount;
        LightBulbs[LightBulbs.Count - 1] = tmp;

        // Check if a power outlet is not used yet and start the recommendation process over
        if (LightBulbs.Count < outlets.Count)
        {
            // Inititalize a new submesh for the next lighting unit with the remaining triangles
            combinedMesh.SetTriangles(remainingTriangles, LightBulbs.Count); 
            CreateRecommendation();
        } // If all power outlets are taken the recommendation is finished 
        else
        {
            EventHandler handler = LightingRecommendationCompleted;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
