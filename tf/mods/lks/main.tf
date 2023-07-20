data linode_lke_cluster ptcdevk8s {
  id = 77314
}

locals {
  ptcdevk8s-kubeconfig = yamldecode(base64decode(data.linode_lke_cluster.ptcdevk8s.kubeconfig))
  k8s                  = {
    auth = {
      cluster-ca-cert = base64decode(local.ptcdevk8s-kubeconfig.clusters[0].cluster.certificate-authority-data)
      endpoint        = local.ptcdevk8s-kubeconfig.clusters[0].cluster.server
      token           = local.ptcdevk8s-kubeconfig.users[0].user.token
    }
  }
  ghcrauth = {
    "ghcr.io" = {
      email    = "dg@xounges.net"
      username = "vector623"
      password = var.github-token
    }
  }
}

resource kubernetes_namespace spot-pricing-dev {
  metadata {
    name = var.k8s-namespace
  }
}

resource kubernetes_secret ghcr-auth {
  metadata {
    name      = "spot-pricing-ghcr-auth"
    namespace = kubernetes_namespace.spot-pricing-dev.metadata[0].name
  }
  type = "kubernetes.io/dockercfg"
  data = {
    ".dockercfg" = jsonencode(local.ghcrauth)
  }
}
