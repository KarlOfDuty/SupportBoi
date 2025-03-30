FROM debian:12
RUN apt update && apt install curl -y
RUN curl "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" > "packages-microsoft-prod.deb"
RUN dpkg -i "packages-microsoft-prod.deb"
RUN apt update && apt install dotnet-sdk-9.0 git build-essential dh-make -y