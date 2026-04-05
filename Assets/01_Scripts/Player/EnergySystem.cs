using UnityEngine;
using UnityEngine.Events;

// ================================================================
//  EnergySystem.cs
//  La energía sube sola. Si llega al máximo sin liberarse,
//  el jugador pierde una extremidad (LimbSystem lo gestiona).
// ================================================================

public class EnergySystem : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────
    [Header("Energía")]
    [SerializeField] private float energiaMaxima    = 100f;
    [SerializeField] private float tasaAumento      = 8f;   // unidades por segundo
    [SerializeField] private float costoLiberacion  = 40f;  // mínimo para liberar
    [SerializeField] private float radioLiberacion  = 4f;
    [SerializeField] private int   danioLiberacion  = 30;

    [Header("Poderes activos")]
    [SerializeField] private bool  vuelo            = false;
    [SerializeField] private bool  rayoLaser        = false;
    [SerializeField] private float multiplicadorDanio = 1f;

    [Header("Efectos")]
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private GameObject fxLiberacion; // partícula/efecto en área

    // ── Eventos ───────────────────────────────────────────────────
    [HideInInspector] public UnityEvent<float> OnEnergiaChanged; // 0..1 normalizado
    [HideInInspector] public UnityEvent        OnSobrecarga;

    // ── Estado ───────────────────────────────────────────────────
    private float energiaActual;
    private bool  sobrecargaDisparada;
    private LimbSystem limbSystem;

    public float EnergiaActual    => energiaActual;
    public float EnergiaNormalizada => energiaActual / energiaMaxima;
    public bool  PuedeLiberar     => energiaActual >= costoLiberacion;
    public float MultiplicadorDanio => multiplicadorDanio;

    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        limbSystem = GetComponent<LimbSystem>();
        OnEnergiaChanged ??= new UnityEvent<float>();
        OnSobrecarga     ??= new UnityEvent();
    }

    private void Update()
    {
        AumentarEnergia();
        VerificarSobrecarga();
    }

    // ── Aumento automático ────────────────────────────────────────

    private void AumentarEnergia()
    {
        if (energiaActual >= energiaMaxima) return;

        energiaActual = Mathf.Min(energiaActual + tasaAumento * Time.deltaTime, energiaMaxima);
        OnEnergiaChanged.Invoke(EnergiaNormalizada);
    }

    // ── Sobrecarga ────────────────────────────────────────────────

    private void VerificarSobrecarga()
    {
        if (energiaActual >= energiaMaxima && !sobrecargaDisparada)
        {
            sobrecargaDisparada = true;
            OnSobrecarga.Invoke();
            limbSystem.PerderExtremidadAleatoria(); // ¡el cuerpo explota!
            energiaActual = energiaMaxima * 0.5f;   // se descarga a la mitad
            sobrecargaDisparada = false;
        }
    }

    // ── Liberación de energía (input del jugador) ─────────────────

    public void LiberarEnergia()
    {
        if (!PuedeLiberar) return;

        energiaActual -= costoLiberacion;
        OnEnergiaChanged.Invoke(EnergiaNormalizada);

        // Daño en área
        Collider2D[] enemigos = Physics2D.OverlapCircleAll(transform.position, radioLiberacion, capaEnemigos);
        foreach (var col in enemigos)
            col.GetComponent<EnemyController>()?.RecibirDanio(
                Mathf.RoundToInt(danioLiberacion * multiplicadorDanio)
            );

        // Efecto visual
        if (fxLiberacion != null)
            Instantiate(fxLiberacion, transform.position, Quaternion.identity);
    }

    // ── Poderes ───────────────────────────────────────────────────

    public void ActivarPoder(TipoPoder poder)
    {
        switch (poder)
        {
            case TipoPoder.AumentoDanio:
                multiplicadorDanio = 2f;
                break;
            case TipoPoder.Vuelo:
                vuelo = true;
                GetComponent<Rigidbody2D>().gravityScale = 0f;
                break;
            case TipoPoder.RayoLaser:
                rayoLaser = true;
                break;
            case TipoPoder.MejoraDash:
                // PlayerController lo lee directamente desde aquí si quieres
                break;
        }
    }

    // ── Añadir energía (p.ej. al matar enemigos) ──────────────────

    public void AgregarEnergia(float cantidad)
    {
        energiaActual = Mathf.Clamp(energiaActual + cantidad, 0f, energiaMaxima);
        OnEnergiaChanged.Invoke(EnergiaNormalizada);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radioLiberacion);
    }
}

public enum TipoPoder { AumentoDanio, Vuelo, RayoLaser, MejoraDash }