# Sobe o build de Windows (ou a demo) para a Steam com o SteamPipe.
#
# Antes (uma vez só):
#   1. Baixe o Steamworks SDK no site da Valve (partner.steamgames.com > SDK) e anote onde ficou o steamcmd.exe
#      (sdk\tools\ContentBuilder\builder\steamcmd.exe).
#   2. Troque os números "480"/"481" dos .vdf desta pasta pelo AppID e pelo depot do jogo (e da demo).
#   3. Gere o build: no Unity, Cilada > Build Windows (Steam)  (ou Cilada > Build Demo).
#
# Uso:
#   .\Publicacao\steam\enviar_steam.ps1 -Usuario minha_conta_steamworks -SteamCmd C:\steamworks\sdk\tools\ContentBuilder\builder\steamcmd.exe
#   .\Publicacao\steam\enviar_steam.ps1 -Usuario minha_conta_steamworks -SteamCmd ... -Demo
#
# A senha e o código do Steam Guard são pedidos pelo próprio steamcmd (não ficam neste arquivo).
# O build sobe SEM ir para o ar: depois, no Steamworks (Builds), marque o build novo na branch "default".
param(
    [Parameter(Mandatory = $true)][string]$Usuario,
    [Parameter(Mandatory = $true)][string]$SteamCmd,
    [switch]$Demo
)

$pasta = Split-Path -Parent $MyInvocation.MyCommand.Path
$raiz = Resolve-Path (Join-Path $pasta "..\..")
$build = if ($Demo) { Join-Path $raiz "Builds\Demo\CiladaAVista.exe" } else { Join-Path $raiz "Builds\Windows\CiladaAVista.exe" }
$roteiro = if ($Demo) { Join-Path $pasta "app_build_demo.vdf" } else { Join-Path $pasta "app_build.vdf" }

if (-not (Test-Path $SteamCmd)) { Write-Error "Não achei o steamcmd em $SteamCmd"; exit 1 }
if (-not (Test-Path $build)) { Write-Error "Não achei o build em $build (gere pelo menu Cilada no Unity)"; exit 1 }
if ((Get-Content $roteiro -Raw) -match '"AppID" "480"') {
    Write-Warning "O $([IO.Path]::GetFileName($roteiro)) ainda está com o AppID 480 (o app de teste da Valve). Troque pelo número do jogo."
    exit 1
}

& $SteamCmd +login $Usuario +run_app_build $roteiro +quit
exit $LASTEXITCODE
