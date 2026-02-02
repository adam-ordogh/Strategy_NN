using UnityEngine;

static class RenderSorter
{
    // Szorzó a Y pozíció alapján történő rendezéshez ami 9 helyiértékű különbséget biztosít
    const int SortMultiplier = 10;

    public static void Sort(SpriteRenderer sr, float yPos, int internalOffset = 0)
    {
        // Y pozíció alapján rendezünk, ahol a nagyobb Y érték (feljebb) kisebb sorrendet kap 
        sr.sortingOrder = (-(int)(yPos * SortMultiplier)) + internalOffset;
    }

    public static void SortBuilding(GameObject buildingGO, float yPos)
    {
        SpriteRenderer baseSr = buildingGO.transform.Find("MainSprite").GetComponent<SpriteRenderer>();
        SpriteRenderer trimSr = buildingGO.transform.Find("ColorTrim").GetComponent<SpriteRenderer>();

        Sort(baseSr, yPos, 0);
        Sort(trimSr, yPos, 1);
    }
}