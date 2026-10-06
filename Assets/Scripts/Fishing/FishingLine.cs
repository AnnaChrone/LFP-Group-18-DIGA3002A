using UnityEngine;

public class FishingLine : MonoBehaviour
{
    public Transform bobberpoint; // bobber end
    public Transform rodpoint;   // rod tip
    public LineRenderer line;

    private void Update()
    {
        if (bobberpoint == null || rodpoint == null || line == null) return;

        line.SetPosition(0, bobberpoint.position);
        line.SetPosition(1, rodpoint.position);
    }
}