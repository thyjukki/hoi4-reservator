pipeline {
  agent { label 'linux' }
  environment {
      HOME='/tmp/home'
      DOTNET_CLI_TELEMETRY_OPTOUT=1
  }
  stages {
    stage('SonarQube') {
      agent {
        docker {
          image 'nosinovacao/dotnet-sonar:21.07.1'
        }
      }
      steps {
        withSonarQubeEnv('SonarQube Jukki') {
          sh '''dotnet /sonar-scanner/SonarScanner.MSBuild.dll begin \
                 /k:"reservator" \
                 /n:"reservator" \
                 /d:sonar.exclusions="**/wwwroot/**, **/obj/**, **/bin/**, **/Migrations/**" \
                 /d:sonar.coverage.exclusions="**" \
                 /d:sonar.host.url="https://sonarqube.jukk.it"'''
          sh 'dotnet build "App/Reservator.csproj" -c Release'
          sh 'dotnet /sonar-scanner/SonarScanner.MSBuild.dll end'
        }
      }
    }
    stage("Quality Gate") {
      steps {
        timeout(time: 30, unit: 'MINUTES') {
          waitForQualityGate abortPipeline: true
        }
      }
    }
    stage("Build") {
      steps {
        script {
          def image = docker.build("jukki.jfrog.io/docker-local/thyjukki/reservator", "-f App/Dockerfile .")
          image.tag("0.${BUILD_NUMBER}")
          image.tag("latest")
        }
      }
    }
    stage("Docker push") {
      steps {
        rtBuildInfo (
          buildName: 'reservator'
        )
        rtDockerPush(
          serverId: 'jukki-artifactory',
          image:  'jukki.jfrog.io/docker-local/thyjukki/reservator:latest',
          targetRepo: 'docker-local'
        )
        rtPublishBuildInfo (
          serverId: 'jukki-artifactory'
        )
      }
    }
    /*stage('Deploy App') {
      steps {
        dir("kube") {
          kubernetesDeploy(configs: "reservator.yaml", kubeconfigId: "jukki-cluster")
        }
      }
    }*/
  }
}
