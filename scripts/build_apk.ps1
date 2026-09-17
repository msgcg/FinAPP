# FinAPP — Скрипт сборки релизного подписанного APK для Android / RuStore
param(
    [string]$Configuration = "Release",
    [string]$KeyAlias = "finapp",
    [string]$KeyPass = "FinAppPass2026!",
    [string]$StorePass = "FinAppPass2026!"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$DistDir = Join-Path $ProjectRoot "dist"
$KeystorePath = Join-Path $DistDir "finapp_release.keystore"
$ProjectFile = Join-Path $ProjectRoot "FinAPP\FinAPP.csproj"

if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir | Out-Null
}

# 1. Поиск утилиты keytool
$KeytoolCmd = "keytool"
if ($env:JAVA_HOME) {
    $JdkKeytool = Join-Path $env:JAVA_HOME "bin\keytool.exe"
    if (Test-Path $JdkKeytool) {
        $KeytoolCmd = $JdkKeytool
    }
}

# 2. Генерация ключа подписи, если он отсутствует
if (-not (Test-Path $KeystorePath)) {
    Write-Host "Генерация ключа подписи релиза: $KeystorePath..." -ForegroundColor Cyan
    & $KeytoolCmd -genkeypair -v `
        -keystore $KeystorePath `
        -alias $KeyAlias `
        -keyalg RSA `
        -keysize 2048 `
        -validity 10000 `
        -storepass $StorePass `
        -keypass $KeyPass `
        -dname "CN=FinAPP, OU=LCT2026, O=MoscowFinance, L=Moscow, ST=Moscow, C=RU"
}

Write-Host "Очистка кэша resizetizer в $env:TEMP\FinAPP..." -ForegroundColor Cyan
Remove-Item -Path "$env:TEMP\FinAPP\obj\$Configuration" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:TEMP\FinAPP\bin\$Configuration" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Запуск компиляции и сборки релизного APK ($Configuration)..." -ForegroundColor Cyan

# 3. Публикация и подпись APK через dotnet publish
$publishArgs = @(
    "publish", $ProjectFile,
    "-f", "net10.0-android",
    "-c", $Configuration,
    "-p:AndroidKeyStore=true",
    "-p:AndroidSigningKeyStore=$KeystorePath",
    "-p:AndroidSigningKeyAlias=$KeyAlias",
    "-p:AndroidSigningKeyPass=$KeyPass",
    "-p:AndroidSigningStorePass=$StorePass"
)

& dotnet $publishArgs

# 4. Поиск собранного подписанного APK в %TEMP%\FinAPP
$SignedApk = Get-ChildItem -Path "$env:TEMP\FinAPP\bin\$Configuration" -Recurse -Filter "*Signed.apk" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $SignedApk) {
    $SignedApk = Get-ChildItem -Path "$ProjectRoot" -Recurse -Filter "*Signed.apk" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

if ($SignedApk) {
    $TargetApk = Join-Path $DistDir "FinAPP.apk"
    Copy-Item -Path $SignedApk.FullName -Destination $TargetApk -Force

    $Hash = (Get-FileHash -Path $TargetApk -Algorithm SHA256).Hash
    $SizeMb = [math]::Round((Get-Item $TargetApk).Length / 1MB, 2)

    Write-Host "`nРелизный APK успешно собран и подписан!" -ForegroundColor Green
    Write-Host "Файл: $TargetApk ($SizeMb MB)" -ForegroundColor Green
    Write-Host "SHA-256: $Hash" -ForegroundColor Green
} else {
    Write-Warning "APK файл не найден в путях публикации. Проверьте вывод сборки."
}
