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
    name      = "ghcr-auth"
    namespace = kubernetes_namespace.spot-pricing-dev.metadata[0].name
  }
  type = "kubernetes.io/dockercfg"
  data = {
    ".dockercfg" = jsonencode(local.ghcrauth)
  }
}

resource kubernetes_secret app-secrets {
  metadata {
    name      = "app-secrets"
    namespace = kubernetes_namespace.spot-pricing-dev.metadata[0].name
  }
  type = "kubernetes.io/dockercfg"
  data = {
    ".dockercfg" = jsonencode(local.ghcrauth)
    "GITHUB_OAUTH_CLIENT_SECRET"="1de50376cd013cafc5e02e9446fd9b6da5c84a1c",
    "AWSSECRETKEY"="mfQ4ILmYP0PjV8WNnDpPiUh5ix+lx9zPp/F2mBNG",
    "POSTGRESQL_PASSWORD"=var.postgresql-password
  }
}

resource kubernetes_config_map app-settings {
  metadata {
    name      = "app-settings"
    namespace = kubernetes_namespace.spot-pricing-dev.metadata[0].name
  }
  data = {
    "appsettings.json" = file("../../config/dev/appsettings.json")
  }
}