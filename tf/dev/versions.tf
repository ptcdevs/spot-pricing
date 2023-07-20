terraform {
  required_providers {
    linode = {
      source  = "linode/linode"
      version = "1.29.4"
    }
    github = {
      source  = "integrations/github"
      version = "5.11.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "2.16.1"
    }
    helm = {
      source = "hashicorp/helm"
      version = "2.7.1"
    }
  }
}
