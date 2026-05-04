using UnityEngine;

public class Carretilla : PoweredItem
{
    [SerializeField] private Transform elevadorCarretilla;
    [SerializeField] private float maxElevation;
    [SerializeField] private float minElevation;
    [SerializeField] private float elevationSpeed;
    private float elevation;

    public void Up()
    {
        elevation = elevationSpeed;
    }
    public void Down()
    {
        elevation = -elevationSpeed;
    }

    public void ToogleStop()
    {
        isTurnedOn.Value = !isTurnedOn.Value;
    }

    // Update is called once per frame
    void Update()
    {
        if (hasPower.Value && isTurnedOn.Value)
        {
            Vector3 newposition = elevadorCarretilla.position + new Vector3(0, elevation * Time.deltaTime, 0);
            if (Vector3.Distance(gameObject.transform.position, newposition) > minElevation && Vector3.Distance(gameObject.transform.position, newposition) < maxElevation)
            {
                elevadorCarretilla.transform.position = newposition;
            }
        }
    }
}
