pipeline
{
  agent any
  stages
  {
    stage('Setup Dependencies')
    {
      steps
      {
        sh 'dotnet restore'
      }
    }
    stage('Build')
    {
      parallel
      {
        stage('Linux')
        {
          steps
          {
            sh 'dotnet publish -r linux-x64 -c Release -p:PublishTrimmed=true --self-contained true --no-restore --output linux-x64/'
            sh 'mv linux-x64/supportboi linux-x64/supportboi-sc'
            sh 'dotnet publish -r linux-x64 -c Release --self-contained false --no-restore --output linux-x64/'
            archiveArtifacts(artifacts: 'linux-x64/supportboi', caseSensitive: true)
            archiveArtifacts(artifacts: 'linux-x64/supportboi-sc', caseSensitive: true)
          }
        }
        stage('Windows')
        {
          steps
          {
            sh 'dotnet publish -r win-x64 -c Release -p:PublishTrimmed=true --self-contained true --no-restore --output windows-x64/'
            sh 'mv windows-x64/supportboi.exe windows-x64/supportboi-sc.exe'
            sh 'dotnet publish -r win-x64 -c Release --self-contained false --no-restore --output windows-x64/'
            archiveArtifacts(artifacts: 'windows-x64/supportboi.exe', caseSensitive: true)
            archiveArtifacts(artifacts: 'windows-x64/supportboi-sc.exe', caseSensitive: true)
          }
        }
        stage('RHEL9')
        {
          agent
          {
            dockerfile
            {
              filename 'packaging/RHEL9.Dockerfile'
            }
          }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
          }
          when
          {
            expression
            {
              return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta' || env.BRANCH_NAME == 'jenkins-testing';
            }
          }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi-nightly.spec --define "_topdir $PWD/.rpmbuild-el9"'
            sh 'mkdir linux-x64'
            sh 'cp .rpmbuild-el9/RPMS/x86_64/supportboi-nightly-*.el9.x86_64.rpm linux-x64/'
            archiveArtifacts(artifacts: 'linux-x64/supportboi-nightly-*.el9.x86_64.rpm', caseSensitive: true)
          }
        }
        stage('RHEL8')
        {
          agent
          {
            dockerfile
            {
              filename 'packaging/RHEL8.Dockerfile'
            }
          }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
          }
          when
          {
            expression
            {
              return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta' || env.BRANCH_NAME == 'jenkins-testing';
            }
          }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi-nightly.spec --define "_topdir $PWD/.rpmbuild-el8"'
            sh 'mkdir linux-x64'
            sh 'cp .rpmbuild-el8/RPMS/x86_64/supportboi-nightly-*.el8.x86_64.rpm linux-x64/'
            archiveArtifacts(artifacts: 'linux-x64/supportboi-nightly-*.el8.x86_64.rpm', caseSensitive: true)
          }
        }
      }
    }
  }
}
