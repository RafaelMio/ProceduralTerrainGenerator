using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapgen = target as MapGenerator;

        if (DrawDefaultInspector())
        {
            if(mapgen.autoUpdate)
            {
                mapgen.DrawnMapInEditor();
            }
        }

        if(GUILayout.Button("Generate"))
        {
            mapgen.DrawnMapInEditor();
        }
    }
}
