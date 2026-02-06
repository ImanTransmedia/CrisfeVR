using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S_CargadorNivel
{
    public static string _NextLevel;
    public static List<string> _Sublevels = new List<string>();

    public static void LoadLevel(
        string Level,
        List<string> Sublevels = null,          // null o lista vacía → no subs
        string LoadingScene = "S_Loading"
    )
    {
        _NextLevel = Level;
        _Sublevels.Clear();

        if (Sublevels != null)
        {
            foreach (var sub in Sublevels)
            {
                if (!string.IsNullOrWhiteSpace(sub))
                    _Sublevels.Add(sub);
                else
                    Debug.LogWarning($"[S_CargadorNivel] Ignorando subnivel vacío/null en la lista");
            }
        }

        Debug.Log($"[S_CargadorNivel] LoadLevel llamado → NextLevel: '{_NextLevel ?? "NULL"}' | Sublevels: {_Sublevels.Count}");
        foreach (var s in _Sublevels) Debug.Log("   - " + s);

        if (string.IsNullOrWhiteSpace(_NextLevel))
        {
            Debug.LogError("[S_CargadorNivel] ¡NextLevel vacío! Abortando.");
            return;
        }

        SceneManager.LoadScene(LoadingScene);
    }
}