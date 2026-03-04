using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Collections;

//[RequireComponent(typeof(TMP_Text))]
public class TMP_RevealByProgress : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("Coloca el texto que quieres animar")]
    [SerializeField] private TMP_Text tmpText;

    [Header("Control de Revelado - Slider")]
    [Range(0f, 1f)]
    [SerializeField] private float revealProgress = 0f;

    [Header("Modo Timer Automático")]
    [Tooltip("Activa esto si quieres que el texto se escriba solo")]
    [SerializeField] private bool useAutoTimer = false;
    [Tooltip("Tiempo total en segundos para mostrar el 100% del texto")]
    [SerializeField] private float revealDuration = 2.5f;

    [Header("Evento al completar")]
    [Tooltip("Se dispara cuando termina de mostrar todos los caracteres")]
    public UnityEvent Iniciar = new UnityEvent();

    [Header("Evento al completar")]
    [Tooltip("Se dispara cuando termina de mostrar todos los caracteres")]
    public UnityEvent onTextFullyRevealed = new UnityEvent();

    [Header("Delay del evento")]
    [Tooltip("¿Quieres esperar un tiempo antes de disparar el evento?")]
    [SerializeField] private bool useDelay = false;
    [Tooltip("Tiempo de espera (segundos)")]
    [SerializeField] private float delayBeforeEvent = 0.6f;


    // === PROPIEDAD PÚBLICA PARA TIMELINE ===
    public float RevealProgress
    {
        get => revealProgress;
        set
        {
            revealProgress = Mathf.Clamp01(value);
            ApplyReveal();

            if (revealProgress < 1f)
                eventAlreadyInvoked = false;
        }
    }
    

    private int totalCharacters = 0;
    private bool eventAlreadyInvoked = false;
    private Coroutine delayCoroutine;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        RefreshTextLength();
    }
    private void Start()
    {
        Iniciar.Invoke();
    }

    private void OnEnable()
    {
        if (useAutoTimer) RevealProgress = 0f; // reinicia al activar
    }

    private void Update()
    {
        if (useAutoTimer && revealProgress < 1f)
        {
            RevealProgress += Time.deltaTime / revealDuration;
        }

        // Detecta cuando llega al 100%
        if (revealProgress >= 1f && !eventAlreadyInvoked)
        {
            TriggerCompletion();
        }
    }

    private void OnValidate()
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        RefreshTextLength();
        ApplyReveal();
    }

    private void ApplyReveal()
    {
        if (tmpText == null || totalCharacters == 0) return;
        tmpText.maxVisibleCharacters = Mathf.CeilToInt(totalCharacters * revealProgress);
    }

    /// <summary>
    /// Actualiza el conteo de caracteres (llámalo si cambias el texto en runtime)
    /// </summary>
    public void RefreshTextLength()
    {
        if (tmpText == null) return;
        tmpText.ForceMeshUpdate(true);
        totalCharacters = tmpText.textInfo.characterCount;
        ApplyReveal();
    }

    private void TriggerCompletion()
    {
        eventAlreadyInvoked = true;

        if (useDelay && delayBeforeEvent > 0f)
        {
            if (delayCoroutine != null) StopCoroutine(delayCoroutine);
            delayCoroutine = StartCoroutine(InvokeEventWithDelay());
        }
        else
        {
            onTextFullyRevealed?.Invoke();
        }
    }

    private IEnumerator InvokeEventWithDelay()
    {
        yield return new WaitForSeconds(delayBeforeEvent);
        onTextFullyRevealed?.Invoke();
    }

    // Métodos útiles extra
    public void ResetReveal() => RevealProgress = 0f;

    public void SetText(string newText)
    {
        if (tmpText != null)
        {
            tmpText.text = newText;
            RefreshTextLength();
            RevealProgress = 0f;
        }
    }
}