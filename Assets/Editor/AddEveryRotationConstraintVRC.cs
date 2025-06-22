using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Animations;
using VRC.SDK3.Dynamics.Constraint;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.Dynamics;


public class AddEveryRotationConstraintVRC : EditorWindow
{
    [SerializeField]
    GameObject fromJoint = null;

    [SerializeField]
    GameObject toJoint = null;

    private VRCConstraintSource fromConstraintSource;
   
    private bool includeInActive = false;

    private string parentName = "None";
    private int childrenSize = 0; 

    private enum CONSTRAINT_TYPE
    {
        VRCRotationConstraint,
        VRCParentConstraint,
        VRCPositionConstraint,
    }

    private CONSTRAINT_TYPE constraintType = CONSTRAINT_TYPE.VRCRotationConstraint;


    [MenuItem("Window/Extension Tools/VRC Add Every Rotation Constraint")]
    static void Open()
    {
        GetWindow<AddEveryRotationConstraintVRC>();
    }

    private void OnGUI()
    {
        GUILayout.Label("", EditorStyles.boldLabel);

        Color defaultColor = GUI.backgroundColor;
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.backgroundColor = Color.gray;
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Original Avatar", EditorStyles.whiteLabel);
            }
            GUI.backgroundColor = defaultColor;

            EditorGUI.indentLevel++;
            fromJoint = EditorGUILayout.ObjectField("GameObject (Joint)", fromJoint, typeof(GameObject), true) as GameObject;
            ShowJointProperty(fromJoint);

            EditorGUI.indentLevel--;

        }


        GUILayout.Label("", EditorStyles.boldLabel);


        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.backgroundColor = Color.gray;
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Target Avatar", EditorStyles.whiteLabel);
            }
            GUI.backgroundColor = defaultColor;

            EditorGUI.indentLevel++;
            toJoint = EditorGUILayout.ObjectField("GameObject (Joint)", toJoint, typeof(GameObject), true) as GameObject;
            ShowJointProperty(toJoint);
            EditorGUI.indentLevel--;
        }
        GUI.backgroundColor = defaultColor;


        GUILayout.Label("", EditorStyles.boldLabel);

        constraintType = (CONSTRAINT_TYPE)EditorGUILayout.EnumPopup("Constraint Type", constraintType);


        using (new GUILayout.HorizontalScope(GUI.skin.box))
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Copy"))
            {
                Copy(fromJoint, toJoint);   
            }
            GUI.backgroundColor = defaultColor;


            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Reset"))  
            {
                Reset(toJoint);
            }
            GUI.backgroundColor = defaultColor;
        }
    }

    void Show(GameObject gameObject)
    {
        Debug.Log(gameObject.name);
    }

    void ShowJointProperty(GameObject obj)
    {
        parentName = GetParentName(obj);
        childrenSize = GetChildrenSize(obj);
        EditorGUILayout.LabelField("Root Name", parentName);
        EditorGUILayout.LabelField("Number of Joints", childrenSize.ToString());

    }

    string GetParentName(GameObject obj)
    {
        if (obj == null)
        {
            return "None";
        }
        
        string nameRoot = obj.transform.root.gameObject.name.ToString();
        return nameRoot;
        
    }


    int GetChildrenSize(GameObject obj)
    {
        if (obj == null)
        {
            return 0;
        }

        int childrenSize = obj.GetComponentsInChildren<Transform>(includeInActive).Length;
        return childrenSize;
    }

    void Copy(GameObject fromJoint, GameObject toJoint)
    {
        if (fromJoint == null)
        {
            Debug.LogError("GameObject(from) not found");
        }

        if (toJoint == null)
        {
            Debug.LogError("GameObject(to) not found");
        }

        Transform[] fromChildren = fromJoint.GetComponentsInChildren<Transform>(includeInActive);
        Transform[] toChildren = toJoint.GetComponentsInChildren<Transform>(includeInActive);


        if (fromChildren.Length != toChildren.Length)
        {
            Debug.LogError("Mismatch of the number of joints, (from): " + fromChildren.Length + " != (to): " + toChildren.Length);
            return;
        }

        ResetConstraintSource(fromConstraintSource);
        fromConstraintSource.Weight = 1.0f;


        foreach (var (joint, index) in toChildren.Select((x, i) => (x, i)))
        {

            switch(constraintType)
            {
                case CONSTRAINT_TYPE.VRCRotationConstraint:
                    VRCRotationConstraint rotationConstraint = joint.gameObject.GetComponent<VRCRotationConstraint>();
                    if (rotationConstraint == null) rotationConstraint = joint.gameObject.AddComponent<VRCRotationConstraint>();
                    if (rotationConstraint.Sources.Count > 0) continue;
                    fromConstraintSource.SourceTransform = fromChildren[index];
                    rotationConstraint.Sources.Add(fromConstraintSource);
                    rotationConstraint.IsActive = true;
                    break;
                case CONSTRAINT_TYPE.VRCParentConstraint:
                    VRCParentConstraint parentConstraint = joint.gameObject.GetComponent<VRCParentConstraint>();
                    if (parentConstraint == null) parentConstraint = joint.gameObject.AddComponent<VRCParentConstraint>();
                    if (parentConstraint.Sources.Count > 0) continue;
                    fromConstraintSource.SourceTransform = fromChildren[index];
                    parentConstraint.Sources.Add(fromConstraintSource);
                    parentConstraint.IsActive = true;
                    break;
                case CONSTRAINT_TYPE.VRCPositionConstraint:
                    VRCPositionConstraint positionConstraint = joint.gameObject.GetComponent<VRCPositionConstraint>();
                    if (positionConstraint == null) positionConstraint = joint.gameObject.AddComponent<VRCPositionConstraint>();
                    if (positionConstraint.Sources.Count > 0) continue;
                    fromConstraintSource.SourceTransform = fromChildren[index];
                    positionConstraint.Sources.Add(fromConstraintSource);
                    positionConstraint.IsActive = true;
                    break;


                default:
                    break;

            }
        }

        Debug.Log($"Add {constraintType} to Every Joints");
        ResetConstraintSource(fromConstraintSource);
        return;
    }



    void ResetConstraintSource(VRCConstraintSource constraintSource)
    {
        //constraintSource.sourceTransform = null;
        constraintSource.SourceTransform = null;
    }


    void Reset(GameObject toJoints)
    {
        if (toJoints == null)
        {
            Debug.LogError("GameObject(from) not found");
            return;
        }

        switch(constraintType)
        {
            case CONSTRAINT_TYPE.VRCRotationConstraint:
                VRCRotationConstraint[] rotationConstraints = toJoints.gameObject.GetComponentsInChildren<VRCRotationConstraint>(includeInActive);
                foreach (VRCRotationConstraint rotationConstraint in rotationConstraints) GameObject.DestroyImmediate(rotationConstraint);
                break;

            case CONSTRAINT_TYPE.VRCParentConstraint:
                VRCParentConstraint[] parentConstraints = toJoints.gameObject.GetComponentsInChildren<VRCParentConstraint>(includeInActive);
                foreach (VRCParentConstraint parentConstraint in parentConstraints) GameObject.DestroyImmediate(parentConstraint);
                break;

            case CONSTRAINT_TYPE.VRCPositionConstraint:
                VRCPositionConstraint[] positionConstraints = toJoints.gameObject.GetComponentsInChildren<VRCPositionConstraint>(includeInActive);
                foreach (VRCPositionConstraint positionConstraint in positionConstraints) GameObject.DestroyImmediate(positionConstraint);
                break;

            default:
                break;

        }

        Debug.Log($"Remove {constraintType}");
    }
}
