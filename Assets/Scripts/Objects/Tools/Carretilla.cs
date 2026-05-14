using UnityEngine;

public partial class Carretilla : PoweredItem
{
    [SerializeField] private Transform elevadorCarretilla;
    [SerializeField] private float elevationSpeed = 2f;
    
    [Header("Límites de Movimiento")]
    [SerializeField] private Transform puntoSuperior;
    [SerializeField] private Transform puntoInferior;

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
        if (!isTurnedOn.Value) elevation = 0;
    }

    void Update()
    {
        if (hasPower.Value && isTurnedOn.Value && elevation != 0)
        {
            MoveElevator();
        }
    }

    private void MoveElevator()
    {
        Vector3 currentPos = elevadorCarretilla.position;
        float targetY = currentPos.y + (elevation * Time.deltaTime);

        float minY = Mathf.Min(puntoInferior.position.y, puntoSuperior.position.y);
        float maxY = Mathf.Max(puntoInferior.position.y, puntoSuperior.position.y);

        float clampedY = Mathf.Clamp(targetY, minY, maxY);

        elevadorCarretilla.position = new Vector3(currentPos.x, clampedY, currentPos.z);

        if (clampedY == minY || clampedY == maxY)
        {
            elevation = 0;
        }
    }
}