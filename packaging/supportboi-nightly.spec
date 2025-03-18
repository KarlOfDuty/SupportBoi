%global debug_package %{nil}
%global repo_root %{_topdir}/..

Summary:    A support ticket Discord bot
Name:       supportboi-nightly
Version:    %(sed -ne '/Version/{s/.*<Version>\(.*\)<\/Version>.*/\1/p;q;}' < SupportBoi.csproj)
Release:    %(date "+%%Y%%m%%d%%H%%M%%S")%{?dist}
License:    GPLv3
URL:        https://github.com/KarlOfDuty/SupportBoi
Source:     https://github.com/KarlOfDuty/SupportBoi/archive/refs/heads/main.zip
Packager:   KarlofDuty

BuildRequires: systemd-rpm-macros
Requires: dotnet-runtime-9.0
%{?systemd_requires}

%description
A support ticket Discord bot. Uses a MySQL database for storage of ticket
information. Creates formatted HTML ticket transcripts when tickets are closed.

%prep
%setup -T -c

%build
dotnet publish %{repo_root}/SupportBoi.csproj -p:PublishSingleFile=true -r linux-x64 -c Release --self-contained false --output %{_builddir}/out

%install
%{__install} -d %{buildroot}/usr/bin
%{__install} %{_builddir}/out/supportboi %{buildroot}/usr/bin/supportboi
# rpmbuild post-processing using the strip command breaks dotnet binaries, remove the executable bit to avoid it
chmod 644 %{buildroot}/usr/bin/supportboi

%{__install} -d %{buildroot}/usr/lib/systemd/system
%{__install} %{repo_root}/packaging/supportboi.service %{buildroot}/usr/lib/systemd/system/

%{__install} -d %{buildroot}/etc/supportboi/
%{__install} %{repo_root}/default_config.yml %{buildroot}/etc/supportboi/config.yml

%pre
getent group supportboi > /dev/null || groupadd supportboi
getent passwd supportboi > /dev/null || useradd -r -s /sbin/nologin -g supportboi supportboi

%post
%systemd_post supportboi.service

%preun
%systemd_preun supportboi.service

%postun
%systemd_postun_with_restart supportboi.service
if [[ "$1" == "0" ]]; then
  getent passwd supportboi > /dev/null && userdel supportboi
fi

%files
%attr(0755,root,root) /usr/bin/supportboi
%attr(0644,root,root) /usr/lib/systemd/system/supportboi.service
%config %attr(0600, supportboi, supportboi) /etc/supportboi/config.yml