using System.Collections.Generic;
using UnityEngine;

public class CocheLoop : MonoBehaviour
{
    private static readonly List<CocheLoop> todosLosCoches = new List<CocheLoop>();

    [Header("Recorrido (coordenadas mundo)")]
    public Vector3 puntoInicio;
    public Vector3 puntoFinal;

    [Header("Movimiento")]
    public float velocidad = 5f;
    public float distanciaMinimaLlegada = 0.1f;

    [Header("Orientación")]
    public Vector3 rotacionOffsetEuler;

    [Header("Semáforo")]
    public SemaforoSimpleBlink semaforo;
    public bool obedecerSemaforo = true;

    [Header("Carril")]
    [Tooltip("Los 2 coches del mismo carril deben compartir este ID")]
    public int carrilId = 0;

    [Header("Parada del coche delantero")]
    [Tooltip("Posición Z donde se debe parar el primer coche del carril")]
    public float zParadaPrimerCoche = 0f;

    [Tooltip("Separación entre el coche delantero y el trasero al parar")]
    public float separacionEntreCochesStop = 8f;

    [Tooltip("Margen para considerar alcanzado el punto de parada")]
    public float margenParada = 0.2f;

    [Header("Zona crítica del cruce")]
    public float zonaCriticaZMin = 0f;
    public float zonaCriticaZMax = 0f;

    [Header("Debug runtime")]
    public int indiceColaActual = 0;   // 0 = delantero, 1 = trasero
    public bool estaParadoPorSemaforo = false;
    public bool estaParadoPorCola = false;

    private bool haEntradoEnZonaCritica = false;

    void OnEnable()
    {
        if (!todosLosCoches.Contains(this))
            todosLosCoches.Add(this);
    }

    void OnDisable()
    {
        todosLosCoches.Remove(this);
    }

    void Start()
    {
        transform.position = puntoInicio;
        MirarHaciaDestino();
    }

    void Update()
    {
        ActualizarEstadoZonaCritica();
        indiceColaActual = CalcularIndiceColaAutomatico();

        estaParadoPorSemaforo = false;
        estaParadoPorCola = false;

        if (DebePararseAhora())
            return;

        MoverHaciaDestino();

        if (HaLlegadoAlFinal())
        {
            ReiniciarEnInicio();
        }
    }

    void ActualizarEstadoZonaCritica()
    {
        float minZ = Mathf.Min(zonaCriticaZMin, zonaCriticaZMax);
        float maxZ = Mathf.Max(zonaCriticaZMin, zonaCriticaZMax);

        if (transform.position.z >= minZ && transform.position.z <= maxZ)
        {
            haEntradoEnZonaCritica = true;
        }
    }

    bool DebePararseAhora()
    {
        // Coche delantero
        if (indiceColaActual == 0)
        {
            bool parar = DebePararComoPrimero();
            estaParadoPorSemaforo = parar;
            return parar;
        }

        // Coche trasero
        bool pararCola = DebePararComoSegundo();
        estaParadoPorCola = pararCola;
        return pararCola;
    }

    bool DebePararComoPrimero()
    {
        if (!obedecerSemaforo || semaforo == null)
            return false;

        if (!semaforo.HayStopActivo())
            return false;

        if (haEntradoEnZonaCritica)
            return false;

        if (!semaforo.EstaEnZonaControl(transform.position.z))
            return false;

        float zObjetivo = zParadaPrimerCoche;
        return YaHaLlegadoAOHaSobrepasado(zObjetivo);
    }

    bool DebePararComoSegundo()
    {
        CocheLoop primero = ObtenerPrimerCocheDelCarril();
        if (primero == null || primero == this)
            return false;

        // El segundo solo se para si el primero está efectivamente parado por semáforo
        // o ya está prácticamente en su punto de parada.
        bool primeroEstaHaciendoCola =
            primero.estaParadoPorSemaforo ||
            primero.YaHaLlegadoAOHaSobrepasado(primero.zParadaPrimerCoche);

        if (!primeroEstaHaciendoCola)
            return false;

        float direccionZ = Mathf.Sign(puntoFinal.z - puntoInicio.z);

        // Si vamos hacia Z menor, el segundo va a una Z mayor.
        // Si vamos hacia Z mayor, el segundo va a una Z menor.
        float zObjetivoSegundo = primero.transform.position.z - direccionZ * separacionEntreCochesStop;

        return YaHaLlegadoAOHaSobrepasado(zObjetivoSegundo);
    }

    bool YaHaLlegadoAOHaSobrepasado(float zObjetivo)
    {
        // Ruta hacia Z decreciente
        if (puntoFinal.z < puntoInicio.z)
        {
            return transform.position.z <= zObjetivo + margenParada;
        }
        // Ruta hacia Z creciente
        else
        {
            return transform.position.z >= zObjetivo - margenParada;
        }
    }

    int CalcularIndiceColaAutomatico()
    {
        CocheLoop otro = ObtenerOtroCocheDelMismoCarril();
        if (otro == null)
            return 0;

        float miProgreso = CalcularProgresoEnRuta(transform.position);
        float progresoOtro = CalcularProgresoEnRuta(otro.transform.position);

        return miProgreso >= progresoOtro ? 0 : 1;
    }

    CocheLoop ObtenerOtroCocheDelMismoCarril()
    {
        for (int i = 0; i < todosLosCoches.Count; i++)
        {
            CocheLoop coche = todosLosCoches[i];

            if (coche == null || coche == this)
                continue;

            if (coche.carrilId == carrilId)
                return coche;
        }

        return null;
    }

    CocheLoop ObtenerPrimerCocheDelCarril()
    {
        CocheLoop primero = null;
        float mejorProgreso = float.MinValue;

        for (int i = 0; i < todosLosCoches.Count; i++)
        {
            CocheLoop coche = todosLosCoches[i];

            if (coche == null)
                continue;

            if (coche.carrilId != carrilId)
                continue;

            float progreso = CalcularProgresoEnRuta(coche.transform.position);

            if (progreso > mejorProgreso)
            {
                mejorProgreso = progreso;
                primero = coche;
            }
        }

        return primero;
    }

    float CalcularProgresoEnRuta(Vector3 posicionMundo)
    {
        Vector3 inicioPlano = new Vector3(puntoInicio.x, 0f, puntoInicio.z);
        Vector3 finalPlano = new Vector3(puntoFinal.x, 0f, puntoFinal.z);
        Vector3 posPlano = new Vector3(posicionMundo.x, 0f, posicionMundo.z);

        Vector3 dirRuta = finalPlano - inicioPlano;
        float longitudRuta = dirRuta.magnitude;

        if (longitudRuta < 0.001f)
            return 0f;

        dirRuta /= longitudRuta;

        return Vector3.Dot(posPlano - inicioPlano, dirRuta);
    }

    void MoverHaciaDestino()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            puntoFinal,
            velocidad * Time.deltaTime
        );
    }

    bool HaLlegadoAlFinal()
    {
        return Vector3.Distance(transform.position, puntoFinal) <= distanciaMinimaLlegada;
    }

    void ReiniciarEnInicio()
    {
        transform.position = puntoInicio;
        haEntradoEnZonaCritica = false;
        MirarHaciaDestino();
    }

    void MirarHaciaDestino()
    {
        Vector3 direccion = puntoFinal - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.0001f)
        {
            Quaternion rotBase = Quaternion.LookRotation(direccion.normalized);
            Quaternion offset = Quaternion.Euler(rotacionOffsetEuler);
            transform.rotation = rotBase * offset;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(puntoInicio, puntoFinal);
        Gizmos.DrawSphere(puntoInicio, 0.4f);
        Gizmos.DrawSphere(puntoFinal, 0.4f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 2f, transform.position.y + 0.2f, zParadaPrimerCoche),
            new Vector3(transform.position.x + 2f, transform.position.y + 0.2f, zParadaPrimerCoche)
        );
    }
}