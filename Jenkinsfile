pipeline {
    agent { 
      dockerfile {
          dir './App/dockerfile'
      }
    }
    stages {
        stage('Test') {
            steps {
                echo 'test'
            }
        }
    }
}
