# Sobe o jogo para o itch.io com o butler (a ferramenta oficial do itch: itch.io/docs/butler).
#
# Antes (uma vez só):
#   1. Crie a página do jogo no itch (itch.io/game/new). O endereço fica <usuario>.itch.io/<jogo>.
#   2. Instale o butler e faça "butler login" (abre o navegador para você entrar na sua conta).
#   3. Gere os builds no Unity: Cilada > Build Windows (Steam) e o WebGL (README: "Como gerar o build WebGL"),
#      que sai em Builds/WebGL. A demo (Cilada > Build Demo) sai em Builds/Demo.
#
# Uso:
#   .\Publicacao\enviar_itch.ps1 -Usuario meu_usuario_do_itch
#   .\Publicacao\enviar_itch.ps1 -Usuario meu_usuario_do_itch -Jogo cilada-a-vista -Versao 1.0.0
#
# Cada pasta vira um "canal" na página: windows, html5 e demo-windows. O butler só manda o que mudou.
param(
    [Parameter(Mandatory = $true)][string]$Usuario,
    [string]$Jogo = "cilada-a-vista",
    [string]$Versao = ""
)

$raiz = Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "..")
if (-not (Get-Command butler -ErrorAction SilentlyContinue)) {
    Write-Error "Não achei o butler. Instale em itch.io/docs/butler e rode 'butler login'."
    exit 1
}

$canais = @(
    @{ Pasta = "Builds\Windows"; Canal = "windows" },
    @{ Pasta = "Builds\WebGL"; Canal = "html5" },
    @{ Pasta = "Builds\Demo"; Canal = "demo-windows" }
)
$algum = $false
foreach ($c in $canais) {
    $pasta = Join-Path $raiz $c.Pasta
    if (-not (Test-Path $pasta)) { Write-Host "(sem $($c.Pasta): pulando o canal $($c.Canal))"; continue }
    $algum = $true
    $argumentos = @("push", $pasta, "$Usuario/$($Jogo):$($c.Canal)")
    if ($Versao -ne "") { $argumentos += @("--userversion", $Versao) }
    Write-Host "Enviando $($c.Pasta) para $Usuario/$($Jogo):$($c.Canal)..."
    & butler @argumentos
    if ($LASTEXITCODE -ne 0) { Write-Error "O butler falhou no canal $($c.Canal)"; exit $LASTEXITCODE }
}
if (-not $algum) { Write-Error "Nenhum build em Builds/: gere pelo menu Cilada no Unity."; exit 1 }
Write-Host "Pronto. Confira a página em https://$Usuario.itch.io/$Jogo"
