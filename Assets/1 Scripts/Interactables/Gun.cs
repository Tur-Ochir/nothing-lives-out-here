using UnityEngine;
using DG.Tweening;

public class Gun : Interactable
{
    [Header("Gun Settings")]
    public Transform muzzlePoint;
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public AudioClip fireSound;
    public AudioSource audioSource;
    
    [Header("Parameters")]
    public float range = 100f;
    public float fireRate = 0.5f;
    public float damage = 10f;
    
    [Header("Recoil")]
    public float recoilZ = -0.1f;
    public float recoilXRotation = -5f;
    public float recoilDuration = 0.1f;
    
    private float nextFireTime;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    protected override void Awake()
    {
        base.Awake();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // canUse is true by default from Interactable
    }

    protected override void Start()
    {
        base.Start();
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
    }

    public override void Use()
    {
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate;
        Shoot();
    }

    private void Shoot()
    {
        // 1. VFX & SFX
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
        }

        if (fireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fireSound);
        }

        // 2. Recoil Animation
        // ApplyRecoil();

        // 3. Raycast Logic
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.Log($"Gun hit: {hit.transform.name}");

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            // Future: hit.transform.GetComponent<IDamageable>()?.TakeDamage(damage);
        }
    }

    private void ApplyRecoil()
    {
        // Cancel previous tweens to avoid stacking weirdly
        transform.DOKill();

        // Procedural kickback
        transform.DOLocalMoveZ(originalLocalPos.z + recoilZ, recoilDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            transform.DOLocalMoveZ(originalLocalPos.z, recoilDuration * 2f).SetEase(Ease.InOutSine);
        });

        // Procedural rotation kick
        transform.DOLocalRotate(new Vector3(recoilXRotation, 0, 0), recoilDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            transform.DOLocalRotate(Vector3.zero, recoilDuration * 2f).SetEase(Ease.InOutSine);
        });
    }

    // Ensure we reset properly when dropped or moved
    public override void Drop()
    {
        transform.DOKill();
        base.Drop();
    }
}
