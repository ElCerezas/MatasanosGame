using UnityEngine;
using System.Collections;

public class CameraMovent : MonoBehaviour
{
    private readonly Vector3 camPosAway = new (-121f, 13f, 47f);
    private readonly Quaternion camRotAway = Quaternion.Euler(-3.5f, 20.327f, 0);
    private readonly Vector3 camPosOnScreen = new (-59.3f, 41f, 203f);
    private readonly Quaternion camRotOnScreen = Quaternion.Euler(0, -11f, 0);
    
    Vector3 startPos;
    Vector3 endPos;
    Quaternion startRot;
    Quaternion endRot;
    
    [SerializeField] float moventSpeed;
    private float startTime;
    private float journeyLength;

    void Start()
    {
        startPos = camPosAway;
        startRot = camRotAway;
        endPos = camPosAway;
        endRot = camRotAway;
        startTime = Time.time;
        journeyLength = Vector3.Distance(startPos, endPos);
    }
    
    void Update()
    {
        float distCovered = (Time.time - startTime) * moventSpeed;
        float fracJourney = distCovered / journeyLength;
        if (startPos != endPos) transform.position = Vector3.Lerp(startPos, endPos, fracJourney);
        if (startRot != endRot) transform.rotation = Quaternion.Lerp(startRot.normalized, endRot.normalized, fracJourney);
    }
    
    public void Enhance()
    {
        startPos = camPosAway;
        startRot = camRotAway; 
        endPos = camPosOnScreen;
        endRot = camRotOnScreen;
        startTime = Time.time;
        journeyLength = Vector3.Distance(startPos, endPos);
    }

    public void Away()
    {
        startPos = camPosOnScreen;
        startRot = camRotOnScreen;
        endPos = camPosAway;
        endRot = camRotAway;
        startTime = Time.time;
        journeyLength = Vector3.Distance(startPos, endPos);
    }
    
}
