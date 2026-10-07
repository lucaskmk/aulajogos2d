using System.Collections;
using UnityEngine;

// 'R' - uma bandeira que FOGE quando você chega perto. Ela pula até o '*' do mapa
// e só depois disso dá pra pegar.
[RequireComponent(typeof(Bandeira))]
public class BandeiraFujona : MonoBehaviour
{
    public Vector3? destino;
    public float distanciaParaFugir = 3f;
    public float tempoDoPulo = 0.8f;
    public float alturaDoPulo = 4f;

    Bandeira bandeira;
    bool fugiu;

    void Awake()
    {
        bandeira = GetComponent<Bandeira>();
        bandeira.podeTocar = false;
    }

    void Start()
    {
        if (!destino.HasValue) bandeira.podeTocar = true; // sem '*' no mapa: vira bandeira normal

        // Já venceu a Baleia Branca e morreu depois? A bandeira já está lá na frente, te esperando.
        // Já fugiu antes: o chefe foi vencido, ou você renasceu num checkpoint DEPOIS dela
        // (a fase é remontada a cada morte; sem isso ela voltava para trás do checkpoint e sumia do fim).
        else if (GerenciadorDoJogo.Instancia.ChefeDerrotado || RenasceuDepoisDela())
        {
            transform.position = destino.Value;
            fugiu = true;
            bandeira.podeTocar = true;
        }
    }

    bool RenasceuDepoisDela()
    {
        Vector3? save = GerenciadorDoJogo.Instancia.PontoDeSave;
        return save.HasValue && save.Value.x > transform.position.x;
    }

    void Update()
    {
        if (fugiu || !destino.HasValue || !GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return;

        Vector2 baseDaBandeira = transform.position + Vector3.up;
        if (Vector2.Distance(jogador, baseDaBandeira) < distanciaParaFugir)
            StartCoroutine(Fugir());
    }

    IEnumerator Fugir()
    {
        fugiu = true;
        GerenciadorDoJogo.Som("risada");

        Vector3 inicio = transform.position;
        Vector3 fim = destino.Value;
        for (float t = 0f; t < tempoDoPulo; t += Time.deltaTime)
        {
            float p = t / tempoDoPulo;
            transform.position = Vector3.Lerp(inicio, fim, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * alturaDoPulo;
            yield return null;
        }
        transform.position = fim;
        bandeira.podeTocar = true;
    }
}
