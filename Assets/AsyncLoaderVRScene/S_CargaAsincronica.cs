using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Events;
using Oculus.Platform;  // Para OVRManager si lo necesitas (opcional)

public class S_CargaAsincronica : MonoBehaviour
{
    [Header("UI - Barra de progreso")]
    [SerializeField] private UnityEngine.UI.Image progressBar;

    [Header("Shader Warmup (Unity 6+ recomendado)")]
    [SerializeField] private GraphicsStateCollection psoCollection;
    [SerializeField] private int psosPerFrame = 32;

    [Header("Cámaras VR - Quest 3")]
    [Tooltip("Referencia al OVR Camera Rig de esta escena de loading")]
    [SerializeField] private OVRCameraRig cameraLoading;

    [Tooltip("Tag del OVR Camera Rig en la escena principal")]
    [SerializeField] private string tagNuevaCamara = "Player";

    [Tooltip("Esperar frames extra después de activar nueva cámara (evita freeze en Quest)")]
    [SerializeField] private bool esperarFramesVR = true;

    [Header("Eventos")]
    [Tooltip("Se ejecuta cuando la escena principal está cargada y activada")]
    public UnityEvent OnMainSceneLoaded;

    [Tooltip("Se ejecuta cuando la escena de loading se descargó")]
    public UnityEvent OnLoadingUnloaded;

    void Start()
    {
        // Buscar cámara de loading si no la arrastraste
        if (cameraLoading == null)
        {
            cameraLoading = FindObjectOfType<OVRCameraRig>();
            if (cameraLoading == null)
                Debug.LogWarning("[VR Loading] No se encontró OVR Camera Rig en loading");
        }

        Debug.Log($"[Carga VR] Iniciando | Principal: '{S_CargadorNivel._NextLevel ?? "NULL"}' | Subs: {S_CargadorNivel._Sublevels.Count}");

        if (string.IsNullOrWhiteSpace(S_CargadorNivel._NextLevel))
        {
            Debug.LogError("[Carga VR] NextLevel vacío → no carga");
            return;
        }

        StartCoroutine(LoadSceneCoroutineVR());
    }

    private IEnumerator LoadSceneCoroutineVR()
    {
        UpdateProgress(0f);

        string main = S_CargadorNivel._NextLevel;
        var subs = S_CargadorNivel._Sublevels;

        // Cargar principal (Additive)
        yield return StartCoroutine(LoadAndActivate(main, LoadSceneMode.Additive, 0f, 0.35f));

        OnMainSceneLoaded?.Invoke();

        // Cargar subs
        float remaining = 0.65f;
        float subWeight = subs.Count > 0 ? remaining / subs.Count : 0f;
        float currentProg = 0.35f;

        foreach (var sub in subs)
        {
            if (string.IsNullOrWhiteSpace(sub)) continue;
            yield return StartCoroutine(LoadAndActivate(sub, LoadSceneMode.Additive, currentProg, subWeight));
            currentProg += subWeight;
        }

        // Warmup shaders
        yield return StartCoroutine(WarmUpShaders());

        UpdateProgress(1f);

        // Cambio crítico de cámaras VR
        yield return StartCoroutine(CambiarCamaraVR(main));

        // Delay visual 100%
        yield return new WaitForSeconds(0.5f);

        // Descargar loading
        Debug.Log("[Carga VR] Descargando loading...");
        var unloadOp = SceneManager.UnloadSceneAsync(gameObject.scene);
        if (unloadOp != null)
        {
            while (!unloadOp.isDone) yield return null;
            Debug.Log("[Carga VR] Loading descargada");
            OnLoadingUnloaded?.Invoke();
        }
        else
        {
            Debug.LogWarning("[Carga VR] Falló unload de loading");
        }
    }

    private IEnumerator LoadAndActivate(string sceneName, LoadSceneMode mode, float progStart, float progWeight)
    {
        Debug.Log($"[Carga] Iniciando '{sceneName}' ({mode})");

        var op = SceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null)
        {
            Debug.LogError($"[Carga] Falló LoadSceneAsync '{sceneName}'");
            yield break;
        }

        while (op.progress < 0.9f)
        {
            float loadProg = Mathf.Clamp01(op.progress / 0.9f);
            UpdateProgress(progStart + loadProg * progWeight);
            yield return null;
        }

        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            UpdateProgress(progStart + progWeight);
            yield return null;
        }

        Debug.Log($"[Carga] '{sceneName}' completada y activada");
    }

    private IEnumerator CambiarCamaraVR(string escenaPrincipalNombre)
    {
        Debug.Log("[Carga VR] Iniciando cambio de cámaras...");

        // Buscar nueva cámara en la escena principal
        GameObject nuevaCamaraGO = BuscadorObjetoPorTag.BuscarEnEscena(escenaPrincipalNombre, tagNuevaCamara);
        if (nuevaCamaraGO == null)
        {
            Debug.LogError($"[Carga VR] No se encontró cámara con tag '{tagNuevaCamara}' en '{escenaPrincipalNombre}'");
            yield break;
        }

        OVRCameraRig nuevaCamara = nuevaCamaraGO.GetComponent<OVRCameraRig>();
        if (nuevaCamara == null)
        {
            Debug.LogError("[Carga VR] Objeto encontrado no es OVRCameraRig");
            yield break;
        }

        // Activar nueva cámara
        nuevaCamaraGO.SetActive(true);
        Debug.Log($"[Carga VR] Nueva cámara ACTIVADA: {nuevaCamaraGO.name}");

        // Esperar frames para reset de tracking en Quest 3
        if (esperarFramesVR)
        {
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
        }

        // Desactivar cámara de loading
        if (cameraLoading != null && cameraLoading.gameObject.activeSelf)
        {
            cameraLoading.gameObject.SetActive(false);
            Debug.Log("[Carga VR] Cámara de loading DESACTIVADA");
        }

        Debug.Log("[Carga VR] Cambio de cámaras completado");
    }

    private IEnumerator WarmUpShaders()
    {
        if (psoCollection != null)
        {
            Debug.Log("[Carga VR] Warmup PSO iniciado");
            var job = psoCollection.WarmUpProgressively(psosPerFrame);
            while (!job.IsCompleted)
            {
                UpdateProgress(0.95f + 0.05f * (Time.time % 1f));
                yield return null;
            }
            Debug.Log("[Carga VR] Warmup completado");
        }
        else
        {
            Debug.LogWarning("[Carga VR] Sin PSO → fallback warmup");
            yield return null;
            Shader.WarmupAllShaders();
        }
    }

    private void UpdateProgress(float normalized)
    {
        if (progressBar != null)
            progressBar.fillAmount = Mathf.Clamp01(normalized);
    }
}