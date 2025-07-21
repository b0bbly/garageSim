using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    public Transform ExitPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CarSeat seat = other.GetComponent<CarSeat>();
        if (seat != null && seat.isOccupied)
        {
            //GameManager.Instance.TransitionToSeededScene(ExitPoint.position, ExitPoint.rotation);
        }
        else
        {
            //GameManager.Instance.TransitionToSeededScene(ExitPoint.position, ExitPoint.rotation);
        }
    }
}