data linode_lke_cluster ptcdevk8s {
  id = 77314
}

data github_repository spot-pricing {
  name = var.repo
}

resource github_repository_environment main {
  environment = var.env
  repository = data.github_repository.spot-pricing.name
}

resource github_actions_environment_secret kubeconfig {
  environment = github_repository_environment.main.environment
  repository = data.github_repository.spot-pricing.name
  secret_name = "KUBECONFIG"
  #plaintext_value = data.local_sensitive_file.kubeconfig.content
  plaintext_value = data.linode_lke_cluster.ptcdevk8s.kubeconfig
}

resource github_actions_environment_secret github-token {
  environment = github_repository_environment.main.environment
  repository = data.github_repository.spot-pricing.name
  secret_name = "GHCR_GITHUB_TOKEN"
  plaintext_value = var.github-token
}
