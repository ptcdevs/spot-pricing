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

