using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    public string TargetSceneName;
    public Transform ExitPoint;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if player is in vehicle
            CarSeat seat = other.GetComponent<CarSeat>();
            if (seat != null && seat.isOccupied)
            {
                // Transition with vehicle
                GameManager.Instance.TransitionToScene(
                    TargetSceneName, 
                    ExitPoint.position, 
                    ExitPoint.rotation
                );
            }
            else
            {
                // Transition just the player
                GameManager.Instance.TransitionToScene(
                    TargetSceneName, 
                    ExitPoint.position, 
                    ExitPoint.rotation
                );
            }
        }
    }
}
