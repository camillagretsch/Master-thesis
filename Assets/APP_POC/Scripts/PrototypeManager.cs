using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PrototypeManager : MonoBehaviour
{
    [Tooltip("UI text panel component.")]
    [SerializeField]
    private GameObject textPanel = null;

    [Tooltip("UI title text component.")]
    [SerializeField]
    private GameObject title = null;

    [Tooltip("UI description text component.")]
    [SerializeField]
    private GameObject description = null;

    [Tooltip("UI button component.")]
    [SerializeField]
    private GameObject buttons = null;

    [Tooltip("Spatial processing (and room volume calculation) component.")]
    [SerializeField]
    private GameObject spatialProcessing = null;

    [Tooltip("Power outlet collection component.")]
    [SerializeField]
    private GameObject powerOutletHandler = null;

    [Tooltip("Lighting unit collection component.")]
    [SerializeField]
    private GameObject lightingUnitHandler = null;

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        StartCoroutine(ShowStartScene());

        // Register for the spatial processing completed event
        spatialProcessing.GetComponent<SpatialProcessingHandler>().SpatialProcessingCompletedEvent += SpatialProcessingCompletedEventHandler;

        // Register for the power outlet completed event
        powerOutletHandler.GetComponent<PowerOutletPlacementHandler>().PowerOutletPlacementCompleted += PowerOutletPlacementCompletedEventHandler;

        // Register for the lighting recommendation completed event
        lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().LightingRecommendationCompleted += LightingRecommendationCompletedEventHandler;
    }

    /// <summary>
    /// Sets the title and description in the text panel. 
    /// </summary>
    /// <param name="title">Title text to be displayed.</param>
    /// <param name="description">Description text to be displaye</param>
    private void SetPanelText(string title, string description)
    {
        this.title.GetComponent<TextMeshPro>().text = title;
        this.description.GetComponent<TextMeshPro>().text = description;
    }

    /// <summary>
    /// Displays the start description panel for 5 seconds. 
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShowStartScene()
    {
        SetPanelText("Ambient Lighting in Mixed Reality", "A sustainable lighting setup recommendation containing" +
            "\n - the placement and visualization of lighting units  " +
            "\n - the light distribution visualization" +
            "\n - the configuration of lighting units");
        yield return new WaitForSeconds(5);
        textPanel.SetActive(false);

        ShowBrightnessPreferenceSettingInstruction();
    }

    /// <summary>
    /// Displays brightness preference settings panel.
    /// </summary>
    private void ShowBrightnessPreferenceSettingInstruction()
    {
        textPanel.GetComponentInParent<SolverHandler>().UpdateSolvers = false;
        SetPanelText("Brightness Preference Setting", "Choose a preference with an Air Tap on the according radio button." +
            "\nThe lighting setup recommendation depends on the preference setting.");
        textPanel.SetActive(true);

        // Display radio and next buttons
        buttons.transform.GetChild(0).gameObject.SetActive(true);
        buttons.transform.GetChild(1).gameObject.SetActive(true);
    }

    /// <summary>
    /// Handles the spatial mesh completion event. 
    /// Displays if more spatial data is needed or if the scanning is completed and the calculated room area. 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    /// <param name="message"></param>
    private void SpatialProcessingCompletedEventHandler(object source, System.EventArgs args, string message)
    {
        if (message.Equals("completed"))
        {
            float area = spatialProcessing.GetComponent<RoomVolumeCalculationHandler>().RoomWidth * spatialProcessing.GetComponent<RoomVolumeCalculationHandler>().RoomLength;
            SetPanelText("Room Volume", area.ToString("F2") + " square meters");
            textPanel.SetActive(true);
            buttons.transform.GetChild(3).gameObject.SetActive(false);
            buttons.transform.GetChild(4).gameObject.SetActive(true);
        } else
        {
            SetPanelText(message, "");
            textPanel.SetActive(true);
            StartCoroutine(SetScanTimeout());
        }
    }

    /// <summary>
    /// Sets a timeout in seconds. 
    /// </summary>
    private IEnumerator SetScanTimeout()
    {
        yield return new WaitForSeconds(2);
        textPanel.SetActive(false);
    }

    /// <summary>
    /// Handles the power outlet placement completion event.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void PowerOutletPlacementCompletedEventHandler(object source, System.EventArgs args)
    {
        SetPanelText("Lighting Setup Recommendation", "Be patient! It can take some time to calculate the whole recommendation" +
            "\nThe color gradient from red to blue represents the light intensity (red = strong illumination)" +
            "\nThe recommendation is based on a combination of brightness preference, room volume and power outlet placement");
        textPanel.SetActive(true);

        StartCoroutine(SetLightingTimeout());
    }

    /// <summary>
    /// Sets a timeout in seconds. 
    /// </summary>
    private IEnumerator SetLightingTimeout()
    {
        yield return new WaitForSeconds(5);
        lightingUnitHandler.SetActive(true);
    }

    /// <summary>
    /// Handles the lighting recommendation completion event.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public void LightingRecommendationCompletedEventHandler(object source, System.EventArgs args)
    {
        string desc = (int)lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().Covering + " % of the room is strongly illuminated (red colored)";

        textPanel.SetActive(false);

        List<LightingUnit> bulbs = lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().LightBulbs;

        if (bulbs.Where(b => b.unitType == "Type A").Count() > 0)
        {
            desc += "\n - " + bulbs.Where(b => b.unitType == "Type A").Count() + " Type A, energy consumption of " + (LightingUnit.GetLightingUnitTypeValues("Type A")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type A")[0] * LightingUnit.GetLightingUnitTypeValues("Type A")[0])).ToString("F1") + " with an estimated durability of " + LightingUnit.GetLightingUnitTypeValues("Type A")[1] + " years";
                
        } 

        if (bulbs.Where(b => b.unitType == "Type B").Count() > 0)
        {
            desc += "\n - " + bulbs.Where(b => b.unitType == "Type B").Count() + " Type B, energy consumption of " + (LightingUnit.GetLightingUnitTypeValues("Type B")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type B")[0] * LightingUnit.GetLightingUnitTypeValues("Type B")[0])).ToString("F1") + " with an estimated durability of " + LightingUnit.GetLightingUnitTypeValues("Type B")[1] + " years";
        } 

        if (bulbs.Where(b => b.unitType == "Type C").Count() > 0)
        {
            desc += "\n - " + bulbs.Where(b => b.unitType == "Type C").Count() + " Type C, energy consumption of " + (LightingUnit.GetLightingUnitTypeValues("Type C")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type C")[0] * LightingUnit.GetLightingUnitTypeValues("Type C")[0])).ToString("F1") + " with an estimated durability of " + LightingUnit.GetLightingUnitTypeValues("Type C")[1] + " years";
        }

        desc += "\nTotal energy consumption per illuminated unit: " + (((LightingUnit.GetLightingUnitTypeValues("Type A")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type A")[0] * LightingUnit.GetLightingUnitTypeValues("Type A")[0])) * bulbs.Where(b => b.unitType == "Type A").Count()) + ((LightingUnit.GetLightingUnitTypeValues("Type B")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type B")[0] * LightingUnit.GetLightingUnitTypeValues("Type B")[0])) * bulbs.Where(b => b.unitType == "Type B").Count()) + ((LightingUnit.GetLightingUnitTypeValues("Type C")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type C")[0] * LightingUnit.GetLightingUnitTypeValues("Type C")[0])) * bulbs.Where(b => b.unitType == "Type C").Count())).ToString("F1");
           
        desc += "\nAir tap on light bulbs for configuration!";

        SetPanelText("Eco Feedback", desc);

        textPanel.SetActive(true);
        textPanel.GetComponentInParent<SolverHandler>().UpdateSolvers = false;
    }

    /// <summary>
    /// Updates brightness preference to high.
    /// </summary>
    public void SetPreferenceHigh()
    {
        lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().Preference = BrightnessPreference.High;
    }

    /// <summary>
    /// Updates brightness preference to medium.
    /// </summary>
    public void SetPreferenceMedium()
    {
        lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().Preference = BrightnessPreference.Medium;
    }

    /// <summary>
    /// Updates brightness preference to low.
    /// </summary>
    public void SetPreferenceLow()
    {
        lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().Preference = BrightnessPreference.Low;
    }

    /// <summary>
    /// Displays the scanning instructions panel.
    /// </summary>
    public void ShowScanningInstructions()
    {
        textPanel.GetComponentInParent<SolverHandler>().UpdateSolvers = true;

        // Hide radio and next buttons
        buttons.transform.GetChild(0).gameObject.SetActive(false);
        buttons.transform.GetChild(1).gameObject.SetActive(false);

        SetPanelText("Room Scanning", "Scan the room by slowely looking and walking around." +
            "\n - the spatial net indicates the scanned parts" +
            "\n - make sure to scan floor, ceiling, walls and tables in the room" +
            "\n - a blue label tags the identfied surfaces" +
            "\n - press scan completed button with Air Tap whenever you scanned enough of the room");

        textPanel.SetActive(true);
        buttons.transform.GetChild(2).gameObject.SetActive(true);
    }

    /// <summary>
    /// Starts the scanning process. 
    /// </summary>
    public void StartScanning()
    {
        textPanel.SetActive(false);
        buttons.transform.GetChild(2).gameObject.SetActive(false);
        spatialProcessing.SetActive(true);
        spatialProcessing.GetComponent<SpatialProcessingHandler>().TimeLimited = false;

        // Display the "scan completed" button
        buttons.transform.GetChild(3).gameObject.SetActive(true);
    }

    /// <summary>
    /// Displays the power outlet placement instructions panel.
    /// </summary>
    public void ShowPowerOutletPlacementInstructions()
    {
        textPanel.SetActive(false);
        buttons.transform.GetChild(4).gameObject.SetActive(false);

        SetPanelText("Power Outlet Placement", "Walk around and place the power outlets with an Air Tap on detected surfaces." +
            "\n - The dected surfaces are tagged with a label in blue" +
            "\n - 3 power outlets can be placed at any of the dected surfaces" +
            "\n - the lighting setup recommendation depends on the placed power outlets");
        textPanel.SetActive(true);
        buttons.transform.GetChild(5).gameObject.SetActive(true);
    }

    /// <summary>
    /// Starts the power outlet placement process.
    /// </summary>
    public void StartPowerOutletPlacement()
    {
        textPanel.SetActive(false);
        buttons.transform.GetChild(5).gameObject.SetActive(false);
        GameObject.Find("Directional Light").SetActive(false);
        GameObject.Find("RoomTagCollection").SetActive(false);

        powerOutletHandler.SetActive(true);
    }

    /// <summary>
    /// Displays updated lighting unit information.
    /// </summary>
    public void LightingUnitUpdated()
    {
        List<LightingUnit> bulbs = lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().LightBulbs;
        int hits = 0;

        for (int i = 0; i < bulbs.Count; i++)
        {
            hits += bulbs[i].hits;
        }

        float covering = ((float)hits / (float)spatialProcessing.GetComponent<SpatialObserverHandler>().CombinedMesh.triangles.Length) * 100;

        string desc = (int)covering + " % of the room is strongly illuminated (red colored)";

        if (bulbs.Where(b => b.unitType == "Type A").Count() > 0)
        {
            desc += "\n - " + bulbs.Where(b => b.unitType == "Type A").Count() + " Type A, energy consumption of " + (LightingUnit.GetLightingUnitTypeValues("Type A")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type A")[0] * LightingUnit.GetLightingUnitTypeValues("Type A")[0])).ToString("F1") + " with an estimated durability of " + LightingUnit.GetLightingUnitTypeValues("Type A")[1] + " years";
        }

        if (bulbs.Where(b => b.unitType == "Type B").Count() > 0)
        {
            desc += "\n - " + bulbs.Where(b => b.unitType == "Type B").Count() + " Type B, energy consumption of " + (LightingUnit.GetLightingUnitTypeValues("Type B")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type B")[0] * LightingUnit.GetLightingUnitTypeValues("Type B")[0])).ToString("F1") + " with an estimated durability of " + LightingUnit.GetLightingUnitTypeValues("Type B")[1] + " years";
        }

        if (bulbs.Where(b => b.unitType == "Type C").Count() > 0)
        {
            desc += "\n - " + bulbs.Where(b => b.unitType == "Type C").Count() + " Type C, energy consumption of " + (LightingUnit.GetLightingUnitTypeValues("Type C")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type C")[0] * LightingUnit.GetLightingUnitTypeValues("Type C")[0])).ToString("F1") + " with an estimated durability of " + LightingUnit.GetLightingUnitTypeValues("Type C")[1] + " years";
        }

        desc += "\nTotal energy consumption per illuminated unit: " + (((LightingUnit.GetLightingUnitTypeValues("Type A")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type A")[0] * LightingUnit.GetLightingUnitTypeValues("Type A")[0])) * bulbs.Where(b => b.unitType == "Type A").Count()) + ((LightingUnit.GetLightingUnitTypeValues("Type B")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type B")[0] * LightingUnit.GetLightingUnitTypeValues("Type B")[0])) * bulbs.Where(b => b.unitType == "Type B").Count()) + ((LightingUnit.GetLightingUnitTypeValues("Type C")[2] / (Mathf.PI * LightingUnit.GetLightingUnitTypeValues("Type C")[0] * LightingUnit.GetLightingUnitTypeValues("Type C")[0])) * bulbs.Where(b => b.unitType == "Type C").Count())).ToString("F1");
        desc += "\nAir tap on light bulbs for configuration!";

        SetPanelText("Eco Feedback", desc);
    }
}
