using UnityEngine;

public class Manager : MonoBehaviour
{
    [Tooltip("Spatial processing (and room volume calculation) component.")]
    [SerializeField]
    private GameObject spatialProcessing = null;

    [Tooltip("Power outlet collection component.")]
    [SerializeField]
    private GameObject powerOutletHandler = null;

    [Tooltip("Light visualization component.")]
    [SerializeField]
    private GameObject lightVisualizationHandler = null;

    [Tooltip("Lighting unit collection component.")]
    [SerializeField]
    private GameObject lightingUnitHandler = null;

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        if (spatialProcessing != null)
        {
            spatialProcessing.SetActive(true);

            // Register for the spatial processing completed event
            spatialProcessing.GetComponent<SpatialProcessingHandler>().SpatialProcessingCompletedEvent += SpatialProcessingCompletedEventHandler;
        }

        if (powerOutletHandler != null)
        {
            // Register for the power outlet completed event
            powerOutletHandler.GetComponent<PowerOutletPlacementHandler>().PowerOutletPlacementCompleted += PowerOutletPlacementCompletedEventHandler;
        }

        if (lightingUnitHandler != null)
        {
            // Register for the lighting recommendation completed event
            lightingUnitHandler.GetComponent<LightingUnitPlacementHandler>().LightingRecommendationCompleted += LightingRecommendationCompletedEventHandler;
        }
    }

    /// <summary>
    /// Handles the spatial processing (an room volume calculation) completion event.
    /// If the power outlet collection component is set then start the power outlet placement otherwise the light visualization is started.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SpatialProcessingCompletedEventHandler(object source, System.EventArgs args, string message)
    {
        if (powerOutletHandler != null)
        {
            powerOutletHandler.SetActive(true);
        } else
        {
            lightVisualizationHandler.SetActive(true);
        }
    }

    /// <summary>
    /// Handles the power outlet placement completion event.
    /// If the lighting unit collection component is set then start the lighting unit recommendation.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void PowerOutletPlacementCompletedEventHandler(object source, System.EventArgs args)
    {
        if (lightingUnitHandler != null)
        {
            lightingUnitHandler.SetActive(true);
        }
    }

    /// <summary>
    /// Handles the lighting unit recommendation completion event.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void LightingRecommendationCompletedEventHandler(object source, System.EventArgs args)
    {
        Debug.Log("Recommendation completed");
    }

    /// <summary>
    /// Called when the game object is unloaded. 
    /// </summary>
    private void OnDestroy()
    {
        if (spatialProcessing.GetComponent<SpatialProcessingHandler>() != null)
        {
            spatialProcessing.GetComponent<SpatialProcessingHandler>().SpatialProcessingCompletedEvent -= SpatialProcessingCompletedEventHandler;
        }

        if (powerOutletHandler.GetComponent<PowerOutletPlacementHandler>() != null)
        {
            powerOutletHandler.GetComponent<PowerOutletPlacementHandler>().PowerOutletPlacementCompleted -= PowerOutletPlacementCompletedEventHandler;
        }
    }
}
