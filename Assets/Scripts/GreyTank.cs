using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class greytank : MonoBehaviour
{
    [Header("Specified")]
    public float movementSpeed;
    public float rotationSpeed;
    public float ShootingSpeed;
    public float DistanceBeforeNextObject;

    [Header("VALUES")]
    public GameObject TypeBullet;
    public bool ShowDebug;
    public bool DrawLines;
    public float tankHeadRotSpeed;
    public float shootCooldown;
    public float shootStun;
    public int muzzleFlashAppearanceFrames;
    public int ScanViewAngle;
    public float ScanViewDistance;
    public int ScanIterations;
    public int RaycastIterationPerformance;
    public int RotationNeededToDrive;

    [Header("REFERENCES")]
    private Vector3 maxMoveAngle;
    private Rigidbody TankRigid;
    private Quaternion RotAngle;
    public GameObject TankHead;
    public GameObject bulletOutput;
    public GameObject TankTrack;
    public GameObject TankTrackHeight;
    public GameObject MuzzleFlash;
    public GameObject DeathCross;
    private GameObject DeathCrossLocation;
    public GameObject LineRendererContainer;
    public GameObject TrackContainer;
    private MeshRenderer TankMeshBody;
    public MeshRenderer TankMeshHead;
    public MeshRenderer TankMeshMuzzle;
    public ParticleSystem SmokeMuzzleParticle;
    private AudioSource TankDrive;
    private AudioSource TankEngine;
    public LineRenderer ScanLineRenderer;
    public List<LineRenderer> Lines;
    public GameObject Player;
    public int ScanIterationDelta;
    public int PatrolPointsReachedCount;
    public bool Shoot;
    public bool shootingBlocked;
    public bool movingBlocked;
    private bool allowDriveSound;
    private bool PaintTankTrack;
    private int muzzleFlashDelta;
    private bool PlayEngineSound;
    private float engineVolume;
    private bool RotationDirection;
    private float rotationDelta;

    void Start()
    {
        maxMoveAngle = new Vector3(0, RotationNeededToDrive, 0);
        TankDrive = GetComponent<AudioSource>();
        TankEngine = GetComponentInChildren<AudioSource>();
        TankRigid = GetComponentInChildren<Rigidbody>();
        TankMeshBody = GetComponentInChildren<MeshRenderer>();
        DeathCrossLocation = TankTrackHeight;
        allowDriveSound = true;
        PaintTankTrack = true;
        PlayEngineSound = true;
        engineVolume = 0f;
        StartCoroutine(ShootSpeed());
        RotationDirection = false;
    }
    void FixedUpdate()
    {
        if (shootingBlocked)
        {
            muzzleFlashDelta++;
            if (muzzleFlashDelta >= muzzleFlashAppearanceFrames && MuzzleFlash.activeInHierarchy)
            {
                MuzzleFlash.SetActive(false);
            }
        }
    }
    void LateUpdate()
    {
        if (Shoot)
        {
            Shoot = false;
            ShootBullet(TankHead.transform.forward);
        }
        if (engineVolume >= 0.1f)
        {
            engineVolume -= 0.012f;
        }
        TankEngine.volume = engineVolume;
        TankEngine.pitch = 1f + engineVolume * 1.5f;
        if (!Player)
        {
            //TankHead.transform.LookAt(Player.transform.position);
        }
    }
    void Update()
    {
        ScanIterationDelta++;
        if (ScanIterationDelta < RaycastIterationPerformance)
        {
            ScanIterationDelta = 0;
            ScanRadius();
        }
    }
    void MoveForward()
    {
        if (engineVolume < 0.2f)
        {
            engineVolume += 0.012f;
        }
        if (allowDriveSound)
        {
            TankDrive.Play();
            StartCoroutine(DriveSoundCooldown());
        }
        if (PaintTankTrack)
        {
            if (TankTrack != null)
            {
                GameObject track = Instantiate(TankTrack);
                if (track != null)
                {
                    track.transform.position = TankTrackHeight.transform.position;
                    track.transform.rotation = TankTrackHeight.transform.rotation;
                    track.transform.SetParent(TrackContainer.transform);
                    StartCoroutine(TankTrackCooldown());
                }
            }
        }
    }
    void ShootBullet(Vector3 direction)
    {
        if (!CheckIfNotShotBySelf(direction))
        {
            if (shootingBlocked == false)
            {
                SmokeMuzzleParticle.Play();
                TankRigid.velocity = new Vector3(0, 0, 0);
                GameObject bullet = Instantiate(TypeBullet);
                bullet.transform.position = bulletOutput.transform.position;
                bullet.transform.LookAt(direction);
                Vector3 dir = bullet.transform.forward;
                StartCoroutine(ShootCooldown());
                StartCoroutine(ShootStun());
            }
        }
    }
    public void GotHitByBullet()
    {
        TankEngine.Stop();
        TankRigid.velocity = new Vector3(0, 0, 0);
        TankMeshHead.enabled = false;
        TankMeshBody.enabled = false;
        TankMeshMuzzle.enabled = false;
        GameObject cross = Instantiate(DeathCross);
        cross.SetActive(true);
        cross.transform.position = DeathCrossLocation.transform.position;
        Destroy(this.transform.parent.gameObject);
    }
    IEnumerator ShootCooldown()
    {
        shootingBlocked = true;
        MuzzleFlash.SetActive(true);
        yield return new WaitForSeconds(shootCooldown);
        shootingBlocked = false;
        muzzleFlashDelta = 0;
    }
    IEnumerator ShootStun()
    {
        movingBlocked = true;
        yield return new WaitForSeconds(shootStun);
        movingBlocked = false;
    }
    IEnumerator DriveSoundCooldown()
    {
        allowDriveSound = false;
        yield return new WaitForSeconds(0.1f);
        allowDriveSound = true;
    }
    IEnumerator TankTrackCooldown()
    {
        PaintTankTrack = false;
        yield return new WaitForSeconds(0.15f);
        PaintTankTrack = true;
    }
    IEnumerator ShootSpeed()
    {
        yield return new WaitForSeconds(ShootingSpeed + Random.Range(0, 2));
        //Shoot = true;
        StartCoroutine(ShootSpeed());
    }
    public bool CheckIfNotShotBySelf(Vector3 dir)
    {
        bool getHit;
        Vector3 mouse = Input.mousePosition;
        RaycastHit FirstHit;
        RaycastHit SecondHit;
        for (int i = 0; i < 3; i++)
        {
            float rotationAngle = 0;
            if (i == 2)
            {
                rotationAngle = -2;
            }
            if (i == 1)
            {
                rotationAngle = 2;
            }
            Quaternion currentRot = TankHead.transform.rotation;
            TankHead.transform.Rotate(new Vector3(0, rotationAngle, 0));
            Quaternion safeRot = TankHead.transform.rotation;
            dir = TankHead.transform.forward;
            TankHead.transform.rotation = currentRot;
            Vector3 bulletOut = bulletOutput.transform.position;
            bulletOut.y -= 0.1f;

            if (Physics.Raycast(bulletOut, dir, out FirstHit, 50000000))
            {
                if (ShowDebug)
                {
                    Debug.DrawLine(bulletOut, FirstHit.point, Color.red, 3f);
                }
                Vector3 dir2 = Vector3.Reflect(dir, FirstHit.normal);
                if (FirstHit.transform.tag == "bot")
                {
                    return true;
                }
                if (Physics.Raycast(FirstHit.point, dir2, out SecondHit, 50000000))
                {
                    if (ShowDebug)
                    {
                        Debug.DrawLine(FirstHit.point, SecondHit.point, Color.green, 3f);
                    }
                    if (SecondHit.transform.tag == "bot")
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    void ScanRadius()
    {
        for (int i=0;i<Lines.Count;i++)
        {
            Destroy(Lines[i].gameObject);
        }
        Lines = new List<LineRenderer>();
        bool reversed = false;
        RaycastHit FirstHit;
        Vector3 bulletOut = bulletOutput.transform.position;
        bulletOut.y -= 0.2f;
        Vector3 dir = TankHead.transform.forward;
        int mask = LayerMask.GetMask("Default", "Destructable", "Player");
        Quaternion originalRot = TankHead.transform.rotation;
        for (int i = 0; i <= ScanIterations; i++)
        {
            float rotateAngle = 0;
            if (i == ScanIterations && reversed == false)
            {
                i = 0;
                reversed = true;
            }
            if (!reversed)
            {
                rotateAngle = ScanViewAngle / ScanIterations * i;
            } else
            {
                rotateAngle = -ScanViewAngle / ScanIterations * i;
            }
            TankHead.transform.Rotate(0, rotateAngle, 0);
            dir = TankHead.transform.forward;
            TankHead.transform.rotation = originalRot;
            if (Physics.Raycast(bulletOut, dir, out FirstHit, ScanViewDistance, mask))
            {
                if (DrawLines)
                {
                    LineRenderer line = Instantiate(ScanLineRenderer);
                    Lines.Add(line);
                    line.SetPosition(0, bulletOut);
                    line.SetPosition(1, FirstHit.point);
                    line.startWidth = 0.1f;
                    line.endWidth = 0.1f;
                    line.transform.SetParent(LineRendererContainer.transform);
                    line.gameObject.SetActive(true);

                    if (FirstHit.transform.tag == "player")
                    {
                        Player = FirstHit.collider.gameObject;
                        line.materials[0].SetColor("_BaseColor", new Color32(255, 50, 50, 200));
                    }
                    if (FirstHit.transform.tag == "bot")
                    {
                        line.materials[0].SetColor("_BaseColor", new Color32(50, 255, 50, 200));
                    }
                    if (FirstHit.transform.tag == "destroyable")
                    {
                        line.materials[0].SetColor("_BaseColor", new Color32(50, 50, 255, 200));
                    }
                }
                //Debug.DrawLine(bulletOut, FirstHit.point, new Color(1, 0, 0), RaycastIterationPerformance / 10);
            }
        }
    }
    void DecideDirectionToDrive(float offset)
    {
        Vector3 TankPosition = TankRigid.transform.position;
        Quaternion TankRotation = TankRigid.transform.rotation;
        float angle = 0;
        RaycastHit hit;
        RaycastHit hit2;
        int mask = LayerMask.GetMask("Default", "Destructable");

        for (int i = 0; i < 45; i++)
        {
            angle = 360 / 45 * i + offset;
            TankRigid.transform.Rotate(0, angle, 0);
            Vector3 dir = TankRigid.transform.forward;
            TankRigid.transform.rotation = TankRotation;
            List<RaycastHit> hits = new List<RaycastHit>();
            if (Physics.Raycast(TankPosition, dir, out hit, 2000, mask))
            {
                hits.Add(hit);
                //Debug.DrawLine(TankPosition, hit.point, new Color(0, 1, 0), 20f);
                if (hit.transform.tag == "PatrolPoint")
                {
                    hits = new List<RaycastHit>();
                    RaycastHit tankpos = new RaycastHit();
                    tankpos.point = TankPosition;
                    hits.Add(tankpos);
                    hits.Add(hit);

                    //PatrolPointFound(hits);
                    return;
                }
                Vector3 dir2 = Vector3.Reflect(dir, hit.normal);
                RaycastHit oldHit = new RaycastHit();
                oldHit.point = hit.point;
                for (int j = 0; j < 5; j++)
                {
                    if (Physics.Raycast(oldHit.point, dir2, out hit2, 2000, mask))
                    {
                        hits.Add(hit2);
                        //Debug.DrawLine(oldHit.point, hit2.point, new Color(1, 0, 0), 20f);
                        if (hit2.transform.tag == "PatrolPoint")
                        {
                            
                            //PatrolPointFound(hits);
                            return;
                        }
                        dir2 = Vector3.Reflect(dir2, hit2.normal);
                    }
                    oldHit.point = hit2.point;
                }
            }
        }
    }
}
