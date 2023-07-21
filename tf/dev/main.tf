provider linode {
  token = var.LINODE_TOKEN
}

provider github {
  token = var.GITHUB_TOKEN
  owner = "ptcdevs"
}

provider kubernetes {
  alias                  = "ptcdevs"
  host                   = local.k8s.auth.endpoint
  cluster_ca_certificate = local.k8s.auth.cluster-ca-cert
  token                  = local.k8s.auth.token
}

data linode_lke_cluster ptcdevk8s {
  id = 77314
}

module lks {
  source    = "../mods/lks"
  providers = {
    kubernetes = kubernetes.ptcdevs
  }
  github-token  = var.GITHUB_TOKEN
  k8s-namespace = "spot-pricing-dev"
  postgresql-password = var.POSTGRESQL_PASSWORD
}

module github {
  source       = "../mods/github"
  env          = "dev"
  repo         = "spot-pricing"
  github-token = var.GITHUB_TOKEN
}

#output kubeconfig {
#  value = nonsensitive(base64decode(module.github.kubeconfig))
#}