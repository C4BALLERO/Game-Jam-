using UnityEngine;
using UnityEngine.Events;

// ================================================================
//  LimbSystem.cs
//  Gestiona las extremidades del jugador. Cada parte del cuerpo
//  tiene su propio estado. Al perder todo → Modo Cabeza.
// ================================================================

public class LimbSystem : MonoBehaviour
{
    // ── Salud por extremidad ──────────────────────────────────────
    [Header("Salud de extremidades")]
    [SerializeField] private int vidaBrazoIzq  = 30;
    [SerializeField] private int vidaBrazoDer  = 30;
    [SerializeField] private int vidaPiernaIzq = 30;
    [SerializeField] private int vidaPiernaDer = 30;
    [SerializeField] private int vidaTorso     = 50;

    [Header("Sprites por estado (opcional)")]
    [SerializeField] private SpriteRenderer spriteJugador;
    [SerializeField] private Sprite spriteLleno;
    [SerializeField] private Sprite spriteSinBrazoIzq;
    [SerializeField] private Sprite spriteSinBrazoDer;
    [SerializeField] private Sprite spriteSinBrazos;
    [SerializeField] private Sprite spriteSinPiernas;
    [SerializeField] private Sprite spriteCabeza;

    // ── Eventos ───────────────────────────────────────────────────
    [HideInInspector] public UnityEvent OnCuerpoActualizado;
    [HideInInspector] public UnityEvent OnModoCabeza;
    [HideInInspector] public UnityEvent OnGameOver;

    // ── Estado interno ────────────────────────────────────────────
    private int hpBrazoIzq;
    private int hpBrazoDer;
    private int hpPiernaIzq;
    private int hpPiernaDer;
    private int hpTorso;

    // ── Propiedades públicas (PlayerController las lee) ───────────
    public bool TieneBrazoIzq  => hpBrazoIzq  > 0;
    public bool TieneBrazoDer  => hpBrazoDer  > 0;
    public bool TienePiernaIzq => hpPiernaIzq > 0;
    public bool TienePiernaDer => hpPiernaDer > 0;
    public bool TieneTorso     => hpTorso     > 0;

    public bool TieneBrazos  => TieneBrazoIzq || TieneBrazoDer;
    public bool TienePiernas => TienePiernaIzq || TienePiernaDer;
    public bool PiernaParcial => TienePiernaIzq != TienePiernaDer; // cojea

    // Modo cabeza: perdió brazos, piernas y torso
    public bool EsModoCabeza => !TieneBrazos && !TienePiernas && !TieneTorso;

    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        OnCuerpoActualizado ??= new UnityEvent();
        OnModoCabeza        ??= new UnityEvent();
        OnGameOver          ??= new UnityEvent();
        Resetear();
    }

    public void Resetear()
    {
        hpBrazoIzq  = vidaBrazoIzq;
        hpBrazoDer  = vidaBrazoDer;
        hpPiernaIzq = vidaPiernaIzq;
        hpPiernaDer = vidaPiernaDer;
        hpTorso     = vidaTorso;
        ActualizarSprite();
    }

    // ── Recibir daño ──────────────────────────────────────────────
    // El daño se distribuye por prioridad: brazos → piernas → torso

    public void AplicarDanio(int cantidad)
    {
        int restante = cantidad;
        restante = DaniarExtremidad(ref hpBrazoIzq,  restante);
        restante = DaniarExtremidad(ref hpBrazoDer,  restante);
        restante = DaniarExtremidad(ref hpPiernaIzq, restante);
        restante = DaniarExtremidad(ref hpPiernaDer, restante);
        restante = DaniarExtremidad(ref hpTorso,     restante);

        ActualizarEstado();
    }

    private int DaniarExtremidad(ref int hp, int danio)
    {
        if (hp <= 0 || danio <= 0) return danio;
        int exceso = Mathf.Max(0, danio - hp);
        hp = Mathf.Max(0, hp - danio);
        return exceso; // el exceso pasa a la siguiente extremidad
    }

    // ── Perder extremidad aleatoria (por sobrecarga de energía) ───

    public void PerderExtremidadAleatoria()
    {
        // Arma una lista con las que aún existen
        System.Collections.Generic.List<System.Action> disponibles = new();

        if (TieneBrazoIzq)  disponibles.Add(() => hpBrazoIzq  = 0);
        if (TieneBrazoDer)  disponibles.Add(() => hpBrazoDer  = 0);
        if (TienePiernaIzq) disponibles.Add(() => hpPiernaIzq = 0);
        if (TienePiernaDer) disponibles.Add(() => hpPiernaDer = 0);
        if (TieneTorso)     disponibles.Add(() => hpTorso     = 0);

        if (disponibles.Count == 0) return;

        int idx = Random.Range(0, disponibles.Count);
        disponibles[idx].Invoke();

        ActualizarEstado();
    }

    // ── Recuperar extremidades (al matar enemigos) ────────────────

    public void RecuperarExtremidad(int cantidadVida)
    {
        // Recupera en orden inverso: torso → piernas → brazos
        if (!TieneTorso)     { hpTorso     = Mathf.Min(cantidadVida, vidaTorso);     ActualizarEstado(); return; }
        if (!TienePiernaDer) { hpPiernaDer = Mathf.Min(cantidadVida, vidaPiernaDer); ActualizarEstado(); return; }
        if (!TienePiernaIzq) { hpPiernaIzq = Mathf.Min(cantidadVida, vidaPiernaIzq); ActualizarEstado(); return; }
        if (!TieneBrazoDer)  { hpBrazoDer  = Mathf.Min(cantidadVida, vidaBrazoDer);  ActualizarEstado(); return; }
        if (!TieneBrazoIzq)  { hpBrazoIzq  = Mathf.Min(cantidadVida, vidaBrazoIzq);  ActualizarEstado(); return; }
    }

    // ── Lógica de estado ──────────────────────────────────────────

    private void ActualizarEstado()
    {
        OnCuerpoActualizado.Invoke();
        ActualizarSprite();

        if (EsModoCabeza)
        {
            OnModoCabeza.Invoke();
            // Iniciar cuenta regresiva para Game Over (el GameManager escucha esto)
        }
    }

    private void ActualizarSprite()
    {
        if (spriteJugador == null) return;

        if (EsModoCabeza)
        {
            spriteJugador.sprite = spriteCabeza;
            return;
        }
        if (!TienePiernas)   { spriteJugador.sprite = spriteSinPiernas;  return; }
        if (!TieneBrazos)    { spriteJugador.sprite = spriteSinBrazos;   return; }
        if (!TieneBrazoIzq)  { spriteJugador.sprite = spriteSinBrazoIzq; return; }
        if (!TieneBrazoDer)  { spriteJugador.sprite = spriteSinBrazoDer; return; }

        spriteJugador.sprite = spriteLleno;
    }

    // ── Debug en Inspector ────────────────────────────────────────

    public string EstadoCuerpo() =>
        $"BI:{hpBrazoIzq} BD:{hpBrazoDer} PI:{hpPiernaIzq} PD:{hpPiernaDer} T:{hpTorso} | Cabeza:{EsModoCabeza}";

    private void OnGUI()
    {
#if UNITY_EDITOR
        GUI.Label(new Rect(10, 10, 400, 20), EstadoCuerpo());
#endif
    }
}