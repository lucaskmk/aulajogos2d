# Publicar e vender o Cilada à Vista!

Plano de 27/11/2026. O que está **feito no código** fica marcado; o resto depende de conta, dinheiro ou
assinatura do dono do jogo (eu não crio contas nem pago taxas).

## Resumo em uma linha

**Vender na Steam** (é onde está o público de plataforma difícil e de coop com amigos), com uma **demo grátis no
Next Fest**, e **abrir já no itch.io** (de graça, para começar a juntar público e testar o online com gente de fora).

## 1. Onde vender (e por quê)

| Loja | Custo para entrar | Quanto a loja fica | Para quê |
|---|---|---|---|
| **Steam** | US$ 100 por jogo (Steam Direct; volta depois de US$ 1.000 vendidos) | 30% | A venda de verdade. Lista de desejos, Next Fest, Remote Play Together, conquistas e placares. |
| **itch.io** | grátis | você escolhe (o padrão é 10%) | Já agora: a versão do navegador (grátis) e a de Windows (paga ou "pague quanto quiser"). Bom para a jam, para professores e para os primeiros fãs. |
| Epic, GOG, consoles | — | — | Só se a Steam for bem. A GOG escolhe a dedo; console precisa de kit e de porte (o Unity ajuda, mas é outro projeto). |

**Por que não grátis com anúncios ou compras dentro do jogo:** jogo de PC difícil vende como produto inteiro
(pagou, tem tudo). E as moedas da Loja e a roleta **nunca** podem ser vendidas por dinheiro de verdade: aí vira jogo de
azar de verdade (hoje é só aposta simulada, com moeda do jogo).

## 2. Preço

- **Sugestão: US$ 5,99** (cerca de 2 a 4 horas de jogo com o Mar de Baixo, as bônus, os desafios e o modo tempestade;
  coop até 4, local e online). Na faixa dos jogos de plataforma difícil feitos por equipes pequenas.
- **Desconto de lançamento de 15% a 20%** na primeira semana (a Steam deixa e ajuda a aparecer).
- **Preços por país:** usar a **sugestão de preço regional da Valve** no Steamworks (o Brasil fica bem mais barato
  que a conversão direta do dólar; não inventar o número na mão).
- **Demo grátis** (fases 1 a 4, até a Capitã: `Cilada > Build Demo`). No itch, a versão do navegador pode ser essa demo.
- No itch: Windows por **US$ 4,99 ou "pague quanto quiser" com mínimo de US$ 2**, até sair na Steam.

> Isto é uma sugestão de preço de produto, não conselho financeiro: confira o mercado na época do lançamento.

## 3. O multiplayer online (FEITO no código)

O coop local até 4 já existia. O online agora tem **dois caminhos**, e os dois usam o mesmo coop testado:

1. **Tripulação Online (dentro do jogo, em qualquer loja):** menu **Jogar online**.
   - O anfitrião clica em **Criar uma sala** e manda o **código da sala** (7 letras) para os amigos.
   - Os amigos clicam em **Entrar na sala de um amigo** e digitam o código. Na mesma casa, a sala aparece sozinha na lista.
   - Funciona como o Remote Play: o jogo roda no computador do anfitrião, os amigos veem a tela dele (imagem e som pela
     rede) e mandam os botões. Para o jogo, cada amigo é **mais um controle ligado**: entra na tripulação pela tela T do mapa.
   - A porta é aberta sozinha no roteador (UPnP). Sem UPnP, ou com **CGNAT** (comum nos provedores do Brasil),
     a tela avisa e sugere uma VPN de jogos (Radmin VPN, ZeroTier, Tailscale) ou a Steam.
   - Código em `Assets/Scripts/Online/` (ideia geral no topo de `Protocolo.cs`); teste automático em
     **Cilada (teste) > Testar online** (`TesteDoOnline.cs`).
2. **Remote Play Together (Steam):** o amigo nem precisa ter o jogo. Botão **Convidar amigos da Steam** na tela online
   (abre o convite da Steam). Precisa marcar "Remote Play Together" na página do Steamworks.

O que fica para depois (só se o jogo pegar): online "de verdade" com cada um rodando o jogo (sem atraso para os
convidados). Isso exige sincronizar física, chefes e armadilhas: é reescrever o jogo. O atraso de hoje é o mesmo do
Remote Play, que milhões de pessoas usam para jogos de plataforma.

## 4. Steam: o passo a passo

### Feito no código
- [x] **Steamworks.NET** no projeto (`Packages/manifest.json`, versão fixa) e `Assets/Scripts/Plataforma/Steam.cs`:
  - liga sozinho quando o jogo abre pela Steam (fora dela, não faz nada e o jogo roda igual);
  - **conquistas**: as 41 de `Conquistas.cs` vão para a Steam (inclusive as que a pessoa já tinha antes);
  - **placares**: tempo e mortes de cada fase, speedrun, nas versões normal e tempestade (`Placares` em `Speedrun.cs`);
  - **presença**: os amigos veem "Jogando: <fase> (tripulação de 3) - sala online aberta";
  - **convite do Remote Play Together**.
- [x] Build de Windows: **Cilada → Build Windows (Steam)**; demo: **Cilada → Build Demo**.
- [x] Scripts de envio: `Publicacao/steam/` (SteamPipe) e `Publicacao/enviar_itch.ps1` (butler).
- [x] Save em arquivo (pronto para o Steam Cloud).

### Só você pode fazer (conta, dinheiro e assinatura)
1. **Conta no Steamworks** (partner.steamgames.com): dados pessoais/da empresa, **banco** e **entrevista fiscal**
   (formulário W-8BEN para quem não é dos EUA). O Brasil não tem tratado de imposto com os EUA: a Valve pode reter
   até 30% das vendas feitas **nos EUA**. Confirmar com um contador antes.
2. **Pagar os US$ 100** do Steam Direct. Daí saem os números: **AppID do jogo** e da **demo**.
3. Pôr os números em `Steam.AppId` e `Steam.AppIdDaDemo` (`Steam.cs`) e em `Publicacao/steam/*.vdf`.
4. **Nome da empresa** em Project Settings > Player (ainda é `DefaultCompany`). Trocar ANTES do lançamento: muda a
   pasta do save.
5. No Steamworks:
   - cadastrar as **conquistas** com os mesmos ids de `Conquistas.Todas` (lista pronta: `python Ferramentas/conquistas_steam.py`);
   - **Steam Cloud** (Auto-Cloud): pasta `%USERPROFILE%/AppData/LocalLow/<empresa>/Cilada à Vista!`, arquivos `*.json` e `fantasmas/*`;
   - marcar **Remote Play Together**, **Suporte total a controle**, **Coop local e online**, idiomas PT-BR e EN;
   - responder o **questionário de conteúdo** (a roleta conta como "aposta simulada");
   - declarar o uso de IA se as imagens de `Assets/Resources/Arte` foram geradas por IA.
6. **Página "Em breve"** com pelo menos 2 semanas de antecedência (o texto está pronto em `Publicacao/pagina_da_loja.md`).
7. **Next Fest** com a demo (as inscrições abrem meses antes: ver o calendário no Steamworks).
8. Enviar o build (`Publicacao/steam/enviar_steam.ps1`), pedir a **revisão** (3 a 5 dias úteis) e lançar.

## 5. itch.io: dá para fazer hoje

1. Criar a página em itch.io/game/new: **Cilada à Vista!**, tipo "Downloadable" + HTML, preço como acima.
2. Gerar os builds: **Cilada → Build Windows (Steam)** e o WebGL (README, "Como gerar o build WebGL").
3. Instalar o **butler** (a ferramenta oficial do itch) e rodar `Publicacao/enviar_itch.ps1 -Usuario <seu usuário do itch>`.
4. Na página: o texto de `Publicacao/pagina_da_loja.md`, 5 capturas, o GIF de uma morte boba e o link da Steam quando existir.

## 6. Como fazer o jogo ser visto (o que mais vende)

- **Listas de desejos** são tudo na Steam: a página "Em breve" o quanto antes.
- **Vídeos curtos de morte** (TikTok, Shorts, Reels): o jogo é feito para isso (a cilada, a piada da morte, o replay
  com todas as tentativas). Um por dia durante o mês antes do Next Fest.
- **Streamers de jogo difícil e de coop com amigos**: mandar chaves da Steam (o Steamworks gera) com a demo e o online.
- **Eventos e festivais de jogos indie brasileiros** e a comunidade da faculdade (a jam do Insper já é um começo).
- O **trailer**: 60 a 90 s, começando por uma morte engraçada nos primeiros 3 s; mostrar o coop caótico e o online.

## 7. Ordem sugerida (com datas a partir de hoje)

1. **Esta semana:** itch.io com a demo no navegador e o Windows; chamar 3 a 5 amigos para testar o online de casas diferentes.
2. **Dezembro:** conta no Steamworks + US$ 100; página "Em breve"; conquistas e placares cadastrados; trailer.
3. **Próximo Next Fest** com a demo (e os vídeos curtos no mês anterior).
4. **Lançamento** logo depois do Next Fest, com desconto de lançamento.
