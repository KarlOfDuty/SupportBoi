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
            dockerfile { filename 'CIUtilities/docker/RHEL9.Dockerfile' }
          }
          environment { DOTNET_CLI_HOME = "/tmp/.dotnet" }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi.spec --define "_topdir $PWD/rhel" --define "dev_build true"'
            sh 'cp rhel/RPMS/x86_64/supportboi-dev-*.x86_64.rpm rhel/'
            stash includes: 'rhel/supportboi-dev-*.x86_64.rpm', name: 'el9-rpm'
          }
        }
        stage('RHEL8')
        {
          agent
          {
            dockerfile { filename 'CIUtilities/docker/RHEL8.Dockerfile' }
          }
          environment { DOTNET_CLI_HOME = "/tmp/.dotnet" }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi.spec --define "_topdir $PWD/rhel" --define "dev_build true"'
            sh 'cp rhel/RPMS/x86_64/supportboi-dev-*.x86_64.rpm rhel/'
            stash includes: 'rhel/supportboi-dev-*.x86_64.rpm', name: 'el8-rpm'
          }
        }
        stage('Fedora')
        {
          agent
          {
            dockerfile { filename 'CIUtilities/docker/Fedora.Dockerfile' }
          }
          environment { DOTNET_CLI_HOME = "/tmp/.dotnet" }
          steps
          {
            sh 'rpmbuild -bb packaging/supportboi.spec --define "_topdir $PWD/fedora" --define "dev_build true"'
            sh 'cp fedora/RPMS/x86_64/supportboi-dev-*.x86_64.rpm fedora/'
            stash includes: 'fedora/supportboi-dev-*.x86_64.rpm', name: 'fedora-rpm'
          }
        }
        stage('Debian')
        {
          agent
          {
            dockerfile { filename 'CIUtilities/docker/Debian.Dockerfile' }
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
            archiveArtifacts(artifacts: 'debian/supportboi-dev_*_amd64.deb, debian/supportboi-dev_*.tar.xz', caseSensitive: true)
            stash includes: 'debian/supportboi-dev_*_amd64.deb, debian/supportboi-dev_*.tar.xz, debian/supportboi-dev_*.dsc', name: 'debian-deb'
          }
        }
        stage('Ubuntu')
        {
          agent
          {
            dockerfile { filename 'CIUtilities/docker/Ubuntu.Dockerfile' }
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
            stash includes: 'ubuntu/supportboi-dev_*_amd64.deb, ubuntu/supportboi-dev_*.tar.xz, ubuntu/supportboi-dev_*.dsc', name: 'ubuntu-deb'
          }
        }
      }
    }
    stage('Sign')
    {
      parallel
      {
        stage('RHEL9')
        {
          steps
          {
            unstash name: 'el9-rpm'
            withCredentials([string(credentialsId: 'JENKINS_GPG_KEY_PASSWORD', variable: 'JENKINS_GPG_KEY_PASSWORD')]) {
              sh '/usr/lib/gnupg/gpg-preset-passphrase --passphrase "$JENKINS_GPG_KEY_PASSWORD" --preset 0D27E4CD885E9DD79C252E825F70A1590922C51E'
              sh 'rpmsign --define "_gpg_name Karl Essinger (Jenkins Signing) <xkaess22@gmail.com>" --addsign rhel/supportboi-dev-*.el9.x86_64.rpm'
              sh 'rpm -vv --checksig rhel/supportboi-dev-*.el9.x86_64.rpm'
            }
            archiveArtifacts(artifacts: 'rhel/supportboi-dev-*.el9.x86_64.rpm', caseSensitive: true)
          }
        }
        stage('RHEL8')
        {
          steps
          {
            unstash name: 'el8-rpm'
            withCredentials([string(credentialsId: 'JENKINS_GPG_KEY_PASSWORD', variable: 'JENKINS_GPG_KEY_PASSWORD')]) {
              sh '/usr/lib/gnupg/gpg-preset-passphrase --passphrase "$JENKINS_GPG_KEY_PASSWORD" --preset 0D27E4CD885E9DD79C252E825F70A1590922C51E'
              sh 'rpmsign --define "_gpg_name Karl Essinger (Jenkins Signing) <xkaess22@gmail.com>" --addsign rhel/supportboi-dev-*.el8.x86_64.rpm'
              sh 'rpm -vv --checksig rhel/supportboi-dev-*.el8.x86_64.rpm'
            }
            archiveArtifacts(artifacts: 'rhel/supportboi-dev-*.el8.x86_64.rpm', caseSensitive: true)
          }
        }
        stage('Fedora')
        {
          steps
          {
            unstash name: 'fedora-rpm'
            withCredentials([string(credentialsId: 'JENKINS_GPG_KEY_PASSWORD', variable: 'JENKINS_GPG_KEY_PASSWORD')]) {
              sh '/usr/lib/gnupg/gpg-preset-passphrase --passphrase "$JENKINS_GPG_KEY_PASSWORD" --preset 0D27E4CD885E9DD79C252E825F70A1590922C51E'
              sh 'rpmsign --define "_gpg_name Karl Essinger (Jenkins Signing) <xkaess22@gmail.com>" --addsign fedora/supportboi-dev-*.x86_64.rpm'
              sh 'rpm -vv --checksig fedora/supportboi-dev-*.x86_64.rpm'
            }
            archiveArtifacts(artifacts: 'fedora/supportboi-dev-*.x86_64.rpm', caseSensitive: true)
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
          environment
          {
            DISTRO="debian"
            REPO_DIR="/usr/share/nginx/repo.karlofduty.com/${env.DISTRO}"
            POOL_DIR="${env.REPO_DIR}/pool/dev/supportboi"
            DISTS_BIN_DIR="${env.REPO_DIR}/dists/${env.DISTRO}/dev/binary-amd64"
            DISTS_SRC_DIR="${env.REPO_DIR}/dists/${env.DISTRO}/dev/source"
          }
          steps
          {
            unstash name: "${env.DISTRO}-deb"

            // Copy package and sources to pool directory
            sh "mkdir -p ${env.POOL_DIR}"
            sh "cp ${env.DISTRO}/supportboi-dev_*_amd64.deb ${env.POOL_DIR}"
            sh "cp ${env.DISTRO}/supportboi-dev_*.tar.xz ${env.POOL_DIR}"
            sh "cp ${env.DISTRO}/supportboi-dev_*.dsc ${env.POOL_DIR}"
            dir("${env.REPO_DIR}")
            {
              // Generate package lists
              sh "mkdir -p ${env.DISTS_BIN_DIR}"
              sh "dpkg-scanpackages --arch amd64 -m pool/ > ${env.DISTS_BIN_DIR}/Packages"
              sh "cat ${env.DISTS_BIN_DIR}/Packages | gzip -9c > ${env.DISTS_BIN_DIR}/Packages.gz"

              // Generate source lists
              sh "mkdir -p ${env.DISTS_SRC_DIR}"
              sh "dpkg-scansources pool/ > ${env.DISTS_SRC_DIR}/Sources"
              sh "cat ${env.DISTS_SRC_DIR}/Sources | gzip -9c > ${env.DISTS_SRC_DIR}/Sources.gz"
            }

            dir("${env.REPO_DIR}/dists/${env.DISTRO}")
            {
              // Generate release file
              sh "${WORKSPACE}/CIUtilities/scripts/generate-deb-release-file.sh > Release"
            }

            sh "rmdir ${env.REPO_DIR}@tmp"
          }
        }
        stage('Ubuntu')
        {
          when
          {
            expression { return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta'; }
          }
          environment
          {
            DISTRO="ubuntu"
            REPO_DIR="/usr/share/nginx/repo.karlofduty.com/${env.DISTRO}"
            POOL_DIR="${env.REPO_DIR}/pool/dev/supportboi"
            DISTS_BIN_DIR="${env.REPO_DIR}/dists/${env.DISTRO}/dev/binary-amd64"
            DISTS_SRC_DIR="${env.REPO_DIR}/dists/${env.DISTRO}/dev/source"
          }
          steps
          {
            unstash name: "${env.DISTRO}-deb"

            // Copy package and sources to pool directory
            sh "mkdir -p ${env.POOL_DIR}"
            sh "cp ${env.DISTRO}/supportboi-dev_*_amd64.deb ${env.POOL_DIR}"
            sh "cp ${env.DISTRO}/supportboi-dev_*.tar.xz ${env.POOL_DIR}"
            sh "cp ${env.DISTRO}/supportboi-dev_*.dsc ${env.POOL_DIR}"
            dir("${env.REPO_DIR}")
            {
              // Generate package lists
              sh "mkdir -p ${env.DISTS_BIN_DIR}"
              sh "dpkg-scanpackages --arch amd64 -m pool/ > ${env.DISTS_BIN_DIR}/Packages"
              sh "cat ${env.DISTS_BIN_DIR}/Packages | gzip -9c > ${env.DISTS_BIN_DIR}/Packages.gz"

              // Generate source lists
              sh "mkdir -p ${env.DISTS_SRC_DIR}"
              sh "dpkg-scansources pool/ > ${env.DISTS_SRC_DIR}/Sources"
              sh "cat ${env.DISTS_SRC_DIR}/Sources | gzip -9c > ${env.DISTS_SRC_DIR}/Sources.gz"
            }

            dir("${env.REPO_DIR}/dists/${env.DISTRO}")
            {
              // Generate release file
              sh "${WORKSPACE}/CIUtilities/scripts/generate-deb-release-file.sh > Release"
            }

            sh "rmdir ${env.REPO_DIR}@tmp"
          }
        }
      }
    }
  }
}
