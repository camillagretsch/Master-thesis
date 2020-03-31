using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit;

/// <summary>
/// Handles the spatial mesh observer. 
/// </summary>
public class SpatialObserverHandler : MonoBehaviour
{
    private SpatialProcessingHandler processor;

    private string observerName = "";
    private const string WINDWS_MIXED_REALITY = "Windows Mixed Reality Spatial Mesh Observer";
    private const string UITY_EDITOR = "Spatial Object Mesh Observer";

    // Returns if the spatial observer is currently running
    public bool IsRunning { get; private set; } = false;

    // Time when the spatial observer started (StartObserver() was called)
    public float StartTime { get; private set; }

    // Returns the combined mesh of the spatial observer
    public Mesh CombinedMesh { get; private set; }

    /// <summary>
    /// Start is called before the first fram update. 
    /// </summary>
    private void Start()
    {
        processor = GetComponent<SpatialProcessingHandler>();
        
        StartObserver();
    }

    /// <summary>
    /// Starts the Windows Mixed Reality Observer or the Editor Observer.
    /// </summary>
    public void StartObserver()
    {
        if (CoreServices.SpatialAwarenessSystem != null)
        {
            // Specify the observer name depnding on the environment
#if UNITY_EDITOR
            observerName = UITY_EDITOR;
#else
            observerName = WINDWS_MIXED_REALITY;
#endif

            // Only starts the spatial observer if it is currently stopped
            if (!IsRunning)
            {
                Debug.Log("Scanning started");
                IsRunning = true;
                StartTime = Time.time;
                CoreServices.SpatialAwarenessSystem.ResumeObserver<IMixedRealitySpatialAwarenessMeshObserver>(observerName);
                SetMeshDisplayOption(SpatialAwarenessMeshDisplayOptions.Visible);

                // Set the scanning timer to 1 second when using the unity editor
#if UNITY_EDITOR
                StartCoroutine(processor.StartScanningTimer(1));
#else
                StartCoroutine(processor.StartScanningTimer());
#endif
            }
        }
    }

    /// <summary>
    /// Stops the currently running spatial observer. 
    /// </summary>
    public void StopObserver()
    {
        if (IsRunning)
        {
            IsRunning = false;
            CoreServices.SpatialAwarenessSystem.SuspendObserver<IMixedRealitySpatialAwarenessMeshObserver>(observerName);
            CreateCombinedMesh();
        }
    }

    /// <summary>
    /// Stops the currently running spatial observer and cleans up all spatial meshes.
    /// </summary>
    public void CleanUpObserver()
    {
        if (IsRunning)
        {
            IsRunning = false;
            CoreServices.SpatialAwarenessSystem.SuspendObserver<IMixedRealitySpatialAwarenessMeshObserver>(observerName);
        }

        CoreServices.SpatialAwarenessSystem.ClearObservations<IMixedRealitySpatialAwarenessMeshObserver>(observerName);
    }

    /// <summary>
    /// Set the display option of the spatial mesh.
    /// </summary>
    /// <param name="meshDisplayOption">visible, occlusion, none</param>
    public void SetMeshDisplayOption(SpatialAwarenessMeshDisplayOptions meshDisplayOption)
    {
        // Cast the spatial awareness system to IMixedRealityDataProviderAccess to get an observer
        var access = CoreServices.SpatialAwarenessSystem as IMixedRealityDataProviderAccess;

        // Get the mesh observer specified by its name
        var observer = access.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>(observerName);

        observer.DisplayOption = meshDisplayOption;
    }

    /// <summary>
    /// Returns all spatial mesh filter objects associated with the current spatial observer.
    /// </summary>
    /// <returns>List of mesh filters</returns>
    public List<MeshFilter> GetMeshFilters()
    {
        List<MeshFilter> meshFilters = new List<MeshFilter>();

        // Cast the spatial awareness system to IMixedRealityDataProviderAccess to get an observer
        var access = CoreServices.SpatialAwarenessSystem as IMixedRealityDataProviderAccess;

        // Get the mesh observer specified by its name
        var observer = access.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>(observerName);

        // Loop through all known meshes
        foreach (SpatialAwarenessMeshObject meshObject in observer.Meshes.Values)
        {
            // Gets all mesh filters that have a valid mesh
            if (meshObject.Filter != null && meshObject.Filter.sharedMesh != null && meshObject.Filter.sharedMesh.vertexCount > 2)
            {
                meshFilters.Add(meshObject.Filter);
            }
        }

        return meshFilters;
    }

    /// <summary>
    /// Create a combined mesh from the submeshes of the Windows Mixed Reality Observer.
    /// </summary>
    private void CreateCombinedMesh()
    {
        List<MeshFilter> meshFilters = GetMeshFilters();

        CombinedMesh = new Mesh();
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Count];

        // Iterate over the submeshes and add them to the combine instance
        if (meshFilters.Count > 0)
        {
            for (int i = 0; i < meshFilters.Count; i++)
            {
                if (meshFilters[i] != null && meshFilters[i].sharedMesh != null && meshFilters[i].sharedMesh.vertexCount > 2)
                {
                    combineInstances[i].mesh = meshFilters[i].sharedMesh;
                    combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
                }
            }

            CombinedMesh.CombineMeshes(combineInstances);
            CombinedMesh.RecalculateNormals();
        }
    }
}
