using UnityEngine;

// A BASE de todos os chefes do jogo (a Baleia Branca, a Elsa...).
// "abstract" quer dizer que ninguém cria um "Chefe" puro: cada chefe de verdade HERDA desta classe
// e diz como responde a cada pergunta abaixo (com "override").
// Assim o GerenciadorDoJogo e a Interface (a barra de vida no topo da tela) funcionam com QUALQUER
// chefe, sem precisar saber se é a Baleia ou a Elsa.
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
}
