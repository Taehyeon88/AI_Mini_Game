using UnityEngine;

public class Test : MonoBehaviour
{
    private GameObject gm;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gm.transform.position = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        gm.transform.position = new Vector3(0, 1, 0.2f);
    }
}
