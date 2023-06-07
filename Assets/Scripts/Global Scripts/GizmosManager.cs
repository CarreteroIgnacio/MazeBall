using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmosManager : MonoBehaviour
{
    public static GizmosManager Instance;
    public delegate void GizmosToDraw();

    public GizmosToDraw DrawGizmos;
    public GizmosToDraw DrawGizmosSelected;



    public void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    
    

    private void OnDrawGizmos() => DrawGizmos?.Invoke();
    private void OnDrawGizmosSelected() => DrawGizmosSelected?.Invoke();
}
