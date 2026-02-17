using UnityEngine;

public class DesktopRayInteractor : MonoBehaviour
{
    public float rayDistance = 100f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance))
            {
                Debug.Log("Touché : " + hit.collider.name);

                // Si ton cercle a un script Target
                hit.collider.SendMessage("OnTargetHit", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}