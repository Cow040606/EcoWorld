using UnityEngine;

public class Arrow_Logic : MonoBehaviour
{
    [Header("--- THONG TIN SO HUU & SAT THUONG ---")]
    public Player_Controller chuSohuu;
    public float damage = 25f;

    [Header("--- HIEU UNG VFX XE GIO & VA CHAM ---")]
    [Tooltip("Vi tri duoi mui ten de phat vet gio (Neu de trong se tu dong tao o duoi)")]
    public Transform tailPoint;

    [Tooltip("Vet xe gio phia sau duoi mui ten")]
    public TrailRenderer windTrail;

    [Tooltip("Hieu ung toe tia khi trung muc tieu (Tuy chon)")]
    public GameObject hitVfxPrefab;

    [Header("--- CAU HINH VET GIO THANH MANH ---")]
    [Tooltip("Thoi gian luu vet gio sau duoi")]
    public float trailTime = 0.18f;

    [Tooltip("Do day vet gio o duoi mui ten (0.025 thanh manh sac net)")]
    public float startWidth = 0.025f;

    [Tooltip("Do day o chop duoi xa")]
    public float endWidth = 0.0f;

    public Color windColor = new Color(0.9f, 0.95f, 1f, 0.6f);

    private Rigidbody rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SetupWindTrail();
    }

    void Start()
    {
        // Tu huy sau 10 giay neu ban truot ra ngoai map
        Destroy(gameObject, 10f);
    }

    private void SetupWindTrail()
    {
        if (windTrail == null) windTrail = GetComponentInChildren<TrailRenderer>();
        if (windTrail == null)
        {
            // Tao GameObject con dat tai duoi mui ten de vet gio bat dau chuan xac tu duoi
            GameObject trailObj = new GameObject("WindTrail_Emitter");
            trailObj.transform.SetParent(tailPoint != null ? tailPoint : transform);
            
            // Neu khong co tailPoint, dich nhe ra phia sau duoi mui ten (-0.35m)
            trailObj.transform.localPosition = (tailPoint != null) ? Vector3.zero : new Vector3(0f, 0f, -0.35f);
            trailObj.transform.localRotation = Quaternion.identity;

            windTrail = trailObj.AddComponent<TrailRenderer>();
        }

        // Cau hinh thong so TrailRenderer thanh manh, sac net
        windTrail.time = trailTime;
        windTrail.startWidth = startWidth;
        windTrail.endWidth = endWidth;
        windTrail.widthMultiplier = startWidth;
        windTrail.minVertexDistance = 0.02f;
        windTrail.alignment = LineAlignment.View; // Luon huong phang ve Camera, khong bi xoan to
        windTrail.textureMode = LineTextureMode.Stretch;
        windTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        windTrail.receiveShadows = false;

        // Curve vuot nhon dan ve sau duoi: 1 -> 0
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        windTrail.widthCurve = curve;

        // Gradient mau xe gio: Trang mo xanh nhat -> Trong suot
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(windColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(windColor.a, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        windTrail.colorGradient = gradient;

        // Tim Shader Universal Render Pipeline thich hop
        if (windTrail.sharedMaterial == null)
        {
            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (trailShader == null) trailShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (trailShader == null) trailShader = Shader.Find("Sprites/Default");

            if (trailShader != null)
            {
                Material trailMat = new Material(trailShader);
                windTrail.material = trailMat;
            }
        }
    }

    void Update()
    {
        // Tu dong xoay dau mui ten chui dan theo quy dao quan tinh bay thuc te
        if (!hasHit && rb != null && rb.linearVelocity.sqrMagnitude > 0.5f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Bo qua neu trung chinh nguoi vua ban
        if (chuSohuu != null && (other.gameObject == chuSohuu.gameObject || other.transform.IsChildOf(chuSohuu.transform))) return;
        
        // Bo qua cac trigger
        if (other.isTrigger) return; 

        hasHit = true;
        if (windTrail != null) windTrail.emitting = false;

        bool isCrit = (damage >= 40f);

        // 1. Kiem tra trung Boss (BossController)
        var boss = other.GetComponent<BossController>();
        if (boss == null) boss = other.GetComponentInParent<BossController>();

        if (boss != null)
        {
            boss.RPC_PlayerHitBoss(damage);
            DamagePopup.Create(transform.position + Vector3.up * 0.5f, (int)damage, isCrit);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        // 2. Kiem tra trung Quai Orc / Skeleton (EnemyAIOrc)
        var enemyOrc = other.GetComponent<EnemyAIOrc>();
        if (enemyOrc == null) enemyOrc = other.GetComponentInParent<EnemyAIOrc>();

        if (enemyOrc != null)
        {
            enemyOrc.RPC_TakeDamageFromPlayer((int)damage);
            DamagePopup.Create(transform.position + Vector3.up * 0.5f, (int)damage, isCrit);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        // 3. Kiem tra trung Thu (AnimalAI_Controller)
        var animalAI = other.GetComponent<ithappy.Animals_FREE.AnimalAI_Controller>();
        if (animalAI == null) animalAI = other.GetComponentInParent<ithappy.Animals_FREE.AnimalAI_Controller>();

        if (animalAI != null && chuSohuu != null)
        {
            animalAI.RPC_AnimalTakeDamage(damage, chuSohuu.Runner.LocalPlayer);
            DamagePopup.Create(transform.position + Vector3.up * 0.5f, (int)damage, isCrit);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }
        
        // 4. Kiem tra trung Nguoi choi khac (Player_Controller)
        var hitPlayer = other.GetComponent<Player_Controller>();
        if (hitPlayer == null) hitPlayer = other.GetComponentInParent<Player_Controller>();

        if (hitPlayer != null && hitPlayer != chuSohuu)
        {
            hitPlayer.RPC_TakeDame(damage);
            DamagePopup.Create(transform.position + Vector3.up * 0.5f, (int)damage, isCrit, true);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        // 5. Cam vao be mat khac (dat, da, cay, tuong...)
        Rigidbody currentRb = GetComponent<Rigidbody>();
        if (currentRb != null)
        {
            currentRb.isKinematic = true;
            currentRb.linearVelocity = Vector3.zero;
        }

        // Cam chat vao be mat
        transform.SetParent(other.transform);
        SpawnHitVfx();

        // Sau 5 giay moi tu huy de don rac
        Destroy(gameObject, 5f);
    }

    private void SpawnHitVfx()
    {
        if (hitVfxPrefab != null)
        {
            Instantiate(hitVfxPrefab, transform.position, transform.rotation);
        }
    }
}
