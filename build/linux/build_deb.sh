#!/usr/bin/env bash
# 构建 FluentSend Linux deb 包
# 用法: ./build_deb.sh [output_dir]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${1:-$ROOT/dist}"
PROJECT="$ROOT/src/FluentSend"
ARCH="${ARCH:-amd64}"
VERSION="${VERSION:-0.45.0}"
PKGNAME="fluentsend_${VERSION}_${ARCH}"

STAGE="$OUT/deb-stage/$PKGNAME"
rm -rf "$STAGE"
mkdir -p "$STAGE/DEBIAN" \
         "$STAGE/usr/bin" \
         "$STAGE/usr/lib/fluentsend" \
         "$STAGE/usr/share/applications" \
         "$STAGE/usr/share/icons/hicolor/512x512/apps" \
         "$STAGE/usr/share/icons/hicolor/scalable/apps"

echo "[1/4] 发布 FluentSend 桌面应用..."
dotnet publish "$PROJECT/FluentSend/FluentSend.csproj" \
    -c Release \
    -f net10.0 \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -o "$STAGE/usr/lib/fluentsend"

echo "[2/4] 复制原生库与入口文件..."
install -m 0755 "$ROOT/build/linux/fluentsend.desktop" \
        "$STAGE/usr/share/applications/fluentsend.desktop"
install -m 0644 "$PROJECT/FluentSend/Assets/Icon.png" \
        "$STAGE/usr/share/icons/hicolor/512x512/apps/fluentsend.png"
if [ -f "$ROOT/build/linux/fluentsend.svg" ]; then
    install -m 0644 "$ROOT/build/linux/fluentsend.svg" \
            "$STAGE/usr/share/icons/hicolor/scalable/apps/fluentsend.svg"
fi

ln -sf ../lib/fluentsend/FluentSend "$STAGE/usr/bin/fluentsend"

echo "[3/4] 复制 deb 元数据..."
install -m 0644 "$ROOT/build/linux/DEBIAN/control" "$STAGE/DEBIAN/control"
install -m 0755 "$ROOT/build/linux/DEBIAN/postinst" "$STAGE/DEBIAN/postinst"
install -m 0755 "$ROOT/build/linux/DEBIAN/postrm"  "$STAGE/DEBIAN/postrm"

# 用实际安装尺寸回填 Installed-Size
SIZE="$(du -sk "$STAGE" | awk '{print $1}')"
sed -i "s/^Installed-Size:.*/Installed-Size: $SIZE/" "$STAGE/DEBIAN/control"
case "$ARCH" in
    amd64) sed -i "s/^Architecture:.*/Architecture: amd64/" "$STAGE/DEBIAN/control" ;;
    arm64) sed -i "s/^Architecture:.*/Architecture: arm64/" "$STAGE/DEBIAN/control" ;;
esac

echo "[4/4] 打包 .deb..."
mkdir -p "$OUT"
( cd "$STAGE/.." && dpkg-deb --build --root-owner-group "$PKGNAME" )
mv "$OUT/deb-stage/$PKGNAME.deb" "$OUT/$PKGNAME.deb"
rm -rf "$OUT/deb-stage"

echo "完成：$OUT/$PKGNAME.deb"
