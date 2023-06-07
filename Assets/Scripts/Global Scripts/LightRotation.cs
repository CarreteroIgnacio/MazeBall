using UnityEngine;

public class LightRotation : MonoBehaviour
{
    public float speed;
    
    private void Update() => transform.eulerAngles += Vector3.up * (Time.deltaTime * speed);
}
