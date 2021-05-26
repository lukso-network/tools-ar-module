using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotation : MonoBehaviour
{
    // Start is called before the first frame update
    public float smooth = 1f;
    private Quaternion targetRotation;
    private float x;
    private float y;
    private float z;
    // Start is called before the first frame update
    void Start() {
        targetRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update2() {
        
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            targetRotation *= Quaternion.AngleAxis(90, Vector3.right);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            targetRotation *= Quaternion.AngleAxis(90, Vector3.left);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            targetRotation *= Quaternion.AngleAxis(90, Vector3.up);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            targetRotation *= Quaternion.AngleAxis(90, Vector3.down);
        }
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 10 * smooth * Time.deltaTime);

    }

    void Update0() {

        if (Input.GetKey(KeyCode.UpArrow)) {
            x += 1;
        }
        if (Input.GetKey(KeyCode.DownArrow)) {
            x -= 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow)) {
            y += 1;
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            y -= 1;
        }

        if (Input.GetKey(KeyCode.W)) {
            z += 1;
        }

        if (Input.GetKey(KeyCode.S)) {
            z -= 1;
        }

        //Quaternion.identity * Quaternion.Anglexis(x, Vector3)
        //transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 10 * smooth * Time.deltaTime);
        transform.localEulerAngles = new Vector3(x, y, z);

    }

    void Update() {

        if (Input.GetKey(KeyCode.UpArrow)) {
            x += 1;
        }
        if (Input.GetKey(KeyCode.DownArrow)) {
            x -= 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow)) {
            y += 1;
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            y -= 1;
        }

        if (Input.GetKey(KeyCode.W)) {
            z += 1;
        }

        if (Input.GetKey(KeyCode.S)) {
            z -= 1;
        }



        var csa = Mathf.Cos(x*Mathf.Deg2Rad);
        var sna = Mathf.Sin(x * Mathf.Deg2Rad);

        var csb = Mathf.Cos(y * Mathf.Deg2Rad);
        var snb = Mathf.Sin(y * Mathf.Deg2Rad);

        var csc = Mathf.Cos(z/2 * Mathf.Deg2Rad);
        var snc = Mathf.Cos(z/2 * Mathf.Deg2Rad);

        //Vector3 ax = new Vector3(-csa * snb, sna, csa * csb);
        Vector3 ax = new Vector3(csa * csb, csa*snb, -sna);
        Debug.Log($"{x} {y} {z} {ax}");
     //   ax = new Vector3(1,0,0).normalized;
        var q = Quaternion.AngleAxis(z, ax);




        //Quaternion.identity * Quaternion.Anglexis(x, Vector3)
        //transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 10 * smooth * Time.deltaTime);
        transform.rotation = q;

    }


}
