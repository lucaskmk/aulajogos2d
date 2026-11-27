using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

// O ONLINE do Cilada à Vista ("Tripulação Online") funciona como o Remote Play da Steam, só que dentro do jogo:
//
//   - o ANFITRIÃO (quem cria a sala) roda o jogo de verdade, do jeito que sempre foi, com o coop local;
//   - cada CONVIDADO manda só os botões que está apertando. No computador do anfitrião, cada convidado vira um
//     CONTROLE VIRTUAL (um Gamepad do Input System): para o jogo, é como se alguém tivesse ligado mais um controle.
//     O convidado entra na tripulação igual a qualquer controle (tecla T no mapa, PULAR para entrar);
//   - o anfitrião manda de volta a IMAGEM da tela (JPEG, uns 30 por segundo) e o SOM (mono, 24 kHz, lei mu).
//
// Por que assim, e não "cada um roda o jogo e sincroniza"? O jogo tem física, chefes, armadilhas que mudam depois da
// morte, replay, tsunami... sincronizar tudo isso pela rede seria reescrever o jogo. Transmitindo a tela, o online não
// mexe em NADA da jogabilidade: as fases, os chefes e o coop são exatamente os testados. O preço é o atraso de rede
// para os convidados (como no Remote Play).
//
// Este arquivo é o "fio": as mensagens trocadas e a Conexao (um TCP com uma linha de leitura e uma de escrita).
// Cada mensagem é: 1 byte do tipo + 4 bytes do tamanho (little-endian) + os dados.
public static class Protocolo
{
    // Sobe quando o formato das mensagens muda: anfitrião e convidado precisam ter a mesma versão.
    public const int Versao = 1;

    public const int PortaPadrao = 27960;   // TCP: a sala
    public const int PortaDaBusca = 27961;  // UDP: o "estou aqui" na rede local (broadcast)
    public const string Assinatura = "CILADA-A-VISTA";

    // Uma sala tem o anfitrião (com os jogadores do computador dele) e até 3 convidados: a tripulação é de 4.
    public const int MaximoDeConvidados = 3;

    // Mensagem maior que isso é lixo (ou alguém tentando derrubar a sala): a conexão cai.
    public const int MaiorMensagem = 4 * 1024 * 1024;

    public enum Tipo : byte
    {
        Ola = 1,        // convidado -> anfitrião: versão (int) + nome (texto)
        BemVindo = 2,   // anfitrião -> convidado: número do convidado (byte) + nome da sala (texto)
        Recusado = 3,   // anfitrião -> convidado: o motivo (texto), e a conexão fecha
        Entrada = 4,    // convidado -> anfitrião: os botões (uint) + analógico (2 sbytes)
        Quadro = 5,     // anfitrião -> convidado: a tela em JPEG
        Som = 6,        // anfitrião -> convidado: amostras em lei mu (1 byte cada, 24 kHz, mono)
        Ping = 7,       // os dois lados: um número (long) que volta no Pong
        Pong = 8,
        Tchau = 9,      // saindo (a conexão fecha logo depois)
    }

    // ------------------------------------------------------------------ os botões

    // Os botões vão como os bits do GamepadState do Input System (o índice é o enum GamepadButton):
    // assim o anfitrião só copia o número para o controle virtual.
    public struct Botoes : IEquatable<Botoes>
    {
        public uint bits;
        public sbyte x, y; // o analógico esquerdo, de -127 a 127

        public bool Equals(Botoes outro) => bits == outro.bits && x == outro.x && y == outro.y;
        public override bool Equals(object obj) => obj is Botoes b && Equals(b);
        public override int GetHashCode() => (int)bits ^ (x << 8) ^ (y << 16);

        public byte[] Escrever()
        {
            var dados = new byte[6];
            dados[0] = (byte)bits; dados[1] = (byte)(bits >> 8); dados[2] = (byte)(bits >> 16); dados[3] = (byte)(bits >> 24);
            dados[4] = (byte)x; dados[5] = (byte)y;
            return dados;
        }

        public static bool Ler(byte[] dados, out Botoes b)
        {
            b = default;
            if (dados == null || dados.Length < 6) return false;
            b.bits = (uint)(dados[0] | dados[1] << 8 | dados[2] << 16 | dados[3] << 24);
            b.x = (sbyte)dados[4];
            b.y = (sbyte)dados[5];
            return true;
        }
    }

    // ------------------------------------------------------------------ textos e números

    public static byte[] Texto(string texto) => System.Text.Encoding.UTF8.GetBytes(texto ?? "");
    public static string Texto(byte[] dados, int inicio = 0) =>
        dados == null || dados.Length <= inicio ? "" : System.Text.Encoding.UTF8.GetString(dados, inicio, Math.Min(dados.Length - inicio, 200));

    public static byte[] Numero(long n) => BitConverter.GetBytes(n);
    public static long Numero(byte[] dados) => dados != null && dados.Length >= 8 ? BitConverter.ToInt64(dados, 0) : 0;

    public static byte[] Ola(string nome)
    {
        byte[] texto = Texto(nome);
        var dados = new byte[4 + texto.Length];
        BitConverter.GetBytes(Versao).CopyTo(dados, 0);
        texto.CopyTo(dados, 4);
        return dados;
    }

    // ------------------------------------------------------------------ o som (lei mu: 16 bits viram 8, sem perder muito)

    const int Vies = 0x84, Teto = 32635;

    public static byte MuLaw(float amostra)
    {
        int pcm = (int)(Math.Max(-1f, Math.Min(1f, amostra)) * 32767f);
        int sinal = (pcm >> 8) & 0x80;
        if (sinal != 0) pcm = -pcm;
        if (pcm > Teto) pcm = Teto;
        pcm += Vies;
        int expoente = 7;
        for (int mascara = 0x4000; (pcm & mascara) == 0 && expoente > 0; mascara >>= 1) expoente--;
        int mantissa = (pcm >> (expoente + 3)) & 0x0F;
        return (byte)~(sinal | (expoente << 4) | mantissa);
    }

    static float[] tabelaMu;

    public static float DeMuLaw(byte b)
    {
        if (tabelaMu == null)
        {
            var tabela = new float[256];
            for (int i = 0; i < 256; i++)
            {
                int u = ~i & 0xFF;
                int sinal = u & 0x80, expoente = (u >> 4) & 0x07, mantissa = u & 0x0F;
                int pcm = (((mantissa << 3) + Vies) << expoente) - Vies;
                tabela[i] = (sinal != 0 ? -pcm : pcm) / 32768f;
            }
            tabelaMu = tabela;
        }
        return tabelaMu[b];
    }
}

public sealed class Mensagem
{
    public Protocolo.Tipo tipo;
    public byte[] dados;
}

// Uma conexão TCP com as mensagens do Protocolo. Ler e escrever acontecem em linhas (threads) próprias:
// o jogo só põe na fila (Enviar) e tira da fila (Receber), sem nunca esperar a rede.
//
// O vídeo tem um tratamento especial: só o quadro MAIS NOVO espera para sair. Se a internet do convidado não dá conta
// de 30 quadros por segundo, os velhos são jogados fora (melhor pular quadro do que a imagem ir ficando atrasada).
// O som também é descartado se acumular demais na fila (um convidado com a rede travada não trava os outros).
public sealed class Conexao : IDisposable
{
    readonly TcpClient cliente;
    readonly NetworkStream fluxo;
    readonly ConcurrentQueue<Mensagem> recebidas = new ConcurrentQueue<Mensagem>();
    readonly ConcurrentQueue<Mensagem> paraEnviar = new ConcurrentQueue<Mensagem>();
    readonly AutoResetEvent temAlgoParaEnviar = new AutoResetEvent(false);
    Mensagem quadroEsperando;
    int bytesNaFila;
    volatile bool fechada;

    public string Endereco { get; }
    public string Motivo { get; private set; } = "";
    public bool Aberta => !fechada;
    public long BytesEnviados => Interlocked.Read(ref enviados);
    public long BytesRecebidos => Interlocked.Read(ref recebidosTotal);
    public int QuadrosDescartados => descartados;
    long enviados, recebidosTotal;
    int descartados;

    // Quanto de som pode ficar na fila antes de começar a jogar fora (uns 2 s de som).
    const int LimiteDaFila = 48 * 1024;

    public Conexao(TcpClient cliente)
    {
        this.cliente = cliente;
        cliente.NoDelay = true; // os botões não podem esperar o "Nagle" juntar pacotes
        cliente.SendBufferSize = 512 * 1024;
        cliente.ReceiveBufferSize = 512 * 1024;
        fluxo = cliente.GetStream();
        try { Endereco = cliente.Client.RemoteEndPoint?.ToString() ?? "?"; } catch { Endereco = "?"; }
        new Thread(Ler) { IsBackground = true, Name = "Cilada rede (leitura)" }.Start();
        new Thread(Escrever) { IsBackground = true, Name = "Cilada rede (escrita)" }.Start();
    }

    public void Enviar(Protocolo.Tipo tipo, byte[] dados)
    {
        if (fechada) return;
        dados ??= Array.Empty<byte>();
        if (tipo == Protocolo.Tipo.Som && Volatile.Read(ref bytesNaFila) > LimiteDaFila) return; // rede entupida: pula este pedaço
        Interlocked.Add(ref bytesNaFila, dados.Length);
        paraEnviar.Enqueue(new Mensagem { tipo = tipo, dados = dados });
        temAlgoParaEnviar.Set();
    }

    // O quadro de vídeo: substitui o que ainda não saiu.
    public void EnviarQuadro(byte[] jpeg)
    {
        if (fechada) return;
        if (Interlocked.Exchange(ref quadroEsperando, new Mensagem { tipo = Protocolo.Tipo.Quadro, dados = jpeg }) != null)
            Interlocked.Increment(ref descartados);
        temAlgoParaEnviar.Set();
    }

    // Ainda tem um quadro esperando para sair? (o anfitrião nem codifica o próximo: economiza o processador)
    public bool QuadroNaFila => Volatile.Read(ref quadroEsperando) != null;

    public bool Receber(out Mensagem mensagem) => recebidas.TryDequeue(out mensagem);

    public void Fechar(string motivo)
    {
        if (fechada) return;
        Motivo = motivo;
        fechada = true;
        temAlgoParaEnviar.Set();
        try { cliente.Close(); } catch { }
    }

    // Manda uma última mensagem (Tchau ou Recusado) e fecha logo depois, dando tempo de ela sair.
    public void Despedir(string motivo, Protocolo.Tipo tipo = Protocolo.Tipo.Tchau, byte[] dados = null)
    {
        if (fechada) return;
        Enviar(tipo, dados);
        System.Threading.Tasks.Task.Delay(300).ContinueWith(_ => Fechar(motivo));
    }

    public void Dispose() => Fechar("fechada");

    void Ler()
    {
        var cabecalho = new byte[5];
        try
        {
            while (!fechada)
            {
                LerTudo(cabecalho, 5);
                int tamanho = BitConverter.ToInt32(cabecalho, 1);
                if (tamanho < 0 || tamanho > Protocolo.MaiorMensagem) throw new IOException("mensagem grande demais");
                var dados = new byte[tamanho];
                LerTudo(dados, tamanho);
                Interlocked.Add(ref recebidosTotal, tamanho + 5);
                recebidas.Enqueue(new Mensagem { tipo = (Protocolo.Tipo)cabecalho[0], dados = dados });
            }
        }
        catch (Exception e)
        {
            Fechar(fechada ? Motivo : e is IOException || e is SocketException || e is ObjectDisposedException ? "a conexão caiu" : e.Message);
        }
    }

    void LerTudo(byte[] destino, int quantos)
    {
        int lidos = 0;
        while (lidos < quantos)
        {
            int n = fluxo.Read(destino, lidos, quantos - lidos);
            if (n <= 0) throw new IOException("o outro lado fechou");
            lidos += n;
        }
    }

    void Escrever()
    {
        var cabecalho = new byte[5];
        try
        {
            while (!fechada)
            {
                temAlgoParaEnviar.WaitOne(250);
                // primeiro tudo o que é pequeno e não pode se perder (botões, som, avisos), depois o quadro mais novo
                while (!fechada && paraEnviar.TryDequeue(out Mensagem m))
                {
                    Interlocked.Add(ref bytesNaFila, -m.dados.Length);
                    Mandar(cabecalho, m);
                }
                Mensagem quadro = Interlocked.Exchange(ref quadroEsperando, null);
                if (quadro != null && !fechada) Mandar(cabecalho, quadro);
            }
        }
        catch (Exception e)
        {
            Fechar(fechada ? Motivo : e is IOException || e is SocketException || e is ObjectDisposedException ? "a conexão caiu" : e.Message);
        }
    }

    void Mandar(byte[] cabecalho, Mensagem m)
    {
        cabecalho[0] = (byte)m.tipo;
        BitConverter.GetBytes(m.dados.Length).CopyTo(cabecalho, 1);
        fluxo.Write(cabecalho, 0, 5);
        if (m.dados.Length > 0) fluxo.Write(m.dados, 0, m.dados.Length);
        Interlocked.Add(ref enviados, m.dados.Length + 5);
    }
}
