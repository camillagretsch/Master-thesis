using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles the lighting unit configuration.
/// </summary>
public class LightingUnitConfigurationHandler : MonoBehaviour, IMixedRealityFocusHandler, IMixedRealityPointerHandler
{
    private SpatialMeshConversionHandler converter;
    private SpatialObserverHandler observer;

    private int index;
    private bool isMovig = false;
    private LightingUnit bulb;

    [Tooltip("UI text panel component.")]
    [SerializeField]
    private GameObject textPanel;

    [Tooltip("UI title text component.")]
    [SerializeField]
    private GameObject title;

    [Tooltip("UI description text component.")]
    [SerializeField]
    private GameObject description;

    [Tooltip("UI configuration button component.")]
    [SerializeField]
    private GameObject configureButtons;

    [Tooltip("UI lighting unit type button component.")]
    [SerializeField]
    private GameObject bulbTypeButtons;

#if UNITY_EDITOR || UNITY_STANDALONE
    // Time in sec the converter can run in Unity Editor before returning control to the main program 
    private const float FRAME_TIME = .016f;
#else
    // Time in sec the converter can run before returning control to the main program
    private const float FRAME_TIME = .008f;
#endif

    /// <summary>
    /// Initializes the lighting unit game object.
    /// </summary>
    /// <param name="b"></param>
    public void Init(LightingUnit b, int index)
    {
        bulb = b;
        this.index = index;

        GameObject spatialProcessing = GameObject.Find("SpatialProcessing_and_RoomVolumeCalculation");
        converter = spatialProcessing.GetComponent<SpatialMeshConversionHandler>();
        observer = spatialProcessing.GetComponent<SpatialObserverHandler>();
    } 

    /// <summary>
    /// Displays infromation about the lighting unit on a text panel. 
    /// </summary>
    public void ShowLampInformations()
    {
        title.GetComponent<TextMeshPro>().text = bulb.unitType;
        description.GetComponent<TextMeshPro>().text = "Light intensity (lux): " + bulb.lux +
            "\nDurability: " + bulb.durability +
            "\nWatt: " + bulb.watt +
            "\nEnergy consumption (watt/h per illuminated surface unit): " + (bulb.watt / (Mathf.PI * bulb.lux * bulb.lux)).ToString("F1") + " with an estimated durability of " + bulb.durability + " years";

        SurfacePlane ceiling = converter.GetFloorOrCeiling(PlaneTypes.Ceiling);
        if (ceiling.Area == 0)
        {
            ceiling = converter.GetFloorOrCeiling(PlaneTypes.Floor);
        }

        Vector3 heading = ceiling.Bounds.Center - bulb.position;
        if (bulb.planeType == PlaneTypes.Ceiling)
        {
            textPanel.transform.position = new Vector3(bulb.position.x, bulb.position.y - 0.5f, bulb.position.z);
        } else
        {
            textPanel.transform.position = new Vector3(bulb.position.x, bulb.position.y + 0.3f, bulb.position.z);
        }
        textPanel.SetActive(true);

    }

    /// <summary>
    /// Hide infromation panel.
    /// </summary>
    public void HideLampInformation()
    {
        textPanel.SetActive(false);
    }

    /// <summary>
    /// Move the lighting unit around by activating the solver which makes the lighting unit follow the user's eye gaze.
    /// </summary>
    public void Move()
    {
        textPanel.SetActive(false);
        gameObject.transform.GetChild(0).GetComponent<SolverHandler>().UpdateSolvers = true;
        isMovig = true;
    }

    /// <summary>
    /// Places the lighting unit with air tap.
    /// </summary>
    private void StopMove()
    {
        gameObject.transform.GetChild(0).GetComponent<SolverHandler>().UpdateSolvers = false;
        isMovig = false;

        bulb.position = transform.GetChild(0).position;
        transform.GetChild(2).gameObject.GetComponent<Renderer>().material.SetVector("_CenterPoint", bulb.position);
        StartCoroutine(CreateSphereRayCasts());
    }

    /// <summary>
    /// Displays the different lighting unit types.
    /// </summary>
    public void Configure()
    {
        textPanel.SetActive(false);
        title.GetComponent<TextMeshPro>().text = "Light Bulb Types";
        description.GetComponent<TextMeshPro>().text = "- Type A has a lux range of 4, a durability of 3 years and a watt consumption of 100 \n" +
            "- Type B has a lux range of 3, a durability of 4 years and watt consumption of 70 \n" +
            "- Type C has a lux range of 1, a durability of 6 years and watt consumption of 10";
        textPanel.SetActive(true);
        configureButtons.SetActive(false);
        bulbTypeButtons.SetActive(true);
    }

    /// <summary>
    /// Updates the lighting unit to type A.
    /// </summary>
    public void SelectTypeA()
    {
        textPanel.SetActive(false);
        configureButtons.SetActive(true);
        bulbTypeButtons.SetActive(false);

        bulb.unitType = "Type A";
        bulb.lux = 4;
        bulb.watt = 100;
        bulb.durability = 3;
        transform.GetChild(2).gameObject.GetComponent<Renderer>().material.SetFloat("_ChangePoint", bulb.lux);
        StartCoroutine(CreateSphereRayCasts());
    }

    /// <summary>
    /// Updates the lighting unit to type B.
    /// </summary>
    public void SelectTypeB()
    {
        textPanel.SetActive(false);
        configureButtons.SetActive(true);
        bulbTypeButtons.SetActive(false);

        bulb.unitType = "Type B";
        bulb.lux = 3;
        bulb.watt = 70;
        bulb.durability = 4;
        transform.GetChild(2).gameObject.GetComponent<Renderer>().material.SetFloat("_ChangePoint", bulb.lux);
        StartCoroutine(CreateSphereRayCasts());
    }

    /// <summary>
    /// Updates the lighting unit to type C.
    /// </summary>
    public void SelectTypeC()
    {
        textPanel.SetActive(false);
        configureButtons.SetActive(true);
        bulbTypeButtons.SetActive(false);

        bulb.unitType ="Type C";
        bulb.lux = 1;
        bulb.watt = 10;
        bulb.durability = 6;
        transform.GetChild(2).gameObject.GetComponent<Renderer>().material.SetFloat("_ChangePoint", bulb.lux);
        StartCoroutine(CreateSphereRayCasts());
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        if (isMovig)
        {
            StopMove();
        }
    }

    /// <summary>
    /// Makes a spherecast from the light position to the mesh triangles.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateSphereRayCasts()
    {
        // List of all triangles in the mesh
        List<int> allTriangles = transform.GetChild(2).gameObject.GetComponent<MeshFilter>().mesh.triangles.Where((x, i) => i % 3 == 0).ToList();

        // Pause work, and continue on the next frame
        yield return null;
        float start = Time.realtimeSinceStartup;

        RaycastHit hit;
        int hitsCount = 0;

        // Iterate over each triangle to see if it is hit by the spherecast
        foreach (int i in allTriangles)
        {
            Vector3 direction = observer.CombinedMesh.vertices[i] - bulb.position;

            if (Physics.SphereCast(bulb.position, 0, direction, out hit, Mathf.Infinity, 1 << 31))
            {
                if (hit.transform != null && hit.transform.gameObject.layer == 31)
                {
                    // Add triangles to remaining if it is not within the lighting unit range
                    if (hit.distance > bulb.lux)
                    {

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

        bulb.hits = hitsCount;
        GetComponentInParent<LightingUnitPlacementHandler>().LightBulbs[index] = bulb;
        GameObject.Find("Manager").GetComponent<PrototypeManager>().LightingUnitUpdated();
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
    }

    public void OnFocusEnter(FocusEventData eventData)
    {
    }

    public void OnFocusExit(FocusEventData eventData)
    {
    }
}
