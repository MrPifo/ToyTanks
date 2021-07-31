using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class patrolBehaviour : MonoBehaviour
{
    [Header("VALUES")]
    public float angle;
    public GameObject PatrolPointStart;
    public List<GameObject> PatrolPoints;
    public float rotationSpeed = 120;
    public float movementSpeed = 30;
    public float DistanceBeforeNextObject = 2;
    private GameObject ActiveNavObject;
    [Header("References")]
    private Rigidbody TankRigid;
    public GameObject TankBody;
    private NavMeshAgent navAgent;
    private NavMeshPath navPath;
    private float velocity;
    private int PatrolPointsReachedCount;
    private Vector3 oldPos;
    private bool DenyDrivingBackward;
    private bool IsDrivingBackward;
    private bool LowSpeedDetected;

    void Start()
    {
        navAgent = GetComponentInChildren<NavMeshAgent>();
        TankRigid = GetComponentInChildren<Rigidbody>();
        ActiveNavObject = PatrolPointStart;
        navAgent.speed = 0;
        navAgent.angularSpeed = 0;
    }
    void FixedUpdate()
    {
        List<Vector3> corners;
        velocity = (oldPos - TankRigid.transform.position).magnitude * 10;
        if (navAgent.remainingDistance <= DistanceBeforeNextObject)
        {
            if (PatrolPointsReachedCount == PatrolPoints.Count)
            {
                PatrolPointsReachedCount = 0;
                ActiveNavObject = PatrolPointStart;
            }
            else
            {
                ActiveNavObject = PatrolPoints[PatrolPointsReachedCount];
                PatrolPointsReachedCount++;
            }
            navAgent.SetDestination(ActiveNavObject.transform.position);
            navPath = new NavMeshPath();
            navAgent.CalculatePath(ActiveNavObject.transform.position, navPath);
            //StartCoroutine(DrivingBackwardProtection());
        }
        navAgent.CalculatePath(ActiveNavObject.transform.position, navPath);
        corners = new List<Vector3>();
        for (int i = 0; i < navAgent.path.corners.Length; i++)
        {
            corners.Add(navAgent.path.corners[i]);
        }
        
        NavMeshHit hit;
        if (NavMesh.FindClosestEdge(TankRigid.transform.position, out hit, NavMesh.AllAreas))
        {
            if (hit.distance < 1.5f)
            {
                //TankRigid.AddForce(hit.normal * movementSpeed);
            }
            if (hit.position == corners[0])
            {
                //corners[1] = corners[1] + hit.normal * 2;
            }
            if (hit.distance < 1) {
                //TankRigid.AddForce(hit.normal * movementSpeed * 5);
            }
            DrawCircle(TankRigid.transform.position, hit.distance, Color.red);
            Debug.DrawRay(hit.position, Vector3.up, Color.red);
        }

        RaycastHit Rhit;
        bool dirFound = false;
        if (Physics.Raycast(corners[1], new Vector3(angle,0,1), out Rhit, 5000) && dirFound == false)
        {
            dirFound = true;
            Debug.DrawLine(corners[1], Rhit.point);
        }
        if (Physics.Raycast(corners[1], new Vector3(-angle, 0, 1), out Rhit, 5000) && dirFound == false)
        {
            dirFound = true;
            Debug.DrawLine(corners[1], Rhit.point);
        }
        if (Physics.Raycast(corners[1], new Vector3(0, 0, angle), out Rhit, 5000) && dirFound == false)
        {
            dirFound = true;
            Debug.DrawLine(corners[1], Rhit.point);
        }
        if (Physics.Raycast(corners[1], new Vector3(0, 0, -angle), out Rhit, 5000) && dirFound == false)
        {
            dirFound = true;
            Debug.DrawLine(corners[1], Rhit.point);
        }
        if (dirFound)
        {
            //corners[1] += -Rhit.normal * 2;
        }
        for (int i = 0; i < navAgent.path.corners.Length - 1; i++)
        {
            Debug.DrawLine(navAgent.path.corners[i], navAgent.path.corners[i + 1], new Color(1, 0, 0));
        }
        if (corners.Count > 1) {
            TankBody.transform.LookAt(corners[1]);
            TankRigid.AddForce(TankBody.transform.forward * movementSpeed);
            oldPos = TankRigid.transform.position;
            SendMessage("MoveForward");
        }
    }
    IEnumerator DriveBackward()
    {
        IsDrivingBackward = true;
        navAgent.isStopped = true;
        rotationSpeed *= 3;
        yield return new WaitForSeconds(0.35f);
        rotationSpeed /= 3;
        navAgent.isStopped = false;
        IsDrivingBackward = false;
        LowSpeedDetected = false;
        StartCoroutine(DrivingBackwardProtection());
    }
    IEnumerator DrivingBackwardProtection()
    {
        DenyDrivingBackward = true;
        yield return new WaitForSeconds(2f);
        DenyDrivingBackward = false;
    }
    IEnumerator InitiateSlowSpeedCheck()
    {
        movementSpeed /= 8;
        yield return new WaitForSeconds(0.05f);
        movementSpeed *= 8;
        if (velocity < 0.2f)
        {
            StartCoroutine(DriveBackward());
        }
        else
        {
            LowSpeedDetected = false;
        }
    }
    bool ScanForDestination(Vector3 dest)
    {
        Vector3 normVec = (TankRigid.transform.position - dest).normalized;
        float dotProd = Vector3.Dot(normVec, TankRigid.transform.forward);

        if (dotProd <= -0.95f || dotProd >= 0.95f)
        {
            return true;
        }
        return false;
    }
    void DrawCircle(Vector3 center, float radius, Color color)
    {
        Vector3 prevPos = center + new Vector3(radius, 0, 0);
        for (int i = 0; i < 30; i++)
        {
            float angle = (float)(i + 1) / 30.0f * Mathf.PI * 2.0f;
            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Debug.DrawLine(prevPos, newPos, color);
            prevPos = newPos;
        }
    }
}
