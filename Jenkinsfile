pipeline {
  agent any
  stages {
    stage('Setup Dependencies') {
      steps {
        sh 'dotnet restore'
      }
    }
    stage('Build') {
      parallel {
        stage('Linux') {
          steps {
            sh 'dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true --no-restore --output Linux-x64/'
            sh 'mv Linux-x64/SupportBoi Linux-x64/SupportBoi-SC'
            sh 'dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true --self-contained false --no-restore --output Linux-x64/'
          }
        }
        stage('Windows') {
          steps {
            sh 'dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true --no-restore --output Windows-x64/'
            sh 'mv Windows-x64/SupportBoi.exe Windows-x64/SupportBoi-SC.exe'
            sh 'dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained false --no-restore --output Windows-x64/'
          }
        }
      }
    }
    stage('Archive') {
      parallel {
        stage('Linux') {
          steps {
            archiveArtifacts(artifacts: 'Linux-x64/SupportBoi', caseSensitive: true)
            archiveArtifacts(artifacts: 'Linux-x64/SupportBoi-SC', caseSensitive: true)
          }
        }
        stage('Windows') {
          steps {
            archiveArtifacts(artifacts: 'Windows-x64/SupportBoi.exe', caseSensitive: true)
            archiveArtifacts(artifacts: 'Windows-x64/SupportBoi-SC.exe', caseSensitive: true)
          }
        }
      }
    }
  }
}
