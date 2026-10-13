# Re:CILADA! — GDD (Design, Dinâmica, Experiência)

> Documento de design organizado pelo **DDE** (*Design, Dynamics, Experience*; Walk, Görlich e Barrett).
> Cada seção curta pode virar um cartão no Milanote.
> - **Design** é o que a equipe controla.
> - **Dinâmica** é o que acontece quando alguém joga.
> - **Experiência** é o que o jogador sente.
>
> Os números deste documento vêm do código (`Assets/Scripts`). O README tem os controles, os créditos e como rodar o jogo.

---

# 0. VISÃO GERAL

## High concept
"*Cat Mario* no mundo de *Re:Zero*": um plataforma 2D em que **tudo mente**. Cada morte é um *Retorno pela Morte*:
o jogador volta, lembra onde errou e passa, até chegar à Emilia.

## Ficha
- **Gênero:** plataforma 2D "troll" / *masocore* leve, com lutas de chefe no estilo *Helltaker*.
- **Público:** fãs de anime e de jogos de "raiva" (*Level Devil*, *Cat Mario*, *I Wanna Be the Guy*), de 14 anos para cima. Também quem só quer rir das próprias mortes.
- **Plataforma:** navegador (WebGL no itch.io), com teclado ou controle. Também roda no editor e em build de PC.
- **Duração:** 16 fases + 1 secreta, de 45 a 90 minutos na primeira vez (estimativa: depende muito das mortes).
- **Engine:** Unity 6 (URP 2D), C#. Toda a arte e a música são procedurais.

## Referências
- **Cat Mario / Syobon Action** e **Level Devil**: as armadilhas que quebram a expectativa.
- **Helltaker**: o chefe com etapas, checkpoints, padrões fixos e ataques telegrafados.
- **Super Mario World**: o mapa do mundo com caminhos e pontos.
- **Octopath Traveler** (HD-2D) e **Stardew Valley** com mods: pixel art com luz, profundidade, raios de sol e sombras de nuvem.
- **Re:Zero**: o tema, o tom, os personagens e o Retorno pela Morte como regra do jogo.

## Pilares
1. **Passar raiva com graça.** A morte é a piada. O jogo ri junto (mensagens de morte, o Puck mentiroso, a bandeira que foge), mas nunca humilha.
2. **Aprender morrendo (Retorno pela Morte).** Morrer é rápido, barato e informativo. Você volta na hora, com a marca da Bruxa mostrando onde morreu.
3. **Sempre justo depois que você sabe.** Toda armadilha pega uma vez. Na segunda tentativa existe uma resposta clara (pular, esperar, não pegar, ir por outro lado).
   Toda fase é passável (o checador de fases confere o terreno), e todo ataque de chefe avisa antes.

---

# 1. DESIGN (o que a equipe controla)

## 1.1 Blueprint: mundo e narrativa

### Mundo
O **Reino de Lugunica**, visto como um mapa do mundo (112 x 21 células) que começa coberto pelo **miasma da Bruxa** (névoa) e vai se revelando:
1. a capital, com o castelo;
2. o rio com a ponte;
3. a floresta;
4. o lago, com a **ilha secreta**;
5. a **mansão Roswaal**;
6. o desfiladeiro (o abismo);
7. a **Biblioteca Proibida**;
8. as montanhas de cristal;
9. a região sombria;
10. o mar escuro, com o **covil da Baleia Branca**.

### Personagens
| Personagem | Papel no jogo |
|---|---|
| **Subaru** | O jogador. Chibi, de agasalho. Morre e volta. |
| **Puck** | As "placas" de dica. Às vezes ajuda, às vezes mente. Comenta no mapa depois das fases. |
| **Emilia** | O objetivo final: espera no fim da fase 16 e comemora. Também comenta no mapa. |
| **Rem** | Chefe da fase 6, com o mangual. Vira **Oni** na etapa 3. |
| **Ram** | O vento da fase 10 e os blocos rosa do ritmo na fase 12. "Barusu". |
| **Beatrice** | Dona da Biblioteca Proibida (fase 11): as portas embaralhadas. "Kashira." |
| **Elsa** | A Caçadora de Entranhas. Joga facas das sombras e é a chefe da fase 13, no escuro. |
| **Bruxa da Inveja (Satella)** | A morte em si: mãos de sombra no cano, a marca da mão onde você morreu, as sombras da transição e o **miasma** que persegue na fase 14 ("Eu te amo"). |
| **Baleia Branca** | O chefe final (fase 16): uma orca gigante com chifre. Também aparece nadando no céu do mapa. |
| Mabeasts e Grandes Coelhos | Inimigos comuns. Os coelhos se multiplicam. |

### Narrativa e tom
- O Subaru foi parar num mundo onde tudo quer matá-lo, e o único "poder" dele é voltar depois de morrer.
- A história é contada:
  - pelas placas do Puck dentro das fases;
  - pelas falas no mapa depois de cada fase (Puck, Emilia e Beatrice, com uma fala diferente se você passou sem morrer);
  - pelas mensagens de morte;
  - pelo final com a Emilia e os créditos.
- **Tom:** comédia de "trollagem" em cima de um tema sombrio. As mensagens de morte misturam meme ("Skill issue.", "KKKKKKKKKKK") com referências ao anime ("A Satella só queria um abraço.", "Porta errada, kashira.").
- *(Em andamento: prólogo da invocação, cartão de capítulo ao entrar em cada fase e epílogo com a Emilia.)*

## 1.2 Mecânicas

### Movimento e física (Jogador.cs)
| Parâmetro | Valor | Efeito |
|---|---|---|
| Velocidade | 7 blocos/s | — |
| Aceleração | 80 blocos/s² | Chega à velocidade máxima em cerca de 0,09 s: resposta quase instantânea. |
| Força do pulo | 14 blocos/s | Pulo máximo de cerca de **2,85 blocos** de altura e cerca de 5,7 blocos de distância. |
| Gravidade | 3,5 × 9,81 | Cerca de 34 blocos/s². |
| Queda máxima | 20 blocos/s | — |
| Pulo variável | Soltar o botão na subida corta a velocidade pela metade | — |
| Coyote time | 0,10 s | Ainda dá para pular depois de sair da beirada. |
| Buffer de pulo | 0,12 s | Apertar pular um pouco antes de tocar o chão também vale. |
| Colisor | 0,62 × 0,86, com bordas arredondadas, sem atrito | Não "gruda" em paredes. |
| Mola | Força 22 | Cerca de 7 blocos de altura. |

- **Game feel:** estica no pulo, achata na aterrissagem (mais forte quanto maior a queda), poeira no pulo e na queda, "respiração" parado e sombra oval no chão.

### Armadilhas clássicas (uma linha cada)
- **Chão que cai (`C`):** parece chão e despenca quando você pisa.
- **Chão falso (`F`):** você atravessa.
- **Bloco invisível (`I`):** aparece quando você bate a cabeça. Normalmente no meio de um pulo sobre um buraco.
- **Espinho escondido (`h`):** pula do chão quando você chega perto.
- **Espinho que cai (`v`):** despenca do teto quando você passa embaixo.
- **Espinho (`^`):** parado e visível. "O mais honesto do jogo."
- **Bloco `?` / bloco surpresa (`K`):** iguais. Um dá moeda e o outro solta um inimigo em cima de você.
- **Moeda assassina (`m`):** igual à moeda. Pegou, virou espinho.
- **Nuvem assassina (`o`):** igual às nuvens do cenário, mas tem dentes.
- **Esmagador (`T`):** despenca quando você chega perto. Se você já passou, ele se arrasta atrás de você.
- **Serra (`<` / `>`):** uma vem por trás e persegue, a outra vem pela frente. Depois de passar, volta uma vez.
- **Cano (`U`):** de tempos em tempos sai uma mão de sombra da Bruxa. Dá para pular por cima no tempo certo.
- **Mola (`S`):** lança para o alto, às vezes direto num espinho.
- **Bloco tímido (`M`):** foge quando você pula perto. O truque é assustá-lo antes com um pulinho.
- **Bandeira fujona (`R`):** foge para outro lugar quando você chega perto.
- **Bandeira falsa (`Z`):** fica vermelha, brotam espinhos e você morre.
- **Bandeira volta (`W`):** manda você de volta para o começo. Não conta como morte, "só como raiva".
- **Inimigos:**
  - **Mabeast (`E`):** pise para derrotar. Se ele passar de você, volta.
  - **Mabeast espinhoso (`e`):** não pise.
  - **Grande Coelho (`r`):** persegue você e **se multiplica**.

### Mecânicas temáticas (uma por fase nova)
- **Mangual da Rem (`Q` / `q`):** a bola de espinhos gira na corrente e só a **bola** mata. Em cima do eixo é seguro ("olho do furacão").
  O `q` fica bravo e inverte o giro, 2x mais rápido.
- **Controles invertidos (`X`):** cruzar a linha avisa "Invertendo os controles em..." e inverte depois de **0,4 s**. Voltar antes cancela.
  Só a fase 7 usa isso, e só num trecho.
- **Plataformas mágicas (`j` / `J` / `D`):** vão e voltam até 6 blocos.
  - `J`: o cristal fica vermelho e ela despenca depois de 0,5 s.
  - `D`: o cristal fica amarelo, ela **dispara** e freia de repente. Quem pula enquanto ela treme cai.
- **Vento da Ram (`y`):** rajadas para a esquerda (calmo 1 s, aviso 0,5 s com folhas, rajada 2 s). Segure para a frente ou se esconda atrás de um bloco.
- **Redemoinho (`n`):** levanta o Subaru até o topo da zona. Os "traiçoeiros" engasgam e desligam por 1,5 s.
- **Portas da Beatrice (`0`–`9`):** pares fixos de ida e volta (`1-4`, `3-5`, `2-9`, `6-7`, `8-0`). Uma leva a um beco cheio de Mabeasts.
- **Blocos do ritmo (`a` / `b` / `A`):** o azul e o rosa se revezam a cada **1,4 s**, piscando e fazendo "tic, tic, TAC" antes da troca.
  O `A` pisca junto com o azul, mas nunca fica sólido.
- **Esmagador do ritmo (`t`):** cai toda vez que o azul acende. "Rosa aceso = pode passar."
- **Facas da Elsa (`l` / `k`):** dois olhos nas sombras. A menos de 14 blocos e na mesma altura, ela joga uma faca a cada 1,6 s (olhos vermelhos 0,4 s antes).
  O `k` joga três facas em leque. A faca crava em blocos, então esconder funciona.
- **Miasma da Bruxa (`N`):** uma parede de sombra com mãos persegue você a **5,2 blocos/s** (o Subaru anda a 7). Parar para pensar custa caro.

### Checkpoints que "mudam de lugar"
- O **cristal de save (`s`)** marca onde você renasce.
- Em **25%** das mortes o save "muda de lugar" ("O ponto de save mudou de lugar... :)") e você volta para o começo da fase.
- Isso nunca acontece em fase com chefe.

### Chefes (molde Helltaker)
- 3 etapas × 2 golpes (vida 6). Tudo mata com um toque.
- Todo ataque tem aviso: 0,6 s na etapa 1, 0,48–0,5 s na etapa 2 e 0,38–0,4 s na etapa 3, mas nunca menos de 0,55 s para o que pede pulo.
- Os padrões são fixos. Morreu, volta na mesma etapa, sem a introdução.
- O único jeito de dar dano é **pular na cabeça** do chefe no momento de vulnerabilidade:
  - **Rem:** a bola fica presa no chão;
  - **Elsa:** o clarão do cristal de luz;
  - **Baleia:** fica atordoada depois do mergulho.

### Moedas
- Moedas `$` e blocos `?` somam no HUD e no save.
- Muitas são **isca** para uma armadilha.
- Pegar **todas** as da fase 5 (numa mesma tentativa, sem pegar a moeda assassina) libera a fase secreta.

### Fase secreta
- O **Santuário do Puck** fica numa ilha do lago.
- Para chegar lá: no ponto 5 do mapa, aperte ↓.
- "Sem pegadinha"... só uma.

### Conquistas
- São 23, salvas para sempre. Alguns exemplos:
  - Retorno pela Morte, Natsuki Subaru honorário (100 mortes), Confiou na bandeira, Ganância, Olhou para o céu, Suspeito de hack (passar de fase sem morrer);
  - Domador de Mabeasts, Controle de pragas, Kashira!, Mudaram meu save!, Explorador, Passageiro da Expressa;
  - as vitórias contra os chefes, Fora do Ritmo e Começando do zero.

### Recorde
- O recorde é zerar com o menor número de mortes, jogando em ordem.
- No fim, o número de mortes dá um título ao jogador, de "Suspeito de hack" até "Natsuki Subaru honorário".

## 1.3 Interface
- **HUD:**
  - nome e número da fase;
  - mortes;
  - moedas;
  - tempo;
  - barra de progresso até a bandeira mais distante (para não entregar qual é a verdadeira);
  - barra de vida do chefe, dividida em etapas;
  - avisos no meio da tela ("CONTROLES INVERTIDOS :)").
- **Título:**
  - o logo "Re:CILADA!" na fonte pixel, no estilo do logo de Re:Zero, com o mapa escurecido atrás;
  - opções: Continuar, Novo jogo, Conquistas (x/23), Opções, Como Jogar, Créditos e Sair;
  - o recorde.
- **Mapa do mundo:**
  - o Subaru anda entre os pontos;
  - pontos verdes (já passou) e amarelos (liberados);
  - caveira com as mortes de cada fase;
  - a névoa some pelo caminho novo;
  - falas dos personagens depois de cada fase.
- **Placas do Puck:** o texto aparece quando o Subaru chega perto. Na biblioteca quem fala é a Beatrice, e há a dica "Porta 1 → porta 4".
- **Pausa (em lista):** Continuar, Reiniciar fase, Opções (volume da música, volume dos efeitos, gráficos HD-2D ou Simples) e Voltar ao mapa.
- **Morte:**
  - a tela perde a cor e as mãos da Bruxa avançam;
  - aparece uma mensagem engraçada;
  - dá para pular a animação depois de 0,5 s.
- **Vitória e créditos:** a Emilia comemora, aparecem o resumo (mortes, moedas, tempo e título) e os créditos rolando.

---

# 2. DINÂMICA (o que acontece quando se joga)

## 2.1 Loops
- **Micro (segundos):** ver → tentar → morrer → **voltar na hora** (1,3 s, ou 0,7 s a partir da 3ª morte, e pode pular) → tentar de outro jeito.
- **Médio (minutos):** atravessar a fase de save em save, decorando as armadilhas. A marca da Bruxa mostra onde você já morreu.
- **Macro (sessão):** passar a fase → o Subaru anda no mapa → a névoa se abre → uma fala → a próxima fase. A cada 5 a 7 fases vem um chefe.
- **Meta:** conquistas, a fase secreta, todas as moedas e o recorde de mortes.

## 2.2 Curva de dificuldade
- **Fases 1 a 5:** ensinam a "regra do jogo" (nada é confiável), uma trollagem clássica por vez.
- **Fase 6:** o primeiro chefe, o mais "didático" (o Puck dá dicas na etapa 1).
- **Fases 7 a 12:** uma mecânica temática nova por fase, cada vez mais longas (até 168 blocos), com 2 saves.
- **Fase 13:** o segundo chefe, que junta escuridão e leitura de aviso.
- **Fase 14:** pressão contínua (o miasma persegue).
- **Fase 15:** "prova final" que revisita as trollagens.
- **Fase 16:** o chefe final, o mais rápido e com mais ataques.
- A **secreta** é um respiro.

## 2.3 Como as trollagens subvertem a expectativa
- **Convenção de plataforma virando armadilha:**
  - bloco `?` → inimigo;
  - moeda → espinho;
  - bandeira → morte, fuga ou volta ao começo;
  - nuvem → dentes.
- **O Puck mente**, mas nem sempre: "Pula com força! (ou não)". O jogador aprende a ler o tom.
- **"Igualzinho ao anterior"**: a segunda plataforma, o segundo redemoinho e o segundo mangual são iguais na aparência e diferentes no comportamento.
- **Nomes que mentem:** "O Final (sem pegadinhas)" é a fase 4. "Eu mudo de ideia" diz "Decorou a fase? Que pena."
- **O save que muda de lugar:** a "promessa" de checkpoint também pode trair (só fora dos chefes).

## 2.4 Ritmo dos chefes
- **Etapa 1:** um ataque por vez, para aprender.
- **Etapa 2:** mais rápido e com combinações (dois ataques juntos, a volta do arremesso em outra altura).
- **Etapa 3:** tudo mais rápido, mais o "twist":
  - a Rem vira Oni;
  - a escuridão da Elsa fecha;
  - o chão da Baleia desmorona e a arena escurece.
- Cada rodada termina na **janela de dano** (pular na cabeça). Erro significa recomeçar a rodada, não a luta.

## 2.5 Economia (moedas e recordes)
- As moedas não compram nada. Elas funcionam como **isca** (puxam o jogador para o perigo) e como **desafio opcional** (a secreta).
- As mortes são a "moeda" que importa: aparecem no HUD, nas caveiras do mapa, no recorde e no título final.

## 2.6 Comportamento emergente
- **Bandos de coelhos** que se multiplicam e lotam a tela se o jogador demora.
- **Esmagadores e serras** que perseguem e às vezes matam inimigos ou bloqueiam o caminho de um jeito diferente a cada tentativa.
- **Speedrun natural:**
  - correr sem parar contra o miasma;
  - pular a animação da morte;
  - ir de save em save.
- **Jogadores testando o Puck:** depois de algumas mortes, o jogador passa a fazer o contrário da placa (e o jogo às vezes recompensa ser honesto: "dessa vez é sério").

---

# 3. EXPERIÊNCIA (o que o jogador sente)

## 3.1 Emoções-alvo
| Momento | Emoção |
|---|---|
| Primeira trollagem de cada fase | **Surpresa** ("COMO ASSIM?") |
| Morrer de novo para a mesma coisa | **Raiva engraçada**: a mensagem de morte ri junto |
| Passar do trecho que matou 10 vezes | **Alívio** |
| Fazer a fase inteira "de cor" | **Domínio** |
| Chefes | **Tensão → foco → triunfo** |
| Mapa entre as fases | **Descanso e curiosidade** (a névoa esconde o que vem) |
| Final com a Emilia | **Recompensa e carinho** |

## 3.2 Emoção por fase (alvo)
1. **Bem-vindo :)** — surpresa ("até o tutorial mente").
2. **Confia em mim** — desconfiança.
3. **Corre!** — urgência.
4. **O Final (sem pegadinhas)** — ironia.
5. **Eu mudo de ideia** — ganância punida.
6. **Rem** — primeiro "boss rush" de adrenalina e aprendizado.
7. **Cadê a direita?** — confusão controlada.
8. **Foge, bloco!** — frustração cômica (a bandeira que volta).
9. **Ponte** — vertigem e paranoia.
10. **Voa, Barusu!** — luta contra a força invisível.
11. **Biblioteca** — quebra-cabeça e memória.
12. **Dança das Gêmeas** — fluxo e ritmo.
13. **Elsa** — medo do escuro.
14. **Eu Te Amo (CORRE!)** — pânico.
15. **Verdadeiro Final** — "eu já sei de tudo" (domínio).
16. **Baleia** — clímax épico.
17. **Secreta** — recompensa.

## 3.3 Estética
- **Visual HD-2D de pixel art:**
  - **blocos e personagens:** blocos com rampas de cor e *hue shifting*, personagens chibi;
  - **profundidade:** fundo em camadas com paralaxe e desfoque de profundidade, primeiro plano desfocado e tilt-shift;
  - **luz:** raios de sol que somem quando uma nuvem passa, sombras de nuvens, hora do dia (manhã no início, entardecer dourado no fim) e luzes 2D;
  - **vida no cenário:** vento nas plantas e partículas de clima (pólen, pétalas, vaga-lumes, poeira dourada);
  - **pixel nítido:** a tela é pixelada em escala inteira.
- **Música orquestral procedural:** tema original em ré menor, no clima de abertura de anime "dark" (piano, cordas, violino com vibrato e bateria), em três versões:
  - fase: 152 bpm;
  - mapa: 98 bpm, calma;
  - chefe: 168 bpm, pesada.
- **Sons:**
  - efeitos 8-bit gerados por código (pulo, moeda, pisão, mola, serra, porta, risada, rugido, conquista, vitória);
  - o áudio do *Retorno pela Morte* do anime, sincronizado para o "TUM" cair no renascimento.

## 3.4 Imersão
- O Retorno pela Morte vira a regra do jogo: o jogador lembra (como o Subaru) e o mundo esquece.
- Os personagens comentam o desempenho (fala diferente se você passou sem morrer).
- A névoa do mapa esconde o mundo até você conquistá-lo, e a Baleia Branca nada no céu do mapa como ameaça distante.

## 3.5 Feedback
- **Morte:**
  - a tela perde a cor;
  - aberração cromática;
  - as mãos da Bruxa avançam;
  - o áudio do anime;
  - uma mensagem de morte (são mais de 60).
- **Renascimento:** um "soco" de distorção de lente e uma tremidinha de tela.
- **Impacto:** tremida de tela (esmagador, chefes, a morte), poeira proporcional à queda e estica-e-achata.
- **Avisos dos perigos:** linhas vermelhas, "!", marcas no chão, olhos vermelhos, cristais mudando de cor, "tic-tic-TAC" e folhas antes do vento.
- **Progresso:** barra de progresso, barra do chefe com as etapas, aviso de conquista, as caveiras no mapa e a marca da Bruxa onde você morreu.

---

# 4. LEVEL DESIGN

Dificuldade de 1 a 5 (estimativa da equipe). "Saves" = cristais `s` na fase. A largura está em blocos (todas as fases têm 15 de altura).

| # | Fase | Largura | Mecânica nova | Trollagem principal | Saves | Dif. |
|---|---|---|---|---|---|---|
| 1 | Bem-vindo :) | 82 | Chão que cai, `?`/`K`, bloco invisível, espinho escondido, espinho do teto, cano, bandeira fujona | O tutorial manda "pular BEM alto" para dentro do bloco invisível, e a bandeira foge | 0 | 1 |
| 2 | Confia em mim | 58 | Esmagador, nuvem assassina, mola, chão falso, bandeira falsa | "Chão 100% seguro à frente" e a primeira bandeira que mata | 0 | 2 |
| 3 | Corre! | 92 | Serras (por trás e pela frente), save | "Não olhe para trás": uma serra persegue desde o primeiro passo | 1 | 2 |
| 4 | O Final (sem pegadinhas) | 70 | Grandes Coelhos | O nome: é só a fase 4 | 0 | 2 |
| 5 | Eu mudo de ideia | 70 | Moeda assassina | "Pegue as moedas! Todas!", e uma delas mata (e a secreta exige todas as outras) | 0 | 2 |
| 6 | Oni de Cabelo Azul | 86 | Mangual da Rem; **chefe Rem** | "Vai lá dar oi. Eu espero aqui. Bem aqui. Longe." | 1 | 3 |
| 7 | Cadê a direita? | 92 | Controles invertidos, Mabeast espinhoso, espinho comum | "Tudo normal por aqui", e depois do save tudo inverte | 1 | 3 |
| 8 | Foge, bloco! | 76 | Bloco tímido, bandeira que volta | A primeira "bandeira" manda você de volta ao começo | 0 | 3 |
| 9 | Ponte para a Capital | 156 | Plataformas `j`, `J` (despenca) e `D` (expressa) | "Essa aqui é IGUALZINHA à outra", e não é | 2 | 3 |
| 10 | Voa, Barusu! | 168 | Vento da Ram e redemoinho | O segundo redemoinho, "igualzinho", desliga no meio | 2 | 4 |
| 11 | Biblioteca Proibida | 61 | Portas da Beatrice | Uma porta leva a um beco cheio de Mabeasts | 0 | 3 |
| 12 | Dança das Gêmeas | 154 | Blocos do ritmo, bloco falso `A`, esmagador do ritmo | "Agora é só ritmo puro, sem pegadinha", e vem uma bandeira falsa no fim | 2 | 4 |
| 13 | A Caçadora de Entranhas | 80 | Fase escura, facas da Elsa; **chefe Elsa** | "A moeda ali em cima também é de confiança. :)" (não é) | 1 | 4 |
| 14 | Eu Te Amo (CORRE!) | 170 | Miasma da Bruxa | Mola "para as moedas", que leva às nuvens assassinas | 2 | 4 |
| 15 | O Verdadeiro Final (juro) | 120 | — (revisão) | "Pode encostar na bandeira. Essa é de verdade", mas há uma bandeira falsa (com degrau para pular por cima) e chão que cai | 1 | 3 |
| 16 | A Baleia Branca | 100 | **Chefe final Baleia Branca** | Aquecimento noturno com nuvens e espinhos, depois a arena | 1 | 5 |
| ★ | Santuário do Puck (secreta) | 72 | — | "Aqui não tem pegadinha"... "tá, só uma." | 0 | 1 |

### Princípios usados
- **Uma ideia nova por fase**, apresentada num lugar seguro e depois combinada com o que já se conhece.
- **Ensinar com o Puck, trair com o Puck:** a placa é tutorial e armadilha ao mesmo tempo. Nos chefes as dicas dele são sempre verdadeiras.
- **Saves antes dos trechos longos** nas fases grandes (9, 10, 12 e 14 têm 2 saves cada).
- **Fases desenhadas em texto** (`Fases.cs`): iterar é editar uma linha e dar Play.

---

# 5. BALANCEAMENTO E TESTES

## Ferramentas
- **Checador de fases** (`Ferramentas/checar_fase.py`):
  - simula a física real do `Jogador.cs` (passos de 0,02 s) e procura um caminho do `P` até a bandeira sem encostar em células mortais;
  - com `--de s`, testa cada trecho a partir de cada save;
  - garante o pilar "sempre passável" a cada mudança de mapa.
- **Simuladores dos chefes:** scripts usados durante o desenvolvimento (fora do repositório). Reproduzem os ataques e os tempos para conferir se sempre existe um lugar seguro e se o aviso dá tempo de reagir.
- **Painel F10 e atalho F9:** permitem testar efeitos, desempenho (FPS e pior quadro) e qualquer fase ou chefe sem jogar tudo.

## Ajustes depois dos playtests
- **Inversão de controles:**
  - antes estava em várias fases; agora fica **só num trecho** da fase 7, depois do save;
  - a contagem ficou em **0,4 s**, com aviso e cancelamento se você voltar;
  - quem corria cruzava as duas linhas e a inversão era cancelada sem querer, então o trecho foi alongado.
- **Bandeiras falsas:** agora têm um **degrau** (tijolo) antes delas, e dá para pular por cima. A bandeira tem 3 de altura e o pulo alcança cerca de 2,8.
- **Folga nas hitboxes dos chefes:**
  - o perigo só mata se encostar de verdade (folga de 0,15 na Rem e 0,08 na Elsa e na Baleia);
  - a "caixa" do Subaru que conta para morrer é menor que o desenho.
- **Aviso mínimo de 0,55 s** para todo ataque que pede pulo, mesmo na etapa 3.
- **A morte ficou mais curta** a partir da 3ª morte na mesma fase, e pode ser pulada.
- **O save não "muda de lugar" em fases de chefe** ("seria maldade demais").
- **Checkpoint por etapa nos chefes** (como em Helltaker), em vez de recomeçar a luta inteira.
- **Gráficos "Simples"** para computadores fracos e navegadores lentos. A tela pixelada também reduz muito o custo de desenho.

---

# 6. GERENCIAMENTO DE ESTADOS E ERROS

## Máquina de estados (GerenciadorDoJogo.cs)
`Título ⇄ Conquistas` → `Mapa` → `Jogando ⇄ Pausado` → `Morreu → Jogando` → `FaseConcluida → Mapa` → ... → `Vitória → Créditos → Título`

- Um único `GerenciadorDoJogo` (singleton) que **se cria sozinho** ao dar Play. A `Interface` só lê o estado e não decide nada.
- **Transições com sombras:** a troca de tela só acontece com a tela coberta. Durante a transição, nenhuma entrada é processada.
- **Pausa:** `Time.timeScale = 0` e o áudio pausado (a música só abaixa). Ao destruir o gerenciador, o `timeScale` volta a 1, para não travar o editor.
- **Morte:** ignorada fora do estado `Jogando` (não dá para morrer duas vezes nem durante "fase concluída").
- **Recriação da fase a cada morte:** a fase é destruída e montada de novo do texto. Nenhuma armadilha fica "meio disparada" e não sobra estado sujo.
  O que precisa sobreviver (save, etapa do chefe, chefe já vencido, marcas das mortes) fica no gerenciador.

## Saves (Progresso.cs, PlayerPrefs)
- O que é salvo:
  - a partida em andamento (o ponto do mapa, a fase mais longe, mortes, moedas, tempo e se a partida vale recorde);
  - as mortes por fase;
  - a fase secreta;
  - o recorde;
  - as conquistas e as opções.
- **Validação de versão:** o save guarda quantas fases o jogo tinha. Se a lista de fases mudou (um save antigo de 9 ou 10 fases), o "Continuar" some, em vez de carregar a fase errada.
- Os índices são limitados (`Mathf.Clamp`) ao carregar o mapa.
- A leitura das mortes por fase tolera texto inválido (vira 0).
- O jogo salva ao entrar numa fase, ao passar de fase, ao voltar ao mapa e ao fechar.

## Robustez
- **Shaders próprios com plano B:** se a placa de vídeo não aceitar o shader, o efeito usa o material padrão (nunca aparece um sprite rosa).
- **Conquista com id desconhecido:** gera um aviso no Console e não quebra o jogo.
- **Letra desconhecida na fonte pixel:** não quebra o texto.
- **"Enter Play Mode Options" sem recarregar o domínio:** os estáticos são limpos com `RuntimeInitializeOnLoadMethod`.

---

# 7. EQUIPE, CRONOGRAMA E RISCOS

## Divisão de tarefas *(editar)*
| Integrante | Responsabilidades |
|---|---|
| (Nome 1) | (ex.: programação do núcleo, chefes) |
| (Nome 2) | (ex.: level design, checador de fases) |
| (Nome 3) | (ex.: arte procedural, efeitos visuais) |
| (Nome 4) | (ex.: música e som, GDD, página do itch.io, playtests) |

## Cronograma *(editar as datas)*
| Semana | Entrega |
|---|---|
| (data) | Conceito, pilares, protótipo do movimento e das primeiras armadilhas |
| (data) | Fases 1 a 5, mapa do mundo, Retorno pela Morte |
| (data) | Mecânicas temáticas (fases 6 a 15) e chefe Rem |
| (data) | Chefes Elsa e Baleia Branca, visual HD-2D |
| (data) | Menus (Opções, Como Jogar, Créditos), narrativa, polimento |
| (data) | Playtests, balanceamento, build WebGL |
| **11/11/2026 23:50** | **Entrega:** build no itch.io (jam), README, GDD |

## Riscos e mitigação
| Risco | Mitigação |
|---|---|
| Difícil demais e o jogador desiste | Morte rápida e pulável, saves, aviso nos chefes, checkpoint por etapa, checador de fases, playtests |
| Desempenho ruim no navegador | Tela pixelada (menos pixels), gráficos "Simples", nada é criado durante o jogo, painel F10 para medir |
| Direitos autorais (tema Re:Zero, áudio do anime) | Fan game sem fins comerciais, créditos explícitos no README e nos créditos, arte toda própria, só 2 áudios externos (fáceis de trocar) |
| Save quebrado entre versões | Validação pelo número de fases e valores limitados |
| Escopo grande para o prazo | Fases em texto (iteração rápida), mecânica nova = arquivo novo, lista de pendências em `PROXIMOS_PASSOS.md` |
| Bug só no build WebGL | Gerar o build cedo e testar no itch.io (página em rascunho) antes da entrega |
