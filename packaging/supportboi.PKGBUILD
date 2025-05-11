# Maintainer: Karl Essinger <xkaess22@gmail.com>
pkgname=supportboi
pkgver=4.0.1
pkgrel=1
pkgdesc="A support ticket Discord bot with automated interviews and rendered HTML transcripts"
arch=("x86_64")
url="https://github.com/KarlOfDuty/SupportBoi"
license=('GPL-3.0-or-later')
options=('!debug' '!strip')
depends=(
  "dotnet-runtime-9.0"
  "mysql"
)
makedepends=(
  "dotnet-sdk-9.0"
)
#checkdepends=()
#optdepends=()
#provides=()
conflicts=(
  "supportboi-git"
)
#replaces=()
#backup=()
#options=()
#install=supportboi.install
#changelog=
#source=("git+${url}.git#tag=${pkgver}")
source=("git+${url}.git")
#noextract=()
sha512sums=("SKIP")
#validpgpkeys=()

_srcdir="SupportBoi"

prepare() {
  cd "$_srcdir"
  dotnet restore
}

build() {
  cd "$_srcdir"
  ls -lah
  dotnet publish SupportBoi.csproj -p:PublishSingleFile=true -r linux-x64 -c Release --self-contained false --output out
}

package() {
  cd "$_srcdir"
  ls -lah out

  install -d "${pkgdir}"/usr/bin
  install -m 755 out/supportboi "${pkgdir}"/usr/bin/supportboi
  
  install -d "${pkgdir}"/usr/lib/systemd/system
  install -m 644 packaging/supportboi.service "${pkgdir}"/usr/lib/systemd/system/
  
  install -d "${pkgdir}"/etc/supportboi/
  install -m 600 default_config.yml "${pkgdir}"/etc/supportboi/config.yml
  
  install -d "${pkgdir}"/var/lib/supportboi/transcripts
}
