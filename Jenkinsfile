pipeline {
  agent any
  stages {
    stage('Build') {
      parallel {
        stage('Linux') {
          steps {
            sh 'msbuild SupportBoi/SupportBoi.csproj -restore -t:Publish -p:PublishProfile=Linux64.pubxml -p:OutputPath=bin/linux/ -p:BaseIntermediateOutputPath=obj/linux/ -p:TargetFramework=netcoreapp2.2 -p:SelfContained=true -p:RuntimeIdentifier=linux-x64'
            sh 'rm -r SupportBoi/bin/linux/*.pdb'
          }
        }
        stage('Windows') {
          steps {
            sh 'msbuild SupportBoi/SupportBoi.csproj -restore -t:Publish -p:PublishProfile=Windows64.pubxml -p:OutputPath=bin/win/ -p:BaseIntermediateOutputPath=obj/win/ -p:TargetFramework=netcoreapp2.2 -p:SelfContained=true -p:RuntimeIdentifier=win-x64'
            sh 'rm -r SupportBoi/bin/win/*.pdb'
          }
        }
      }
    }
    stage('Package') {
      parallel {
        stage('Linux') {
          steps {
            sh 'mkdir Linux-x64'
            dir(path: './Linux-x64') {
              sh 'warp-packer --arch linux-x64 --input_dir ../SupportBoi/bin/linux/publish --exec SupportBoi --output SupportBoi'
            }

          }
        }
        stage('Windows') {
          steps {
            sh 'mkdir Windows-x64'
            dir(path: './Windows-x64') {
              sh 'warp-packer --arch windows-x64 --input_dir ../SupportBoi/bin/win/publish --exec SupportBoi.exe --output SupportBoi.exe'
            }

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