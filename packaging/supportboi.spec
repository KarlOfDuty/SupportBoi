%global debug_package %{nil}
%global repo_root %{_topdir}/..
%global base_version %(echo "$(sed -ne '/Version/{s/.*<Version>\\(.*\\)<\\/Version>.*/\\1/p;q;}' < SupportBoi.csproj)")

%if %{defined dev_build}
Name:       supportboi-dev
Summary:    A support ticket Discord bot (dev build)
Version:    %{base_version}~%(date "+%%Y%%m%%d%%H%%M%%S")git%(git rev-parse --short HEAD)
Source:     https://github.com/KarlOfDuty/SupportBoi/archive/%(git rev-parse HEAD).zip
Provides:   supportboi
%else
Name:       supportboi
Summary:    A support ticket Discord bot
Version:    %{base_version}
Source:     https://github.com/KarlOfDuty/SupportBoi/archive/refs/tags/%{base_version}.zip
%endif
Release:    1%{?dist}
License:    GPLv3
URL:        https://github.com/KarlOfDuty/SupportBoi
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
if [[ -d %{_rpmdir}/%{_arch} ]]; then
  %{__rm} %{_rpmdir}/%{_arch}/*
fi

%{__install} -d %{buildroot}/usr/bin
# rpmbuild post-processing using the strip command breaks dotnet binaries, remove the executable bit to avoid it
%{__install} -m 644 %{_builddir}/out/supportboi %{buildroot}/usr/bin/supportboi

%{__install} -d %{buildroot}/usr/lib/systemd/system
%{__install} -m 644 %{repo_root}/packaging/supportboi.service %{buildroot}/usr/lib/systemd/system/

%{__install} -d %{buildroot}/etc/supportboi/
%{__install} -m 600 %{repo_root}/default_config.yml %{buildroot}/etc/supportboi/config.yml

%{__install} -d %{buildroot}/var/lib/supportboi/transcripts

%pre
getent group supportboi > /dev/null || groupadd supportboi
getent passwd supportboi > /dev/null || useradd -r -m -d /var/lib/supportboi -s /sbin/nologin -g supportboi supportboi

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
%dir %attr(0700, supportboi, supportboi) /var/lib/supportboi
%dir %attr(0755, supportboi, supportboi) /var/lib/supportboi/transcripts