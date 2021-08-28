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
          dir ("App") {
            app = docker.build("jukki.jfrog.io/reservator:0.${BUILD_NUMBER}") 
          } 
        }
      }
    }
    /*stage("Docker push") {
      steps {
        rtDockerPush(
            serverId: 'jukki-artifactory',
            image:  'jukki.jfrog.io/reservator:latest',
            targetRepo: 'docker-local',
            // Jenkins spawns a new java process during this step's execution.
            // You have the option of passing any java args to this new process.
            javaArgs: '-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:5005'
        )
      }
    }*/
  }
}
