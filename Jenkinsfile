pipeline
{
  agent any
  stages
  {
    stage('Preparation')
    {
      steps
      {
        sh 'dotnet restore'
        script
        {
          common = load("${env.WORKSPACE}/ci-utilities/scripts/common.groovy")
          common.prepare_gpg_key()
        }
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
        stage('RHEL')
        {
          agent { dockerfile { filename 'ci-utilities/docker/RHEL8.Dockerfile' } }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
            DISTRO="rhel"
            PACKAGE_NAME="supportboi"
          }
          steps
          {
            script
            {
              common.build_rpm_package(env.DISTRO, "packaging/supportboi.spec", env.PACKAGE_NAME, "--define 'dev_build true'")
              env.RHEL_RPM_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}-*.x86_64.rpm", returnStdout: true).trim()
              env.RHEL_RPM_PATH = "${env.DISTRO}/${env.RHEL_RPM_NAME}"
              env.RHEL_SRPM_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}-*.src.rpm", returnStdout: true).trim()
              env.RHEL_SRPM_PATH = "${env.DISTRO}/${env.RHEL_RPM_NAME}"
            }
            stash(includes: "${env.RHEL_RPM_PATH}, ${env.RHEL_SRPM_NAME}", name: "${env.DISTRO}-rpm")
          }
        }
        stage('Fedora')
        {
          agent
          {
            dockerfile { filename 'ci-utilities/docker/Fedora42.Dockerfile' }
          }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
            DISTRO="fedora"
            PACKAGE_NAME="supportboi"
          }
          steps
          {
            script
            {
              common.build_rpm_package(env.DISTRO, "packaging/supportboi.spec", env.PACKAGE_NAME, "--define 'dev_build true'")
              env.FEDORA_RPM_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}-*.x86_64.rpm", returnStdout: true).trim()
              env.FEDORA_RPM_PATH = "${env.DISTRO}/${env.FEDORA_RPM_NAME}"
              env.FEDORA_SRPM_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}-*.src.rpm", returnStdout: true).trim()
              env.FEDORA_SRPM_PATH = "${env.DISTRO}/${env.FEDORA_SRPM_NAME}"
            }
            stash(includes: "${env.FEDORA_RPM_PATH}, ${env.FEDORA_SRPM_PATH}", name: "${env.DISTRO}-rpm")
          }
        }
        stage('Debian')
        {
          agent
          {
            dockerfile { filename 'ci-utilities/docker/Debian12.Dockerfile' }
          }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
            DEBEMAIL="xkaess22@gmail.com"
            DEBFULLNAME="Karl Essinger"
            DISTRO="debian"
            PACKAGE_ROOT="${WORKSPACE}/debian"
            DEV_BUILD="true"
            PACKAGE_NAME="supportboi-dev"
          }
          steps
          {
            sh './packaging/generate-deb.sh'
            script
            {
              env.DEBIAN_DEB_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}_*_amd64.deb", returnStdout: true).trim()
              env.DEBIAN_DEB_PATH = "${env.DISTRO}/${env.DEBIAN_DEB_NAME}"
              env.DEBIAN_DSC_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}_*.dsc", returnStdout: true).trim()
              env.DEBIAN_DSC_PATH = "${env.DISTRO}/${env.DEBIAN_DSC_NAME}"
              env.DEBIAN_SRC_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}_*.tar.xz", returnStdout: true).trim()
              env.DEBIAN_SRC_PATH = "${env.DISTRO}/${env.DEBIAN_SRC_NAME}"
            }
            stash(includes: "${env.DEBIAN_DEB_PATH}, ${env.DEBIAN_SRC_PATH}, ${env.DEBIAN_DSC_PATH}", name: "${env.DISTRO}-deb")
          }
        }
        stage('Ubuntu')
        {
          agent
          {
            dockerfile { filename 'ci-utilities/docker/Ubuntu24.04.Dockerfile' }
          }
          environment
          {
            DOTNET_CLI_HOME = "/tmp/.dotnet"
            DEBEMAIL="xkaess22@gmail.com"
            DEBFULLNAME="Karl Essinger"
            DISTRO="ubuntu"
            PACKAGE_ROOT="${WORKSPACE}/ubuntu"
            DEV_BUILD="true"
            PACKAGE_NAME="supportboi-dev"
          }
          steps
          {
            sh './packaging/generate-deb.sh'
            script
            {
              env.UBUNTU_DEB_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}_*_amd64.deb", returnStdout: true).trim()
              env.UBUNTU_DEB_PATH = "${env.DISTRO}/${env.UBUNTU_DEB_NAME}"
              env.UBUNTU_DSC_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}_*.dsc", returnStdout: true).trim()
              env.UBUNTU_DSC_PATH = "${env.DISTRO}/${env.UBUNTU_DSC_NAME}"
              env.UBUNTU_SRC_NAME = sh(script: "cd ${env.DISTRO} && ls ${env.PACKAGE_NAME}_*.tar.xz", returnStdout: true).trim()
              env.UBUNTU_SRC_PATH = "${env.DISTRO}/${env.UBUNTU_SRC_NAME}"
            }
            stash(includes: "${env.UBUNTU_DEB_PATH}, ${env.UBUNTU_SRC_PATH}, ${env.UBUNTU_DSC_PATH}", name: "${env.DISTRO}-deb")
          }
        }
      }
    }
    stage('Sign')
    {
      parallel
      {
        stage('RHEL')
        {
          steps
          {
            unstash(name: 'rhel-rpm')
            script
            {
              common.sign_rpm_package(env.RHEL_RPM_PATH)
              common.sign_rpm_package(env.RHEL_SRPM_PATH)
            }
            archiveArtifacts(artifacts: "${env.RHEL_RPM_PATH}, ${env.RHEL_SRPM_PATH}", caseSensitive: true)
          }
        }
        stage('Fedora')
        {
          steps
          {
            unstash(name: 'fedora-rpm')
            script
            {
              common.sign_rpm_package(env.FEDORA_RPM_PATH)
              common.sign_rpm_package(env.FEDORA_SRPM_PATH)
            }
            archiveArtifacts(artifacts: "${env.FEDORA_RPM_PATH}, ${env.FEDORA_SRPM_PATH}", caseSensitive: true)
          }
        }
        stage('Debian')
        {
          steps
          {
            unstash(name: "debian-deb")
            script { common.sign_deb_package(env.DEBIAN_DEB_PATH, env.DEBIAN_DSC_PATH) }
            archiveArtifacts(artifacts: "${env.DEBIAN_DEB_PATH}, ${env.DEBIAN_SRC_PATH}", caseSensitive: true)
          }
        }
        stage('Ubuntu')
        {
          steps
          {
            unstash(name: "ubuntu-deb")
            script { common.sign_deb_package(env.UBUNTU_DEB_PATH, env.UBUNTU_DSC_PATH) }
            archiveArtifacts(artifacts: "${env.UBUNTU_DEB_PATH}, ${env.UBUNTU_SRC_PATH}", caseSensitive: true)
          }
        }
      }
    }
    stage('Deploy')
    {
      parallel
      {
        stage('RHEL')
        {
          when
          {
            expression { return env.BRANCH_NAME == 'main' || env.BRANCH_NAME == 'beta'; }
          }
          steps
          {
            script
            {
              common.publish_rpm_package("rhel/el8", env.RHEL_RPM_PATH, env.RHEL_SRPM_PATH, "supportboi-dev")
              common.publish_rpm_package("rhel/el9", env.RHEL_RPM_PATH, env.RHEL_SRPM_PATH, "supportboi-dev")
            }
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
            script
            {
              common.publish_rpm_package("fedora", env.FEDORA_RPM_PATH, env.FEDORA_SRPM_PATH, "supportboi-dev")
            }
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
            PACKAGE_NAME="supportboi-dev"
            COMPONENT="main"
          }
          steps
          {
            script
            {
              common.publish_deb_package(env.DISTRO, env.PACKAGE_NAME, env.PACKAGE_NAME, "${WORKSPACE}/${env.DISTRO}", env.COMPONENT)
              common.generate_debian_release_file("${WORKSPACE}/ci-utilities", env.DISTRO)
            }
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
            PACKAGE_NAME="supportboi-dev"
            COMPONENT="main"
          }
          steps
          {
            script
            {
              common.publish_deb_package(env.DISTRO, env.PACKAGE_NAME, env.PACKAGE_NAME, "${WORKSPACE}/${env.DISTRO}", env.COMPONENT)
              common.generate_debian_release_file("${WORKSPACE}/ci-utilities", env.DISTRO)
            }
          }
        }
      }
    }
  }
}
