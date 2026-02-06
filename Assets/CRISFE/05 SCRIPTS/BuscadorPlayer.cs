using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class BuscadorObjetoPorTag
{
    /// <summary>
    /// Busca el PRIMER GameObject con el tag indicado en la escena especificada.
    /// Encuentra objetos aunque estén desactivados (incluyendo si un padre está desactivado).
    /// </summary>
    public static GameObject BuscarEnEscena(string nombreEscena, string tagBuscado)
    {
        if (string.IsNullOrWhiteSpace(nombreEscena))
        {
            Debug.LogWarning("[Buscador] Nombre de escena vacío");
            return null;
        }

        if (string.IsNullOrWhiteSpace(tagBuscado))
        {
            Debug.LogWarning("[Buscador] Tag vacío");
            return null;
        }

        Scene escena = SceneManager.GetSceneByName(nombreEscena);

        if (!escena.IsValid() || !escena.isLoaded)
        {
            Debug.LogWarning($"[Buscador] Escena '{nombreEscena}' no está cargada o no existe");
            return null;
        }

        Debug.Log($"[Buscador] Buscando tag '{tagBuscado}' en '{nombreEscena}' (incluye desactivados)");

        GameObject[] roots = escena.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            // Buscamos incluso si el root está desactivado
            GameObject encontrado = BuscarEnHijosIncluyendoDesactivados(root.transform, tagBuscado);
            if (encontrado != null)
            {
                Debug.Log($"[Buscador] Encontrado: {encontrado.name} (activo: {encontrado.activeInHierarchy})");
                return encontrado;
            }
        }

        Debug.Log($"[Buscador] No encontrado tag '{tagBuscado}' en '{nombreEscena}'");
        return null;
    }

    /// <summary>
    /// Versión que devuelve TODOS los objetos con ese tag (incluso desactivados)
    /// </summary>
    public static List<GameObject> BuscarTodosEnEscena(string nombreEscena, string tagBuscado)
    {
        var lista = new List<GameObject>();

        if (string.IsNullOrWhiteSpace(nombreEscena) || string.IsNullOrWhiteSpace(tagBuscado))
            return lista;

        Scene escena = SceneManager.GetSceneByName(nombreEscena);
        if (!escena.IsValid() || !escena.isLoaded)
            return lista;

        GameObject[] roots = escena.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            BuscarTodosEnHijosIncluyendoDesactivados(root.transform, tagBuscado, lista);
        }

        Debug.Log($"[Buscador] Encontrados {lista.Count} objetos con tag '{tagBuscado}' en '{nombreEscena}'");
        return lista;
    }

    // Recursivo - encuentra el primero (incluye desactivados)
    private static GameObject BuscarEnHijosIncluyendoDesactivados(Transform current, string tagBuscado)
    {
        // Verificamos este transform (aunque esté desactivado)
        if (current.CompareTag(tagBuscado))
        {
            return current.gameObject;
        }

        // Recorremos TODOS los hijos, sin importar estado
        for (int i = 0; i < current.childCount; i++)
        {
            Transform child = current.GetChild(i);
            GameObject encontrado = BuscarEnHijosIncluyendoDesactivados(child, tagBuscado);
            if (encontrado != null)
                return encontrado;
        }

        return null;
    }

    // Recursivo - encuentra todos (incluye desactivados)
    private static void BuscarTodosEnHijosIncluyendoDesactivados(Transform current, string tagBuscado, List<GameObject> lista)
    {
        if (current.CompareTag(tagBuscado))
        {
            lista.Add(current.gameObject);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform child = current.GetChild(i);
            BuscarTodosEnHijosIncluyendoDesactivados(child, tagBuscado, lista);
        }
    }
}