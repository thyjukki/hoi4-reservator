pipeline {
  agent {
    dockerfile {
      filename 'Jenkins-Dockerfile'
      args '-u root'
    }
  }
  stages {
    stage('SonarQube') {
      steps {
        dir('/src') {
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
}
