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
                 /d:sonar.exclusions="**/wwwroot/**, **/obj/**, **/bin/**" \
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
          app = docker.build("thyjukki/reservator", "-f App/Dockerfile .")
          app.tag(["0.${BUILD_NUMBER}","latest"])
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
          image:  'thyjukki/reservator:latest',
          targetRepo: 'docker-local',
          // Jenkins spawns a new java process during this step's execution.
          // You have the option of passing any java args to this new process.
          javaArgs: '-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:5005'
        )
        rtPublishBuildInfo (
          serverId: 'jukki-artifactory'
        )
      }
    }
  }
}
