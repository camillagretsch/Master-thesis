using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the placement of the power outlets in the room. 
/// </summary>
public class PowerOutletPlacementHandler : BaseInputHandler, IMixedRealityPointerHandler
{
    private SpatialObserverHandler observer;
    private SpatialMeshConversionHandler converter;
    private GameObject planeCollection;

    // Returns if the list of power outlets
    public List<PowerOutlet> PowerOutletCollection { get; private set; } = new List<PowerOutlet>();

    // Delegate is called when the power outlet placement event is triggered
    public delegate void EventHandler(object source, EventArgs args);
    // Event handler which is triggered when the power outlet placement is completed
    public event EventHandler PowerOutletPlacementCompleted;

    [Tooltip("power outlet object to place in room.")]
    [SerializeField]
    private GameObject powerOutletPrefab = null;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    protected override void Start()
    {
        base.Start();
        InputSystem.RegisterHandler<IMixedRealityPointerHandler>(this);
        PowerOutletCollection = new List<PowerOutlet>();

        SetPlaneCollectionActive();
    }

    /// <summary>
    /// Finds the surface planes in the scene and set them active.
    /// Helps to find locations where the power outlets can be attached. 
    /// </summary>
    private void SetPlaneCollectionActive()
    {
        GameObject spatialProcessing = GameObject.Find("SpatialProcessing");
        if (spatialProcessing == null)
        {
            spatialProcessing = GameObject.Find("SpatialProcessing_and_RoomVolumeCalculation");
        }
        observer = spatialProcessing.GetComponent<SpatialObserverHandler>();
        converter = spatialProcessing.GetComponent<SpatialMeshConversionHandler>();
        planeCollection = converter.SurfacePlanesParent;
        planeCollection.SetActive(true);
    }

    /// <summary>
    /// Checks if the power outlet placed on a wall, floor, ceiling or table and saves it.
    /// </summary>
    private void SavePowerOutlet()
    {
        int childCount = gameObject.transform.childCount;
        GameObject powerOutlet = gameObject.transform.GetChild(childCount - 1).gameObject;

        // Detect on which detected surface plane the power outlet is attached
        foreach (Transform plane in planeCollection.transform)
        {
            if (plane.GetComponent<BoxCollider>().bounds.Contains(powerOutlet.transform.localPosition))
            {
                // Stop the surface magnetisme solver for this power outlet object
                powerOutlet.GetComponent<SolverHandler>().UpdateSolvers = false;

                powerOutlet.transform.GetChild(0).GetComponent<Renderer>().material.SetColor("_RimColor", Color.green);
                powerOutlet.transform.GetChild(1).GetComponent<Renderer>().material.SetColor("_RimColor", Color.green);
                StartCoroutine(SetGreyColour(powerOutlet));

                Vector3 position = CalculatePossibleLampPosition(plane, powerOutlet);

                // Save the power outlet, its location and the surface plane where it is attached to
                PowerOutletCollection.Add(new PowerOutlet(powerOutlet, plane.GetComponent<SurfacePlaneExtension>().Plane, position, CreateSphereCastToMesh(position)));
                Debug.Log("Placed on " + plane.GetComponent<SurfacePlaneExtension>().Plane.Type);

                // Power outlet placement is done and triggers the completion event
                if (PowerOutletCollection.Count >= 3)
                {
                    EventHandler handler = PowerOutletPlacementCompleted;

                    if (handler != null)
                    {
                        InputSystem.UnregisterHandler<IMixedRealityPointerHandler>(this);
                        planeCollection.SetActive(false);
                        handler(this, EventArgs.Empty);
                    }
                } else
                {
                    // Add new power outlet to the scene
                    Instantiate(powerOutletPrefab, new Vector3(0, 0, 0), Quaternion.identity, gameObject.transform);
                }
                return;
            }
        }
        powerOutlet.transform.GetChild(0).GetComponent<Renderer>().material.SetColor("_RimColor", Color.red);
        powerOutlet.transform.GetChild(1).GetComponent<Renderer>().material.SetColor("_RimColor", Color.red);
        StartCoroutine(SetGreyColour(powerOutlet));
    }

    /// <summary>
    /// Handles the flashing of the power outlet.
    /// </summary>
    /// <param name="outlet">Respective power outlet.</param>
    /// <returns></returns>
    private IEnumerator SetGreyColour(GameObject outlet)
    {
        Color rimColor = new Color(0.8679245f, 0.8679245f, 0.8474546f, 1);
        
        yield return new WaitForSeconds(2);
        outlet.transform.GetChild(0).GetComponent<Renderer>().material.SetColor("_RimColor", rimColor);
        outlet.transform.GetChild(1).GetComponent<Renderer>().material.SetColor("_RimColor", rimColor);
    }

    /// <summary>
    /// A lamp will always be placed near a power oulet.
    /// Foreach power outlet a possible lamp position will be saved.
    /// </summary>
    /// <param name="plane">Surface plane where the power outlet is attached to.</param>
    /// <param name="outlet">The actual poweroutlet.</param>
    /// <returns></returns>
    private Vector3 CalculatePossibleLampPosition(Transform plane, GameObject outlet)
    {
        // If the power outlet is on the floor the potential lamp position is above it
        if (plane.GetComponent<SurfacePlaneExtension>().Plane.Type == PlaneTypes.Floor)
        {
            return new Vector3(outlet.transform.position.x, outlet.transform.position.y + 1.5f, outlet.transform.position.z);
        }
        else if (plane.GetComponent<SurfacePlaneExtension>().Plane.Type == PlaneTypes.Ceiling) // If the üpower outlet is on the ceiling the potential lamp position is below it
        {
            return new Vector3(outlet.transform.position.x, outlet.transform.position.y - 0.5f, outlet.transform.position.z);
        }
        else if (plane.GetComponent<SurfacePlaneExtension>().Plane.Type == PlaneTypes.Table) // If the power outlet is on a table the potential lamp position is above it
        {
            return new Vector3(outlet.transform.position.x, outlet.transform.position.y + 0.3f, outlet.transform.position.z);
        }
        else // If the power outlet is on a wall the potential lamp position is in front of it
        {
            //Vector3 heading = new Vector3(plane.forward.x, outlet.transform.position.y, plane.forward.z) - outlet.transform.position;
            SurfacePlane ceiling = converter.GetFloorOrCeiling(PlaneTypes.Ceiling);
            if (ceiling.Area == 0)
            {
                ceiling = converter.GetFloorOrCeiling(PlaneTypes.Floor);
            }

            Vector3 heading = new Vector3(ceiling.Bounds.Center.x, outlet.transform.position.y, ceiling.Bounds.Center.z) - outlet.transform.position;
            return new Vector3(outlet.transform.position.x + (0.3f * Mathf.Sign(heading.x)), outlet.transform.position.y, outlet.transform.position.z + (0.3f * Mathf.Sign(heading.z)));
        }
    }

    /// <summary>
    /// Makes a sphere cast from the potential lamp position to the mesh. 
    /// This is relevant for the lamp placement recommendation.
    /// </summary>
    /// <param name="origin">Potential lamp position.</param>
    /// <returns>List of ray cast hits.</returns>
    private List<RaycastHit> CreateSphereCastToMesh(Vector3 origin)
    {
        List<int> allTriangleApices = observer.CombinedMesh.triangles.Where((x, i) => i % 3 == 0).ToList();
        List<int> selectedTriangles = allTriangleApices.Where((x, i) => i % 100 == 0).ToList();

        RaycastHit hit;
        List<int> hitTriangles = new List<int>();
        List<RaycastHit> hits = new List<RaycastHit>();

        foreach (int index in selectedTriangles)
        {
            Vector3 direction = observer.CombinedMesh.vertices[index] - origin;

            if (Physics.SphereCast(origin, 0, direction, out hit, Mathf.Infinity, 1 << 31))
            {
                if (hit.transform != null && hit.transform.gameObject.layer == 31)
                {
                    //Debug.DrawLine(origin, hit.point, Color.blue, 100);
                    hitTriangles.Add(observer.CombinedMesh.triangles[hit.triangleIndex * 3 + 0]);
                    hitTriangles.Add(observer.CombinedMesh.triangles[hit.triangleIndex * 3 + 1]);
                    hitTriangles.Add(observer.CombinedMesh.triangles[hit.triangleIndex * 3 + 2]);
                    hits.Add(hit);
                }
            }
        }

        Debug.Log(hits.Count);
        return hits;
    }

    protected override void RegisterHandlers() { }

    protected override void UnregisterHandlers() { }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        SavePowerOutlet();
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData) { }

    public void OnPointerUp(MixedRealityPointerEventData eventData) { }

    public void OnPointerClicked(MixedRealityPointerEventData eventData) { }
}