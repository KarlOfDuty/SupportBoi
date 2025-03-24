FROM fedora:latest
RUN dnf install dotnet-sdk-9.0 rpm-build git systemd-rpm-macros -y