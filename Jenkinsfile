node {
  // JenkinsFile Groovy-based PipeLine workflow for Jenkins-CI
  // Documentation:  https://jenkins.io/doc/pipeline/

  wrap([$class: 'AnsiColorBuildWrapper']){
    stage('Checkout'){
	    checkout scm 
    }
    stage('Docker Build'){
      bat 'powershell.exe -NonInteractive -c "$ErrorActionPreference=\'Stop\';cd .\\tools;Import-Module .\\jenkinsutils.psm1;New-PsDockerImage"'
    }
  }
}