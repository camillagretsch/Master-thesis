using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the calculation of the room volume. 
/// </summary>
public class RoomVolumeCalculationHandler : MonoBehaviour
{
    // Save the floor width/length to index 0, ceiling width/length to index 1 and wall width/length to index 2
    private float[] widths = new float[3] { 0, 0, 0 };
    private float[] lengths = new float[3] { 0, 0, 0 };

    private SurfacePlane floor = new SurfacePlane();
    private SurfacePlane ceiling = new SurfacePlane();
    private List<SurfacePlane> walls = new List<SurfacePlane>();

    // Returns the room volume data
    public float RoomVolume { get; private set; } = 0;
    public float RoomHeight { get; private set; } = 0;
    public float RoomWidth { get; private set; } = 0;
    public float RoomLength { get; private set; } = 0;

    [Tooltip("Cube to create wall game objects.")]
    [SerializeField]
    private GameObject surfacePlanePrefab;

    /// <summary> 
    /// Returns the volume of the room by calculating height * width * length.
    /// </summary>
    /// <param name="floor">Surface plane which represents the floor.</param>
    /// <param name="ceiling">Surface plane which represents the ceiling.</param>
    /// <param name="walls">Surface planes which represents the walls.</param>
    public void CalculateRoomVolume(SurfacePlane floor, SurfacePlane ceiling, List<SurfacePlane> walls)
    {
        this.floor = floor;
        this.ceiling = ceiling;
        this.walls = walls;

        // Case 1: floor, ceiling and wall(s) exist
        if (floor.Area > 0 && ceiling.Area > 0 && walls.Count >= 1)
        {
            CalculateByFloorCeilingWalls();
        } // Case 2: floor and ceiling exist
        else if (floor.Area > 0 && ceiling.Area > 0 && walls.Count == 0)
        {
            CalculateByFloorCeiling();
        } // Case 3: floor and wall(s) exist
        else if (floor.Area > 0 && ceiling.Area == 0 && walls.Count > 0)
        {
            CalculateByFloorWalls();
        } // Case 4: ceiling and wall(s) exist
        else if (floor.Area == 0 && ceiling.Area > 0 && walls.Count > 0)
        {
            CalculateByCeilingWalls();
        } // Case 5: walls exist
        else if (floor.Area == 0 && ceiling.Area == 0 && walls.Count > 1)
        {
            CalculateByWalls();
        }
        else // Cannot calculate volume
        {
            Debug.Log("Cannot calculate volume");
            return;
        }

        // Calculate the average of the width and length from the floor, ceiling and walls
        RoomWidth = widths.Where(w => w > 0).Average();
        RoomLength = lengths.Where(l => l > 0).Average();

        RoomVolume = RoomHeight * RoomWidth * RoomLength;
        Debug.Log("Volume: " + RoomVolume);
    }

    /// <summary>
    /// Case 1: Calculates the room volume with values from floor, ceiling and wall(s).
    /// </summary>
    private void CalculateByFloorCeilingWalls()
    {
        // Take the largest x value of the walls as the height 
        float wallHeight = walls.Max<SurfacePlane>(max => (max.Bounds.Extents.x * 2));

        // Take the distance of the floor to ceiling as the height
        float floorCeilingDistance = floor.Plane.GetDistanceToPoint(new Vector3(floor.Bounds.Center.x, ceiling.Bounds.Center.y, floor.Bounds.Center.z));

        // Compare the two hights calculate by walls and floor to ceiling distance and take the largest value as the room height
        if (wallHeight > floorCeilingDistance)
        {
            RoomHeight = wallHeight;
        }
        else
        {
            RoomHeight = floorCeilingDistance;
        }

        // Set the width and length of floor, ceiling and wall(s)
        SetFloorValues();
        SetCeilingValues();
        SetWallValues();
        RemoveOutliners();
    }

    /// <summary>
    /// Case 2: Calculates the room volume with values from floor and ceiling.
    /// </summary>
    private void CalculateByFloorCeiling()
    {
        // Take the distance of the floor to ceiling as the height
        RoomHeight = floor.Plane.GetDistanceToPoint(new Vector3(floor.Bounds.Center.x, ceiling.Bounds.Center.y, floor.Bounds.Center.z));

        // Set the width and length of floor and ceiling
        SetFloorValues();
        SetCeilingValues();
        RemoveOutliners();
    }

    /// <summary>
    /// Case 3: Calculates the room volume with values from floor and wall(s).
    /// </summary>
    private void CalculateByFloorWalls()
    {
        // Take the largest x value of the walls as the height 
        RoomHeight = walls.Max<SurfacePlane>(max => (max.Bounds.Extents.x * 2));

        // Set the width and length of floor and wall(s)
        SetFloorValues();
        SetWallValues();
        RemoveOutliners();
    }

    /// <summary>
    /// Case 4: Calculates the room volume with values from ceiling and wall(s).
    /// </summary>
    private void CalculateByCeilingWalls()
    {
        // Take the largest x value of the walls as the height 
        RoomHeight = walls.Max<SurfacePlane>(max => (max.Bounds.Extents.x * 2));

        // Set the width and length of ceiling and wall(s)
        SetCeilingValues();
        SetWallValues();
        RemoveOutliners();
    }

    /// <summary>
    /// Case 5: Calculates the room volume with values from walls
    /// </summary>
    private void CalculateByWalls()
    {
        // Take the largest x value of the walls as the height 
        RoomHeight = walls.Max<SurfacePlane>(max => (max.Bounds.Extents.x * 2));

        // Set the width and length of walls
        SetWallValues();
    }

    /// <summary>
    /// Set the larger of the x and y values as the floor length and the other as the floor width. 
    /// </summary>
    private void SetFloorValues()
    {
        if (floor.Bounds.Extents.x > floor.Bounds.Extents.y)
        {
            widths[0] = (floor.Bounds.Extents.y * 2);
            lengths[0] = (floor.Bounds.Extents.x * 2);
        }
        else
        {
            widths[0] = (floor.Bounds.Extents.x * 2);
            lengths[0] = (floor.Bounds.Extents.y * 2);
        }
    }

    /// <summary>
    /// Set the larger of the x and y values as the ceiling length and the other as the ceiling width. 
    /// </summary>
    private void SetCeilingValues()
    {
        if (ceiling.Bounds.Extents.x > ceiling.Bounds.Extents.y)
        {
            widths[1] = (ceiling.Bounds.Extents.y * 2);
            lengths[1] = (ceiling.Bounds.Extents.x * 2);
        }
        else
        {
            widths[1] = (ceiling.Bounds.Extents.x * 2);
            lengths[1] = (ceiling.Bounds.Extents.y * 2);
        }
    }

    /// <summary>
    /// Calculate the width and length by max distance of parallel walls. 
    /// </summary>
    private void SetWallValues()
    {
        // Divide walls in parallel and orthogonal walls 
        List<SurfacePlane> parallelWalls = new List<SurfacePlane>();
        List<SurfacePlane> orthogonalWalls = new List<SurfacePlane>();
        foreach (SurfacePlane wall in walls)
        {
            SurfacePlane w = wall;

            // If the cross product to the frist wall is 0 then walls are parallel otherwise they are orthogonal 
            if (Vector3.Cross(walls[0].Plane.normal, w.Plane.normal).magnitude < 0.5)
            {
                // Make sure that all parallel walls have the same orientation
                w.Bounds.Rotation = walls[0].Bounds.Rotation;
                parallelWalls.Add(w);
            }
            else
            {
                if (orthogonalWalls.Count > 0)
                {
                    // Make sure that all orthogonal walls have the same orientation
                    w.Bounds.Rotation = orthogonalWalls[0].Bounds.Rotation;
                }
                orthogonalWalls.Add(w);
            }
        }

        // Set the distance within parallel and orthogonal walls
        float parallelDistance = GetMaxWallDistance(parallelWalls);
        float orthogonalDistance = GetMaxWallDistance(orthogonalWalls);

        // Set the larger of the distance as the wall length and the other as the wall width
        if (parallelDistance > orthogonalDistance)
        {
            widths[2] = orthogonalDistance;
            lengths[2] = parallelDistance;
        }
        else
        {
            widths[2] = parallelDistance;
            lengths[2] = orthogonalDistance;
        }
    }

    /// <summary>
    /// Returns the max distance in y direction of the walls.
    /// </summary>
    /// <param name="planes">List of surface planes which only contains parallel walls.</param>
    /// <returns></returns>
    private float GetMaxWallDistance(List<SurfacePlane> planes)
    {
        // If no walls exist the distance is 0
        if (planes.Count == 0)
        {
            return 0;
        } // If only one wall exist the distance is the y value of the walls 
        else if (planes.Count == 1)
        {
            return (planes[0].Bounds.Extents.y * 2);
        } // If several walls exist the distance needs to be calculated 
        else
        {
            // Create a parent object and hide it and its children from the scene
            GameObject wallContainer = new GameObject("WallContainer");
            wallContainer.transform.parent = gameObject.transform;
            wallContainer.layer = 30;
            wallContainer.SetActive(false);
            wallContainer.transform.rotation = planes[0].Bounds.Rotation;

            // Create a plane object foreach parallel wall
            foreach (SurfacePlane p in planes)
            {
                GameObject figure = Instantiate(surfacePlanePrefab, wallContainer.transform);
                figure.GetComponent<SurfacePlaneExtension>().Init(p, false);
            }

            // Rotate the container so that we only have to compare the y value to find the max distance
            wallContainer.transform.eulerAngles = new Vector3(0, 90, planes[0].Bounds.Rotation.eulerAngles.z);

            float yMinValue = wallContainer.GetComponentsInChildren<Transform>().Min<Transform>(min => (min.localPosition.y - (min.localScale.y / 2)));
            float yMaxValue = wallContainer.GetComponentsInChildren<Transform>().Max<Transform>(max => (max.localPosition.y + (max.localScale.y / 2)));

            Destroy(wallContainer);
            return (yMinValue - yMaxValue);
        }
    }

    /// <summary>
    /// Removes length and width outliners by check if a value is small than half the size of the others. 
    /// </summary>
    private void RemoveOutliners()
    {
        // Compare width values
        if (((widths[0] * 2) < widths[1]) || ((widths[0] * 2) < widths[2]))
        {
            widths[0] = 0;
        }
        if (((widths[1] * 2) < widths[0]) || ((widths[1] * 2) < widths[2]))
        {
            widths[1] = 0;
        }
        if (((widths[2] * 2) < widths[0]) || ((widths[2] * 2) < widths[1]))
        {
            widths[2] = 0;
        }

        // Compare length values
        if (((lengths[0] * 2) < lengths[1]) || ((lengths[0] * 2) < lengths[2]))
        {
            lengths[0] = 0;
        }
        if (((lengths[1] * 2) < lengths[0]) || ((lengths[1] * 2) < lengths[2]))
        {
            lengths[1] = 0;
        }
        if (((lengths[2] * 2) < lengths[0]) || ((lengths[2] * 2) < lengths[1]))
        {
            lengths[2] = 0;
        }
    }
}
