using UnityEngine;

public class BaseHovercarController : MonoBehaviour
{

    [Tooltip("Ordered list of checkpoints for the AI to follow.")]
    public Transform[] checkpoints;

    public bool canAccelerate = false;
}