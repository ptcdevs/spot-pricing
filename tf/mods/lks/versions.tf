terraform {
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "2.16.1"
    }
    linode = {
      source  = "linode/linode"
      version = "1.29.4"
    }
  }
}
