using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// Como os amigos acham a sala:
//
//  1. NA MESMA REDE (mesma casa, mesmo Wi-Fi): o anfitrião grita "estou aqui" por broadcast UDP e a sala aparece
//     sozinha na lista do "Entrar numa sala" (BuscaNaRede).
//  2. PELA INTERNET: o jogo pede ao roteador para abrir a porta (UPnP, que a maioria dos roteadores de casa tem) e
//     descobre o IP público. O CÓDIGO DA SALA é esse IP (e a porta) escrito em letras: "K7Q2-M9X" é mais fácil de
//     mandar no zap do que "189.45.12.200:27960".
//  3. Sem UPnP, ou com o provedor usando CGNAT (comum no Brasil: o "IP público" do roteador começa com 100.64 a
//     100.127), não dá para receber conexões de fora. Aí vale uma VPN de jogos (Radmin VPN, ZeroTier, Tailscale: os
//     amigos entram pelo IP da VPN) ou, na Steam, o Remote Play Together.
public static class CodigoDaSala
{
    // Base 32 de Crockford: sem I, L, O e U (não confunde com 1, 0 e V).
    const string Letras = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    // IP + porta -> "XXXX-XXX" (só o IP, na porta padrão) ou "XXXX-XXXX-XX" (com a porta).
    public static string Gerar(IPAddress ip, int porta)
    {
        byte[] b = ip.GetAddressBytes();
        if (b.Length != 4) return ip + ":" + porta;
        ulong n = (ulong)b[0] << 24 | (ulong)b[1] << 16 | (ulong)b[2] << 8 | b[3];
        int bits = 32;
        if (porta != Protocolo.PortaPadrao)
        {
            n = n << 16 | (uint)porta;
            bits = 48;
        }
        int letras = (bits + 4) / 5;
        var texto = new StringBuilder();
        for (int i = letras - 1; i >= 0; i--) texto.Append(Letras[(int)(n >> (i * 5) & 31)]);
        string s = texto.ToString();
        return s.Length > 4 ? s.Substring(0, 4) + "-" + s.Substring(4) : s;
    }

    // O que o convidado digitou: um código, um IP, IP:porta ou um nome (meupc.local). Devolve falso se não entendeu.
    public static bool Ler(string digitado, out string host, out int porta)
    {
        host = null;
        porta = Protocolo.PortaPadrao;
        if (string.IsNullOrWhiteSpace(digitado)) return false;
        string s = digitado.Trim();

        // tem ponto ou dois-pontos: é um endereço (IPv4, IPv4:porta ou nome)
        if (s.Contains(".") || s.Contains(":") || s.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            int doisPontos = s.LastIndexOf(':');
            if (doisPontos > 0 && s.IndexOf(':') == doisPontos)
            {
                if (!int.TryParse(s.Substring(doisPontos + 1), out porta) || porta <= 0 || porta > 65535) return false;
                s = s.Substring(0, doisPontos);
            }
            host = s;
            return s.Length > 0;
        }

        // um código: tira os tracinhos e os espaços, e troca as letras que parecem números
        var limpo = new StringBuilder();
        foreach (char c0 in s.ToUpperInvariant())
        {
            char c = c0 == 'O' ? '0' : c0 == 'I' || c0 == 'L' ? '1' : c0 == 'U' ? 'V' : c0;
            if (c == '-' || c == ' ') continue;
            if (Letras.IndexOf(c) < 0) return false;
            limpo.Append(c);
        }
        if (limpo.Length != 7 && limpo.Length != 10) return false;
        ulong n = 0;
        foreach (char c in limpo.ToString()) n = n << 5 | (uint)Letras.IndexOf(c);
        if (limpo.Length == 10)
        {
            porta = (int)(n & 0xFFFF);
            n >>= 16;
            if (porta == 0) return false;
        }
        if (n > 0xFFFFFFFF) return false;
        host = $"{n >> 24 & 255}.{n >> 16 & 255}.{n >> 8 & 255}.{n & 255}";
        return true;
    }
}

public static class Rede
{
    // O IP deste computador na rede de casa (o que o roteador deu). Pega a placa que sai para a internet.
    public static IPAddress IpLocal()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 53); // (UDP não manda nada ao "conectar": só escolhe a placa de rede)
            return ((IPEndPoint)s.LocalEndPoint).Address;
        }
        catch
        {
            try
            {
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) return ip;
            }
            catch { }
            return IPAddress.Loopback;
        }
    }

    // 10.x, 172.16-31.x, 192.168.x: endereços de casa (não dá para entrar pela internet com eles).
    public static bool Privado(IPAddress ip)
    {
        byte[] b = ip.GetAddressBytes();
        return b.Length == 4 && (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || b[0] == 127);
    }

    // 100.64.0.0/10: o CGNAT dos provedores. O roteador "acha" que tem IP público, mas quem tem é o provedor.
    public static bool Cgnat(IPAddress ip)
    {
        byte[] b = ip.GetAddressBytes();
        return b.Length == 4 && b[0] == 100 && b[1] >= 64 && b[1] <= 127;
    }
}

// ------------------------------------------------------------------ a sala na rede local (broadcast)

// O anfitrião manda "CILADA-A-VISTA|versão|porta|nome" a cada segundo; o convidado escuta e monta a lista.
public sealed class AnuncioNaRede : IDisposable
{
    readonly UdpClient udp;
    readonly Timer relogio;
    readonly byte[] mensagem;

    public AnuncioNaRede(int porta, string nome)
    {
        mensagem = Encoding.UTF8.GetBytes($"{Protocolo.Assinatura}|{Protocolo.Versao}|{porta}|{nome}");
        udp = new UdpClient { EnableBroadcast = true };
        relogio = new Timer(_ => Gritar(), null, 0, 1000);
    }

    void Gritar()
    {
        try { udp.Send(mensagem, mensagem.Length, new IPEndPoint(IPAddress.Broadcast, Protocolo.PortaDaBusca)); }
        catch { } // sem rede: tenta de novo daqui a 1 s
    }

    public void Dispose()
    {
        relogio.Dispose();
        try { udp.Close(); } catch { }
    }
}

public sealed class BuscaNaRede : IDisposable
{
    public struct Sala
    {
        public string nome, host;
        public int porta;
        public bool versaoCerta;
        public DateTime vistaEm;
    }

    readonly UdpClient udp;
    readonly Dictionary<string, Sala> salas = new Dictionary<string, Sala>();
    volatile bool parar;
    public string Erro { get; private set; } = "";

    public BuscaNaRede()
    {
        try
        {
            udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, Protocolo.PortaDaBusca));
            new Thread(Escutar) { IsBackground = true, Name = "Cilada busca na rede" }.Start();
        }
        catch (Exception e)
        {
            Erro = e.Message;
            udp = null;
        }
    }

    void Escutar()
    {
        var de = new IPEndPoint(IPAddress.Any, 0);
        while (!parar)
        {
            try
            {
                byte[] dados = udp.Receive(ref de);
                string[] partes = Encoding.UTF8.GetString(dados).Split('|');
                if (partes.Length < 4 || partes[0] != Protocolo.Assinatura || !int.TryParse(partes[2], out int porta)) continue;
                var sala = new Sala
                {
                    host = de.Address.ToString(), porta = porta, vistaEm = DateTime.UtcNow,
                    nome = partes[3].Length > 32 ? partes[3].Substring(0, 32) : partes[3],
                    versaoCerta = partes[1] == Protocolo.Versao.ToString(),
                };
                lock (salas) salas[sala.host + ":" + porta] = sala;
            }
            catch { if (parar) return; Thread.Sleep(200); }
        }
    }

    // As salas vistas nos últimos 4 segundos (a ordem não muda de um quadro para o outro).
    public List<Sala> Salas()
    {
        var lista = new List<Sala>();
        lock (salas)
        {
            foreach (var par in salas)
                if ((DateTime.UtcNow - par.Value.vistaEm).TotalSeconds < 4) lista.Add(par.Value);
        }
        lista.Sort((a, b) => string.CompareOrdinal(a.host, b.host));
        return lista;
    }

    public void Dispose()
    {
        parar = true;
        try { udp?.Close(); } catch { }
    }
}

// ------------------------------------------------------------------ UPnP: pedir ao roteador para abrir a porta

// O protocolo "Internet Gateway Device": acha o roteador por SSDP (multicast), lê a descrição dele (XML) e manda os
// pedidos SOAP AddPortMapping / GetExternalIPAddress / DeletePortMapping. Tudo numa Task (o jogo não trava).
public sealed class PortaNoRoteador
{
    public enum Situacao { Procurando, Aberta, SemUpnp, Falhou }
    public Situacao Estado { get; private set; } = Situacao.Procurando;
    public IPAddress IpPublico { get; private set; }
    public string Detalhe { get; private set; } = "";

    readonly int porta;
    string urlDeControle, tipoDoServico;
    IPAddress ipLocal;
    static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };

    public PortaNoRoteador(int porta)
    {
        this.porta = porta;
        Task.Run(Abrir);
    }

    async Task Abrir()
    {
        try
        {
            ipLocal = Rede.IpLocal();
            string local = await Procurar();
            if (local == null) { Estado = Situacao.SemUpnp; Detalhe = "o roteador não respondeu ao UPnP"; return; }
            if (!await LerDescricao(local)) { Estado = Situacao.SemUpnp; Detalhe = "o roteador não tem o serviço de abrir portas"; return; }

            string resposta = await Soap("AddPortMapping",
                "<NewRemoteHost></NewRemoteHost>" +
                $"<NewExternalPort>{porta}</NewExternalPort><NewProtocol>TCP</NewProtocol>" +
                $"<NewInternalPort>{porta}</NewInternalPort><NewInternalClient>{ipLocal}</NewInternalClient>" +
                "<NewEnabled>1</NewEnabled><NewPortMappingDescription>Cilada a Vista</NewPortMappingDescription>" +
                "<NewLeaseDuration>0</NewLeaseDuration>");
            if (resposta == null) { Estado = Situacao.Falhou; Detalhe = "o roteador não deixou abrir a porta " + porta; return; }

            string externo = await Soap("GetExternalIPAddress", "");
            Match m = externo == null ? null : Regex.Match(externo, @"<NewExternalIPAddress>\s*([0-9.]+)\s*</NewExternalIPAddress>");
            if (m != null && m.Success && IPAddress.TryParse(m.Groups[1].Value, out IPAddress ip)) IpPublico = ip;
            Estado = Situacao.Aberta;
        }
        catch (Exception e)
        {
            Estado = Situacao.Falhou;
            Detalhe = e.Message;
        }
    }

    // Fecha a porta no roteador (ao fechar a sala). Espera no máximo 1,5 s.
    public void Fechar()
    {
        if (Estado != Situacao.Aberta) return;
        Estado = Situacao.Falhou;
        try
        {
            Soap("DeletePortMapping", $"<NewRemoteHost></NewRemoteHost><NewExternalPort>{porta}</NewExternalPort><NewProtocol>TCP</NewProtocol>")
                .Wait(1500);
        }
        catch { }
    }

    static async Task<string> Procurar()
    {
        string[] alvos = { "urn:schemas-upnp-org:device:InternetGatewayDevice:1", "urn:schemas-upnp-org:service:WANIPConnection:1",
                           "urn:schemas-upnp-org:service:WANPPPConnection:1", "urn:schemas-upnp-org:device:InternetGatewayDevice:2" };
        using var udp = new UdpClient(0);
        var destino = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
        foreach (string alvo in alvos)
        {
            byte[] pedido = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: " + alvo + "\r\n\r\n");
            await udp.SendAsync(pedido, pedido.Length, destino);
        }
        var limite = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < limite)
        {
            Task<UdpReceiveResult> receber = udp.ReceiveAsync();
            if (await Task.WhenAny(receber, Task.Delay(limite - DateTime.UtcNow)) != receber) break;
            string resposta = Encoding.ASCII.GetString(receber.Result.Buffer);
            Match m = Regex.Match(resposta, @"^LOCATION:\s*(\S+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success) return m.Groups[1].Value.Trim();
        }
        return null;
    }

    async Task<bool> LerDescricao(string local)
    {
        string xml = await http.GetStringAsync(local);
        foreach (string servico in new[] { "WANIPConnection:2", "WANIPConnection:1", "WANPPPConnection:1" })
        {
            Match m = Regex.Match(xml, @"<serviceType>\s*(urn:schemas-upnp-org:service:" + servico + @")\s*</serviceType>.*?<controlURL>\s*([^<]+?)\s*</controlURL>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            tipoDoServico = m.Groups[1].Value;
            var baseUri = new Uri(local);
            Match urlBase = Regex.Match(xml, @"<URLBase>\s*([^<]+?)\s*</URLBase>", RegexOptions.IgnoreCase);
            if (urlBase.Success && Uri.TryCreate(urlBase.Groups[1].Value, UriKind.Absolute, out Uri dada)) baseUri = dada;
            urlDeControle = new Uri(baseUri, m.Groups[2].Value).ToString();
            return true;
        }
        return false;
    }

    async Task<string> Soap(string acao, string argumentos)
    {
        string corpo = "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                       "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body>" +
                       $"<u:{acao} xmlns:u=\"{tipoDoServico}\">{argumentos}</u:{acao}></s:Body></s:Envelope>";
        using var pedido = new HttpRequestMessage(HttpMethod.Post, urlDeControle) { Content = new StringContent(corpo, Encoding.UTF8, "text/xml") };
        pedido.Headers.TryAddWithoutValidation("SOAPAction", $"\"{tipoDoServico}#{acao}\"");
        using HttpResponseMessage resposta = await http.SendAsync(pedido);
        return resposta.IsSuccessStatusCode ? await resposta.Content.ReadAsStringAsync() : null;
    }
}
