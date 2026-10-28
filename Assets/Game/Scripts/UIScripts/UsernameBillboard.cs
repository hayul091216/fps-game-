using UnityEngine;

public class UserNameBillBoard : MonoBehaviour
{
    Camera maincam;


    void Update()
    {
        if(maincam== null)
        {
            maincam = FindAnyObjectByType<Camera>();

        }
        if (maincam == null)
            return;

        transform.LookAt(maincam.transform);
        transform.Rotate(Vector3.up * 180);
    }


}
