FROM redhat/ubi8:latest
RUN dnf install dotnet-sdk-9.0 rpm-build git systemd-rpm-macros -y