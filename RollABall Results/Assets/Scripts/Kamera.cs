using UnityEngine;

public class Kamera : MonoBehaviour
{
    public Transform spieler;
    public Vector3 offset = new Vector3(0f, 10f, -10f);

    void LateUpdate()
    {
        if (!spieler) return;

        transform.position = spieler.position + offset;
        transform.LookAt(spieler.position);
    }
}
