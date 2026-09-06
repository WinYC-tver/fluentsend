#!/usr/bin/env bash
# 构建 FluentSend Android APK
# 用法: ./build_apk.sh [output_dir]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${1:-$ROOT/dist}"
PROJECT="$ROOT/src/FluentSend"
CONFIG="${CONFIG:-Release}"

mkdir -p "$OUT"

echo "[1/2] 发布 FluentSend.Android..."
dotnet publish "$PROJECT/FluentSend.Android/FluentSend.Android.csproj" \
    -c "$CONFIG" \
    -f net10.0-android \
    -p:AndroidPackageFormat=apk \
    -p:AndroidUseAapt2=true \
    -p:AndroidCreatePackagePerAbi=true \
    -p:AndroidLinkMode=sdkAssemblies \
    -o "$OUT/apk"

echo "[2/2] 收集 APK 到 dist 根目录..."
for apk in "$OUT"/apk/FluentSend.Android-*-Signed.apk "$OUT"/apk/FluentSend.Android-Signed.apk; do
    if [ -f "$apk" ]; then
        cp -f "$apk" "$OUT/"
        echo "完成：$(basename "$apk")"
    fi
done

exit 0
