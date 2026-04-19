using System.Collections;
using UnityEngine;

public class SemaforoSimpleBlink : MonoBehaviour
{
    [Header("Configuración de Luces (Mesh Renderers)")]
    public MeshRenderer esferaRoja;
    public MeshRenderer esferaVerde;
    public MeshRenderer esferaAmbar;

    [Header("Materiales")]
    public Material matRojo;
    public Material matVerde;
    public Material matNegro;

    [Header("Tiempos de Duración (en segundos)")]
    public float tiempoEnRojo = 10f;
    public float tiempoEnVerdeTotal = 20f;

    [Header("Configuración del Parpadeo Verde")]
    public float tiempoParpadeoFinal = 5f;
    [Range(0.1f, 1f)] public float velocidadParpadeo = 0.3f;

    [Header("Lógica STOP")]
    [Tooltip("Cuántos segundos antes de ponerse en rojo se activa STOP")]
    public float tiempoStopAntesDeRojo = 2f;

    [Tooltip("Cuántos segundos después de ponerse en verde se mantiene STOP")]
    public float tiempoRetrasoSalidaEnVerde = 2f;

    [Tooltip("Zona cercana al paso de cebra donde los coches deben tener en cuenta el STOP")]
    public float rangoZMin = 0f;
    public float rangoZMax = 0f;

    [Header("Estado actual (solo lectura en runtime)")]
    public bool stopActivo = false;

    void Start()
    {
        if (esferaAmbar != null)
            esferaAmbar.material = matNegro;

        if (tiempoParpadeoFinal >= tiempoEnVerdeTotal)
        {
            Debug.LogWarning("El tiempo de parpadeo final es mayor o igual que el tiempo total en verde. Se ajustará automáticamente.");
            tiempoParpadeoFinal = Mathf.Max(0.1f, tiempoEnVerdeTotal - velocidadParpadeo);
        }

        if (tiempoStopAntesDeRojo > tiempoEnVerdeTotal)
        {
            Debug.LogWarning("tiempoStopAntesDeRojo no puede ser mayor que tiempoEnVerdeTotal. Se ajustará automáticamente.");
            tiempoStopAntesDeRojo = tiempoEnVerdeTotal;
        }

        if (tiempoRetrasoSalidaEnVerde > tiempoEnVerdeTotal)
        {
            Debug.LogWarning("tiempoRetrasoSalidaEnVerde no puede ser mayor que tiempoEnVerdeTotal. Se ajustará automáticamente.");
            tiempoRetrasoSalidaEnVerde = tiempoEnVerdeTotal;
        }

        StartCoroutine(CicloSemaforo());
    }

    IEnumerator CicloSemaforo()
    {
        while (true)
        {
            // =========================
            // ROJO
            // =========================
            EncenderRojo();
            stopActivo = true;
            yield return new WaitForSeconds(tiempoEnRojo);

            // =========================
            // VERDE (con retraso de salida)
            // =========================
            EncenderVerdeFijo();

            // Aunque esté verde, mantenemos STOP unos segundos
            stopActivo = true;

            if (tiempoRetrasoSalidaEnVerde > 0f)
                yield return new WaitForSeconds(tiempoRetrasoSalidaEnVerde);

            // Ya pueden arrancar
            stopActivo = false;

            // Tiempo verde libre restante antes del STOP final
            float tiempoAntesStop =
                tiempoEnVerdeTotal
                - tiempoRetrasoSalidaEnVerde
                - tiempoStopAntesDeRojo;

            if (tiempoAntesStop > 0f)
                yield return new WaitForSeconds(tiempoAntesStop);

            // =========================
            // ÚLTIMOS SEGUNDOS ANTES DE ROJO
            // =========================
            stopActivo = true;

            float tiempoRestanteVerde = tiempoStopAntesDeRojo;
            float tiempoParpadeoReal = Mathf.Min(tiempoParpadeoFinal, tiempoRestanteVerde);
            float tiempoVerdeFijoFinal = tiempoRestanteVerde - tiempoParpadeoReal;

            if (tiempoVerdeFijoFinal > 0f)
            {
                esferaVerde.material = matVerde;
                yield return new WaitForSeconds(tiempoVerdeFijoFinal);
            }

            float tiempoParpadeado = 0f;
            bool estaEncendido = true;

            while (tiempoParpadeado < tiempoParpadeoReal)
            {
                esferaVerde.material = estaEncendido ? matVerde : matNegro;
                estaEncendido = !estaEncendido;

                yield return new WaitForSeconds(velocidadParpadeo);
                tiempoParpadeado += velocidadParpadeo;
            }

            esferaVerde.material = matNegro;
        }
    }

    void EncenderRojo()
    {
        esferaRoja.material = matRojo;
        esferaVerde.material = matNegro;
    }

    void EncenderVerdeFijo()
    {
        esferaRoja.material = matNegro;
        esferaVerde.material = matVerde;
    }

    public bool EstaEnZonaControl(float zGlobal)
    {
        float minZ = Mathf.Min(rangoZMin, rangoZMax);
        float maxZ = Mathf.Max(rangoZMin, rangoZMax);
        return zGlobal >= minZ && zGlobal <= maxZ;
    }

    public bool HayStopActivo()
    {
        return stopActivo;
    }
}