using UnityEngine;
using VRM;

public class load_vrm : MonoBehaviour
{

  private GameObject model;
  
  public GameObject target;
  public GameObject platform;
  private Camera camera;
  public Camera camera2;


  private Matrix4x4 mat1;
  private Transform tr1;

  private Quaternion q;
  private Vector3 p;
  private Vector3 s;
    // Start is called before the first frame update
    void Start()
    {
      //LoadVrm("d:/1/models/vrm/4377650496181842344.vrm");
      LoadVrm("d:/1/models/vrm/vrm/dress.vrm");
      camera = Camera.main;
    mat1 = camera.transform.localToWorldMatrix;
    //mat1 = camera.transform.localToWorldMatrix;
    tr1 = camera2.transform;

    q = tr1.rotation;
    p = tr1.position;
    s = tr1.localScale;

    mat1 = Matrix4x4.TRS(p, q, s);

    //q = tr1.worldToLocalMatrix
    
    }


  void Update() {
    var rot = target.transform.localRotation;
    var pos = target.transform.localPosition;
    var scale = target.transform.localScale;

    var m = Matrix4x4.TRS(pos, rot, scale);
    var res = m.inverse;

    rot = res.ExtractRotation();
    pos = res.ExtractPosition();
    scale = res.ExtractScale();

    platform.transform.localScale = scale;
    platform.transform.rotation = rot;
    platform.transform.position = pos;

    res = res * mat1;

    rot = res.ExtractRotation();
    pos = res.ExtractPosition();
    scale = res.ExtractScale();

    camera2.transform.localScale = scale;
    camera2.transform.rotation = rot;
    camera2.transform.position = pos;

    return;
    platform.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
    platform.transform.rotation = Quaternion.Inverse(rot);
    platform.transform.position = -pos;
  }

  // Update is called once per frame
  void Update2()
    {
    if (model != null) {
      //  model.transform.Rotate(Vector3.up, 2f);



      var m1 = this.target.transform.localToWorldMatrix;
      var m2 = m1.inverse;



      var newm = m1 * mat1;
      //var newm = mat1 * m1;

      // var 

      //mc =m1.inverse;


      //Debug.Log(camera.transform.worldToLocalMatrix.MultiplyPoint(Vector3.zero));

      //return;
      //camera.transform.position = mc.ExtractPosition();
      camera.transform.rotation = newm.ExtractRotation();
      camera.transform.position = newm.ExtractPosition();
      camera.transform.localScale = newm.ExtractScale();
      Vector3 axis;
      float angle;
      this.target.transform.rotation.ToAxisAngle(out axis, out angle);


      /*camera.transform.position = this.p;
      camera.transform.rotation = this.q;
      camera.transform.localScale = this.s;
      camera.transform.RotateAround(target.transform.position, axis, angle);*/

    }
    }


  private async void LoadVrm(string url) {
    var loaded = await VrmUtility.LoadAsync(url);
    loaded.ShowMeshes();
    //loaded.EnableUpdateWhenOffscreen();
    var model = loaded.gameObject;
    this.model = model;
    //this.model = GameObject.Find("root");

    var obj = GameObject.Instantiate(this.model);
    //this.target.name = "target";
    //this.target.transform.position = new Vector3(3, 0, 0);


    obj.transform.parent = target.transform;
    obj.transform.localPosition = Vector3.zero;
    obj.transform.localRotation = Quaternion.identity;


    

  }

}
