using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hanldes the process of mapping spatial data to real-world objects. 
/// </summary>
public class SpatialProcessingHandler : MonoBehaviour
{
    private SpatialMeshConversionHandler converter;
    private SpatialObserverHandler observer;
    private RoomVolumeCalculationHandler calculator;

    public bool TimeLimited = true;
    private bool buttonPressed = false;

    // Delegate is called when the spatial porcessing completed event is triggered
    public delegate void EventHandler(object source, EventArgs args, string message);
    // Event handler which is triggered when the spatial processing is completed
    public event EventHandler SpatialProcessingCompletedEvent;

    [Tooltip("Time in seconds that the Mesh Observer will maximal run before it stops.")]
    [SerializeField]
    private float maxScanningTime = 120.0f;

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        converter = GetComponent<SpatialMeshConversionHandler>();
        observer = GetComponent<SpatialObserverHandler>();
        calculator = gameObject.GetComponent<RoomVolumeCalculationHandler>();

        // Register for the create surface planes complete event
        converter.CreateSurfacePlanesCompletedEvent += CreateSurfacePlanesCompletedEventHandler;

    }

    /// <summary>
    /// Handles the time the mesh observer can run before it stops and the processing starts. 
    /// </summary>
    /// <param name="timeInterval">Specify the amount of time the mesh obeserver runs (default = 30 seconds).</param>
    /// <returns></returns>
    public IEnumerator StartScanningTimer(float timeInterval = 30)
    {
        while (timeInterval > 0)
        {
            yield return new WaitForSeconds(1.0f);
            timeInterval--;
        }

        StartProcessing();
    }

    public void StopScanningByButton()
    {
        buttonPressed = true;
        StartProcessing();
    }

    /// <summary>
    /// Starts processing the mesh data and creating the surface planes. 
    /// </summary>
    private void StartProcessing()
    {
        if (converter != null && converter.enabled)
        {
            converter.CreateSurfacePlanes();
        }
    }

    /// <summary>
    /// Handles the spatial meshes to surface planes converter completion event.
    /// Checks if enough surface planes are found to calculate the room volume or restarts the scanning timer if more spatial data is needed. 
    /// After a maximal scanning time (default = 2 min) the mesh observer will be stopped and the room volume calculated with the available surface planes. 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void CreateSurfacePlanesCompletedEventHandler(object source, System.EventArgs args)
    {
        SurfacePlane floor = converter.GetFloorOrCeiling(PlaneTypes.Floor);
        SurfacePlane ceiling = converter.GetFloorOrCeiling(PlaneTypes.Ceiling);
        List<SurfacePlane> walls = converter.GetDetectedSurfacePlanesByType(PlaneTypes.Wall);

        if (TimeLimited)
        {
            // If a floor, ceiling and several walls are available or the max scanning time is reached it stops the mesh observer and starts calculating the room volume
            if ((floor.Area >= 10 && ceiling.Area >= 10 && walls.Count > 1) || ((Time.time - observer.StartTime) >= maxScanningTime))
            {
                observer.StopObserver();
                Debug.Log("Scanning completed");

                if (calculator != null)
                {
                    calculator.CalculateRoomVolume(floor, ceiling, walls);
                }

                // Spatial processing is done and triggers the completion event
                EventHandler handler = SpatialProcessingCompletedEvent;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty, "time");
                }

            } // Restarts the scanning timer to collect more spatial data
            else
            {
                Debug.Log("Not enough spatial data availabe and restart scanning process");
                StartCoroutine(StartScanningTimer());
            }
        } else if (!TimeLimited && buttonPressed)
        {
            buttonPressed = false;

            if ((floor.Area != 0 && ceiling.Area != 0 && walls.Count > 0) || (floor.Area != 0 && ceiling.Area != 0) || (floor.Area != 0 && walls.Count > 0) || (ceiling.Area != 0 && walls.Count > 0) || (walls.Count > 1))
            {
                observer.StopObserver();
                Debug.Log("Scanning completed");

                if (calculator != null)
                {
                    calculator.CalculateRoomVolume(floor, ceiling, walls);
                }

                // Spatial processing is done and triggers the completion event
                EventHandler handler = SpatialProcessingCompletedEvent;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty, "completed");
                }
            } else
            {
                buttonPressed = false;
                // Spatial processing is done and triggers the completion event
                EventHandler handler = SpatialProcessingCompletedEvent;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty, "Keep scanning!");
                }
            }
        }
    }

    /// <summary>
    /// Called when the game object is unloaded. 
    /// </summary>
    private void OnDestroy()
    {
        if (converter != null)
        {
            converter.CreateSurfacePlanesCompletedEvent -= CreateSurfacePlanesCompletedEventHandler;
        }
    }
}
