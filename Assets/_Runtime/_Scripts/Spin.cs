using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] 
    private Vector3 rotationPerSecond;

    void Update() => transform.Rotate(rotationPerSecond * Time.deltaTime);
}
