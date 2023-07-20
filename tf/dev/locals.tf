locals {
  ptcdevk8s-kubeconfig = yamldecode(base64decode(data.linode_lke_cluster.ptcdevk8s.kubeconfig))
  k8s                  = {
    auth = {
      cluster-ca-cert = base64decode(local.ptcdevk8s-kubeconfig.clusters[0].cluster.certificate-authority-data)
      endpoint        = local.ptcdevk8s-kubeconfig.clusters[0].cluster.server
      token           = local.ptcdevk8s-kubeconfig.users[0].user.token
    }
  }
  ghcrcreds = {
    "ghcr.io" = {
      email    = "dg@xounges.net"
      username = "vector623"
      password = var.GITHUB_TOKEN
    }
  }
}

