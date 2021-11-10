pipeline {
  agent any
  stages {
    stage('Build') {
      steps {
        sh 'dotnet restore'
      }
    }
    stage('Build') {
      parallel {
        stage('Linux') {
          steps {
            sh 'dotnet publish -p:PublishSingleFile=true -r linux-x64 -c Release --self-contained true -p:PublishTrimmed=true'
          }
        }
        stage('Windows') {
          steps {
            sh 'dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release --self-contained true -p:PublishTrimmed=true'
          }
        }
      }
    }
    stage('Archive') {
      parallel {
        stage('Linux') {
          steps {
            archiveArtifacts(artifacts: 'Linux-x64/SupportBoi', caseSensitive: true)
          }
        }
        stage('Windows') {
          steps {
            archiveArtifacts(artifacts: 'Windows-x64/SupportBoi.exe', caseSensitive: true)
          }
        }
      }
    }
  }
}
