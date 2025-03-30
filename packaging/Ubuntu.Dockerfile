FROM ubuntu:24.04
RUN apt update && apt install software-properties-common -y
RUN add-apt-repository ppa:dotnet/backports
RUN apt update && apt install dotnet-sdk-9.0 git build-essential dh-make -y