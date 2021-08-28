pipeline {
    agent { 
      dockerfile {
          dir './App/to/dockerfile'
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
