using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class browntank : MonoBehaviour
{
    [Header("VALUES")]
    public GameObject TypeBullet;
    public bool ShowDebug;
    public float ShootingSpeed;
    public float RotationSwitchSpeed;
    public float movementSpeed;
    public float rotationSpeed;
    public float tankHeadRotSpeed;
    public float shootCooldown;
    public float shootStun;
    public int muzzleFlashAppearanceFrames;
    public Vector3 maxMoveAngle;
    [Header("REFERENCES")]
    public Rigidbody TankRigid;
    private Quaternion RotAngle;
    public GameObject TankHead;
    public GameObject bulletOutput;
    public GameObject TankTrack;
    public GameObject TankTrackHeight;
    public GameObject MuzzleFlash;
    public GameObject DeathCross;
    public GameObject DeathCrossLocation;
    public MeshRenderer TankMeshBody;
    public MeshRenderer TankMeshHead;
    public MeshRenderer TankMeshMuzzle;
    public ParticleSystem SmokeMuzzleParticle;
    public AudioSource TankDrive;
    public AudioSource TankEngine;
    public bool MoveRight;
    public bool MoveLeft;
    public bool MoveUp;
    public bool MoveDown;
    public bool Shoot;
    public bool shootingBlocked;
    public bool movingBlocked;
    public bool allowDriveSound;
    public bool PaintTankTrack;
    private int muzzleFlashDelta;
    public bool PlayEngineSound;
    public float engineVolume;
    public bool RotationDirection;
    public float rotationDelta;

    void Start()
    {
        allowDriveSound = true;
        PaintTankTrack = true;
        PlayEngineSound = true;
        engineVolume = 0f;
        StartCoroutine(ShootSpeed());
        StartCoroutine(HeadTurn());
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
        if (MoveUp)
        {
            RotAngle = Quaternion.Euler(new Vector3(0, 0, 0));
            if (MoveRight)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, -45, 0));
            }
            if (MoveLeft)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, 45, 0));
            }
            MoveForward();
        }
        if (MoveLeft)
        {
            RotAngle = Quaternion.Euler(new Vector3(0, -90, 0));
            if (MoveUp)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, -45, 0));
            }
            if (MoveDown)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, -135, 0));
            }
            MoveForward();
        }
        if (MoveRight)
        {
            RotAngle = Quaternion.Euler(new Vector3(0, 90, 0));
            if (MoveUp)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, 45, 0));
            }
            if (MoveDown)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, 135, 0));
            }
            MoveForward();
        }
        if (MoveDown)
        {
            RotAngle = Quaternion.Euler(new Vector3(0, 180, 0));
            if (MoveRight)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, 225, 0));
            }
            if (MoveLeft)
            {
                RotAngle = Quaternion.Euler(new Vector3(0, -135, 0));
            }
            MoveForward();
        }
    }
    void LateUpdate()
    {
        if (RotationDirection)
        {
            rotationDelta += Random.Range(0, rotationSpeed);
        } else
        {
            rotationDelta -= Random.Range(0, rotationSpeed);
        }
        Quaternion originalRot;
        Quaternion lookRot;
        originalRot = TankHead.transform.rotation;
        lookRot = TankHead.transform.rotation;
        lookRot.x = 0;
        lookRot.z = 0;
        lookRot.y = Quaternion.Euler(0, rotationDelta, 0).y;
        TankHead.transform.rotation = Quaternion.Lerp(TankHead.transform.rotation, lookRot, tankHeadRotSpeed * Time.deltaTime);
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
    }
    void MoveForward()
    {
        if (engineVolume < 0.2f)
        {
            engineVolume += 0.012f;
        }
        TankRigid.transform.rotation = Quaternion.Slerp(TankRigid.transform.rotation, RotAngle, Time.deltaTime * rotationSpeed);
        Quaternion currentRot = TankRigid.transform.rotation;
        float diffAngle = Quaternion.Angle(currentRot, RotAngle);
        if (movingBlocked == false)
        {
            if (diffAngle <= maxMoveAngle.y && diffAngle >= 0)
            {
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
                            StartCoroutine(TankTrackCooldown());
                        }
                    }
                }
                TankRigid.AddForce(TankRigid.transform.forward * movementSpeed);
            }
        }
    }
    void ShootBullet(Vector3 direction)
    {
        if (!CheckIfNotShotBySelf(direction)) {
            if (shootingBlocked == false)
            {
                SmokeMuzzleParticle.Play();
                TankRigid.velocity = new Vector3(0, 0, 0);
                GameObject bullet = Instantiate(TypeBullet);
                bullet.transform.position = bulletOutput.transform.position;
                bullet.transform.LookAt(direction);
                bullet.GetComponent<Bullet>().ShotByBot = true;
                Vector3 dir = bullet.transform.forward;
                bullet.GetComponent<Bullet>().direction = direction;
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
        Shoot = true;
        StartCoroutine(ShootSpeed());
    }
    IEnumerator HeadTurn()
    {
        yield return new WaitForSeconds(Random.Range(1, RotationSwitchSpeed));
        if (RotationDirection)
        {
            RotationDirection = false;
        } else
        {
            RotationDirection = true;
        }
        StartCoroutine(HeadTurn());
    }
    public bool CheckIfNotShotBySelf(Vector3 dir)
    {
        bool getHit;
        Vector3 mouse = Input.mousePosition;
        RaycastHit FirstHit;
        RaycastHit SecondHit;
        for (int i = 0; i < 3;i++) {
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
                Vector3 dir2 = Vector3.Reflect(dir, FirstHit.normal); ;
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
}
