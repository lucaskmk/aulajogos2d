using System.Collections.Generic;
using UnityEngine;

// 'M' - plataforma "tímida": quando você PULA perto dela, ela foge para longe de você.
// Truque para passar: dê um pulinho na beirada para assustá-la ANTES e depois pule de verdade.
// Blocos 'M' encostados fogem juntos. Cada grupo só foge uma vez.
public class BlocoQueFoge : MonoBehaviour
{
    public float distanciaParaFugir = 3f;
    public float distanciaDaFuga = 3f;
    public float tempoDaFuga = 0.25f;

    readonly List<Transform> blocos = new List<Transform>();
    Rigidbody2D corpo;
    bool fugiu;
    bool fugindo;
    Vector2 origem, destino;
    float progresso;

    void Awake()
    {
        corpo = gameObject.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void AdicionarBloco(Vector3 posicao)
    {
        var bloco = ConstrutorDeFase.Visual("Bloco", transform, posicao, "tijolo", 0);
        bloco.AddComponent<BoxCollider2D>().size = Vector2.one;
        blocos.Add(bloco.transform);
    }

    void Update()
    {
        if (fugiu || !GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return;
        if (GerenciadorDoJogo.JogadorAtual.NoChao) return; // só se assusta quando você pula

        foreach (Transform bloco in blocos)
        {
            Vector2 d = (Vector2)bloco.position - jogador;
            if (Mathf.Abs(d.x) < distanciaParaFugir && Mathf.Abs(d.y) < 3f)
            {
                Fugir(Mathf.Sign(d.x));
                return;
            }
        }
    }

    void Fugir(float direcao)
    {
        fugiu = true;
        fugindo = true;
        progresso = 0f;
        origem = corpo.position;
        destino = origem + Vector2.right * direcao * distanciaDaFuga;
        GerenciadorDoJogo.Som("risada");
    }

    void FixedUpdate()
    {
        if (!fugindo) return;
        progresso += Time.fixedDeltaTime / tempoDaFuga;
        corpo.MovePosition(Vector2.Lerp(origem, destino, Mathf.SmoothStep(0f, 1f, progresso)));
        if (progresso >= 1f) fugindo = false;
    }
}
