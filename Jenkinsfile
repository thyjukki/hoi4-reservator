pipeline {
  agent {
    dockerfile {
      filename 'Jenkins-Dockerfile'
    }
  }
  environment {
      DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
  }
  stages {
    stage('setup') {
      steps {
        sh 'dotnet restore "App/Reservator.csproj"'
      }
    }
    stage('SonarQube') {
      steps {
        withSonarQubeEnv('SonarQube Jukki') {
          sh '''sudo /tmp/DOTNET_CLI_HOME/.dotnet/tools/dotnet-sonarscanner begin \
                 /k:"reservator" \
                 /n:"reservator" \
                 /d:sonar.exclusions="**/wwwroot/**, **/obj/**, **/bin/**" \
                 /d:sonar.host.url="https://sonarqube.jukk.it"'''
          sh 'dotnet build "App/Reservator.csproj" -c Release'
          sh 'sudo /tmp/DOTNET_CLI_HOME/.dotnet/tools/dotnet-sonarscanner end'
        }
      }
    }
  }
}
