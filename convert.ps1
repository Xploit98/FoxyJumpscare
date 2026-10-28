param(
    [Parameter(Mandatory)][string]$Video,
    [int]$Fps = 20,
    [string]$Key = "0x00FF00"   # green screen color
)
$out = "FoxyJumpscare"
New-Item -ItemType Directory -Force "$out\frames" | Out-Null
ffmpeg -y -i $Video -vf "fps=$Fps,scale=640:-2,colorkey=${Key}:0.3:0.1,vflip,format=rgba" -f image2 -c:v rawvideo "$out\frames\f_%04d.rgba"
ffmpeg -y -i $Video -vn -ac 2 -ar 44100 "$out\foxy.wav"
Write-Host "Done. Copy the '$out' folder into BepInEx\plugins\"