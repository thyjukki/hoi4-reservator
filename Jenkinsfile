def app = null
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
          app = docker.build("thyjukki/reservator", "-f App/Dockerfile .")
        }
      }
    }
    stage("Docker push") {
      steps {
        script {
          docker.withRegistry('https://nexus.jukk.it', 'nexus-jenkins-user' ) {
            app.push("0.${BUILD_NUMBER}")            
            app.push("latest") 
          }
        }
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
