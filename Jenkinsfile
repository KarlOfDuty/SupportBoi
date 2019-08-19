pipeline {
  agent any
  stages {
    stage('Linux Build') {
      steps {
        sh 'dotnet publish ./SupportBoi/ -c Release -f netcoreapp2.2 -r linux-x64'
      }
    }
    stage('Windows Build') {
      steps {
        sh 'dotnet publish ./SupportBoi/ -c Release -f netcoreapp2.2 -r win-x64'
      }
    }
    stage('Zip') {
      parallel {
        stage('Linux') {
          steps {
            sh 'zip -r SupportBoi_Linux-x64.zip ./SupportBoi/bin/Release/netcoreapp2.2/linux-x64/publish'
          }
        }
        stage('Windows') {
          steps {
            sh 'zip -r SupportBoi_Win-x64.zip ./SupportBoi/bin/Release/netcoreapp2.2/win-x64/publish'
          }
        }
      }
    }
    stage('Archive') {
      parallel {
        stage('Linux') {
          steps {
            archiveArtifacts(artifacts: 'SupportBoi_Linux-x64.zip', caseSensitive: true)
          }
        }
        stage('Windows') {
          steps {
            archiveArtifacts(artifacts: 'SupportBoi_Win-x64.zip', caseSensitive: true)
          }
        }
      }
    }
  }
}
