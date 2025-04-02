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
    stage('Build / Package')
    {
      parallel
      {
        stage('Basic Linux')
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
        stage('Basic Windows')
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
            dockerfile { filename 'packaging/RHEL9.Dockerfile' }
          }
          environment { DOTNET_CLI_HOME = "/tmp/.dotnet" }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi.spec --define "_topdir $PWD/rhel" --define "dev_build true"'
            sh 'cp rhel/RPMS/x86_64/supportboi-dev-*.x86_64.rpm rhel/'
            archiveArtifacts(artifacts: 'rhel/supportboi-dev-*.x86_64.rpm', caseSensitive: true)
            stash includes: 'rhel/supportboi-dev-*.x86_64.rpm', name: 'el9-rpm'
          }
        }
        stage('RHEL8')
        {
          agent
          {
            dockerfile { filename 'packaging/RHEL8.Dockerfile' }
          }
          environment { DOTNET_CLI_HOME = "/tmp/.dotnet" }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi.spec --define "_topdir $PWD/rhel" --define "dev_build true"'
            sh 'cp rhel/RPMS/x86_64/supportboi-dev-*.x86_64.rpm rhel/'
            archiveArtifacts(artifacts: 'rhel/supportboi-dev-*.x86_64.rpm', caseSensitive: true)
            stash includes: 'rhel/supportboi-dev-*.x86_64.rpm', name: 'el8-rpm'
          }
        }
        stage('Fedora')
        {
          agent
          {
            dockerfile { filename 'packaging/Fedora.Dockerfile' }
          }
          environment { DOTNET_CLI_HOME = "/tmp/.dotnet" }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi.spec --define "_topdir $PWD/fedora" --define "dev_build true"'
            sh 'cp fedora/RPMS/x86_64/supportboi-dev-*.x86_64.rpm fedora/'
            archiveArtifacts(artifacts: 'fedora/supportboi-dev-*.x86_64.rpm', caseSensitive: true)
            stash includes: 'fedora/supportboi-dev-*.x86_64.rpm', name: 'fedora-rpm'
          }
        }
        stage('Debian')
        {
          agent
          {
            dockerfile { filename 'packaging/Debian.Dockerfile' }
          }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
            DEBEMAIL="xkaess22@gmail.com"
            DEBFULLNAME="Karl Essinger"
            PACKAGE_ROOT="${WORKSPACE}/debian"
            DEV_BUILD="true"
          }
          steps
          {
            sh './packaging/generate-deb.sh'
            archiveArtifacts(artifacts: 'debian/supportboi-dev_*_amd64.deb, debian/supportboi-dev_*.orig.tar.gz, debian/supportboi-dev_*.tar.xz', caseSensitive: true)
            stash includes: 'debian/supportboi-dev_*_amd64.deb, debian/supportboi-dev_*.orig.tar.gz, debian/supportboi-dev_*.tar.xz', name: 'debian-deb'
          }
        }
        stage('Ubuntu')
        {
          agent
          {
            dockerfile { filename 'packaging/Ubuntu.Dockerfile' }
          }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
            DEBEMAIL="xkaess22@gmail.com"
            DEBFULLNAME="Karl Essinger"
            PACKAGE_ROOT="${WORKSPACE}/ubuntu"
            DEV_BUILD="true"
          }
          steps
          {
            sh './packaging/generate-deb.sh'
            archiveArtifacts(artifacts: 'ubuntu/supportboi-dev_*_amd64.deb, ubuntu/supportboi-dev_*.orig.tar.gz, ubuntu/supportboi-dev_*.tar.xz', caseSensitive: true)
            stash includes: 'ubuntu/supportboi-dev_*_amd64.deb, ubuntu/supportboi-dev_*.orig.tar.gz, ubuntu/supportboi-dev_*.tar.xz', name: 'ubuntu-deb'
          }
        }
      }
    }
    stage('Deploy')
    {
      parallel
      {
        stage('RHEL9')
        {
          when
          {
            expression { return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta'; }
          }
          steps
          {
            unstash name: 'el9-rpm'
            sh 'mkdir -p /usr/share/nginx/repo.karlofduty.com/rhel/el9/packages/supportboi/'
            sh 'cp rhel/supportboi-dev-*.el9.x86_64.rpm /usr/share/nginx/repo.karlofduty.com/rhel/el8/packages/supportboi/'
            sh 'createrepo_c --update /usr/share/nginx/repo.karlofduty.com/rhel/el9'
          }
        }
        stage('RHEL8')
        {
          when
          {
            expression { return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta'; }
          }
          steps
          {
            unstash name: 'el8-rpm'
            sh 'mkdir -p /usr/share/nginx/repo.karlofduty.com/rhel/el8/packages/supportboi/'
            sh 'cp rhel/supportboi-dev-*.el8.x86_64.rpm /usr/share/nginx/repo.karlofduty.com/rhel/el8/packages/supportboi/'
            sh 'createrepo_c --update /usr/share/nginx/repo.karlofduty.com/rhel/el8'
          }
        }
        stage('Fedora')
        {
          when
          {
            expression { return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta'; }
          }
          steps
          {
            unstash name: 'fedora-rpm'
            sh 'mkdir -p /usr/share/nginx/repo.karlofduty.com/fedora/packages/supportboi/'
            sh 'cp fedora/supportboi-dev-*.fc*.x86_64.rpm /usr/share/nginx/repo.karlofduty.com/fedora/packages/supportboi/'
            sh 'createrepo_c --update /usr/share/nginx/repo.karlofduty.com/fedora'
          }
        }
        stage('Debian')
        {
          when
          {
            expression { return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta'; }
          }
          steps
          {
            unstash name: 'debian-deb'
            sh 'mkdir -p /usr/share/nginx/repo.karlofduty.com/debian/packages/supportboi/'
            sh 'cp debian/supportboi-dev_*_amd64.deb /usr/share/nginx/repo.karlofduty.com/debian/packages/supportboi'
            sh 'mkdir -p /usr/share/nginx/repo.karlofduty.com/debian/sources/supportboi/'
            sh 'cp debian/supportboi-dev_*.tar.xz /usr/share/nginx/repo.karlofduty.com/debian/sources/supportboi'
            sh 'dpkg-scanpackages -m /usr/share/nginx/repo.karlofduty.com/debian | gzip -9c > /usr/share/nginx/repo.karlofduty.com/debian/Packages.gz'
          }
        }
        stage('Ubuntu')
        {
          when
          {
            expression { return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta'; }
          }
          steps
          {
            unstash name: 'ubuntu-deb'
            sh 'mkdir -p /usr/share/nginx/repo.karlofduty.com/ubuntu/packages/supportboi/'
            sh 'cp ubuntu/supportboi-dev_*_amd64.deb /usr/share/nginx/repo.karlofduty.com/ubuntu/packages/supportboi'
            sh 'mkdir -p /usr/share/nginx/repo.karlofduty.com/ubuntu/sources/supportboi/'
            sh 'cp ubuntu/supportboi-dev_*.tar.xz /usr/share/nginx/repo.karlofduty.com/ubuntu/sources/supportboi'
            sh 'dpkg-scanpackages -m /usr/share/nginx/repo.karlofduty.com/ubuntu | gzip -9c > /usr/share/nginx/repo.karlofduty.com/ubuntu/Packages.gz'
          }
        }
      }
    }
  }
}
