using UnityEditor;
using UnityEngine;

public class AutoIncrementer : EditorWindow
{
    private GameObject parentObject;
    private string fieldName = "desiredField";  // Replace with the field name if you want
    
    [MenuItem("Tools/Auto Increment Field")]
    public static void ShowWindow()
    {
        GetWindow<AutoIncrementer>("Auto Incrementer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-Increment Field", EditorStyles.boldLabel);

        // Select the parent GameObject (if the objects are grouped)
        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        
        fieldName = EditorGUILayout.TextField("Field Name", fieldName);
        
        if (GUILayout.Button("Auto-Increment"))
        {
            if (parentObject != null)
            {
                AutoIncrement();
            }
            else
            {
                Debug.LogWarning("Please select a parent GameObject containing the objects.");
            }
        }
    }

    private void AutoIncrement()
    {
        int counter = 0;
        
        foreach (Transform child in parentObject.transform)
        {
            var component = child.GetComponent<MonoBehaviour>(); // Assuming a custom MonoBehaviour script

            if (component != null)
            {
                var field = component.GetType().GetField(fieldName);

                if (field != null && field.FieldType == typeof(int))
                {
                    field.SetValue(component, counter);
                    counter++;
                }
                else
                {
                    Debug.LogWarning($"Field '{fieldName}' not found or not of type 'int' on {child.name}");
                }
            }
        }

        Debug.Log("Auto-incrementing finished!");
    }
}
