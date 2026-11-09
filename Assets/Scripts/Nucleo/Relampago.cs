using UnityEngine;

// O RELÂMPAGO da tempestade (modo difícil): de tempos em tempos a tela clareia de uma vez e apaga (quem aplica é o
// EfeitosDeTela, na exposição) e, logo depois, vem o trovão. Um clarão só por vez, com pelo menos 4 s entre eles:
// tempestade, não luz estroboscópica.
public class Relampago : MonoBehaviour
{
    public static float Brilho { get; private set; } // 0 = nada, 1 = o clarão cheio

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => Brilho = 0f;

    float esperaDoClarao = 2.5f;
    float esperaDoTrovao = -1f;

    public static void Criar(Transform raiz)
    {
        var objeto = new GameObject("Relampago");
        objeto.transform.SetParent(raiz, false);
        objeto.AddComponent<Relampago>();
    }

    void Update()
    {
        float dt = Time.deltaTime; // na pausa é 0: congela sozinho
        Brilho = Mathf.MoveTowards(Brilho, 0f, 3.5f * dt);

        // (a fase continua montada atrás das cutscenes e da tela de vitória: ali não troveja)
        EstadoDoJogo estado = GerenciadorDoJogo.Instancia.Estado;
        if (estado != EstadoDoJogo.Jogando && estado != EstadoDoJogo.Mapa) return;

        esperaDoClarao -= dt;
        if (esperaDoClarao <= 0f)
        {
            esperaDoClarao = Random.Range(4f, 10f);
            esperaDoTrovao = Random.Range(0.2f, 0.8f); // o som chega depois da luz
            Brilho = 1f;
        }
        if (esperaDoTrovao > 0f)
        {
            esperaDoTrovao -= dt;
            if (esperaDoTrovao <= 0f) GerenciadorDoJogo.Som("trovao", 0.6f);
        }
    }

    void OnDestroy() => Brilho = 0f;
}
