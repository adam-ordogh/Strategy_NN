using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public MapData mapData;

    public void Initialize(MapData mapData)
    {
        this.mapData = mapData;
    }

    public void CreateBuilding(Building building)
    {
        // Jelenleg nem nezi meg hogy a mezo foglalt-e
        mapData.buildings[building.position] = building;
    }
}
