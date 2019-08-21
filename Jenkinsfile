pipeline {
  agent any
  stages {
	stage('Build') {
	  parallel {
		stage('Linux') {
		  steps {
			sh 'msbuild SupportBoi/SupportBoi.csproj -restore -t:Publish -p:PublishProfile=Linux64.pubxml -p:OutputPath=bin/linux/ -p:TargetFramework=netcoreapp2.2 -p:SelfContained=true -p:RuntimeIdentifier=linux-x64'
		  }
		}
		stage('Windows') {
		  steps {
			sh 'msbuild SupportBoi/SupportBoi.csproj -restore -t:Publish -p:PublishProfile=Windows64.pubxml -p:OutputPath=bin/win/ -p:TargetFramework=netcoreapp2.2 -p:SelfContained=true -p:RuntimeIdentifier=win-x64'
		  }
		}
      }
	}
    stage('Zip') {
      parallel {
        stage('Linux') {
          steps {
            dir(path: './SupportBoi/bin/linux/publish') {
              sh 'zip -r SupportBoi_Linux-x64.zip *'
            }

          }
        }
        stage('Windows') {
          steps {
            dir(path: './SupportBoi/bin/win/publish') {
              sh 'zip -r SupportBoi_Win-x64.zip *'
            }

          }
        }
      }
    }
    stage('Archive') {
      parallel {
        stage('Linux') {
          steps {
            archiveArtifacts(artifacts: 'SupportBoi/bin/linux/publish/SupportBoi_Linux-x64.zip', caseSensitive: true)
          }
        }
        stage('Windows') {
          steps {
            archiveArtifacts(artifacts: 'SupportBoi/bin/win/publish/SupportBoi_Win-x64.zip', caseSensitive: true)
          }
        }
      }
    }
  }
}
