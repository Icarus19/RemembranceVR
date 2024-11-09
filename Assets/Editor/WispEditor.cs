using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WispManager))]
public class WispEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        WispManager manager = (WispManager)target;

        if (manager.drawGizmos)
        {
            if(GUILayout.Button("Disable debug"))
            {
                manager.DrawGizmos();
            }
        } else {
            if(GUILayout.Button("Enable debug"))
            {
                manager.DrawGizmos();
            }
        }

        if (GUILayout.Button("UpdateBoids"))
        {
            manager.UpdateBoidSettings();
        }
    }    
}
