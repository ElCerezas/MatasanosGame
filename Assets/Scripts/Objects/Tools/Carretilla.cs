using UnityEngine;

public partial class Carretilla : PoweredItem
{
    [SerializeField] private Transform elevadorCarretilla;
    [SerializeField] private float elevationSpeed = 2f;

    [Header("Límites de Movimiento")]
    [SerializeField] private float boostTime = 0.5f;
    [SerializeField] private Transform puntoSuperior;
    [SerializeField] private Transform puntoInferior;
    private float timer;


    public void Up()
    {
        timer = boostTime;
        isTurnedOn.Value = true;
        Debug.Log("resetedTimer");
    }

    void Update()
    {
        if (!IsOwner) return;
        if (!isTurnedOn.Value) { ElevatorGoBack(); return; }
        MoveElevator();
    }

    private void ElevatorGoBack()
    {
        if (transform.position == puntoInferior.position) return;
        timer -= Time.deltaTime;
        float speedMult = -timer / boostTime;

        float finalSpeed = ((elevationSpeed * speedMult) * Time.deltaTime);

        elevadorCarretilla.position = Vector3.MoveTowards(elevadorCarretilla.position, puntoInferior.position, finalSpeed);

        /*float minY = Mathf.Min(puntoInferior.position.y, puntoSuperior.position.y);
        float maxY = Mathf.Max(puntoInferior.position.y, puntoSuperior.position.y);

        float clampedY = Mathf.Clamp(targetY, minY, maxY);


        if (clampedY == minY || clampedY == maxY)
        {
            return;
        }*/
    }

    private void MoveElevator()
    {
        if (timer < 0) { isTurnedOn.Value = false; return;}

        timer -= Time.deltaTime;
        float speedMult = timer / boostTime;

        float finalSpeed = ((elevationSpeed * speedMult) * Time.deltaTime);

        elevadorCarretilla.position = Vector3.MoveTowards(elevadorCarretilla.position, puntoSuperior.position, finalSpeed);
        /*
        timer -= Time.deltaTime;
        float speedMult = timer / boostTime;

        Vector3 currentPos = elevadorCarretilla.position;
        float targetY = currentPos.y + ((elevationSpeed * elevationSpeed) * Time.deltaTime);

        float minY = Mathf.Min(puntoInferior.position.y, puntoSuperior.position.y);
        float maxY = Mathf.Max(puntoInferior.position.y, puntoSuperior.position.y);

        float clampedY = Mathf.Clamp(targetY, minY, maxY);


        if (clampedY == minY || clampedY == maxY)
        {
            return;   
        }
        elevadorCarretilla.position = new Vector3(currentPos.x, clampedY, currentPos.z);*/
    }
}