using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face2User : MonoBehaviour
{
    [SerializeField]
    private Transform userHead;

    public bool yRotation = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(userHead.transform);
        transform.rotation *= Quaternion.Euler(0, 180, 0);

        if (!yRotation)
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
    }
}
