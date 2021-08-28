pipeline {
  agent {
    dockerfile {
      filename 'Jenkins-Dockerfile'
    }
  }
  environment {
    DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
    PATH = "${PATH}:/tmp/DOTNET_CLI_HOME/.dotnet/tools"
  }
  stages {
    stage('Setup') {
      steps {
        sh 'dotnet restore "App/Reservator.csproj"'
        sh 'dotnet tool install --global dotnet-sonarscanner --version 5.2.2'
      }
    }
    stage('SonarQube') {
      steps {
        withSonarQubeEnv('SonarQube Jukki') {
          sh '''dotnet sonarscanner begin \
                 /k:"reservator" \
                 /n:"reservator" \
                 /d:sonar.exclusions="**/wwwroot/**, **/obj/**, **/bin/**" \
                 /d:sonar.host.url="https://sonarqube.jukk.it"'''
          sh 'dotnet build "Reservator.csproj" -c Release -o /app/build'
          sh 'dotnet sonarscanner end'
        }
      }
    }
  }
}
