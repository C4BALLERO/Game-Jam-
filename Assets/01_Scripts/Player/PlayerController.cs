using UnityEngine;

// ================================================================
//  PlayerController.cs
//  Requiere: Rigidbody2D, Animator, EnergySystem, LimbSystem
// ================================================================

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // ── Movimiento ───────────────────────────────────────────────
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float speedSinPiernas = 2f; // velocidad reducida sin piernas

    // ── Dash ─────────────────────────────────────────────────────
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.8f;

    // ── Combate ──────────────────────────────────────────────────
    [Header("Combate")]
    [SerializeField] private Transform puntoAtaque;
    [SerializeField] private float radioAtaque = 0.6f;
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private int danioGolpe = 10;
    [SerializeField] private int danioPata = 18;

    // ── Referencias ──────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator anim;
    private EnergySystem energySystem;
    private LimbSystem limbSystem;

    // ── Estado interno ───────────────────────────────────────────
    private Vector2 inputDir;
    private bool isDashing;
    private bool isInvincible;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    // Bloqueos de acción según extremidades perdidas
    public bool PuedeAtacar  => limbSystem.TieneBrazos;
    public bool PuedeMoverse => limbSystem.TienePiernas || limbSystem.PiernaParcial;
    public bool EsInvencible => isInvincible;

    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb           = GetComponent<Rigidbody2D>();
        anim         = GetComponent<Animator>();
        energySystem = GetComponent<EnergySystem>();
        limbSystem   = GetComponent<LimbSystem>();
    }

    private void Update()
    {
        LeerInput();
        GestionarDash();
        GestionarAtaques();
        ActualizarAnimaciones();
    }

    private void FixedUpdate()
    {
        if (!isDashing)
            Mover();
    }

    // ── Input ─────────────────────────────────────────────────────

    private void LeerInput()
    {
        // Sin piernas: no puede moverse (modo cabeza o torso crítico)
        if (!PuedeMoverse)
        {
            inputDir = Vector2.zero;
            return;
        }

        inputDir = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
    }

    // ── Movimiento ────────────────────────────────────────────────

    private void Mover()
    {
        float velocidadActual = limbSystem.TienePiernas ? moveSpeed : speedSinPiernas;
        rb.linearVelocity = inputDir * velocidadActual;

        // Flip del sprite según dirección horizontal
        if (inputDir.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(inputDir.x), 1f, 1f);
    }

    // ── Dash ──────────────────────────────────────────────────────

    private void GestionarDash()
    {
        dashCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && dashCooldownTimer <= 0f && PuedeMoverse)
            IniciarDash();

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            rb.linearVelocity = dashDirection * dashSpeed;

            if (dashTimer <= 0f)
                TerminarDash();
        }
    }

    private void IniciarDash()
    {
        isDashing    = true;
        isInvincible = true;                              // invencible durante el dash
        dashTimer    = dashDuration;
        dashCooldownTimer = dashCooldown;

        // Dash en la dirección del input; si está quieto, usa la dirección del sprite
        dashDirection = inputDir != Vector2.zero
            ? inputDir
            : new Vector2(transform.localScale.x, 0f);

        anim.SetTrigger("Dash");

        // Daño a enemigos durante el dash (dash letal)
        Collider2D[] golpeados = Physics2D.OverlapCircleAll(transform.position, 1f, capaEnemigos);
        foreach (var col in golpeados)
            col.GetComponent<EnemyController>()?.RecibirDanio(danioGolpe * 2);
    }

    private void TerminarDash()
    {
        isDashing    = false;
        isInvincible = false;
        rb.linearVelocity = Vector2.zero;
    }

    // ── Ataques ───────────────────────────────────────────────────

    private void GestionarAtaques()
    {
        if (!PuedeAtacar) return;

        if (Input.GetKeyDown(KeyCode.Z))
            Atacar("Golpe", danioGolpe);

        if (Input.GetKeyDown(KeyCode.X))
            Atacar("Pata", danioPata);

        // Liberar energía en área (requiere energía acumulada)
        if (Input.GetKeyDown(KeyCode.C))
            energySystem.LiberarEnergia();
    }

    private void Atacar(string animTrigger, int danio)
    {
        anim.SetTrigger(animTrigger);

        Collider2D[] enemigos = Physics2D.OverlapCircleAll(puntoAtaque.position, radioAtaque, capaEnemigos);
        foreach (var col in enemigos)
            col.GetComponent<EnemyController>()?.RecibirDanio(danio);
    }

    // ── Animaciones ───────────────────────────────────────────────

    private void ActualizarAnimaciones()
    {
        anim.SetFloat("Speed", inputDir.magnitude);
        anim.SetBool("SinPiernas",  !limbSystem.TienePiernas);
        anim.SetBool("SinBrazos",   !limbSystem.TieneBrazos);
        anim.SetBool("ModoCabeza",  limbSystem.EsModoCabeza);
    }

    // ── Recibir daño (llamado desde EnemyController) ──────────────

    public void RecibirDanio(int cantidad)
    {
        if (isInvincible) return;
        limbSystem.AplicarDanio(cantidad);
    }

    // ── Gizmos (debug en editor) ──────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (puntoAtaque == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(puntoAtaque.position, radioAtaque);
    }
}