using UnityEngine;

// PRIMEIRO PLANO desfocado: capim e folhas BEM perto da "lente", escuros e borrados,
// passando na frente da fase mais rápido que o chão (paralaxe com fator negativo).
// É o que faz a fase parecer uma maquete fotografada (o tilt-shift do HD-2D):
// longe borrado -> fase nítida -> perto borrado.
//
// Diferente das camadas da Paralaxe (que cobrem a fase inteira com pedaços), aqui só existem os
// pedaços que cabem na tela: quando um sai por um lado, ele é reaproveitado do outro.
// A faixa fica presa à beirada de baixo (ou de cima) da TELA, então funciona com qualquer altura de fase.
[DefaultExecutionOrder(105)] // depois da câmera (CameraSeguir)
public class PrimeiroPlano : MonoBehaviour
{
    float fator;          // negativo: passa na frente da fase, mais rápido que ela
    bool emCima;          // presa no alto da tela (galhos) ou embaixo (capim)
    float escondido;      // quantos blocos do desenho ficam para fora da tela (a base cheia do capim, o galho grosso)
    float largura;        // largura de um pedaço, em blocos
    float deslocamentoX;  // para o capim e o galho não começarem no mesmo lugar
    Transform[] pedacos;

    // sprite: desenho que se repete; ordem: na frente de tudo (40); escondido: em blocos.
    public static PrimeiroPlano Criar(Transform raiz, string nome, string sprite, float fator, bool emCima, Color cor, float escondido, float deslocamentoX = 0f)
    {
        var objeto = new GameObject("Primeiro plano " + nome);
        objeto.transform.SetParent(raiz, false);
        var plano = objeto.AddComponent<PrimeiroPlano>();
        plano.fator = fator;
        plano.emCima = emCima;
        plano.escondido = escondido;

        Sprite desenho = FabricaDeSprites.Pegar(sprite);
        plano.largura = desenho.bounds.size.x;

        // quantos pedaços cobrem a tela mais larga que der para ter (com folga de um de cada lado).
        // (a câmera ainda pode estar com o tamanho da fase anterior: por isso o mínimo de 12)
        Camera cam = Camera.main;
        float meiaAltura = Mathf.Max(cam != null ? cam.orthographicSize : 0f, 12f);
        float larguraDaTela = meiaAltura * 2f * Mathf.Max(cam != null ? cam.aspect : 0f, 2.4f);
        int quantos = Mathf.CeilToInt(larguraDaTela / plano.largura) + 2;
        plano.pedacos = new Transform[quantos];
        for (int i = 0; i < quantos; i++)
        {
            var pedaco = new GameObject("Pedaco");
            pedaco.transform.SetParent(objeto.transform, false);
            var visual = pedaco.AddComponent<SpriteRenderer>();
            visual.sprite = desenho;
            visual.color = cor;
            visual.sortingOrder = 40; // na frente de tudo (menos da interface)
            plano.pedacos[i] = pedaco.transform;
        }
        plano.deslocamentoX = deslocamentoX;
        return plano;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || pedacos == null) return;
        Vector3 posCam = cam.transform.position;
        float meiaTela = cam.orthographicSize * cam.aspect;

        // Igual à Paralaxe: a camada inteira fica em (x da câmera * fator). Os pedaços são colocados
        // lado a lado a partir do primeiro que encosta na beirada esquerda da tela.
        float origem = posCam.x * fator + deslocamentoX;
        float primeiro = origem + Mathf.Floor((posCam.x - meiaTela - origem) / largura) * largura;
        float y = emCima ? posCam.y + cam.orthographicSize + escondido
                         : posCam.y - cam.orthographicSize - escondido;

        // (o pivô dos desenhos é o meio da base, ou o meio do topo para os galhos: por isso o "+ largura / 2")
        for (int i = 0; i < pedacos.Length; i++)
            pedacos[i].position = new Vector3(primeiro + largura * (i + 0.5f), y, 0f);
    }
}
