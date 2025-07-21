using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
public class HingedItemHingeJointController : MonoBehaviour
{
    [Header("Hinge Settings")]
    public float minClosedAngle = 0f;
    public float maxOpenAngle = 80f;
    public float openCloseThreshold = 5f; // Snap if within this many degrees
    public float dragSpeed = 100f; // How fast it moves with mouse input

    [Header("Hinge Axis (Local)")]
    public Vector3 localHingeAxis = Vector3.right;

    [Header("Snapping")]
    public float snapSpringStrength = 200f;
    public float snapDamper = 20f;

    private HingeJoint hinge;
    private Rigidbody rb;
    private bool isDragging = false;

    [SerializeField] private Rigidbody chassisBody; // Set this in Inspector or at runtime
    private bool isTightened = false;


    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        if (hinge == null)
            hinge = gameObject.AddComponent<HingeJoint>();

        hinge.autoConfigureConnectedAnchor = false; // Important for stability
        hinge.connectedBody = null; // Leave disconnected initially

        hinge.axis = localHingeAxis;
        hinge.useLimits = false;
        hinge.useSpring = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void Update()
    {
         if (!isTightened)
        return;
        // Begin dragging
        if (Input.GetKeyDown(KeyCode.E))
        {
            isDragging = true;
            hinge.useSpring = false;
            rb.angularDrag = 1f; // Less resistance when dragging
        }

        // End dragging
        if (Input.GetKeyUp(KeyCode.E))
        {
            isDragging = false;
            rb.angularDrag = 5f;

            float currentAngle = hinge.angle;

            // Snap if within threshold
            if (Mathf.Abs(currentAngle - maxOpenAngle) < openCloseThreshold)
            {
                SnapToAngle(maxOpenAngle);
            }
            else if (Mathf.Abs(currentAngle - minClosedAngle) < openCloseThreshold)
            {
                SnapToAngle(minClosedAngle);
            }
            else
            {
                hinge.useSpring = false; // Let gravity handle it
            }
        }

        if (isDragging)
        {
            DragWithMouse();
        }
    }

    private void DragWithMouse()
    {
        float input;

        // Determine input direction based on axis
        if (localHingeAxis == Vector3.right || localHingeAxis == Vector3.left)
            input = Input.GetAxis("Mouse Y");
        else
            input = Input.GetAxis("Mouse X");

        float torque = input * dragSpeed;
        rb.AddTorque(transform.TransformDirection(localHingeAxis) * torque, ForceMode.Force);
    }

    private void SnapToAngle(float targetAngle)
    {
        hinge.useSpring = true;
        JointSpring spring = hinge.spring;
        spring.spring = snapSpringStrength;
        spring.damper = snapDamper;
        spring.targetPosition = targetAngle;
        hinge.spring = spring;
    }

    public void AttachToChassis()
{
    if (isTightened || chassisBody == null)
        return;

    Rigidbody rb = GetComponent<Rigidbody>();
    rb.isKinematic = false;
    rb.useGravity = true;

    hinge.connectedBody = chassisBody;
    hinge.anchor = transform.InverseTransformPoint(transform.position); // OR fine-tune manually
    hinge.axis = localHingeAxis;

    hinge.useLimits = true;
    JointLimits limits = new JointLimits
    {
        min = minClosedAngle,
        max = maxOpenAngle
    };
    hinge.limits = limits;

    isTightened = true;

    Debug.Log("Hood tightened and attached via hinge.");
}
}
