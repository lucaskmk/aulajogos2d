# Onde paramos (e o que falta)

Arquivo de passagem de bastão: se a sessão na nuvem acabar, dá para continuar daqui (no Claude Code local ou na mão).
Tudo o que está listado como "pronto" já está na `main`.

## Objetivo desta leva
- 15 fases + 1 fase só do chefe (16 no total).
- A Baleia Branca como chefe DIFÍCIL, estilo chefe final do Helltaker: 3 etapas com checkpoint entre elas, ataques rápidos e telegrafados, morre com um toque. O visual é uma orca com chifre.
- Só UMA fase com controles invertidos ("Cadê a direita?"), e só num trecho.
- O jogo continua troll ("passar raiva"), mas sempre possível e justo depois que você sabe.
- Depois: revisão completa pela rubrica da disciplina (12 frentes), GDD, README com créditos e página do itch.io.

## Pronto
- **Base** (commit `77ccb87`):
  - `FabricaDeSprites.Registrar(nome, criar)`: cada arquivo novo registra os próprios sprites.
  - `Jogador.Empurrar(velX)` (vento) e `Jogador.Carregar(deslocamento)` (plataformas).
  - `Fase.noite`, `Fase.escura`, `Fase.petalas` e `Fase.corDoFundo` (em `Fases.cs`).
  - `GerenciadorDoJogo.EtapaDoChefe`: lembra a etapa da luta entre uma morte e outra.
  - Pontos do mapa de `1` a `9` e de `A` a `G` (fases 10 a 16).
  - Checador de fases: `Ferramentas/checar_fase.py`.
- **Inversão:** "Cadê a direita?" agora só tem um trecho invertido, depois do checkpoint.
- **Fase "Ponte para a Capital"** (`PlataformaMovel.cs`, letras `j` / `J` / `D`):
  - `j` normal; `J` com cristal vermelho, que despenca; `D` com cristal amarelo, que dispara.
  - Já está integrada em `Fases.cs` e `ConstrutorDeFase.cs`, com a conquista "expressa".
- **"O Verdadeiro Final (juro)":** fim novo (bandeira falsa, chão que cai), sem a arena; o chefe vai ter fase própria.
- **Cor do céu, pétalas e fala do mapa** agora são presas a cada fase (pelo nome), não à posição. Reordenar fases não bagunça mais nada.
- **Mapa do mundo novo** (`MapaDoMundo.cs`): 112 x 21, com 16 pontos (capital, floresta, lago com a ilha secreta, mansão, abismo, biblioteca, montanhas de cristal, região sombria e o covil da Baleia no mar). Prévia em `Ferramentas/previews/mapa_do_mundo.png`.
- **Arte da orca com chifre** (`Objetos/ArteDaBaleia.cs`):
  - Registra `baleia`, `baleia_0..3`, `baleia_boca`, `baleia_carga` e `baleia_tonta`.
  - Também registra os sprites dos ataques: raio, linha e elo.
  - Prévia em `Ferramentas/previews/orca_chefe.png`.

## Em andamento / falta fazer
A ordem final das 16 fases:

| # | Fase | Mecânica nova | Situação |
|---|---|---|---|
| 1 | Bem-vindo :) | — | pronta |
| 2 | Confia em mim | — | pronta |
| 3 | Corre! | — | pronta |
| 4 | O Final (sem pegadinhas) | — | pronta |
| 5 | Eu mudo de ideia (libera a secreta) | — | pronta |
| 6 | (Rem) | **Mangual da Rem** `Q`/`q` | `MangualDaRem.cs` existe, mas falta revisar e desenhar a fase |
| 7 | Cadê a direita? | (inversão, um trecho só) | pronta |
| 8 | Foge, bloco! | — | pronta |
| 9 | Ponte para a Capital | plataformas `j` `J` `D` | **pronta** |
| 10 | (Ram) | **Vento da Ram** `y` (rajadas para trás) e `n` (redemoinho para cima) | falta |
| 11 | Biblioteca Proibida | portas | pronta |
| 12 | (ritmo) | **Blocos do ritmo** `a`/`b` (alternam a cada 1,4 s e piscam antes) | falta |
| 13 | (Elsa, `escura = true`) | **Facas da Elsa** `l` (lançador com aviso nos olhos; a faca tem luz própria) | falta |
| 14 | (miasma) | **Miasma da Bruxa** `N` (parede que persegue; renasce atrás do checkpoint) | falta |
| 15 | O Verdadeiro Final (juro) | — | pronta |
| 16 | A Baleia Branca (`noite = true`) | **Chefe** (reescrever `BaleiaBranca.cs`) | arte pronta; falta a luta e a fase |

Cada mecânica nova é **um arquivo novo** em `Assets/Scripts/Armadilhas/` (com `.meta`). Os sprites são registrados com `FabricaDeSprites.Registrar` num método `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`. Integrar uma fase nova é:
1. Pôr um `case` no `switch` de `ConstrutorDeFase.Construir`.
2. Acrescentar a fase no lugar certo de `Fases.Todas`, com `corDoFundo` e, se for o caso, `noite`/`escura`. Pôr também a linha da legenda no topo de `Fases.cs`.
3. Pôr a fala do mapa em `Textos.DepoisDaFase`, pelo nome da fase.
4. Se tiver conquista nova, pôr em `Conquistas.Todas`.
5. Testar o terreno:
   ```
   python3 Ferramentas/extrair_fases.py Assets/Scripts/Nucleo/Fases.cs /tmp/fases
   python3 Ferramentas/checar_fase.py /tmp/fases/fase_XX.json --de s
   ```

Sprites que o mapa já espera (precisam existir): `mangual`, `vento`, `bloco_ritmo`, `faca`, `miasma` (e `plataforma`, que já existe).

### O chefe (Helltaker)
- 3 etapas, 2 golpes por etapa (vida 6).
- Ao começar a luta, chamar `GerenciadorDoJogo.Instancia.SalvarPonto(início da arena)`; ao passar de etapa, `EtapaDoChefe++`. Morreu → renasce no começo da arena, já na etapa em que estava, sem a introdução longa.
- Ataques, todos com aviso curto (0,45–0,6 s na etapa 1; 0,3–0,4 s na 3):
  - investida alta ou baixa;
  - chuva de névoa;
  - correntes/raios horizontais e verticais (piscam e depois matam por 0,4 s);
  - onda de choque no chão;
  - raio do chifre que varre a arena;
  - bolas em anel;
  - mergulho → ela fica atordoada → **pular na cabeça** (o único jeito de dar dano).
- Etapa 2: 2 ataques juntos. Etapa 3: mais rápida, com o chão sumindo e um ataque final "desesperado".
- A fase: aquecimento curto, depois um checkpoint `s`, a arena começando no `w` e, no fim, a Emilia `L` (ao encostar nela, o jogo é zerado).
- Pode mudar a barra de vida em `Interface.DesenharVidaDoChefe` para mostrar "ETAPA 2/3".

## Depois disso: revisão pela rubrica
- **Menus:**
  - No título: Opções (volume, tela cheia, modo de jogo), Como Jogar, Créditos, Sair (fora do WebGL).
  - Pausa em lista: Continuar, Reiniciar fase, Opções, Voltar ao mapa.
  - Mouse nos menus.
- **Modo Justo** (opcional, desligado por padrão): o save nunca "muda de lugar" e a bandeira `W` não manda para o começo. O modo troll continua sendo o padrão.
- **Narrativa:**
  - Prólogo (o Subaru é invocado, e o Retorno pela Morte vira a regra do jogo: aprender morrendo).
  - Cartão de capítulo ao entrar em cada fase.
  - Epílogo com a Emilia.
- **Rejogabilidade:** recorde por fase (tempo, mortes e todas as moedas), medalhas no mapa e "novo recorde!".
- **Som:** efeitos para cada mecânica nova (mangual, vento, faca, miasma, tic do ritmo, plataforma) e para o chefe.
- **Robustez:**
  - Saves antigos de 9 fases (o índice da fase salva mudou: limitar/validar).
  - Pausa durante as transições; morte durante "fase concluída".
- **Documentação:**
  - `GDD.md` organizado pelo DDE (para colar no Milanote).
  - `README.md` com os créditos. Atenção: `Assets/Resources/Sons/retorno_pela_morte.mp3` e `ost_morte.mp3` são do anime Re:Zero e precisam de crédito.
  - Texto da página do itch.io: como jogar e os segredos (fase secreta: todas as moedas da fase 5, e depois seta para baixo no ponto 5).

## Como testar sem abrir a Unity (só na nuvem)
- O compilador e o stub da Unity ficaram em `/tmp/claude-0/` (não vão para o git).
- Na sua máquina é só abrir o projeto no Unity 6 e dar Play.
- Para testar uma fase direto: selecione `GerenciadorDoJogo` no Play, mude `Fase Inicial`, volte ao título e escolha Novo Jogo.
