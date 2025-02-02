using UnityEditor;
using UnityEngine;
using System.Reflection;

public class PopulateTransformArray : EditorWindow
{
    private GameObject parentObject;
    private MonoBehaviour targetScript;
    private string arrayFieldName = "transformArray";  // Change this as needed
    
    [MenuItem("Tools/Populate Transform Array")]
    public static void ShowWindow()
    {
        GetWindow<PopulateTransformArray>("Populate Transform Array");
    }

    private void OnGUI()
    {
        GUILayout.Label("Populate Array of Transforms", EditorStyles.boldLabel);

        // Select the GameObject whose subchildren will populate the array
        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        
        // Select the script containing the array field
        targetScript = (MonoBehaviour)EditorGUILayout.ObjectField("Target Script", targetScript, typeof(MonoBehaviour), true);
        
        arrayFieldName = EditorGUILayout.TextField("Array Field Name", arrayFieldName);

        if (GUILayout.Button("Populate Array"))
        {
            if (parentObject != null && targetScript != null)
            {
                PopulateArray();
            }
            else
            {
                Debug.LogWarning("Please assign both a parent object and a target script.");
            }
        }
    }

    private void PopulateArray()
    {
        var targetType = targetScript.GetType();
        var arrayField = targetType.GetField(arrayFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (arrayField == null || arrayField.FieldType != typeof(Transform[]))
        {
            Debug.LogError($"Field '{arrayFieldName}' not found or is not a Transform[] array.");
            return;
        }

        // Gather all subchildren into an array
        Transform[] transformArray = new Transform[parentObject.transform.childCount];
        int index = 0;
        foreach (Transform child in parentObject.transform)
        {
            transformArray[index] = child;
            index++;
        }

        // Record the change for Unity serialization
        Undo.RecordObject(targetScript, "Populate Transform Array");
        arrayField.SetValue(targetScript, transformArray);

        // Mark the object as dirty to ensure changes are saved
        EditorUtility.SetDirty(targetScript);

        Debug.Log($"Array populated with {transformArray.Length} transforms from {parentObject.name}");
    }
}
