# Re:CILADA! — um jogo de plataforma nada confiável

Jogo 2D de plataforma "troll" (no estilo Cat Mario / Level Devil): tudo parece normal,
mas o chão cai, espinhos brotam do nada, a bandeira foge e as nuvens têm dentes.

Tema **Re:Zero**: você é o Subaru, os inimigos são Mabeasts e Grandes Coelhos, quem dá as "dicas" é o Puck
e cada morte é um *Retorno pela Morte*: a tela escurece, as mãos da Bruxa avançam e você renasce no pico do áudio
`Assets/Resources/Sons/retorno_pela_morte.mp3` (o momento do pico é detectado sozinho; para trocar o som,
é só substituir o arquivo mantendo o nome). No lugar das suas últimas mortes fica a marca da mão da Bruxa.
Na última fase a Baleia Branca aparece, e a Emilia espera o Subaru no final.

## Como jogar
1. Abra o projeto no Unity 6 (6000.6.2f1).
2. Abra `Assets/Scenes/SampleScene` e aperte **Play**. O jogo se monta sozinho.

| Tecla | Ação |
|---|---|
| A / D ou setas | andar |
| Espaço / W / ↑ | pular (segure para pular mais alto) |
| R | reiniciar a fase (conta como morte 😈) |
| Esc | pausar / continuar (no mapa: voltar ao título) |
| Q (na pausa) | voltar ao mapa (o progresso fica salvo) |
| Espaço / Enter (morto) | pular a animação da morte |
| ↑ / ↓ e Enter (título) | escolher no menu |
| Setas e Enter (mapa) | andar pelo caminho e entrar na fase |

Também funciona com controle (gamepad).

## Mapa do mundo
Depois do título vem um **mapa no estilo Super Mario World**: cada ponto é uma fase, ligados por um caminho
pontilhado. O Subaru anda de ponto em ponto e em cada ponto fica o "personagem" daquela fase
(Puck, Mabeast, serra, nuvem malvada, coelho...). No fim do caminho ficam a mansão e a Emilia.
- Verde = já passou nesta partida, amarelo = liberada, cinza = trancada (o personagem aparece só como sombra).
- Ao passar de fase você volta ao mapa, o caminho até a próxima aparece e o Subaru anda sozinho até ela.
- Dá para voltar e jogar de novo qualquer fase liberada. Fases liberadas em partidas anteriores continuam
  abertas (bom para apresentar), mas pular fases pelo mapa tira a partida do recorde.

No título dá para **Continuar** a partida salva (fica em PlayerPrefs: onde o Subaru está, mortes, moedas e tempo)
ou começar um **Novo jogo**.

A animação da morte pode ser pulada depois de 0,5 s e fica mais curta a partir da 3ª morte na mesma fase.

## Música
O jogo toca `Assets/Resources/Sons/musica_fundo` (mp3, ogg ou wav) se o arquivo existir.
Se não existir, toca uma música **lofi 8-bit original gerada por código** (`FabricaDeMusica`), com uma variação por fase.
A música abaixa sozinha quando você morre (para a OST do Retorno pela Morte aparecer) e na pausa.

## Organização do código (`Assets/Scripts`)
- **Nucleo/**
  - `GerenciadorDoJogo` — máquina de estados (Título → Mapa → Jogando ⇄ Pausado → Morreu → Fase concluída → Mapa ... → Vitória),
    contagem de mortes/moedas/tempo, menu do título e as marcas das mortes.
  - `Interface` — tudo o que é desenhado por cima do jogo (HUD, título, pausa, morte, vitória, falas, transição entre fases). Só lê o estado do `GerenciadorDoJogo`.
  - `SonsDaMorte` — o áudio do Retorno pela Morte (sincronizado com o renascimento) e a OST com fade.
  - `Musica` e `FabricaDeMusica` — música de fundo (arquivo ou lofi gerado por código).
  - `MapaDoMundo` — o mapa de fases (pontos, caminho, personagens, Subaru andando). As posições ficam em `Pontos`.
  - `Progresso` — o que fica salvo em PlayerPrefs (continuar, fase máxima, recorde).
  - `Fases` — **as fases são desenhadas em texto**; cada caractere é um bloco (legenda no topo do arquivo).
  - `ConstrutorDeFase` — lê o texto e cria os objetos. O chão vira um único `CompositeCollider2D`.
  - `FabricaDeSprites` — toda a arte é gerada por código (pixel art escrita em texto + desenhos por fórmula),
    inclusive a fonte pixel (`TextoEmPixel`, com acentos) usada no logo e no HUD.
  - `FabricaDeSons` — efeitos sonoros 8-bit gerados por código (ondas quadradas e ruído).
  - `Animacao` (enfeites e nuvens com paralaxe), `AnimacaoDeQuadros` (bandeira tremulando, brilho dos blocos `?`),
    `Paralaxe`, `MarcaDaMorte`, `CameraSeguir`, `Controles` (Input System), `Efeitos`, `Particula`, `Sombra`.
- **Jogador/** `Jogador` — física com Rigidbody2D, pulo variável, *coyote time* e *buffer* de pulo.
- **Armadilhas/** uma classe por pegadinha: `ChaoQueCai`, `EspinhoEscondido`, `EspinhoQueCai`,
  `BlocoInvisivel`, `BlocoSurpresa`, `BlocoQueFoge`, `Mola`, `Serra`, `Esmagador`, `Cano`, `NuvemAssassina`,
  `MoedaAssassina`, `Espinho`, e `Perigo` (base: "encostou, morreu").
- **Objetos/** `Inimigo`, `Coelho`, `BaleiaBranca`, `Emilia`, `Moeda`, `Placa` (o Puck), `Bandeira`, `BandeiraFujona`,
  `BandeiraFalsa`, `BandeiraVolta`.

## Fases que mudam
Recurso opcional (desligado em todas as fases): uma fase pode **mudar depois que você morre** (estilo Level Devil),
com armadilhas trocando de lugar. Para usar, adicione uma lista `mudancas` à fase em `Fases.cs`
(o formato está explicado no topo do arquivo).

## Criando uma fase nova
Copie um bloco `new Fase { ... }` em `Fases.cs` e desenhe com os caracteres da legenda
(`#` chão, `P` jogador, `G` bandeira, `C` chão que cai, `h` espinho escondido, `r` coelho, `w` Baleia Branca...).
Acrescente também um ponto em `MapaDoMundo.Pontos` (senão a fase nova não aparece no mapa).
Para testar uma fase direto: durante o Play, selecione o objeto `GerenciadorDoJogo`, mude `Fase Inicial` no
Inspector, volte ao título e escolha **Novo jogo**: o Subaru começa nesse ponto do mapa, já liberado.
