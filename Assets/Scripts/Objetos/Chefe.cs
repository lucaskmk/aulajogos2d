using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A BASE de todos os chefes do jogo (a Baleia Branca, a Cozinheira...).
// "abstract" quer dizer que ninguém cria um "Chefe" puro: cada chefe de verdade HERDA desta classe
// e diz como responde a cada pergunta abaixo (com "override").
// Assim o GerenciadorDoJogo e a Interface (a barra de vida no topo da tela) funcionam com QUALQUER
// chefe, sem precisar saber se é a Baleia ou a Cozinheira.
//
// Aqui também fica o que os chefes fazem IGUAL na luta:
//  - o PISÃO: pular na cabeça do chefe só dá dano na HORA CERTA (cada chefe diz quando, no TestarPisao dele:
//    a âncora presa, o clarão do lampião, a baleia atordoada). Depois de um golpe ele fica TempoSemApanhar sem
//    poder apanhar de novo (senão o mesmo pulo contaria duas vezes);
//  - as RODADAS que um golpe interrompe: levou um pisão? TODAS as corrotinas do chefe param na hora
//    (StopAllCoroutines: a rodada, os ataques soltos, tudo), ele arruma o que ficou pela metade (Arrumar),
//    reage (Reagir) e a luta continua (Combate) de onde parou: como o Combate só depende da etapa e da vida,
//    ele sabe sozinho se vem a próxima rodada, a próxima etapa ou a derrota. Assim o chefe nunca continua
//    atacando com a vida zerada, e nenhum ataque fica "pendurado" pela metade.
public abstract class Chefe : MonoBehaviour
{
    // Nome que aparece na barra de vida (ex.: "BALEIA BRANCA").
    public abstract string Nome { get; }

    // A luta está acontecendo? (a barra de vida só aparece durante a luta)
    public abstract bool EmCombate { get; }

    public abstract int Vida { get; }
    public abstract int VidaMaxima { get; }

    // Em que etapa da luta estamos: 0 = primeira, 1 = segunda...
    public abstract int EtapaAtual { get; }
    public abstract int Etapas { get; }
    public abstract int GolpesPorEtapa { get; }

    // A vida que a barra MOSTRA. Por padrão é a vida de verdade; um chefe pode fazer a barra
    // "escorrer" devagar até ela (a Baleia faz isso).
    public virtual float VidaMostrada => Vida;

    // A música da luta: cada chefe tem a sua (veja Musicas).
    public abstract Musicas.Tema Musica { get; }

    // Onde o jogador renasce quando morre na luta (o começo da arena). O GerenciadorDoJogo usa para saber
    // se a luta vai continuar depois da morte (aí a música do chefe continua tocando, sem recomeçar).
    public abstract Vector3 PontoDeRenascer { get; }

    // ------------------------------------------------------------------ o pisão

    protected const float TempoSemApanhar = 0.8f;
    float podeApanharEm;  // Time.time em que ele pode apanhar de novo (Time.time para na pausa)
    bool emRodada;        // só dá para apanhar DURANTE uma rodada (não na introdução nem na troca de etapa)

    // Pode levar um pisão agora? (no meio de uma rodada, e fora do tempinho depois do último golpe)
    protected bool Vulneravel => emRodada && Time.time >= podeApanharEm;

    // O jogador está CAINDO com os pés em cima da cabeça (entre xMin e xMax, perto da altura "topo")?
    // "abaixo" é quanto os pés podem já ter afundado na cabeça (a queda é rápida: num quadro ele desce bastante).
    protected static bool PesNaCabeca(Jogador jogador, float xMin, float xMax, float topo, float abaixo = 0.45f)
    {
        if (jogador == null || jogador.Morto || jogador.Corpo.linearVelocity.y > 0.5f) return false;
        Vector3 pes = jogador.transform.position + Vector3.down * 0.5f;
        return pes.x > xMin && pes.x < xMax && pes.y > topo - abaixo && pes.y < topo + 0.8f;
    }

    // Chamado por cada chefe quando leva um pisão (depois de tirar a vida). Tem que ser chamado do Update
    // (o TestarPisao), nunca de dentro de uma corrotina do chefe: ele para TODAS elas.
    protected void Golpeado()
    {
        podeApanharEm = Time.time + TempoSemApanhar;
        emRodada = false;
        StopAllCoroutines(); // a rodada, os ataques soltos, as poses... tudo para aqui
        soltos.Clear();
        emParalelo = 0;
        Arrumar();
        StartCoroutine(DepoisDoGolpe());
    }

    IEnumerator DepoisDoGolpe()
    {
        yield return Reagir();
        yield return Combate(); // a luta continua: a próxima rodada, a próxima etapa ou a derrota
    }

    // ------------------------------------------------------------------ rodadas e ataques soltos

    readonly List<Coroutine> soltos = new List<Coroutine>();
    int emParalelo;

    // Começa um ataque "solto" (sem esperar ele acabar) e conta quantos ainda estão rodando.
    protected void Paralelo(IEnumerator ataque) => soltos.Add(StartCoroutine(Contar(ataque)));

    // Roda o ataque "achatado": se ele chama outras corrotinas (yield return OutroAtaque()), elas rodam AQUI
    // DENTRO, uma de cada vez. Assim, parando esta corrotina (PararSoltos), o ataque inteiro para junto,
    // até as partes de dentro (nenhuma faca continua voando sozinha depois do clarão).
    IEnumerator Contar(IEnumerator ataque)
    {
        emParalelo++;
        var pilha = new Stack<IEnumerator>();
        pilha.Push(ataque);
        while (pilha.Count > 0)
        {
            IEnumerator atual = pilha.Peek();
            if (!atual.MoveNext())
            {
                pilha.Pop();
                continue;
            }
            if (atual.Current is IEnumerator dentro)
            {
                pilha.Push(dentro);
                continue;
            }
            yield return atual.Current; // null, WaitForSeconds...: quem espera é a Unity
        }
        emParalelo--;
    }

    // Espera todos os ataques soltos acabarem.
    protected IEnumerator EsperarTodos()
    {
        while (emParalelo > 0) yield return null;
    }

    // Para todos os ataques soltos de uma vez.
    protected void PararSoltos()
    {
        foreach (Coroutine solto in soltos) if (solto != null) StopCoroutine(solto);
        soltos.Clear();
        emParalelo = 0;
    }

    // Roda uma rodada: enquanto ela acontece, o chefe pode levar pisão. (Se levar, o Golpeado para tudo,
    // inclusive esta corrotina.) Uso, dentro do Combate: yield return RodarRodada(Etapa1Rodada1());
    protected IEnumerator RodarRodada(IEnumerator rodada)
    {
        emRodada = true;
        yield return rodada;
        emRodada = false;
    }

    // O "laço" da luta: repete as rodadas de cada etapa até a vida chegar ao fim da etapa, passa de etapa
    // e, quando a vida zera, a cena da derrota. Tem que depender SÓ da etapa e da vida (os campos do chefe),
    // porque depois de cada pisão ele recomeça do zero (veja DepoisDoGolpe).
    protected abstract IEnumerator Combate();

    // Um pisão parou tudo: apague os avisos e ataques que ficaram pela metade e volte ao "normal".
    protected abstract void Arrumar();

    // A reação ao pisão (tonto, piscando...) antes de a luta continuar. Se a vida chegou a zero, deixe o
    // chefe parado onde está: a cena da derrota começa logo depois.
    protected abstract IEnumerator Reagir();
}
